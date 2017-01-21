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


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using DokanNet;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using FileAccess = DokanNet.FileAccess;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Sshfs
{

    internal sealed class SftpFilesystem : SftpClient, IDokanOperations
    {

        /// <summary>
        /// Sets the last-error code for the calling thread.
        /// </summary>
        /// <param name="dwErrorCode">The last-error code for the thread.</param>
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern void SetLastError(uint dwErrorCode);

        #region Constants

        #endregion

        #region Fields

        private readonly MemoryCache _cache = MemoryCache.Default;

        private SshClient _sshClient;
        private readonly TimeSpan _operationTimeout = TimeSpan.FromSeconds(30);//new TimeSpan(0, 0, 0, 0, -1);
        private string _rootpath;

        private readonly bool _useOfflineAttribute;
        private readonly bool _debugMode;


        private int _userId;
        private string _idCommand = "id";
        private string _dfCommand = "df";
        private HashSet<int> _userGroups;

        private readonly int _attributeCacheTimeout;
        private readonly int _directoryCacheTimeout;

        private readonly string _volumeLabel;

        #endregion

        #region Constructors

        public SftpFilesystem(ConnectionInfo connectionInfo, string rootpath, string label = null,
                              bool useOfflineAttribute = false,
                              bool debugMode = false, int attributeCacheTimeout = 5, int directoryCacheTimeout = 60)
            : base(connectionInfo)
        {
            _rootpath = rootpath;
            _directoryCacheTimeout = directoryCacheTimeout;
            _attributeCacheTimeout = attributeCacheTimeout;
            _useOfflineAttribute = useOfflineAttribute;
            _debugMode = debugMode;
            _volumeLabel = label ?? String.Format("{0} on '{1}'", ConnectionInfo.Username, ConnectionInfo.Host);
        }

        #endregion

        #region Method overrides

        protected override void OnConnected()
        {
            base.OnConnected();

            _sshClient = new SshClient(ConnectionInfo);

            this.Log("Connected %s", _volumeLabel);
            _sshClient.Connect();

            CheckAndroid();

            _userId = GetUserId();
            if (_userId != -1)
                _userGroups = new HashSet<int>(GetUserGroupsIds());


            if (String.IsNullOrWhiteSpace(_rootpath))
            {
                _rootpath = this.WorkingDirectory;
            }
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();
            this.Log("disconnected %s", _volumeLabel);
        }

        protected override void Dispose(bool disposing)
        {
            if (_sshClient != null)
            {
                _sshClient.Dispose();
                _sshClient = null;
            }
            base.Dispose(disposing);
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
        private void LogFSAction(String action, String path, SftpContext context, string format, params object[] arg)
        {
            Debug.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + (context == null ? "[-------]" : context.ToString()) + "\t" + action + "\t" + _volumeLabel + "\t" + path + "\t");
            Debug.WriteLine(format, arg);
        }

        [Conditional("DEBUG")]
        private void LogFSActionInit(String action, String path, SftpContext context, string format, params object[] arg)
        {
            LogFSAction(action + "^", path, context, format, arg);
        }
        [Conditional("DEBUG")]
        private void LogFSActionSuccess(String action, String path, SftpContext context, string format, params object[] arg)
        {
            LogFSAction(action + "$", path, context, format, arg);
        }
        [Conditional("DEBUG")]
        private void LogFSActionError(String action, String path, SftpContext context, string format, params object[] arg)
        {
            LogFSAction(action + "!", path, context, format, arg);
        }
        [Conditional("DEBUG")]
        private void LogFSActionOther(String action, String path, SftpContext context, string format, params object[] arg)
        {
            LogFSAction(action + "|", path, context, format, arg);
        }

        #endregion

        #region Cache

        private void CacheAddAttr(string path, SftpFileAttributes attributes, DateTimeOffset expiration)
        {
            LogFSActionSuccess("CacheSetAttr", path, null, "Expir:{1} Size:{0}", attributes.Size, expiration);
            _cache.Add(_volumeLabel+"A:"+path, attributes, expiration);
        }

        private void CacheAddDir(string path, Tuple<DateTime, IList<FileInformation>> dir, DateTimeOffset expiration)
        {
            LogFSActionSuccess("CacheSetDir", path, null, "Expir:{1} Count:{0}", dir.Item2.Count, expiration);
            _cache.Add(_volumeLabel + "D:" + path, dir, expiration);
        }

        private void CacheAddDiskInfo(Tuple<long, long, long> info, DateTimeOffset expiration)
        {
            LogFSActionSuccess("CacheSetDInfo", _volumeLabel, null, "Expir:{0}", expiration);
            _cache.Add(_volumeLabel + "I:", info, expiration);
        }


        private SftpFileAttributes CacheGetAttr(string path)
        {
            SftpFileAttributes attributes = _cache.Get(_volumeLabel + "A:" + path) as SftpFileAttributes;
            LogFSActionSuccess("CacheGetAttr", path, null, "Size:{0} Group write:{1} ", (attributes == null) ? "miss" : attributes.Size.ToString(), (attributes == null ? "miss" : attributes.GroupCanWrite.ToString()) );
            return attributes;
        }

        private Tuple<DateTime, IList<FileInformation>> CacheGetDir(string path)
        {
            Tuple<DateTime, IList<FileInformation>> dir = _cache.Get(_volumeLabel + "D:" + path) as Tuple<DateTime, IList<FileInformation>>;
            LogFSActionSuccess("CacheGetDir", path, null, "Count:{0}", (dir==null) ? "miss" : dir.Item2.Count.ToString());
            return dir;
        }

        private Tuple<long, long, long> CacheGetDiskInfo()
        {
            Tuple<long, long, long> info = _cache.Get(_volumeLabel + "I:") as Tuple<long, long, long>;
            LogFSActionSuccess("CacheGetDInfo", _volumeLabel, null, "");
            return info;
        }

        private void CacheReset(string path)
        {
            LogFSActionSuccess("CacheReset", path, null, "");
            _cache.Remove(_volumeLabel + "A:" + path);
            _cache.Remove(_volumeLabel + "D:" + path);
        }

        private void CacheResetParent(string path)
        {
            int index = path.LastIndexOf('/');
            if (index > 0)
            {
                this.CacheReset(path.Substring(0, index));
            }
            else
            {
                this.CacheReset("/");
            }
        }


        #endregion

        #region  Methods

        private string GetUnixPath(string path)
        {
            return String.Format("{0}{1}", _rootpath, path.Replace('\\', '/').Replace("//","/"));
        }

        private void CheckAndroid()
        {
            using (var cmd = _sshClient.CreateCommand("test -f /system/build.prop", Encoding.UTF8))
            {
                cmd.Execute();
                if (cmd.ExitStatus == 0)
                {
                    _idCommand = "busybox id";
                    _dfCommand = "busybox df";
                }
            }
        }
        private IEnumerable<int> GetUserGroupsIds()
        {
            using (var cmd = _sshClient.CreateCommand(_idCommand + " -G ", Encoding.UTF8))
            {
                cmd.Execute();
                return cmd.Result.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse);
            }
        }

        private int GetUserId()
        {
            using (var cmd = _sshClient.CreateCommand(_idCommand + " -u ", Encoding.UTF8))
            // Thease commands seems to be POSIX so the only problem would be Windows enviroment
            {
                cmd.Execute();
                return cmd.ExitStatus == 0 ? Int32.Parse(cmd.Result) : -1;
            }
        }

        private bool UserCanRead(SftpFileAttributes attributes)
        {
            return _userId <= 0 || (attributes.OwnerCanRead && attributes.UserId == _userId ||
                                     (attributes.GroupCanRead && _userGroups.Contains(attributes.GroupId) ||
                                      attributes.OthersCanRead));
        }

        private bool UserCanWrite(SftpFileAttributes attributes)
        {
            return _userId <= 0 || (attributes.OwnerCanWrite && attributes.UserId == _userId ||
                                     (attributes.GroupCanWrite && _userGroups.Contains(attributes.GroupId) ||
                                      attributes.OthersCanWrite));
        }

        private bool UserCanExecute(SftpFileAttributes attributes)
        {
            return _userId <= 0 || (attributes.OwnerCanExecute && attributes.UserId == _userId ||
                                     (attributes.GroupCanExecute && _userGroups.Contains(attributes.GroupId) ||
                                      attributes.OthersCanExecute));
        }

        private bool GroupRightsSameAsOwner(SftpFileAttributes attributes)
        {
            return (attributes.GroupCanWrite == attributes.OwnerCanWrite)
                    && (attributes.GroupCanRead == attributes.OwnerCanRead)
                    && (attributes.GroupCanExecute == attributes.OwnerCanExecute);
        }

        override public SftpFileAttributes GetAttributes(string path)
        {
            SftpFileAttributes attributes = base.GetAttributes(path);
            this.ExtendSFtpFileAttributes(path, attributes);
            return attributes;
        }

        private SftpFileAttributes ExtendSFtpFileAttributes(string path, SftpFileAttributes attributes)
        {
            if (attributes.IsSymbolicLink)
            {
                SftpFile symTarget;
                try {
                    symTarget = this.GetSymbolicLinkTarget(path);
                }
                catch (SftpPathNotFoundException)
                {
                    //invalid symlink
                    attributes.SymbolicLinkTarget = null;
                    return attributes;
                }
                attributes.IsSymbolicLinkToDirectory = symTarget.Attributes.IsDirectory;
                attributes.SymbolicLinkTarget = symTarget.FullName;
                if (!attributes.IsSymbolicLinkToDirectory)
                {
                    attributes.Size = symTarget.Attributes.Size;
                }
            }
            return attributes;
        }

        #endregion

        #region DokanOperations

        NtStatus IDokanOperations.CreateFile(string fileName, FileAccess access, FileShare share,
                                               FileMode mode, FileOptions options,
                                               FileAttributes attributes, DokanFileInfo info)
        {
            //Split into four methods?
#if DEBUG
            //info.ProcessId
            string processName = Process.GetProcessById(info.ProcessId).ProcessName;
            LogFSActionInit("CreateFile", fileName, (SftpContext)info.Context, "ProcessName:{0} Mode:{1} Options:{2} IsDirectory:{3}", processName, mode, options, info.IsDirectory);
#endif

            if (fileName.Contains("symlinkfile"))
            {
            }

            if (info.IsDirectory)
            {
                SftpFileAttributes attributesDir = null;
                try {
                     attributesDir = this.GetAttributes(this.GetUnixPath(fileName));//todo load from cache first
                }
                catch (SftpPathNotFoundException){}
                if (attributesDir == null || attributesDir.IsDirectory || attributesDir.IsSymbolicLinkToDirectory)
                {

                    if (mode == FileMode.Open)
                    {
                        NtStatus status = OpenDirectory(fileName, info);

                        try
                        {
                            if (status == NtStatus.ObjectNameNotFound)
                            {
                                GetAttributes(fileName);
                                //no expception -> its file
                                return (NtStatus)0xC0000103L; //STATUS_NOT_A_DIRECTORY    
                            }
                        }
                        catch (SftpPathNotFoundException)
                        {
                        }
                        return status;

                    }
                    if (mode == FileMode.CreateNew)
                        return CreateDirectory(fileName, info);

                    return NtStatus.NotImplemented;
                }
                else
                {
                    //its symbolic link behaving like directory?
                    return NtStatus.NotImplemented;
                }
            }

            if (fileName.EndsWith("desktop.ini", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith("autorun.inf", StringComparison.OrdinalIgnoreCase)) //....
            {
                return NtStatus.NoSuchFile;
            }

            LogFSActionInit("OpenFile", fileName, (SftpContext)info.Context, "Mode:{0} Options:{1}", mode,options);


            string path = GetUnixPath(fileName);
            var sftpFileAttributes = this.CacheGetAttr(path);

            if (sftpFileAttributes == null)
            {
                //Log("cache miss");
                try
                {
                    sftpFileAttributes = GetAttributes(path);
                }
                catch(SftpPathNotFoundException)
                {
                    Debug.WriteLine("File not found");
                    sftpFileAttributes = null;
                }

                if (sftpFileAttributes != null)
                    CacheAddAttr(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                else
                {
                    LogFSActionOther("OpenFile", fileName, (SftpContext)info.Context, "get attributes failed");
                }
            }

            switch (mode)
            {
                case FileMode.Open:
                    if (sftpFileAttributes != null)
                    {
                        if (((uint)access & 0xe0000027) == 0 || sftpFileAttributes.IsDirectory)
                        //check if only wants to read attributes,security info or open directory
                        {
                            info.IsDirectory = sftpFileAttributes.IsDirectory || sftpFileAttributes.IsSymbolicLinkToDirectory;
                            
                            if (options.HasFlag(FileOptions.DeleteOnClose))
                            {
                                return NtStatus.Error;//this will result in calling DeleteFile in Windows Explorer
                            }
                            info.Context = new SftpContext(sftpFileAttributes, false);

                            LogFSActionOther("OpenFile", fileName, (SftpContext)info.Context, "Dir open or get attrs");
                            return NtStatus.Success;
                        }
                    }
                    else
                    {
                        LogFSActionError("OpenFile", fileName, (SftpContext)info.Context, "File not found");
                        return NtStatus.NoSuchFile;
                    }
                    break;
                case FileMode.CreateNew:
                    if (sftpFileAttributes != null)
                        return NtStatus.ObjectNameCollision;

                    CacheResetParent(path);
                    break;
                case FileMode.Truncate:
                    if (sftpFileAttributes == null)
                        return NtStatus.NoSuchFile;
                    CacheResetParent(path);
                    //_cache.Remove(path);
                    this.CacheReset(path);
                    break;
                default:

                    CacheResetParent(path);
                    break;
            }
            try
            {
                info.Context = new SftpContext(this, path, mode,
                                               ((ulong) access & 0x40010006) == 0
                                                   ? System.IO.FileAccess.Read
                                                   : System.IO.FileAccess.ReadWrite, sftpFileAttributes);
                if ( sftpFileAttributes != null)
                {

                    SetLastError(183); //ERROR_ALREADY_EXISTS
                }
            }
            catch (SshException) // Don't have access rights or try to read broken symlink
            {
                var ownerpath = path.Substring(0, path.LastIndexOf('/'));
                var sftpPathAttributes = CacheGetAttr(ownerpath);

                if (sftpPathAttributes == null)
                {
                    //Log("cache miss");
                    try
                    {
                        sftpFileAttributes = GetAttributes(ownerpath);
                    }
                    catch (SftpPathNotFoundException)
                    {
                        Debug.WriteLine("File not found");
                        sftpFileAttributes = null;
                    }
                    if (sftpPathAttributes != null)
                        CacheAddAttr(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                    else
                    {
                        //Log("Up directory must be created");
                        LogFSActionError("OpenFile", fileName, (SftpContext)info.Context, "Up directory mising:{0}", ownerpath);
                        return NtStatus.ObjectPathNotFound;
                    }
                }
                LogFSActionError("OpenFile", fileName, (SftpContext)info.Context, "Access denied");
                return NtStatus.AccessDenied;
            }

            LogFSActionSuccess("OpenFile", fileName, (SftpContext)info.Context, "Mode:{0}", mode);
            return NtStatus.Success;
        }

        private NtStatus OpenDirectory(string fileName, DokanFileInfo info)
        {
#if DEBUG
            string processName = Process.GetProcessById(info.ProcessId).ProcessName;
            LogFSActionInit("OpenDir", fileName, (SftpContext)info.Context, "ProcessName:{0}", processName);
#endif

            string path = GetUnixPath(fileName);
            var sftpFileAttributes = CacheGetAttr(path);

            if (sftpFileAttributes == null)
            {
                //Log("cache miss");

                try
                {
                    sftpFileAttributes = GetAttributes(path);
                }
                catch (SftpPathNotFoundException)
                {
                    Debug.WriteLine("Dir not found");
                    sftpFileAttributes = null;
                }
                if (sftpFileAttributes != null)
                    CacheAddAttr(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
            }

            if (sftpFileAttributes != null)
            {
                if (!sftpFileAttributes.IsDirectory && !sftpFileAttributes.IsSymbolicLinkToDirectory)
                {
                    return (NtStatus)0xC0000103L; //STATUS_NOT_A_DIRECTORY
                }
                if (!UserCanExecute(sftpFileAttributes) || !UserCanRead(sftpFileAttributes))
                {
                    return NtStatus.AccessDenied;
                }


                info.IsDirectory = true;
                info.Context = new SftpContext(sftpFileAttributes);
                
                var dircache = CacheGetDir(path);
                if (dircache != null && dircache.Item1 != sftpFileAttributes.LastWriteTime)
                {
                    CacheReset(path);
                }
                LogFSActionSuccess("OpenDir", fileName, (SftpContext)info.Context,"");
                return NtStatus.Success;
            }
            LogFSActionError("OpenDir", fileName, (SftpContext)info.Context,"Path not found");
            //return NtStatus.ObjectPathNotFound;            
            return NtStatus.ObjectNameNotFound;
        }

        private NtStatus CreateDirectory(string fileName, DokanFileInfo info)
        {
            LogFSActionInit("OpenDir", fileName, (SftpContext)info.Context, "");

            string path = GetUnixPath(fileName);
            try
            {
                CreateDirectory(path);
                CacheResetParent(path);
            }
            catch (SftpPermissionDeniedException)
            {
                LogFSActionError("OpenDir", fileName, (SftpContext)info.Context, "Access denied");
                return NtStatus.AccessDenied;
            }
            catch (SshException) // operation should fail with generic error if file already exists
            {
                LogFSActionError("OpenDir", fileName, (SftpContext)info.Context, "Already exists");
                return NtStatus.ObjectNameCollision;
            }
            LogFSActionSuccess("OpenDir", fileName, (SftpContext)info.Context,"");
            return NtStatus.Success;
        }

        void IDokanOperations.Cleanup(string fileName, DokanFileInfo info)
        {
            LogFSActionInit("Cleanup", fileName, (SftpContext)info.Context, "");

            bool deleteOnCloseWorkAround = false;//TODO not used probably, can be removed

            if (info.Context != null)
            {
                deleteOnCloseWorkAround = ((SftpContext)info.Context).deleteOnCloseWorkaround;

                (info.Context as SftpContext).Release();

                info.Context = null;
            }

            if (info.DeleteOnClose || deleteOnCloseWorkAround)
            {
                string path = GetUnixPath(fileName);
                if (info.IsDirectory) //can be also symlink file!
                {
                    try
                    {
                        SftpFileAttributes attributes = this.CacheGetAttr(path);
                        if (attributes == null)
                        {
                            attributes = this.GetAttributes(path);
                        }
                        if (attributes == null)
                        {
                            //shoud never happen
                            throw new SftpPathNotFoundException();
                        }
                        if (attributes.IsSymbolicLink) //symlink file or dir, can be both
                        {
                            DeleteFile(path);
                        }
                        else
                        {
                            DeleteDirectory(path);
                        }

                    }
                    catch (Exception) //in case we are dealing with simbolic link
                    {
                        //This may cause an error
                    }
                }
                else
                {
                    try
                    {
                        DeleteFile(path);
                    }
                    catch (SftpPathNotFoundException)
                    {
                        //not existing file
                    }
                }
                CacheReset(path);
                CacheResetParent(path);
            }

            LogFSActionSuccess("Cleanup", fileName, (SftpContext)info.Context, "");
        }

        void IDokanOperations.CloseFile(string fileName, DokanFileInfo info)
        {
            LogFSActionInit("CloseFile", fileName, (SftpContext)info.Context, "");
            
            if (info.Context != null)
            {
                SftpContext context = (SftpContext) info.Context;
                if (context.Stream != null)
                {
                    (info.Context as SftpContext).Stream.Flush();
                    (info.Context as SftpContext).Stream.Dispose();
                }
            }


            /* cache reset for dir close is not good idea, will read it very soon again */
            if (!info.IsDirectory)
            {
                CacheReset(GetUnixPath(fileName));
            }
        }


        NtStatus IDokanOperations.ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset,
                                             DokanFileInfo info)
        {
            LogFSActionInit("ReadFile", fileName, (SftpContext)info.Context, "BuffLen:{0} Offset:{1}", buffer.Length, offset);

            if (info.Context == null)
            {
                //called when file is read as memory memory mapeded file usualy notepad and stuff
                SftpFileStream handle = Open(GetUnixPath(fileName), FileMode.Open);
                if (offset == 0)
                {
                    handle.Seek(offset, SeekOrigin.Begin);
                }
                else
                {
                    handle.Seek(offset, SeekOrigin.Current);
                }
                bytesRead = handle.Read(buffer, 0, buffer.Length);
                handle.Close();
                LogFSActionOther("ReadFile", fileName, (SftpContext)info.Context, "NOCONTEXT BuffLen:{0} Offset:{1} Read:{2}", buffer.Length, offset,bytesRead);
            }
            else
            {
                SftpContextStream stream = (info.Context as SftpContext).Stream;
                lock (stream)
                {
                    stream.Position = offset;
                    bytesRead = stream.Read(buffer, 0, buffer.Length);

                    LogFSActionOther("ReadFile", fileName, (SftpContext)info.Context, "BuffLen:{0} Offset:{1} Read:{2}", buffer.Length, offset, bytesRead);                    
                }
            }
            LogFSActionSuccess("ReadFile", fileName, (SftpContext)info.Context, "");
#if DEBUG && DEBUGSHADOWCOPY
            try {
                string shadowCopyDir = Environment.CurrentDirectory + "\\debug-shadow";
                string tmpFilePath = shadowCopyDir + "\\" + fileName.Replace("/", "\\");
                FileStream fs = File.OpenRead(tmpFilePath);
                byte[] localDataShadowBuffer = new byte[buffer.Length];
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Close();
                int readedShadow = fs.Read(localDataShadowBuffer, 0, localDataShadowBuffer.Length);
                if (readedShadow != bytesRead)
                {
                    throw new Exception("Length of readed data from "+fileName+" differs");
                }
                if (!localDataShadowBuffer.SequenceEqual(buffer))
                {
                    throw new Exception("Data readed from " + fileName + " differs");
                }
            }
            catch (Exception)
            {

            }
#endif
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset,
                                              DokanFileInfo info)
        {
            bytesWritten = 0;
            LogFSActionInit("WriteFile", fileName, (SftpContext)info.Context, "Ofs:{0} Len:{1}", offset, buffer.Length);
            try {
#if DEBUG && DEBUGSHADOWCOPY
                string shadowCopyDir = Environment.CurrentDirectory + "\\debug-shadow";
                string tmpFilePath = shadowCopyDir + "\\" + fileName.Replace("/","\\");
                if (!Directory.Exists(tmpFilePath))
                {
                    Directory.CreateDirectory(Directory.GetParent(tmpFilePath).FullName);
                }
                FileStream tmpFile = File.OpenWrite(tmpFilePath);
                if (tmpFile.Length < offset + buffer.Length)
                {
                    tmpFile.SetLength(offset + buffer.Length);
                }
                tmpFile.Seek(offset, SeekOrigin.Begin);
                tmpFile.Write(buffer, 0, buffer.Length);
                tmpFile.Close();
#endif

                if (info.Context == null) // who would guess
                {
                    SftpFileStream handle = Open(GetUnixPath(fileName), FileMode.Create);
                    handle.Write(buffer, 0, buffer.Length);
                    handle.Close();
                    bytesWritten = buffer.Length;
                    LogFSActionOther("WriteFile", fileName, (SftpContext)info.Context, "NOCONTEXT Ofs:{1} Len:{0} Written:{2}", buffer.Length, offset, bytesWritten);
                }
                else
                {

                    SftpContextStream stream = (info.Context as SftpContext).Stream;
                    lock (stream)
                    {
                        stream.Position = offset;
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    stream.Flush();
                    bytesWritten = buffer.Length;
                    // TODO there are still some apps that don't check disk free space before write
                }
            }
            catch(Exception)
            {
                return NtStatus.Error;
            }
              
            LogFSActionSuccess("WriteFile", fileName, (SftpContext)info.Context, "Ofs:{1} Len:{0} Written:{2}", buffer.Length, offset, bytesWritten);
            return NtStatus.Success;
        }
        

        NtStatus IDokanOperations.FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            LogFSActionInit("FlushFile", fileName, (SftpContext)info.Context,"");

            (info.Context as SftpContext).Stream.Flush(); //git use this

            CacheReset(GetUnixPath(fileName));

            LogFSActionSuccess("FlushFile", fileName, (SftpContext)info.Context, "");
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.GetFileInformation(string fileName, out FileInformation fileInfo,
                                                       DokanFileInfo info)
        {
            LogFSActionInit("FileInfo", fileName, (SftpContext)info.Context, "");

            var context = info.Context as SftpContext;

            SftpFileAttributes sftpFileAttributes;
            string path = GetUnixPath(fileName);
            
            if (context != null)
            {
                /*
                 * Attributtes in streams are causing trouble with git. GetInfo returns wrong length if other context is writing.
                 */
                if (context.Stream != null)
                    try
                    {
                        sftpFileAttributes = GetAttributes(path);
                    }
                    catch (SftpPathNotFoundException)
                    {
                        Debug.WriteLine("File not found");
                        sftpFileAttributes = null;
                    }
                else
                    sftpFileAttributes = context.Attributes;
            }
            else
            {
                sftpFileAttributes = CacheGetAttr(path);

                if (sftpFileAttributes == null)
                {
                    try
                    {
                        sftpFileAttributes = GetAttributes(path);
                    }
                    catch (SftpPathNotFoundException)
                    {
                        Debug.WriteLine("File not found");
                        sftpFileAttributes = null;
                    }
                    if (sftpFileAttributes != null)
                        CacheAddAttr(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                }
            }
            if (sftpFileAttributes == null)
            {
                LogFSActionError("FileInfo", fileName, (SftpContext)info.Context, "No such file - unable to get info");
                fileInfo = new FileInformation();
                return NtStatus.NoSuchFile;

            }


            fileInfo = new FileInformation
            {
                               FileName = Path.GetFileName(fileName), //String.Empty,
                               // GetInfo info doesn't use it maybe for sorting .
                               CreationTime = sftpFileAttributes.LastWriteTime,
                               LastAccessTime = sftpFileAttributes.LastAccessTime,
                               LastWriteTime = sftpFileAttributes.LastWriteTime,
                               Length = sftpFileAttributes.Size
                           };
            if (sftpFileAttributes.IsDirectory || sftpFileAttributes.IsSymbolicLinkToDirectory)
            {
                fileInfo.Attributes |= FileAttributes.Directory;
                fileInfo.Length = 0; // Windows directories use length of 0 
            }
            if (fileName.Length != 1 && fileName[fileName.LastIndexOf('\\') + 1] == '.')
                //aditional check if filename isn't \\
            {
                fileInfo.Attributes |= FileAttributes.Hidden;
            }

            /*if (GroupRightsSameAsOwner(sftpFileAttributes))
            {
                fileInfo.Attributes |= FileAttributes.Archive;
            }*/
            if (_useOfflineAttribute)
            {
                fileInfo.Attributes |= FileAttributes.Offline;
            }

            if (!this.UserCanWrite(sftpFileAttributes))
            {
                fileInfo.Attributes |= FileAttributes.ReadOnly;
            }

            if (fileInfo.Attributes == 0)
            {
                fileInfo.Attributes = FileAttributes.Normal;//can be only alone
            }

            LogFSActionSuccess("FileInfo", fileName, (SftpContext)info.Context, "Length:{0} Attrs:{1}", fileInfo.Length, fileInfo.Attributes);

            return NtStatus.Success;
        }

        NtStatus IDokanOperations.FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info)
        {
            files = null;
            return NtStatus.NotImplemented;
        }

        NtStatus IDokanOperations.FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            //Log("FindFiles:{0}", fileName);
            LogFSActionInit("FindFiles", fileName, (SftpContext)info.Context, "");

            //byte[] handle;
            List<SftpFile> sftpFiles;

            try
            {
                sftpFiles = ListDirectory(GetUnixPath(fileName)).ToList();
                //handle = _sshClient.RequestOpenDir(GetUnixPath(fileName));
            }
            catch (SftpPermissionDeniedException)
            {
                files = null;
                return NtStatus.AccessDenied;
            }


            files = new List<FileInformation>();

            (files as List<FileInformation>).AddRange(sftpFiles.Select(
                file =>
                    {
                        var sftpFileAttributes = this.ExtendSFtpFileAttributes(file.FullName, file.Attributes);

                        var fileInformation = new FileInformation
                                                    {
                                                        Attributes =
                                                            FileAttributes.NotContentIndexed,
                                                        CreationTime
                                                            =
                                                            sftpFileAttributes
                                                            .
                                                            LastWriteTime,
                                                        FileName
                                                            =
                                                            file.Name
                                                        ,
                                                        LastAccessTime
                                                            =
                                                            sftpFileAttributes
                                                            .
                                                            LastAccessTime,
                                                        LastWriteTime
                                                            =
                                                            sftpFileAttributes
                                                            .
                                                            LastWriteTime,
                                                        Length
                                                            =
                                                            sftpFileAttributes
                                                            .
                                                            Size
                                                    };
                        if (sftpFileAttributes.IsSymbolicLink)
                        {
                            /* Also files must be marked as dir to reparse work on files */
                            fileInformation.Attributes |= FileAttributes.ReparsePoint | FileAttributes.Directory;
                        }

                        if (sftpFileAttributes.IsSocket)
                        {
                            fileInformation.Attributes
                                |=
                                FileAttributes.NoScrubData | FileAttributes.System | FileAttributes.Device;
                        }else if (sftpFileAttributes.IsDirectory || sftpFileAttributes.IsSymbolicLinkToDirectory)
                        {
                            fileInformation.Attributes
                                |=
                                FileAttributes.
                                    Directory;
                            fileInformation.Length = 4096;//test
                        }
                        else
                        {
                            fileInformation.Attributes |= FileAttributes.Normal;
                        }

                        if (file.Name[0] == '.')
                        {
                            fileInformation.Attributes
                                |=
                                FileAttributes.
                                    Hidden;
                        }

                        if (GroupRightsSameAsOwner(sftpFileAttributes))
                        {
                            fileInformation.Attributes |= FileAttributes.Archive;
                        }
                        if (!this.UserCanWrite(sftpFileAttributes))
                        {
                            fileInformation.Attributes |= FileAttributes.ReadOnly;
                        }
                        if (_useOfflineAttribute)
                        {
                            fileInformation.Attributes
                                |=
                                FileAttributes.
                                    Offline;
                        }
                        return fileInformation;
                    }));



            int timeout = Math.Max(_attributeCacheTimeout + 2, _attributeCacheTimeout +  sftpFiles.Count / 10);

            foreach (
                var file in
                    sftpFiles/*.Where(
                        pair => !pair.IsSymbolicLink)*/)
            {
                CacheAddAttr(GetUnixPath(String.Format("{0}\\{1}", fileName , file.Name)), file.Attributes,
                            DateTimeOffset.UtcNow.AddSeconds(timeout));
            }

            try
            {
                CacheAddDir( GetUnixPath(fileName), new Tuple<DateTime, IList<FileInformation>>(
                                         (info.Context as SftpContext).Attributes.LastWriteTime,
                                         files),
                           DateTimeOffset.UtcNow.AddSeconds(Math.Max(_attributeCacheTimeout,
                                                                     Math.Min(files.Count, _directoryCacheTimeout))));
            }
            catch
            {
            }
            LogFSActionSuccess("FindFiles", fileName, (SftpContext)info.Context, "Count:{0}", files.Count);
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            LogFSActionError("SetFileAttr", fileName, (SftpContext)info.Context, "Attrs:{0}", attributes);

            //get actual attributes
            string path = GetUnixPath(fileName);
            SftpFileAttributes currentattr;
            try
            {
                currentattr = GetAttributes(path);
            }
            catch (SftpPathNotFoundException)
            {
                Debug.WriteLine("File not found");
                currentattr = null;
            }

            //rules for changes:
            bool rightsupdate = false;
                if (attributes.HasFlag(FileAttributes.Archive) && !GroupRightsSameAsOwner(currentattr))
                {
                    LogFSActionSuccess("SetFileAttr", fileName, (SftpContext)info.Context, "Setting group rights to owner");
                    //Archive goes ON, rights of group same as owner:
                    currentattr.GroupCanWrite = currentattr.OwnerCanWrite;
                    currentattr.GroupCanExecute = currentattr.OwnerCanExecute;
                    currentattr.GroupCanRead = currentattr.OwnerCanRead;
                    rightsupdate = true;
                }
                if (!attributes.HasFlag(FileAttributes.Archive) && GroupRightsSameAsOwner(currentattr))
                {
                    LogFSActionSuccess("SetFileAttr", fileName, (SftpContext)info.Context, "Setting group rights to others");
                    //Archive goes OFF, rights of group same as others:
                    currentattr.GroupCanWrite = currentattr.OthersCanWrite;
                    currentattr.GroupCanExecute = currentattr.OthersCanExecute;
                    currentattr.GroupCanRead = currentattr.OthersCanRead;
                    rightsupdate = true;
                }


            //apply new settings:
            if (rightsupdate)
            {
                //apply and reset cache
                try
                {
                    SetAttributes(GetUnixPath(fileName), currentattr);
                }
                catch(SftpPermissionDeniedException)
                {
                    return NtStatus.AccessDenied;
                }
                CacheReset(path);
                CacheResetParent(path); //parent cache need reset also
                
                //if context exists, update new rights manually is needed
                SftpContext context = (SftpContext)info.Context;
                if (info.Context != null)
                {
                    context.Attributes.GroupCanWrite = currentattr.GroupCanWrite;
                    context.Attributes.GroupCanExecute = currentattr.GroupCanExecute;
                    context.Attributes.GroupCanRead = currentattr.GroupCanRead;
                }
            }

            return NtStatus.Success;
        }

        NtStatus IDokanOperations.SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
                                                DateTime? lastWriteTime, DokanFileInfo info)
        {
            //Log("TrySetFileTime:{0}\n|c:{1}\n|a:{2}\n|w:{3}", filename, creationTime, lastAccessTime,lastWriteTime);
            LogFSActionInit("SetFileTime", fileName, (SftpContext)info.Context, "");

            var sftpattributes = (info.Context as SftpContext).Attributes;
            SftpFileAttributes tempAttributes;
            try
            {
                tempAttributes = GetAttributes(GetUnixPath(fileName));
            }
            catch (SftpPathNotFoundException)
            {
                Debug.WriteLine("File not found");
                tempAttributes = null;
            }

            tempAttributes.LastWriteTime = lastWriteTime ?? (creationTime ?? sftpattributes.LastWriteTime);
            tempAttributes.LastAccessTime = lastAccessTime ?? sftpattributes.LastAccessTime;

            SetAttributes(GetUnixPath(fileName), tempAttributes);

            LogFSActionSuccess("SetFileTime", fileName, (SftpContext)info.Context, "");
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.DeleteFile(string fileName, DokanFileInfo info)
        {
            //Log("DeleteFile:{0}", fileName);
            LogFSActionInit("DeleteFile", fileName, (SftpContext)info.Context, "");

            string parentPath = GetUnixPath(fileName.Substring(0, fileName.LastIndexOf('\\')));

            var sftpFileAttributes = CacheGetAttr(parentPath);

            if (sftpFileAttributes == null)
            {

                try
                {
                    sftpFileAttributes = GetAttributes(parentPath);
                }
                catch (SftpPathNotFoundException)
                {
                    Debug.WriteLine("File not found");
                    sftpFileAttributes = null;
                }
                if (sftpFileAttributes != null)
                    //_cache.Add(parentPath, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                    CacheAddAttr(parentPath, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
            }
            /* shoud be tested, but performance...
            if (IsDirectory)
            {
                return NtStatus.AccessDenied;
            }*/

            LogFSActionSuccess("DeleteFile", fileName, (SftpContext)info.Context, "Success:{0}", UserCanWrite(sftpFileAttributes));
            return
                UserCanWrite(
                    sftpFileAttributes)
                    ? NtStatus.Success
                    : NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.DeleteDirectory(string fileName, DokanFileInfo info)
        {
            //Log("DeleteDirectory:{0}", fileName);
            LogFSActionSuccess("DeleteDir", fileName, (SftpContext)info.Context, "");


            string parentPath = GetUnixPath(fileName.Substring(0, fileName.LastIndexOf('\\')));

            var sftpFileAttributes = CacheGetAttr(parentPath);

            if (sftpFileAttributes == null)
            {

                try
                {
                    sftpFileAttributes = GetAttributes(parentPath);
                }
                catch (SftpPathNotFoundException)
                {
                    Debug.WriteLine("File not found");
                    sftpFileAttributes = null;
                }
                if (sftpFileAttributes != null)
                    CacheAddAttr(parentPath, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
            }


            if (
                !UserCanWrite(
                    sftpFileAttributes))
            {
                LogFSActionError("DeleteDir", fileName, (SftpContext)info.Context, "Access denied");
                return NtStatus.AccessDenied;
            }

            var fileNameUnix = GetUnixPath(fileName);
            sftpFileAttributes = this.CacheGetAttr(fileNameUnix);
            if (sftpFileAttributes == null)
            {
                try
                {
                    sftpFileAttributes = GetAttributes(fileNameUnix);
                }
                catch (SftpPathNotFoundException)
                {
                    return NtStatus.NoSuchFile;//not sure if can happen and what to return
                }
            }
            if (sftpFileAttributes.IsSymbolicLink)
            {
                return NtStatus.Success;
            }

            //test content:
            var dircache = CacheGetDir(GetUnixPath(fileName));
            if (dircache != null)
            {
                bool test = dircache.Item2.Count == 0 || dircache.Item2.All(i => i.FileName == "." || i.FileName == "..");
                if (!test)
                {
                    LogFSActionError("DeleteDir", fileName, (SftpContext)info.Context, "Dir not empty");
                    return NtStatus.DirectoryNotEmpty;
                }
                LogFSActionSuccess("DeleteDir", fileName, (SftpContext)info.Context, "");
                return NtStatus.Success;
            }

            //no cache hit, test live, maybe we will get why:
            var dir = ListDirectory(GetUnixPath(fileName)).ToList();
            if (dir == null)
            {
                LogFSActionError("DeleteDir", fileName, (SftpContext)info.Context, "Open failed, access denied?");
                return NtStatus.AccessDenied;
            }

            bool test2 = dir.Count == 0 || dir.All(i => i.Name == "." || i.Name == "..");
            if (!test2)
            {
                LogFSActionError("DeleteDir", fileName, (SftpContext)info.Context, "Dir not empty");
                return NtStatus.DirectoryNotEmpty;
            }

            LogFSActionSuccess("DeleteDir", fileName, (SftpContext)info.Context, "");
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            LogFSActionInit("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Replace:{1}",newName, replace);



            string oldpath = GetUnixPath(oldName);
            string newpath = GetUnixPath(newName);
            SftpFileAttributes sftpFileAttributes;
            try
            {
                sftpFileAttributes = GetAttributes(newpath);
            }
            catch (SftpPathNotFoundException)
            {
                Debug.WriteLine("File not found");
                sftpFileAttributes = null;
            }
            if (sftpFileAttributes == null)
            {
                (info.Context as SftpContext).Release();

                info.Context = null;
                try
                {
                    RenameFile(oldpath, newpath, false);
                    CacheResetParent(oldpath);
                    CacheResetParent(newpath);
                    CacheReset(oldpath);
#if DEBUG && DEBUGSHADOWCOPY
                    try
                    {
                        string shadowCopyDir = Environment.CurrentDirectory + "\\debug-shadow";
                        string tmpFilePath = shadowCopyDir + "\\" + oldName.Replace("/", "\\");
                        string tmpFilePath2 = shadowCopyDir + "\\" + newName.Replace("/", "\\");
                        Directory.CreateDirectory(Directory.GetParent(tmpFilePath2).FullName);
                        if (Directory.Exists(tmpFilePath))
                        {
                            Directory.Move(tmpFilePath, tmpFilePath2);
                        }
                        else
                        {
                            File.Move(tmpFilePath, tmpFilePath2);
                        }
                    }
                    catch (Exception e)
                    {

                    }
#endif
                }
                catch (SftpPermissionDeniedException)
                {
                    LogFSActionError("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Access denied", newName);
                    return NtStatus.AccessDenied;
                }
                LogFSActionSuccess("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Target didnt exists", newName);
                return NtStatus.Success;
            }
            else if (replace)
            {
                (info.Context as SftpContext).Release();

                info.Context = null;

                if (sftpFileAttributes.IsDirectory || sftpFileAttributes.IsSymbolicLinkToDirectory)
                {
                    return NtStatus.AccessDenied;
                }

                try
                {
                    try
                    {
                        RenameFile(oldpath, newpath, true);
                    }
                    catch (NotSupportedException)
                    {
                        if (!info.IsDirectory)
                            DeleteFile(newpath);
                        RenameFile(oldpath, newpath, false);
                    }
                    catch (SftpPathNotFoundException)
                    {
                        return NtStatus.AccessDenied;
                    }

                    CacheReset(oldpath);
                    CacheResetParent(oldpath);
                    CacheResetParent(newpath);
#if DEBUG && DEBUGSHADOWCOPY
                    try
                    {
                        string shadowCopyDir = Environment.CurrentDirectory + "\\debug-shadow";
                        string tmpFilePath = shadowCopyDir + "\\" + oldName.Replace("/", "\\");
                        string tmpFilePath2 = shadowCopyDir + "\\" + newName.Replace("/", "\\");
                        Directory.CreateDirectory(Directory.GetParent(tmpFilePath2).FullName);
                        if (Directory.Exists(tmpFilePath))
                        {
                            Directory.Move(tmpFilePath, tmpFilePath2);
                        }
                        else
                        {
                            File.Move(tmpFilePath, tmpFilePath2);
                        }
                    }
                    catch (Exception e)
                    {

                    }
#endif
                }

                catch (SftpPermissionDeniedException)
                {
                    LogFSActionError("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Access denied", newName);
                    return NtStatus.AccessDenied;
                } // not tested on sftp3

                LogFSActionSuccess("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Target was replaced", newName);
                return NtStatus.Success;
            }
            LogFSActionError("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Target already exists", newName);
            return NtStatus.ObjectNameCollision;
        }

        NtStatus IDokanOperations.SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            //Log("SetEnd");
            LogFSActionInit("SetEndOfFile", fileName, (SftpContext)info.Context, "Length:{0}", length);
            (info.Context as SftpContext).Stream.SetLength(length);
            CacheResetParent(GetUnixPath(fileName));
            LogFSActionSuccess("SetEndOfFile", fileName, (SftpContext)info.Context, "Length:{0}", length);
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            //Log("SetSize");
            LogFSActionInit("SetAllocSize", fileName, (SftpContext)info.Context, "Length:{0}", length);
            (info.Context as SftpContext).Stream.SetLength(length);
            CacheResetParent(GetUnixPath(fileName));
            LogFSActionSuccess("SetAllocSize", fileName, (SftpContext)info.Context, "Length:{0}", length);
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            LogFSActionError("LockFile", fileName, (SftpContext)info.Context, "NI");
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            LogFSActionError("UnlockFile", fileName, (SftpContext)info.Context, "NI");
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.GetDiskFreeSpace(out long free, out long total,
                                                     out long used, DokanFileInfo info)
        {
            //Log("GetDiskFreeSpace");
            LogFSActionInit("GetDiskFreeSpace", this._volumeLabel, (SftpContext)info.Context, "");

            
            Log("GetDiskFreeSpace");

            var diskSpaceInfo = CacheGetDiskInfo();

            bool dfCheck = false;

            if (diskSpaceInfo != null)
            {
                free = diskSpaceInfo.Item1;
                total = diskSpaceInfo.Item2;
                used = diskSpaceInfo.Item3;
            }
            else
            {
                total = 0x1900000000; //100 GiB
                used = 0xc80000000; // 50 Gib
                free = 0xc80000000;
                try
                {
                    var information = GetStatus(_rootpath);
                    total = (long)(information.TotalBlocks * information.BlockSize);
                    free = (long)(information.FreeBlocks * information.BlockSize);
                    used = (long)(information.AvailableBlocks * information.BlockSize);
                }
                catch (NotSupportedException)
                {
                    dfCheck = true;
                }
                catch (SshException)
                {
                    dfCheck = true;
                }
                if(dfCheck)
                {
                    using (var cmd = _sshClient.CreateCommand(String.Format(_dfCommand + " -Pk  {0}", _rootpath), Encoding.UTF8))
                    // POSIX standard df
                    {
                        cmd.Execute();
                        if (cmd.ExitStatus == 0)
                        {
                            var values = cmd.Result.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            total = Int64.Parse(values[values.Length - 5]) << 10;
                            used = Int64.Parse(values[values.Length - 4]) << 10;
                            free = Int64.Parse(values[values.Length - 3]) << 10; //<======maybe to cache all this
                        }
                    }
                }

                CacheAddDiskInfo(new Tuple<long, long, long>(free, total, used),
                        DateTimeOffset.UtcNow.AddMinutes(3));
            }
            LogFSActionSuccess("GetDiskFreeSpace", this._volumeLabel, (SftpContext)info.Context, "Free:{0} Total:{1} Used:{2}", free, total, used);
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
                                                         out string filesystemName, DokanFileInfo info)
        {
            LogFSActionInit("GetVolumeInformation", this._volumeLabel, (SftpContext)info.Context, "");

            volumeLabel = _volumeLabel;

            filesystemName = "SSHFS";

            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch |
                       FileSystemFeatures.SupportsRemoteStorage | FileSystemFeatures.UnicodeOnDisk | FileSystemFeatures.SequentialWriteOnce;
            //FileSystemFeatures.PersistentAcls

            LogFSActionSuccess("GetVolumeInformation", this._volumeLabel, (SftpContext)info.Context, "FS:{0} Features:{1}", filesystemName, features);
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.GetFileSecurity(string filename, out FileSystemSecurity security,
                                                    AccessControlSections sections, DokanFileInfo info)
        {
            LogFSActionInit("GetFileSecurity", filename, (SftpContext)info.Context, "Sections:{0}",sections);


            var sftpattributes = (info.Context as SftpContext).Attributes;
            var rights = FileSystemRights.ReadPermissions | FileSystemRights.ReadExtendedAttributes |
                         FileSystemRights.ReadAttributes | FileSystemRights.Synchronize;


            if (UserCanRead(sftpattributes))
            {
                rights |= FileSystemRights.ReadData;
            }
            if (UserCanWrite(sftpattributes))
            {
                rights |= FileSystemRights.Write;
            }
            if (UserCanExecute(sftpattributes) && info.IsDirectory)
            {
                rights |= FileSystemRights.Traverse;
            }
            security = info.IsDirectory ? new DirectorySecurity() as FileSystemSecurity : new FileSecurity();
            security.AddAccessRule(new FileSystemAccessRule("Everyone", rights, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl ^ rights,
                                                            AccessControlType.Deny));
            //not sure this works at all, needs testing
            // if (sections.HasFlag(AccessControlSections.Owner))
            //security.SetOwner(new NTAccount("None"));
            // if (sections.HasFlag(AccessControlSections.Group))
            security.SetGroup(new NTAccount("None"));

            LogFSActionSuccess("GetFileSecurity", filename, (SftpContext)info.Context, "Sections:{0} Rights:{1}", sections, rights);
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.SetFileSecurity(string filename, FileSystemSecurity security,
                                                    AccessControlSections sections, DokanFileInfo info)
        {
            LogFSActionError("SetFileSecurity", filename, (SftpContext)info.Context, "NI");

            return NtStatus.AccessDenied;
        }

        NtStatus IDokanOperations.Unmounted(DokanFileInfo info)
        {
            LogFSActionError("Unmounted", this._volumeLabel, (SftpContext)info.Context, "NI");
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.Mounted(DokanFileInfo info)
        {
            LogFSActionError("Mounted", this._volumeLabel, (SftpContext)info.Context, "NI");
            return NtStatus.Success;
        }

        NtStatus IDokanOperations.FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            //Alternate Data Streams are NFTS-only feature, no need to handle
            streams = new FileInformation[0];
            return NtStatus.NotImplemented;
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs> Disconnected
        {
            add { Session.Disconnected += value; }
            remove { Session.Disconnected -= value; }
        }

        #endregion
    }
}