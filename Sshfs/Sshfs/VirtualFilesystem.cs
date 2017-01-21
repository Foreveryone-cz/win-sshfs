using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using DokanNet;
using System.Text.RegularExpressions;

using FileAccess = DokanNet.FileAccess;


namespace Sshfs
{
    internal sealed class VirtualFilesystem : IDokanOperations
    {

        #region Fields

        private readonly string _volumeLabel;
        private bool _debugMode = false;

        private readonly List<SftpDrive> _subsytems = new List<SftpDrive>();
        private SftpDrive lastActiveSubsytem;

        #endregion

        #region Constructors

        public VirtualFilesystem(string label = null)
        {
            _volumeLabel = label;
        }

        #endregion

        #region  Methods

        internal void AddSubFS(SftpDrive sftpDrive)
        {
            _subsytems.Add(sftpDrive);
        }

        internal void RemoveSubFS(SftpDrive sftpDrive)
        {
            _subsytems.Remove(sftpDrive);
        }

        #endregion


        #region Logging
        [Conditional("DEBUG")]
        private void Log(string format, params object[] arg)
        {
            if (_debugMode)
            {
                Console.WriteLine(format, arg);
            }

            Debug.AutoFlush = false;
            Debug.Write(DateTime.Now.ToLongTimeString() + " ");
            Debug.WriteLine(format, arg);
            Debug.Flush();
        }

        [Conditional("DEBUG")]
        private void LogFSAction(String action, String path, SftpDrive subsystem, string format, params object[] arg)
        {
            Debug.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + "[--VFS--]" + "\t" + action + "\t" + ( subsystem!=null? subsystem.Name : "-       ") + "\t" + path + "\t");
            Debug.WriteLine(format, arg);
        }

        [Conditional("DEBUG")]
        private void LogFSActionInit(String action, String path, SftpDrive subsystem, string format, params object[] arg)
        {
            LogFSAction(action + "^", path, subsystem, format, arg);
        }
        [Conditional("DEBUG")]
        private void LogFSActionSuccess(String action, String path, SftpDrive subsystem, string format, params object[] arg)
        {
            LogFSAction(action + "$", path, subsystem, format, arg);
        }
        [Conditional("DEBUG")]
        private void LogFSActionError(String action, String path, SftpDrive subsystem, string format, params object[] arg)
        {
            LogFSAction(action + "!", path, subsystem, format, arg);
        }
        [Conditional("DEBUG")]
        private void LogFSActionOther(String action, String path, SftpDrive subsystem, string format, params object[] arg)
        {
            LogFSAction(action + "|", path, subsystem, format, arg);
        }

        #endregion


        #region DokanOperations

