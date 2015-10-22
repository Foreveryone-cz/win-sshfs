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
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace Sshfs
{
    internal sealed class SftpContext : IDisposable
    {
        private SftpFileAttributes _attributes;
        private SftpFileStream _stream;

        public bool deleteOnCloseWorkaround = false;

        public SftpContext(SftpFileAttributes attributes)
        {
            _attributes = attributes;
        }

        public SftpContext(SftpFileAttributes attributes, bool aDeleteOnCloseWorkaround)
        {
            _attributes = attributes;
            this.deleteOnCloseWorkaround = aDeleteOnCloseWorkaround;
        }

        public SftpContext(SftpClient client, string path, FileMode mode, FileAccess access,
                        SftpFileAttributes attributes)
        {
            _stream = client.Open(path, mode, access);
            _attributes = attributes;
        }

        public SftpFileAttributes Attributes
        {
            get { return _attributes; }
        }

        public SftpFileStream Stream
        {
            get { return _stream; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            _attributes = null;

            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }


            GC.SuppressFinalize(this);
        }

        #endregion

        public void Release()
        {
            _attributes = null;

            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return String.Format("[{0:x}]", this.GetHashCode());
        }
    }
}