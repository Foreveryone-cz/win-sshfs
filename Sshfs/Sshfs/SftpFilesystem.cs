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


namespace Sshfs
{
    internal sealed class SftpFilesystem : SftpClient, IDokanOperations
    {
        
        #region Constants

        // ReSharper disable InconsistentNaming
        //  private static readonly string[] _filter = {
        //       "desktop.ini", "Desktop.ini", "autorun.inf",
        //    "AutoRun.inf", //"Thumbs.db",
        // };

        // private static readonly Regex _dfregex = new Regex(@"^[a-z0-9/]+\s+(?<blocks>[0-9]+)K\s+(?<used>[0-9]+)K"
        // , RegexOptions.Compiled);

        // ReSharper restore InconsistentNaming 

        #endregion

        #region Fields

        private readonly MemoryCache _cache = MemoryCache.Default;

        private SshClient _sshClient;
        private readonly TimeSpan _operationTimeout = TimeSpan.FromSeconds(30);//new TimeSpan(0, 0, 0, 0, -1);
        private string _rootpath;

        private readonly bool _useOfflineAttribute;
        private readonly bool _debugMode;


        private int _userId;
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

            _sshClient = new SshClient(ConnectionInfo); //new (Session, _operationTimeout, Encoding.UTF8);

            this.Log("Connected %s", _volumeLabel);
            _sshClient.Connect();
            

            _userId = GetUserId();
            if (_userId != -1)
                _userGroups = new HashSet<int>(GetUserGroupsIds());


            if (String.IsNullOrWhiteSpace(_rootpath))
            {
                _rootpath = this.WorkingDirectory; //_sshClient.RequestRealPath(".").First().Key;
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
                //_cache.Remove(index != 0 ? fileName.Substring(0, index) : "\\");
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
            // return String.Concat(_rootpath, path.Replace('\\', '/'));
            return String.Format("{0}{1}", _rootpath, path.Replace('\\', '/').Replace("//","/"));
        }

        private IEnumerable<int> GetUserGroupsIds()
        {
            using (var cmd = _sshClient.CreateCommand("id -G ", Encoding.UTF8))
            {
                cmd.Execute();
                return cmd.Result.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse);
            }
        }