        NtStatus IDokanOperations.CreateFile(string fileName, FileAccess access, FileShare share,
                                               FileMode mode, FileOptions options,
                                               FileAttributes attributes, DokanFileInfo info)
        {
            if (info.IsDirectory)
            {
                if (mode == FileMode.Open)
                    return OpenDirectory(fileName, info);
                if (mode == FileMode.CreateNew)
                    return CreateDirectory(fileName, info);

                return NtStatus.NotImplemented;
            }

            if (fileName.EndsWith("desktop.ini", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith("autorun.inf", StringComparison.OrdinalIgnoreCase)) //....
            {
                return NtStatus.NoSuchFile;
            }

            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            LogFSActionInit("OpenFile", fileName, drive, "Mode:{0}", mode);
            if (drive != null)
            {
                LogFSActionSuccess("OpenFile", fileName, drive, "Mode:{0} NonVFS", mode);
                IDokanOperations idops = GetSubSystemOperations(drive);
                if (idops == null)
                {
                    //this happens if mounting failed
                    return NtStatus.AccessDenied;
                }
                return idops.CreateFile(fileName, access, share, mode, options, attributes, info);
            }

            //check against mountpoints if virtual dir exists

            string path = fileName.Substring(1);
            if (path == "")
            {
                //info.IsDirectory = true;
                info.Context = null;
                LogFSActionSuccess("OpenFile", fileName, null, "VFS root");
                return NtStatus.Success;
            }
            foreach (SftpDrive drive2 in this._subsytems)
            {
                if (drive2.MountPoint.Length > 0)
                {
                    if (drive2.MountPoint.IndexOf(path) == 0)
                    {
                        info.IsDirectory = true;
                        info.Context = drive2;
                        LogFSActionSuccess("OpenFile", fileName, drive2, "VFS (sub)mountpoint");
                        return NtStatus.Success;
                    }
                }
            }

            //pathnotfound detection?

            LogFSActionError("OpenFile", fileName, null, "File not found");
            return NtStatus.NoSuchFile;
        }

        private SftpDrive GetDriveByMountPoint(string fileName, out string subfspath)
        {
            LogFSActionInit("LookupMP", fileName, null, "");

            if (fileName.Length>1)
            {
                string path = fileName.Substring(1);
                foreach (SftpDrive drive in this._subsytems)
                {
                    if (drive.MountPoint.Length > 0)
                    {
                        string mpWithPath = drive.MountPoint + "\\";
                        if ( path == drive.MountPoint || path.IndexOf(mpWithPath) == 0 )
                        {
                            subfspath = path.Substring(drive.MountPoint.Length);
                            if (subfspath == "") 
                                subfspath = "\\";
                            LogFSActionSuccess("LookupMP", fileName, drive, "Subsystem path: {0}",subfspath);
                            return drive;
                        }
                    }
                }
            }
            subfspath = fileName;

            LogFSActionSuccess("LookupMP", fileName, null, "VFS path");
            return null;
        }

        private IDokanOperations GetSubSystemOperations(SftpDrive drive)
        {
            if (drive == null)
                return null;

            if ((drive.Status != DriveStatus.Mounted)&&(drive.Status != DriveStatus.Mounting))
            {
                try
                {
                    LogFSActionInit("MOUNT", "", drive, "Mounting...");
                    drive.Mount();
                }
                catch (Exception e)
                {
                    if (e.Message == "Pageant not running")
                    {

                        return null;
                    }

                    LogFSActionError("MOUNT", "", drive, "Mounting failed: {0}",e.Message);
                    //Log("VFS: Mount error: {0}", e.Message);

                    //maybe failed because of letter blocked, but we dont need the letter:
                    if (drive.Letter != ' ')
                    {
                        LogFSActionError("MOUNT", "", drive, "Trying without mounting drive {0}", drive.Letter);
                        char l = drive.Letter;
                        drive.Letter = ' ';
                        try
                        {
                            drive.Mount();
                            drive.Letter = l;
                        }
                        catch
                        {
                            LogFSActionError("MOUNT", "", drive, "Mounting failed again: {0}", e.Message);
                            //connection error
                            drive.Letter = l;
                            //Log("VFS: Mount error: {0}", e.Message);
                            return null;
                        }
                    }
                    else
                    {

                        return null;
                    }

                }
            }

            return ((IDokanOperations)drive._filesystem);
        }

        private NtStatus OpenDirectory(string fileName, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            LogFSActionInit("OpenDir", fileName, drive, "");

            if (drive != null)
            {
                lastActiveSubsytem = drive;

                IDokanOperations ops = GetSubSystemOperations(drive);
                if (ops == null)
                {
                    LogFSActionError("OpenDir", fileName, drive, "Cannot open, mount failed?");
                    return NtStatus.AccessDenied;
                }
                LogFSActionSuccess("OpenDir", fileName, drive, "Found, subsytem");
                return ops.CreateFile(fileName, FileAccess.GenericRead, FileShare.None, FileMode.Open, FileOptions.None, FileAttributes.Directory, info);
            }

            if (fileName.Length == 1) //root dir
            {
                LogFSActionSuccess("OpenDir", fileName, drive, "Found, VFS root");
                info.IsDirectory = true;
                return NtStatus.Success;
            }

            

            //root test shoud keep lastactive if drag and drop(win8)
            lastActiveSubsytem = null;

            string path = fileName.Substring(1);//cut leading \
            
            foreach (SftpDrive subdrive in _subsytems)
            {
                string mp = subdrive.MountPoint; //  mp1 || mp1\mp2 ...
                if (path == mp)
                {
                    info.Context = subdrive;
                    info.IsDirectory = true;
                    LogFSActionSuccess("OpenDir", fileName, drive, "Found, final mountpoint");
                    return NtStatus.Success;
                }

                if (mp.IndexOf(path + '\\') == 0)
                { //path is part of mount point
                    info.Context = subdrive;
                    info.IsDirectory = true;
                    LogFSActionSuccess("OpenDir", fileName, drive, "Found, part of mountpoint");
                    return NtStatus.Success;
                }
            }
            LogFSActionError("OpenDir", fileName, drive, "Path not found");
            return NtStatus.ObjectPathNotFound;
        }

        private NtStatus CreateDirectory(string fileName, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).CreateFile(fileName, FileAccess.GenericRead, FileShare.None, FileMode.CreateNew, FileOptions.None, FileAttributes.Directory, info);

            return NtStatus.AccessDenied;
        }

