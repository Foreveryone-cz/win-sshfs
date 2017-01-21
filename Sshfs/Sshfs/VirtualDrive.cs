
#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DokanNet;
using Sshfs.Properties;
using System.Collections.Generic;
#endregion

namespace Sshfs
{
    [Serializable]
    public class VirtualDrive : IDisposable, ISerializable
    {
        private CancellationTokenSource _mountCancel;
        private AutoResetEvent _pauseEvent;
        private CancellationTokenSource _threadCancel;
        private Thread _mountThread;
        private Exception _lastExeption;
        private bool _exeptionThrown;

        private VirtualFilesystem _filesystem;
        private List<SftpDrive> _drives = new List<SftpDrive>();

        public string Name { get; set; }

        public char Letter { get; set; }
        private char mountedLetter { get; set; }

        public DriveStatus Status { get; private set; }

        //private readonly Dictionary<string, SftpFilesystem> _subsytems = new Dictionary<string,SftpFilesystem>();



        public VirtualDrive() { }


        internal void AddSubFS(SftpDrive sftpDrive)
        {
            _drives.Add(sftpDrive);
            if (_filesystem!=null)
                _filesystem.AddSubFS(sftpDrive);
        }

        internal void RemoveSubFS(SftpDrive sftpDrive)
        {
            _drives.Remove(sftpDrive);
            if (_filesystem!=null)
                _filesystem.RemoveSubFS(sftpDrive);
        }


        private void OnStatusChanged(EventArgs args)
        {
            if (StatusChanged != null)
            {
                StatusChanged(this, args);
            }
        }

        public event EventHandler<EventArgs> StatusChanged;



        private void SetupFilesystem()
        {
            Debug.WriteLine("SetupVirtualFilesystem");

            
        }

        private void SetupMountThread()
        {
            if (_mountThread == null)
            {
                Debug.WriteLine("Thread:Created");
                _threadCancel = new CancellationTokenSource();
                _pauseEvent = new AutoResetEvent(false);
                _mountCancel = new CancellationTokenSource();
                _mountThread = new Thread(MountLoop) { IsBackground = true };
                _mountThread.Start();
                
            }
        }

        private void MountLoop()
        {
            while (true)
            {
                Debug.WriteLine("Thread:Pause");

                _pauseEvent.WaitOne(-1);
                if (_threadCancel.IsCancellationRequested)
                {
                    Debug.WriteLine("Thread:Cancel");
                    break;
                }

                Debug.WriteLine("Thread:Mount");


                try
                {
                    _filesystem = new VirtualFilesystem("WinSshFS spool");
                    foreach (SftpDrive drive in _drives)
                    {
                        if (drive.MountPoint != "")
                        {
                            _filesystem.AddSubFS(drive);
                        }
                    }
                    mountedLetter = Letter;
                    int threadCount = 8;
#if DEBUG
                    threadCount = 1;
#endif
                    _filesystem.Mount(String.Format("{0}:\\", mountedLetter), Settings.Default.UseNetworkDrive ? DokanOptions.NetworkDrive : DokanOptions.RemovableDrive, threadCount);
                }
                catch (Exception e)
                {

                    _lastExeption = e;
                    _exeptionThrown = true;
                    _mountCancel.Cancel();
                }
                Status = DriveStatus.Unmounted;
                this._mountThread = null;
                if (!_exeptionThrown)
                {

                    OnStatusChanged(EventArgs.Empty);
                }

            }
        }
        

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Mount()
        {
            Debug.WriteLine("Mount");

            if (Directory.GetLogicalDrives().Any(drive => drive[0] == Letter))
            {
                throw new Exception("Drive with the same letter exists");
            }


            Status = DriveStatus.Mounting;

            try
            {
                SetupFilesystem();
            }
            catch
            {

                Status = DriveStatus.Unmounted;
                throw;
            }

            SetupMountThread();


            
            var mountEvent = Task.Factory.StartNew(() =>
            {
                while (!_mountCancel.IsCancellationRequested &&
                       Directory.GetLogicalDrives().All(
                           drive => drive[0] != Letter))
                {
                    Thread.Sleep(200);
                }
            }, _mountCancel.Token);


            _pauseEvent.Set();

            mountEvent.Wait();

            if (_exeptionThrown)
            {

                _exeptionThrown = false;

                throw _lastExeption;
            }
            if (Settings.Default.UseNetworkDrive)
                Utilities.SetNetworkDriveName("WinSshFS spool drive" , Name);
            Status = DriveStatus.Mounted;
            OnStatusChanged(EventArgs.Empty);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Unmount()
        {
            if(this._threadCancel != null)
                this._threadCancel.Cancel();
            if(this._pauseEvent != null)
                this._pauseEvent.Set();

            Debug.WriteLine("Unmount");

            Status = DriveStatus.Unmounting;
            try
            {
                Dokan.RemoveMountPoint(String.Format("{0}:\\", mountedLetter));
            }
            catch
            {
                //Status = DriveStatus.Unmounted;
                //  OnStatusChanged(EventArgs.Empty);
            }
            finally
            {
                _filesystem = null;
            }

        }

        public override string ToString()
        {
            return String.Format("{0}[{1}:]", Name, Letter);
        }
        #region Implementation of IDisposable

        public void Dispose()
        {
            Debug.WriteLine("Dispose");

            try
            {
                Dokan.RemoveMountPoint(String.Format("{0}:\\", Letter));
            }
            catch
            {
                Status = DriveStatus.Unmounted;
            }
            finally
            {
                _filesystem = null;
            }
        }

        #endregion

        #region Implementation of ISerializable

        public VirtualDrive(SerializationInfo info,
                         StreamingContext context)
        {
            Letter = info.GetChar("letter");
        }



        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("letter", Letter);
        }

        #endregion

    }
}