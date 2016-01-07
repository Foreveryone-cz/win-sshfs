// Copyright (c) 2012 Dragan Mladjenovic
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


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
using Renci.SshNet;
using Sshfs.Properties;
using Renci.SshNet.Pageant;
#endregion

namespace Sshfs
{
    [Serializable]
    public class SftpDrive : IDisposable, ISerializable
    {
        
        private CancellationTokenSource _mountCancel = new CancellationTokenSource();
        private AutoResetEvent _pauseEvent = new AutoResetEvent(false);
        private CancellationTokenSource _threadCancel = new CancellationTokenSource();
        private bool _exeptionThrown;
        internal SftpFilesystem _filesystem;
      
        private Exception _lastExeption;
        private Thread _mountThread;

        private string _connection;

        public string Name { get; set; }

        public char Letter { get; set; }

        public ConnectionType ConnectionType { get; set; }

        public string PrivateKey { get; set; }

        public string Password { get; set; }

        public string Passphrase { get; set; }

        public string Username { get; set; }

        public string Host { get; set; }


        public int Port { get; set; }


        public bool Automount { get; set; }


        public string Root { get; set; }

        public object Tag { get; set; }

        public DriveStatus Status { get; private set; }

        public string MountPoint { get; set; }

        public int ProxyType { get; set; }
        public string ProxyHost { get; set; }
        public string ProxyUser { get; set; }
        public string ProxyPass { get; set; }

        public int KeepAliveInterval { get; set; }

        public SftpDrive(){}
       
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
            Debug.WriteLine("SetupFilesystem {0},{1},{2},{3}",Host,Port,Username,ConnectionType.ToString());

            ProxyTypes pt = ProxyTypes.None;
            switch (ProxyType) {
              case 1: pt = ProxyTypes.Http; break;
              case 2: pt = ProxyTypes.Socks4; break;
              case 3: pt = ProxyTypes.Socks5; break;
            }
            int ProxyPort = 8080;
            var Proxy = ProxyHost;
            if (ProxyHost != null)
            {
                var s = ProxyHost.Split(':');
                if (s.Length > 1)
                {
                    Int32.TryParse(s[1], out ProxyPort);
                    Proxy = s[0];
                }
            }
            
            if(KeepAliveInterval <= 0)
            {
                KeepAliveInterval = 1;
            }

            ConnectionInfo info;
            switch (ConnectionType)
            {
                case ConnectionType.Pageant:
                    var agent = new PageantProtocol();
                    if (pt == ProxyTypes.None)
                    {
                        info = new AgentConnectionInfo(Host, Port, Username, agent);
                    }
                    else if (ProxyUser.Length > 0)
                    {
                        info = new AgentConnectionInfo(Host, Port, Username, pt, Proxy, ProxyPort, ProxyUser, ProxyPass, agent);
                    }
                    else
                    {
                        info = new AgentConnectionInfo(Host, Port, Username, pt, Proxy, ProxyPort, agent);
                    }
                    break;
                case ConnectionType.PrivateKey:
                    if (pt == ProxyTypes.None) {
                      info = new PrivateKeyConnectionInfo(Host, Port, Username, new PrivateKeyFile(PrivateKey, Passphrase));
                    }
                    else if (ProxyUser.Length > 0) {
                      info = new PrivateKeyConnectionInfo(Host, Port, Username, pt, Proxy, ProxyPort, ProxyUser, ProxyPass, new PrivateKeyFile(PrivateKey, Passphrase));
                    }
                    else {
                      info = new PrivateKeyConnectionInfo(Host, Port, Username, pt, Proxy, ProxyPort, new PrivateKeyFile(PrivateKey, Passphrase));
                    }
                    break;
                default:
                    if (pt == ProxyTypes.None) {
                      info = new PasswordConnectionInfo(Host, Port, Username, Password);
                    }
                    else if (ProxyUser.Length > 0) {
                      info = new PasswordConnectionInfo(Host, Username, Password, pt, Proxy, ProxyPort, ProxyUser, ProxyPass);
                    }
                    else {
                      info = new PasswordConnectionInfo(Host, Port, Username, Password, pt, Proxy, ProxyPort);
                    }
                    break;
            }

            _connection = Settings.Default.UseNetworkDrive ? String.Format("\\\\{0}\\{1}\\{2}", info.Host, Root, info.Username) : Name;

            _filesystem = new SftpFilesystem(info, Root,_connection,Settings.Default.UseOfflineAttribute,false, (int) Settings.Default.AttributeCacheTimeout,  (int) Settings.Default.DirContentCacheTimeout);
            Debug.WriteLine("Connecting...");
            _filesystem.KeepAliveInterval = new TimeSpan(0, 0, KeepAliveInterval);
            _filesystem.Connect();
            _filesystem.Disconnected += OnDisconnectedFSEvent;
            _filesystem.ErrorOccurred += OnDisconnectedFSEvent;
        }

        Thread reconnectThread;

        private void OnDisconnectedFSEvent(object sender, EventArgs ea)
        {
            this.startReconnect();
        }

        private void startReconnect()
        {
            this.stopReconnect();

            this.reconnectThread = new Thread(new ThreadStart(reconnectJob));
            this.reconnectThread.Start();
        }
        private void stopReconnect()
        {
            this._threadCancel.Cancel();
            if (this.reconnectThread != null && this.reconnectThread.IsAlive)
            {
                this.reconnectThread.Abort();
            }
            this.reconnectThread = null;
        }