        void IDokanOperations.Cleanup(string fileName, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            LogFSActionInit("Cleanup", fileName, drive, "");
            if (drive != null)
            {
                LogFSActionSuccess("Cleanup", fileName, drive, "nonVFS clean");
                GetSubSystemOperations(drive).Cleanup(fileName, info);
                return;
            }

            if (info.Context != null)
            {
                drive = info.Context as SftpDrive;
                info.Context = null;
            }

            LogFSActionSuccess("Cleanup", fileName, drive, "VFS clean");
            return;
        }

        void IDokanOperations.CloseFile(string fileName, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            LogFSActionInit("CloseFile", fileName, drive, "");
            if (drive != null)
            {
                LogFSActionSuccess("CloseFile", fileName, drive, "NonVFS close");
                GetSubSystemOperations(drive).CloseFile(fileName, info);
                return;
            }

            if (info.Context != null)
            {
                drive = info.Context as SftpDrive;
                info.Context = null;
            }

            LogFSActionSuccess("CloseFile", fileName, drive, "VFS close");
            return;
        }


        NtStatus IDokanOperations.ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset,
                                             DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).ReadFile(fileName, buffer, out bytesRead, offset, info);

            bytesRead = 0;
            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset,
                                              DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).WriteFile(fileName, buffer, out bytesWritten, offset, info);

            bytesWritten = 0;
            return NtStatus.AccessDenied;
        }


        NtStatus IDokanOperations.FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).FlushFileBuffers(fileName, info);

            return NtStatus.Success;
        }

        NtStatus IDokanOperations.GetFileInformation(string fileName, out FileInformation fileInfo,
                                                       DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            LogFSActionInit("FileInfo", fileName, drive, "");
            if (drive != null)
            {
                LogFSActionSuccess("FileInfo", fileName, drive, "NonVFS");
                return GetSubSystemOperations(drive).GetFileInformation(fileName, out fileInfo, info);
            }

            fileInfo = new FileInformation
            {
                Attributes =
                    FileAttributes.NotContentIndexed | FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.Offline,
                FileName = Path.GetFileName(fileName), //String.Empty,
                // GetInfo info doesn't use it maybe for sorting .
                CreationTime = DateTime.Now,
                LastAccessTime = DateTime.Now,
                LastWriteTime = DateTime.Now,
                Length = 4096
            };

            if (fileName.Length == 1)
            { //root dir
                LogFSActionSuccess("FileInfo", fileName, drive, "root info");
                return NtStatus.Success;
            }

            string path = fileName.Substring(1);//cut leading \

            if (info.Context != null)
            {
                drive = info.Context as SftpDrive;
                LogFSActionSuccess("FileInfo", fileName, drive, "from context");
                return NtStatus.Success;
            }

            foreach (SftpDrive subdrive in _subsytems)
            {
                string mp = subdrive.MountPoint; //  mp1 || mp1\mp2 ...
                if (path == mp)
                {
                    info.Context = mp;
                    //fileInfo.FileName = path.Substring(path.LastIndexOf("\\")+1);
                    LogFSActionSuccess("FileInfo", fileName, drive, "final mountpoint");
                    return NtStatus.Success;
                }

                if (mp.IndexOf(path + '\\') == 0)
                { //path is part of mount point
                    //fileInfo.FileName = path.Substring(path.LastIndexOf("\\") + 1);
                    LogFSActionSuccess("FileInfo", fileName, drive, "part of mountpoint");
                    return NtStatus.Success;
                }
            }

            LogFSActionError("FileInfo", fileName, drive, "path not found");
            return NtStatus.ObjectPathNotFound;


        }

        NtStatus IDokanOperations.FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info)
        {
            files = null;
            return NtStatus.NotImplemented;
        }

        NtStatus IDokanOperations.FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            LogFSActionError("FindFiles", fileName, drive, "!? not using FindFilesWithPattern !?");

            if (drive != null)
                return GetSubSystemOperations(drive).FindFiles(fileName, out files, info);
            
            //this shoud be never called

            files = new List<FileInformation>();

            string path = fileName.Substring(1);//cut leading \
            foreach(SftpDrive subdrive in _subsytems)
            {
                string mp = subdrive.MountPoint; //  mp1 || mp1\mp2 ...

                if (path.Length > 0) //not root dir
                {
                    if (path == mp) //this shoud not happend, because is managed by drive
                    {
                        Log("Error, mountpoint not in drives?");
                        break;
                    }

                    if (mp.IndexOf(path + '\\') == 0) //path is part of mount point =>implies=> length of path>mp
                    {
                        mp = mp.Substring(path.Length + 1); //cut the path
                    }
                    else
                    {
                        continue;
                    }
                }

                int cuttmp = mp.IndexOf('\\');
                if (cuttmp>0) // have submountpoint like  mp1\mp2 
                {
                    mp = mp.Substring(0, cuttmp);
                }

                if (!files.Select(file => file.FileName).Contains(mp) && mp != "")
                {
                    FileInformation fi = new FileInformation();
                    fi.FileName = mp;
                    fi.Attributes = FileAttributes.NotContentIndexed | FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.Offline;
                    fi.CreationTime = DateTime.Now;
                    fi.LastWriteTime = DateTime.Now;
                    fi.LastAccessTime = DateTime.Now;

                    files.Add(fi);
                }
            }
           
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            Log("VFS TrySetAttributes:{0}\n{1};", fileName, attributes);
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).SetFileAttributes(fileName, attributes, info);

            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
                                                DateTime? lastWriteTime, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).SetFileTime(fileName, creationTime, lastAccessTime, lastWriteTime, info);

            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.DeleteFile(string fileName, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).DeleteFile(fileName, info);

            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.DeleteDirectory(string fileName, DokanFileInfo info)
        {
            Log("VFS DeleteDirectory:{0}", fileName);
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).DeleteDirectory(fileName, info);

            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            Log("VFS MoveFile |Name:{0} ,NewName:{3},Reaplace{4},IsDirectory:{1} ,Context:{2}",
                oldName, info.IsDirectory,
                info.Context, newName, replace);
            //todo: check newname?
            SftpDrive drive = this.GetDriveByMountPoint(oldName, out oldName);
            if (drive != null)
            {
                SftpDrive drive2 = this.GetDriveByMountPoint(newName, out newName);
                if (drive2 != drive)
                {
                    //This is server2server move - Total commander handles this by copy&delete, explorer ends with error
                    //background direct copy between 2 sftp is nice but not real
                    return NtStatus.NotImplemented;
                }

                return GetSubSystemOperations(drive).MoveFile(oldName, newName, replace, info);
            }

            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).SetEndOfFile(fileName,length, info);
            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            Log("VFS SetSize");
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).SetAllocationSize(fileName, length, info);

            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).LockFile(fileName, offset, length, info);

            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).UnlockFile(fileName, offset, length, info);
            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.GetDiskFreeSpace(out long free, out long total,
                                                     out long used, DokanFileInfo info)
        {
            Log("VFS GetDiskFreeSpace");
            if (lastActiveSubsytem != null)
            {
                IDokanOperations ops = GetSubSystemOperations(lastActiveSubsytem);
                if (ops != null)
                {
                    return ops.GetDiskFreeSpace(out free, out total, out used, info);    
                }
            }

            long terabyte = (long)1024 * 1024 * 1024 * 1024;

            total = 10*terabyte;//1TB for Explorer to see space
            used = terabyte;
            free = total - used;

            return NtStatus.Success;
        }

        NtStatus IDokanOperations.GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
                                                         out string filesystemName, DokanFileInfo info)
        {
            LogFSActionSuccess("DiskInfo", _volumeLabel, null, "");

            volumeLabel = _volumeLabel;

            filesystemName = "SSHVFS";

            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch |
                       FileSystemFeatures.SupportsRemoteStorage | FileSystemFeatures.UnicodeOnDisk | FileSystemFeatures.SupportsObjectIDs;
            //FileSystemFeatures.PersistentAcls


            return NtStatus.Success;
        }

        NtStatus IDokanOperations.GetFileSecurity(string fileName, out FileSystemSecurity security,
                                                    AccessControlSections sections, DokanFileInfo info)
        {
            Log("VFS GetSecurrityInfo:{0}:{1}", fileName, sections);

            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).GetFileSecurity(fileName, out security, sections, info);

            security = null;
            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.SetFileSecurity(string fileName, FileSystemSecurity security,
                                                    AccessControlSections sections, DokanFileInfo info)
        {
            Log("VFS TrySetSecurity:{0}", fileName);
            SftpDrive drive = this.GetDriveByMountPoint(fileName, out fileName);
            if (drive != null)
                return GetSubSystemOperations(drive).SetFileSecurity(fileName, security, sections, info);

            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.Unmounted(DokanFileInfo info)
        {
            Log("UNMOUNTED");
            // Disconnect();
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.Mounted(DokanFileInfo info)
        {
            Log("MOUNTED");
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            streams = new FileInformation[0];
            return NtStatus.NotImplemented;
        }

        #endregion

        /*
        #region Events

        public event EventHandler<EventArgs> Disconnected
        {
            add { Session.Disconnected += value; }
            remove { Session.Disconnected -= value; }
        }

        #endregion*/
    }
}