        private int GetUserId()
        {
            using (var cmd = _sshClient.CreateCommand("id -u ", Encoding.UTF8))
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

        //private SftpFileAttributes GetAttributes(string path)
        //{
        //    var sftpLStatAttributes = _sshClient.RequestLStat(path);
        //    if (sftpLStatAttributes == null || !sftpLStatAttributes.IsSymbolicLink)
        //    {
        //        return sftpLStatAttributes;
        //    }
        //    var sftpStatAttributes = _sshClient.RequestStat(path, true);
        //    return sftpStatAttributes ?? sftpLStatAttributes;
        //}

        #endregion

        #region DokanOperations

        DokanError IDokanOperations.CreateFile(string fileName, FileAccess access, FileShare share,
                                               FileMode mode, FileOptions options,
                                               FileAttributes attributes, DokanFileInfo info)
        {
            if (fileName.EndsWith("desktop.ini", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith("autorun.inf", StringComparison.OrdinalIgnoreCase)) //....
            {
                return DokanError.ErrorFileNotFound;
            }

            LogFSActionInit("OpenFile", fileName, (SftpContext)info.Context, "Mode:{0} Options:{1}", mode,options);
            

            string path = GetUnixPath(fileName);
            //  var  sftpFileAttributes = GetAttributes(path);
            //var sftpFileAttributes = _cache.Get(path) as SftpFileAttributes;
            var sftpFileAttributes = this.CacheGetAttr(path);

            if (sftpFileAttributes == null)
            {
                //Log("cache miss");
                try {
                    sftpFileAttributes = GetAttributes(path);
                }
                catch(SftpPathNotFoundException e)
                {
                    Debug.WriteLine("File not found");
                }

                if (sftpFileAttributes != null)
                    //_cache.Add(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                    CacheAddAttr(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                else
                {
                    LogFSActionOther("OpenFile", fileName, (SftpContext)info.Context, "get attributes failed");
                }
            }
            /*Log("Open| Name:{0},\n Mode:{1},\n Share{2},\n Disp:{3},\n Flags{4},\n Attr:{5},\nPagingIO:{6} NoCache:{7} SynIO:{8}\n", fileName, access,
                share, mode, options, attributes, info.PagingIo, info.NoCache, info.SynchronousIo);*/

            switch (mode)
            {
                case FileMode.Open:
                    if (sftpFileAttributes != null)
                    {
                        if (((uint)access & 0xe0000027) == 0 || sftpFileAttributes.IsDirectory)
                        //check if only wants to read attributes,security info or open directory
                        {
                            //Log("JustInfo:{0},{1}", fileName, sftpFileAttributes.IsDirectory);
                            info.IsDirectory = sftpFileAttributes.IsDirectory;
                            
                            if (options.HasFlag(FileOptions.DeleteOnClose))
                            {
                                return DokanError.ErrorError;//this will result in calling DeleteFile in Windows Explorer
                            }
                            info.Context = new SftpContext(sftpFileAttributes, false);

                            LogFSActionOther("OpenFile", fileName, (SftpContext)info.Context, "Dir open or get attrs");
                            return DokanError.ErrorSuccess;
                        }
                    }
                    else
                    {
                        LogFSActionError("OpenFile", fileName, (SftpContext)info.Context, "File not found");
                        return DokanError.ErrorFileNotFound;
                    }
                    break;
                case FileMode.CreateNew:
                    if (sftpFileAttributes != null)
                        return DokanError.ErrorAlreadyExists;

                    CacheResetParent(path);
                    break;
                case FileMode.Truncate:
                    if (sftpFileAttributes == null)
                        return DokanError.ErrorFileNotFound;
                    CacheResetParent(path);
                    //_cache.Remove(path);
                    this.CacheReset(path);
                    break;
                default:

                    CacheResetParent(path);
                    break;
            }
            //Log("NotJustInfo:{0}-{1}", info.Context, mode);
            try
            {
                info.Context = new SftpContext(this, path, mode,
                                               ((ulong) access & 0x40010006) == 0
                                                   ? System.IO.FileAccess.Read
                                                   : System.IO.FileAccess.ReadWrite, sftpFileAttributes);
            }
            catch (SshException ex) // Don't have access rights or try to read broken symlink
            {
                var ownerpath = path.Substring(0, path.LastIndexOf('/'));
                //var sftpPathAttributes = _cache.Get(ownerpath) as SftpFileAttributes;
                var sftpPathAttributes = CacheGetAttr(ownerpath);

                if (sftpPathAttributes == null)
                {
                    //Log("cache miss");

                    sftpPathAttributes = GetAttributes(ownerpath);
                    if (sftpPathAttributes != null)
                        //_cache.Add(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                        CacheAddAttr(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                    else
                    {
                        //Log("Up directory must be created");
                        LogFSActionError("OpenFile", fileName, (SftpContext)info.Context, "Up directory mising:{0}", ownerpath);
                        return DokanError.ErrorPathNotFound;
                    }
                }
                LogFSActionError("OpenFile", fileName, (SftpContext)info.Context, "Access denied");
                return DokanError.ErrorAccessDenied;
            }

            LogFSActionSuccess("OpenFile", fileName, (SftpContext)info.Context, "Mode:{0}", mode);
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.OpenDirectory(string fileName, DokanFileInfo info)
        {
            //Log("OpenDir:{0}", fileName);
            LogFSActionInit("OpenDir", fileName, (SftpContext)info.Context,"");




            string path = GetUnixPath(fileName);
            // var sftpFileAttributes = GetAttributes(GetUnixPath(fileName));
            //var sftpFileAttributes = _cache.Get(path) as SftpFileAttributes;
            var sftpFileAttributes = CacheGetAttr(path);

            if (sftpFileAttributes == null)
            {
                //Log("cache miss");
               
                sftpFileAttributes = GetAttributes(path);
                if (sftpFileAttributes != null)
                    //_cache.Add(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                    CacheAddAttr(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
            }
            
         


            if (sftpFileAttributes != null && sftpFileAttributes.IsDirectory)
            {
                //???
                if (!UserCanExecute(sftpFileAttributes) || !UserCanRead(sftpFileAttributes))
                {
                    return DokanError.ErrorAccessDenied;
                }


                info.IsDirectory = true;
                info.Context = new SftpContext(sftpFileAttributes);

                //var dircahe = _cache.Get(fileName) as Tuple<DateTime, IList<FileInformation>>;
                var dircahe = CacheGetDir(path);
                if (dircahe != null && dircahe.Item1 != sftpFileAttributes.LastWriteTime)
                {
                    //_cache.Remove(fileName);
                    CacheReset(path);
                }
                LogFSActionSuccess("OpenDir", fileName, (SftpContext)info.Context,"");
                return DokanError.ErrorSuccess;
            }
            LogFSActionError("OpenDir", fileName, (SftpContext)info.Context,"Path not found");
            return DokanError.ErrorPathNotFound;
        }

        DokanError IDokanOperations.CreateDirectory(string fileName, DokanFileInfo info)
        {
            //Log("CreateDir:{0}", fileName);
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
                return DokanError.ErrorAccessDenied;
            }
            catch (SshException) // operation should fail with generic error if file already exists
            {
                LogFSActionError("OpenDir", fileName, (SftpContext)info.Context, "Already exists");
                return DokanError.ErrorAlreadyExists;
            }
            LogFSActionSuccess("OpenDir", fileName, (SftpContext)info.Context,"");
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.Cleanup(string fileName, DokanFileInfo info)
        {
            //Log("Cleanup:{0},Delete:{1}", info.Context,info.DeleteOnClose);
            LogFSActionInit("Cleanup", fileName, (SftpContext)info.Context, "");

            bool deleteOnCloseWorkAround = false;

            if (info.Context != null)
            {
                deleteOnCloseWorkAround = ((SftpContext)info.Context).deleteOnCloseWorkaround;

                (info.Context as SftpContext).Release();

                info.Context = null;
            }

            if (info.DeleteOnClose || deleteOnCloseWorkAround)
            {
                string path = GetUnixPath(fileName);
                if (info.IsDirectory)
                {
                    try
                    {
                        DeleteDirectory(path);
                    }
                    catch (SftpPathNotFoundException) //in case we are dealing with simbolic link
                    {
                        //This may cause an error
                        DeleteFile(path);
                    }
                }
                else
                {
                    DeleteFile(path);
                }
                CacheReset(path);
                CacheResetParent(path);
            }

            LogFSActionSuccess("Cleanup", fileName, (SftpContext)info.Context, "");
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.CloseFile(string fileName, DokanFileInfo info)
        {
            //Log("Close:{0}", info.Context);
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


            /* cache reset for dir close is not good idea, will read it verz soon probablz again,          */
            if (!info.IsDirectory)
            {
                CacheReset(GetUnixPath(fileName));
            }
            

            return DokanError.ErrorSuccess;
        }


        DokanError IDokanOperations.ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset,
                                             DokanFileInfo info)
        {
            //Log("ReadFile:{0}:{1}|lenght:[{2}]|offset:[{3}]", fileName,info.Context , buffer.Length, offset);
            LogFSActionInit("ReadFile", fileName, (SftpContext)info.Context, "BuffLen:{0} Offset:{1}", buffer.Length, offset);

            if (info.Context == null)
            {
                //called when file is read as memory memory mapeded file usualy notepad and stuff
                SftpFileStream handle = Open(GetUnixPath(fileName), FileMode.Open);
                if (offset == 0)
                    handle.Seek(offset, SeekOrigin.Begin);
                else
                    handle.Seek(offset, SeekOrigin.Current);
                //var handle = _sshClient.RequestOpen(GetUnixPath(fileName), Flags.Read);
                //var data = new Array();
                bytesRead = handle.Read(buffer, 0, buffer.Length);
                //var data = _sshClient.RequestRead(handle, (ulong) offset, (uint) buffer.Length);
                //_sshClient.RequestClose(handle);
                handle.Close();
                handle = null;
                //Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
                LogFSActionOther("ReadFile", fileName, (SftpContext)info.Context, "NOCONTEXT BuffLen:{0} Offset:{1} Read:{2}", buffer.Length, offset,bytesRead);
            }
            else
            {
                // var watch = Stopwatch.StartNew();
                SftpFileStream stream = (info.Context as SftpContext).Stream;
                lock (stream)
                {
                    stream.Position = offset;
                    bytesRead = stream.Read(buffer, 0, buffer.Length);

                    LogFSActionOther("ReadFile", fileName, (SftpContext)info.Context, "BuffLen:{0} Offset:{1} Read:{2}", buffer.Length, offset, bytesRead);                    
                }
                //  watch.Stop();
                // Log("{0}",watch.ElapsedMilliseconds);
            }
            //Log("END READ:{0},{1}",offset,info.Context);
            LogFSActionSuccess("ReadFile", fileName, (SftpContext)info.Context, "");
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset,
                                              DokanFileInfo info)
        {
           

                //Log("WriteFile:{0}:{1}|lenght:[{2}]|offset:[{3}]", fileName,info.Context, buffer.Length, offset);
            LogFSActionInit("WriteFile", fileName, (SftpContext)info.Context, "Ofs:{0} Len:{1}", offset, buffer.Length);
               
               
                if (info.Context == null) // who would guess
                {
                    SftpFileStream handle = Open(GetUnixPath(fileName), FileMode.Create);
                    //var handle = _sshClient.RequestOpen(GetUnixPath(fileName), Flags.Write);
                    //using (var wait = new AutoResetEvent(false))
                    //{
                        handle.Write(buffer, 0, buffer.Length);
                        //_sshClient.RequestWrite(handle, (ulong) offset, buffer, buffer.Length, null,null/*, wait*/);
                    //}
                    handle.Close();
                    //_sshClient.RequestClose(handle);
                    bytesWritten = buffer.Length;
                    LogFSActionOther("WriteFile", fileName, (SftpContext)info.Context, "NOCONTEXT Ofs:{1} Len:{0} Written:{2}", buffer.Length, offset, bytesWritten);
                }
                else
                {
                    if (fileName == "\\public\\1\\test\\.git\\config.lock")
                    {
                        Log("Data: {0}", Encoding.ASCII.GetString(buffer));
                    }
                    
                    SftpFileStream stream = (info.Context as SftpContext).Stream;
                    lock (stream)
                    {
                        stream.Position = offset;
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    stream.Flush();
                    bytesWritten = buffer.Length;
                    // TODO there are still some apps that don't check disk free space before write
                }
              
               // Log("END WRITE:{0},{1},{2}", offset,info.Context,watch.ElapsedMilliseconds);
                LogFSActionSuccess("WriteFile", fileName, (SftpContext)info.Context, "Ofs:{1} Len:{0} Written:{2}", buffer.Length, offset, bytesWritten);
                return DokanError.ErrorSuccess;
            }
        

        DokanError IDokanOperations.FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            //Log("FLUSH:{0}", fileName);
            LogFSActionInit("FlushFile", fileName, (SftpContext)info.Context,"");

            (info.Context as SftpContext).Stream.Flush(); //git use this
            //_cache.Remove(fileName);
            CacheReset(GetUnixPath(fileName));

            LogFSActionSuccess("FlushFile", fileName, (SftpContext)info.Context, "");
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.GetFileInformation(string fileName, out FileInformation fileInfo,
                                                       DokanFileInfo info)
        {
            //Log("GetInfo:{0}:{1}", fileName,info.Context);
            LogFSActionInit("FileInfo", fileName, (SftpContext)info.Context, "");

            var context = info.Context as SftpContext;

            SftpFileAttributes sftpFileAttributes;
            string path = GetUnixPath(fileName);
            
            if (context != null)
            {
                /*
                 * Attributtes in streams are causing trouble with git. GetInfo returns wrong length if other context is writing.
                 */
                //sftpFileAttributes = context.Attributes;
                //test:
                if (context.Stream != null)
                    sftpFileAttributes = GetAttributes(path);
                else
                    sftpFileAttributes = context.Attributes;
            }
            else
            {
                
                //sftpFileAttributes = _cache.Get(path) as SftpFileAttributes;
                sftpFileAttributes = CacheGetAttr(path);

                if (sftpFileAttributes == null)
                {
                    sftpFileAttributes = GetAttributes(path);
                    if (sftpFileAttributes != null)
                        //_cache.Add(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                        CacheAddAttr(path, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                }
            }
            if (sftpFileAttributes == null)
            {
                //try again?
                //sftpFileAttributes = GetAttributes(path);

                LogFSActionError("FileInfo", fileName, (SftpContext)info.Context, "No such file - unable to get info");
                fileInfo = new FileInformation();
                return DokanError.ErrorFileNotFound;

            }


            fileInfo = new FileInformation
                           {
                               Attributes =
                                   FileAttributes.NotContentIndexed,
                               FileName = Path.GetFileName(fileName), //String.Empty,
                               // GetInfo info doesn't use it maybe for sorting .
                               CreationTime = sftpFileAttributes.LastWriteTime,
                               LastAccessTime = sftpFileAttributes.LastAccessTime,
                               LastWriteTime = sftpFileAttributes.LastWriteTime,
                               Length = sftpFileAttributes.Size
                           };
            if (sftpFileAttributes.IsDirectory)
            {
                fileInfo.Attributes |= FileAttributes.Directory;
                fileInfo.Length = 0; // Windows directories use length of 0 
            }
            else
            {
                fileInfo.Attributes |= FileAttributes.Normal;
            }
            if (fileName.Length != 1 && fileName[fileName.LastIndexOf('\\') + 1] == '.')
                //aditional check if filename isn't \\
            {
                fileInfo.Attributes |= FileAttributes.Hidden;
            }

            if (GroupRightsSameAsOwner(sftpFileAttributes))
            {
                fileInfo.Attributes |= FileAttributes.Archive;
            }
            if (_useOfflineAttribute)
            {
                fileInfo.Attributes |= FileAttributes.Offline;
            }

            if (!this.UserCanWrite(sftpFileAttributes))
            {
                fileInfo.Attributes |= FileAttributes.ReadOnly;
            }
            //  Console.WriteLine(sftpattributes.UserId + "|" + sftpattributes.GroupId + "L" +
            //  sftpattributes.OthersCanExecute + "K" + sftpattributes.OwnerCanExecute);

            LogFSActionSuccess("FileInfo", fileName, (SftpContext)info.Context, "Length:{0} Attrs:{1}", fileInfo.Length, fileInfo.Attributes);

            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            //Log("FindFiles:{0}", fileName);
            LogFSActionInit("FindFiles", fileName, (SftpContext)info.Context, "");

            /*
            var dircache = _cache.Get(fileName) as Tuple<DateTime, IList<FileInformation>>;
            if (dircache != null)
            {
                files = (dircache).Item2;
                Log("CacheHit:{0}", fileName);
                return DokanError.ErrorSuccess;
            }*/


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
                return DokanError.ErrorAccessDenied;
            }


            files = new List<FileInformation>();
            //for (var sftpFiles = _sshClient.RequestReadDir(handle);
            //     sftpFiles != null;
            //     sftpFiles = _sshClient.RequestReadDir(handle))
            //{

              


            (files as List<FileInformation>).AddRange(sftpFiles.Select(
                file =>
                    {
                        var sftpFileAttributes = file.Attributes;
                        //if (sftpFileAttributes.IsSymbolicLink)
                        //{
                        //    sftpFileAttributes = _sshClient.RequestStat(
                        //        GetUnixPath(String.Format("{0}\\{1}", fileName, file.Key)), true) ??
                        //                            file.Value;
                        //}


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
                            //fileInformation.Attributes |= FileAttributes.ReparsePoint;
                            //link?
                        }

                        if (sftpFileAttributes.IsSocket)
                        {
                            fileInformation.Attributes
                                |=
                                FileAttributes.NoScrubData | FileAttributes.System | FileAttributes.Device;
                        }else if (sftpFileAttributes.IsDirectory)
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
                    sftpFiles.Where(
                        pair => !pair.IsSymbolicLink))
            {
                /*_cache.Set(GetUnixPath(String.Format("{0}{1}", fileName, file.Key)), file.Value,
                            DateTimeOffset.UtcNow.AddSeconds(timeout));*/
                CacheAddAttr(GetUnixPath(String.Format("{0}\\{1}", fileName , file.Name)), file.Attributes,
                            DateTimeOffset.UtcNow.AddSeconds(timeout));
            }
            //}


            //_sshClient.RequestClose(handle);

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
            return DokanError.ErrorSuccess;
        }

        //DokanError IDokanOperations.FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info)
        //{
        //    /* SFTP does not support patterns, but we can use patterns other than '*' with different cache method
        //     */
        //    //Log("FindFilesWithPattern:{0},{1}", fileName,searchPattern);
        //    LogFSActionInit("FindFilesPat", fileName, (SftpContext)info.Context, "Pattern:{0}", searchPattern);

        //    //* -> list all without cache
        //    if (searchPattern == "*")
        //    {
        //        return ((IDokanOperations)this).FindFiles(fileName, out files, info);
        //    }

        //    //get files from cache || load them
        //    var dircache = CacheGetDir(GetUnixPath(fileName));
        //    if (dircache != null)
        //    {
        //        files = (dircache).Item2;
        //        //Log("CacheHit:{0}", fileName);
        //    }
        //    else
        //    {
        //        DokanError result = ((IDokanOperations)this).FindFiles(fileName, out files, info);
        //        if (result != DokanError.ErrorSuccess)
        //            return result;
        //    }

        //    //apply pattern
        //    List<FileInformation> filteredfiles = new List<FileInformation>();
        //    foreach(FileInformation fi in files){
        //        if (Dokan.IsNameInExpression(searchPattern, fi.FileName, true))
        //        {
        //            filteredfiles.Add(fi);
        //            LogFSActionOther("FindFilesPat", fileName, (SftpContext)info.Context, "Result:{0}", fi.FileName);
        //        }
        //    }
        //    files = filteredfiles;

        //    LogFSActionSuccess("FindFilesPat", fileName, (SftpContext)info.Context, "Pattern:{0} Count:{1}", searchPattern, files.Count);
        //    return DokanError.ErrorSuccess;
        //}

        DokanError IDokanOperations.SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            LogFSActionError("SetFileAttr", fileName, (SftpContext)info.Context, "Attrs:{0}", attributes);

            //get actual attributes
            string path = GetUnixPath(fileName);
            SftpFileAttributes currentattr = GetAttributes(path);

            
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
                catch(SftpPermissionDeniedException e)
                {
                    return DokanError.ErrorAccessDenied;
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

            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
                                                DateTime? lastWriteTime, DokanFileInfo info)
        {
            //Log("TrySetFileTime:{0}\n|c:{1}\n|a:{2}\n|w:{3}", filename, creationTime, lastAccessTime,lastWriteTime);
            LogFSActionInit("SetFileTime", fileName, (SftpContext)info.Context, "");

            var sftpattributes = (info.Context as SftpContext).Attributes;
            var tempAttributes = GetAttributes(GetUnixPath(fileName));

            tempAttributes.LastWriteTime = lastWriteTime ?? (creationTime ?? sftpattributes.LastWriteTime);
            tempAttributes.LastAccessTime = lastAccessTime ?? sftpattributes.LastAccessTime;

            SetAttributes(GetUnixPath(fileName), tempAttributes);

            LogFSActionSuccess("SetFileTime", fileName, (SftpContext)info.Context, "");
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.DeleteFile(string fileName, DokanFileInfo info)
        {
            //Log("DeleteFile:{0}", fileName);
            LogFSActionInit("DeleteFile", fileName, (SftpContext)info.Context, "");

            string parentPath = GetUnixPath(fileName.Substring(0, fileName.LastIndexOf('\\')));

            var sftpFileAttributes = CacheGetAttr(parentPath);

            if (sftpFileAttributes == null)
            {
                sftpFileAttributes = GetAttributes(parentPath);
                if (sftpFileAttributes != null)
                    //_cache.Add(parentPath, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
                    CacheAddAttr(parentPath, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
            }


            LogFSActionSuccess("DeleteFile", fileName, (SftpContext)info.Context, "Success:{0}", UserCanWrite(sftpFileAttributes));
            return
                UserCanWrite(
                    sftpFileAttributes)
                    ? DokanError.ErrorSuccess
                    : DokanError.ErrorAccessDenied;
        }

        DokanError IDokanOperations.DeleteDirectory(string fileName, DokanFileInfo info)
        {
            //Log("DeleteDirectory:{0}", fileName);
            LogFSActionSuccess("DeleteDir", fileName, (SftpContext)info.Context, "");


            string parentPath = GetUnixPath(fileName.Substring(0, fileName.LastIndexOf('\\')));

            var sftpFileAttributes = CacheGetAttr(parentPath);

            if (sftpFileAttributes == null)
            {
                sftpFileAttributes = GetAttributes(parentPath);
                if (sftpFileAttributes != null)
                    CacheAddAttr(parentPath, sftpFileAttributes, DateTimeOffset.UtcNow.AddSeconds(_attributeCacheTimeout));
            }


            if (
                !UserCanWrite(
                    sftpFileAttributes))
            {
                LogFSActionError("DeleteDir", fileName, (SftpContext)info.Context, "Access denied");
                return DokanError.ErrorAccessDenied;
            }
            var dircache = CacheGetDir(GetUnixPath(fileName));
            if (dircache != null)
            {
                //Log("DelateCacheHit:{0}", fileName);
                bool test = dircache.Item2.Count == 0 || dircache.Item2.All(i => i.FileName == "." || i.FileName == "..");
                
                if (test)
                    LogFSActionSuccess("DeleteDir", fileName, (SftpContext)info.Context, "");
                else
                    LogFSActionError("DeleteDir", fileName, (SftpContext)info.Context, "Dir not empty");

                return test ? DokanError.ErrorSuccess : DokanError.ErrorDirNotEmpty;
            }

            var dir = ListDirectory(GetUnixPath(fileName)).ToList();

            if (dir == null)
            {
                LogFSActionError("DeleteDir", fileName, (SftpContext)info.Context, "Open failed, access denied?");
                return DokanError.ErrorAccessDenied;
            }
            
            // usualy there are two entries . and ..

            bool test2 = dir.Count == 0 || dir.All(i => i.Name == "." || i.Name == "..");
            
            if (test2)
                LogFSActionSuccess("DeleteDir", fileName, (SftpContext)info.Context, "");
            else
                LogFSActionError("DeleteDir", fileName, (SftpContext)info.Context, "Dir not empty");

            return test2 ? DokanError.ErrorSuccess : DokanError.ErrorDirNotEmpty;
        }

        DokanError IDokanOperations.MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            //Log("MoveFile |Name:{0} ,NewName:{3},Replace:{4},IsDirectory:{1} ,Context:{2}",oldName, info.IsDirectory, info.Context, newName, replace);
            LogFSActionInit("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Replace:{1}",newName, replace);


            string oldpath = GetUnixPath(oldName);
            /*  if (_generalSftpSession.RequestLStat(oldpath, true) == null)
                return DokanError.ErrorPathNotFound;
            if (oldName.Equals(newName))
                return DokanError.ErrorSuccess;*/
            string newpath = GetUnixPath(newName);

            if (GetAttributes(newpath) == null)
            {
                (info.Context as SftpContext).Release();

                info.Context = null;
                try
                {
                    RenameFile(oldpath, newpath, false);
                    CacheResetParent(oldpath);
                    CacheResetParent(newpath);
                    CacheReset(oldpath);
                }
                catch (SftpPermissionDeniedException)
                {
                    LogFSActionError("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Access denied", newName);
                    return DokanError.ErrorAccessDenied;
                }
                LogFSActionSuccess("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Target didnt exists", newName);
                return DokanError.ErrorSuccess;
            }
            else if (replace)
            {
                (info.Context as SftpContext).Release();

                info.Context = null;


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

                    CacheReset(oldpath);
                    CacheResetParent(oldpath);
                    CacheResetParent(newpath);
                }
                
                catch (SftpPermissionDeniedException)
                {
                    LogFSActionError("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Access denied", newName);
                    return DokanError.ErrorAccessDenied;
                } // not tested on sftp3

                LogFSActionSuccess("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Target was replaced", newName);
                return DokanError.ErrorSuccess;
            }
            LogFSActionError("MoveFile", oldName, (SftpContext)info.Context, "To:{0} Target already exists", newName);
            return DokanError.ErrorAlreadyExists;
        }

        DokanError IDokanOperations.SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            //Log("SetEnd");
            LogFSActionInit("SetEndOfFile", fileName, (SftpContext)info.Context, "Length:{0}", length);
            (info.Context as SftpContext).Stream.SetLength(length);
            CacheResetParent(GetUnixPath(fileName));
            LogFSActionSuccess("SetEndOfFile", fileName, (SftpContext)info.Context, "Length:{0}", length);
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            //Log("SetSize");
            LogFSActionInit("SetAllocSize", fileName, (SftpContext)info.Context, "Length:{0}", length);
            (info.Context as SftpContext).Stream.SetLength(length);
            CacheResetParent(GetUnixPath(fileName));
            LogFSActionSuccess("SetAllocSize", fileName, (SftpContext)info.Context, "Length:{0}", length);
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            LogFSActionError("LockFile", fileName, (SftpContext)info.Context, "NI");
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            LogFSActionError("UnlockFile", fileName, (SftpContext)info.Context, "NI");
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.GetDiskFreeSpace(out long free, out long total,
                                                     out long used, DokanFileInfo info)
        {
            //Log("GetDiskFreeSpace");
            LogFSActionInit("GetDiskFreeSpace", this._volumeLabel, (SftpContext)info.Context, "");

            
            Log("GetDiskFreeSpace");

            var diskSpaceInfo = CacheGetDiskInfo();

            if (diskSpaceInfo != null)
            {
                free = diskSpaceInfo.Item1;
                total = diskSpaceInfo.Item2;
                used = diskSpaceInfo.Item3;
            }
            else
            {
                try
                {
                    var information = GetStatus(_rootpath);
                    total = (long)(information.TotalBlocks * information.BlockSize);
                    free = (long)(information.FreeBlocks * information.BlockSize);
                    used = (long)(information.AvailableBlocks * information.BlockSize);
                }
                catch (NotSupportedException)
                {
                    using (var cmd = _sshClient.CreateCommand(String.Format(" df -Pk  {0}", _rootpath), Encoding.UTF8))
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
                        else
                        {
                            total = 0x1900000000; //100 GiB
                            used = 0xc80000000; // 50 Gib
                            free = 0xc80000000;
                        }
                    }
                }

                CacheAddDiskInfo(new Tuple<long, long, long>(free, total, used),
                        DateTimeOffset.UtcNow.AddMinutes(3));
            }
            LogFSActionSuccess("GetDiskFreeSpace", this._volumeLabel, (SftpContext)info.Context, "Free:{0} Total:{1} Used:{2}", free, total, used);
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
                                                         out string filesystemName, DokanFileInfo info)
        {
            //Log("GetVolumeInformation");
            LogFSActionInit("GetVolumeInformation", this._volumeLabel, (SftpContext)info.Context, "");

            volumeLabel = _volumeLabel;

            filesystemName = "SSHFS";

            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch |
                       FileSystemFeatures.SupportsRemoteStorage | FileSystemFeatures.UnicodeOnDisk | FileSystemFeatures.SequentialWriteOnce;
            //FileSystemFeatures.PersistentAcls

            LogFSActionSuccess("GetVolumeInformation", this._volumeLabel, (SftpContext)info.Context, "FS:{0} Features:{1}", filesystemName, features);
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.GetFileSecurity(string filename, out FileSystemSecurity security,
                                                    AccessControlSections sections, DokanFileInfo info)
        {
            //Log("GetSecurrityInfo:{0}:{1}", filename, sections);
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
            // if(sections.HasFlag(AccessControlSections.Access))
            security.AddAccessRule(new FileSystemAccessRule("Everyone", rights, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl ^ rights,
                                                            AccessControlType.Deny));
            //not sure this works at all, needs testing
            // if (sections.HasFlag(AccessControlSections.Owner))
            security.SetOwner(new NTAccount("None"));
            // if (sections.HasFlag(AccessControlSections.Group))
            security.SetGroup(new NTAccount("None"));

            LogFSActionSuccess("GetFileSecurity", filename, (SftpContext)info.Context, "Sections:{0} Rights:{1}", sections, rights);
            return DokanError.ErrorSuccess;
        }

        DokanError IDokanOperations.SetFileSecurity(string filename, FileSystemSecurity security,
                                                    AccessControlSections sections, DokanFileInfo info)
        {
            //Log("TrySetSecurity:{0}", filename);
            LogFSActionError("SetFileSecurity", filename, (SftpContext)info.Context, "NI");

            return DokanError.ErrorAccessDenied;
        }

        DokanError IDokanOperations.Unmount(DokanFileInfo info)
        {
            //Log("UNMOUNT");
            LogFSActionError("Unmount", this._volumeLabel, (SftpContext)info.Context, "NI");
            // Disconnect();
            return DokanError.ErrorSuccess;
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