        private void reconnectJob()
        {
            this.Unmount();

            while (this.Status != DriveStatus.Mounted)
            {
                try
                {
                    if (_threadCancel.IsCancellationRequested)
                    {
                        Debug.WriteLine("Reconnect thread:Cancel");
                        break;
                    }
                    this.Mount();
                }
                catch 
                {}
                if (this.Status != DriveStatus.Mounted){
                    Thread.Sleep(1000);
                }
            }
        }

        

        private void SetupMountThread()
        {
            _threadCancel = new CancellationTokenSource();
            _pauseEvent = new AutoResetEvent(false);
            _mountCancel = new CancellationTokenSource();

            Debug.WriteLine("Thread:Created");
            _mountThread = new Thread(MountLoop) {IsBackground = true};

            _mountThread.Start();
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
                    int threadCount = 8;
#if DEBUG
                threadCount=1;
#endif
                    _filesystem.Mount(String.Format("{0}:\\", Letter),
                        Settings.Default.UseNetworkDrive?DokanOptions.NetworkDrive|DokanOptions.KeepAlive: DokanOptions.RemovableDrive|DokanOptions.KeepAlive, threadCount);
                }
                catch (Exception e)
                {
                    
                    _lastExeption = e;
                    _exeptionThrown = true;
                    _mountCancel.Cancel();
                }
                Status = DriveStatus.Unmounted;
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
           

            if (Directory.GetLogicalDrives().Any(drive=>drive[0]==Letter))
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

            if (Letter != ' ')
            {
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
                    Utilities.SetNetworkDriveName(_connection, Name);
            }
            Status= DriveStatus.Mounted;
            OnStatusChanged(EventArgs.Empty);



        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Unmount()
        {
            if (this.reconnectThread != Thread.CurrentThread)
            {
                this.stopReconnect();
            }

            if (_threadCancel != null) _threadCancel.Cancel();
            if (_pauseEvent != null) _pauseEvent.Set();

            Debug.WriteLine("Unmount");
            Status = DriveStatus.Unmounting;
            try
            {
                Dokan.RemoveMountPoint(String.Format("{0}:\\", Letter));
                if (_filesystem != null)
                {
                    _filesystem.Dispose();
                }
            }
            catch
            {
                //Status = DriveStatus.Unmounted;
                //  OnStatusChanged(EventArgs.Empty);
            }
            finally
            {
                _filesystem = null;
                Status = DriveStatus.Unmounted;
                OnStatusChanged(EventArgs.Empty);
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


            if (_threadCancel != null) _threadCancel.Cancel();
            if (_pauseEvent != null) _pauseEvent.Set();

            try
            {
                Dokan.RemoveMountPoint(String.Format("{0}:\\", Letter));
                if (_filesystem != null)
                {
                    _filesystem.Dispose();


                    _filesystem = null;
                }
            }
            catch
            {
                if(Status != DriveStatus.Unmounted)
                    Status = DriveStatus.Unmounted;
            }
            finally
            {
                _filesystem = null;
            }


            if (_mountCancel != null) {_mountCancel.Dispose();}
            if (_threadCancel != null) {_threadCancel.Dispose();}
            if (_pauseEvent != null) {_pauseEvent.Dispose();}
        }

        #endregion

        #region Implementation of ISerializable

        public SftpDrive(SerializationInfo info,
                         StreamingContext context)
        {
            Name = info.GetString("name");
            Host = info.GetString("host");
            Port = info.GetInt32("port");
            Letter = info.GetChar("drive");
            Root = info.GetString("path");
            Automount = info.GetBoolean("mount");
            Username = info.GetString("user");
            try {
              ProxyType = info.GetInt32("proxyType");
              ProxyHost = info.GetString("proxyHost");
              ProxyUser = info.GetString("proxyUser");
              ProxyPass = info.GetString("proxyPass");
            }
            catch { }
            try
            {
                KeepAliveInterval = info.GetInt16("keepAliveInterval");
            }
            catch
            {
                KeepAliveInterval = 1;
            }
            ConnectionType = (ConnectionType) info.GetByte("c");
            if (ConnectionType == ConnectionType.Password)
            {
                Password = Utilities.UnprotectString(info.GetString("p"));
            }
            else
            {
                Passphrase = Utilities.UnprotectString(info.GetString("p"));
                PrivateKey = info.GetString("k");
            }
            try
            {
                MountPoint = info.GetString("mountpoint");
            }
            catch
            {
                MountPoint = Name;//default is name after version update
            }
        }



        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", Name);
            info.AddValue("host", Host);
            info.AddValue("port", Port);
            info.AddValue("drive", Letter);
            info.AddValue("path",Root);
            info.AddValue("mount", Automount);
            info.AddValue("user", Username);
            info.AddValue("c", (byte)ConnectionType);
            info.AddValue("mountpoint", MountPoint);
            info.AddValue("proxyType", ProxyType);
            info.AddValue("proxyHost", ProxyHost);
            info.AddValue("proxyUser", ProxyUser);
            info.AddValue("proxyPass", ProxyPass);
            info.AddValue("keepAliveInterval", KeepAliveInterval);
            if (ConnectionType == ConnectionType.Password)
            {
                info.AddValue("p", Utilities.ProtectString(Password));
            }
            else
            {
                info.AddValue("p", Utilities.ProtectString(Passphrase));
                info.AddValue("k", PrivateKey);
            }
        }

        #endregion

    }
}