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
using System.Diagnostics;
using System.IO;
using System.Threading;
using Renci.SshNet.Sftp;

namespace Sshfs
{
    internal sealed class SftpContextStream : Stream
    {

        private const int WRITE_BUFFER_SIZE = 32 * 1024 - 38;// (1024*32 - 38)*4;
        private const int READ_BUFFER_SIZE = 128*1024;
        private readonly byte[] _writeBuffer;
        private byte[] _readBuffer = new byte[0];


        private readonly SftpSession _session;
        private SftpFileAttributes _attributes;
        private byte[] _handle;

        private bool _writeMode;
        private int _writeBufferPosition;
        private int _readBufferPosition;
        private long _position;

        internal SftpContextStream(SftpSession session, string path, FileMode mode, FileAccess access,
                                   SftpFileAttributes attributes)
        {
            Flags flags = Flags.None;

            switch (access)
            {
                case FileAccess.Read:
                    flags = Flags.Read;
                    break;
                case FileAccess.Write:
                    flags = Flags.Write;
                    break;
                case FileAccess.ReadWrite:
                    flags = Flags.Read | Flags.Write;
                    break;
            }

            switch (mode)
            {
                case FileMode.Append:
                    flags |= Flags.Append;
                    break;
                case FileMode.Create:
                    if (attributes == null)
                    {
                        flags |= Flags.CreateNew;
                    }
                    else
                    {
                        flags |= Flags.Truncate;
                    }
                    break;
                case FileMode.CreateNew:
                    flags |= Flags.CreateNew;
                    break;
                case FileMode.Open:
                    break;
                case FileMode.OpenOrCreate:
                    flags |= Flags.CreateNewOrOpen;
                    break;
                case FileMode.Truncate:
                    flags |= Flags.Truncate;
                    break;
            }

            _session = session;

            _handle = _session.RequestOpen(path, flags);

            _attributes = attributes ?? _session.RequestFStat(_handle);


            if (access.HasFlag(FileAccess.Write))
            {
                _writeBuffer = new byte[WRITE_BUFFER_SIZE];
                _writeMode = true;
            }

            _position = mode != FileMode.Append ? 0 : _attributes.Size;
        }


        public SftpFileAttributes Attributes
        {
            get
            {
                lock (this)
                {
                    if (_writeMode)
                    {




                        //FlushWriteBuffer();
                        SetupRead();
                        _attributes = _session.RequestFStat(_handle);

                    }
                }
                return _attributes;
            }
        }


        public override bool CanRead
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }


        public override long Position
        {
            get { return _position; }
            set
            {
                if (!_writeMode)
                {
                    long newPosn = _position - _readBufferPosition;
                    if (value >= newPosn && value <
                        (newPosn + _readBuffer.Length))
                    {
                        _readBufferPosition = (int) (value - newPosn);
                    }
                    else
                    {
                        _readBufferPosition = 0;
                        _readBuffer = new byte[0];
                    }
                }
                else
                {
                   // Console.WriteLine("Position:{0}=?{1}",value,_position);
                    if (_position != value)
                    {
                        FlushWriteBuffer();
                    }
                }
                _position = value;
            }
        }


        public override void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public override void Flush()
        {
            lock (this)
            {
                if (_writeMode)
                {
                    FlushWriteBuffer();
                }
                else
                {
                    /*  if (_bufferPosn < _bufferLen)
                {
                    _position -= _bufferPosn;
                }*/
                    _readBufferPosition = 0;
                    _readBuffer = new byte[0];
                }
            }
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            int readLen = 0;


            // Lock down the file stream while we do this.

            // Set up for the read operation.
            SetupRead();

            // Read data into the caller's buffer.
            while (count > 0)
            {
                // How much data do we have available in the buffer?
                int tempLen = _readBuffer.Length - _readBufferPosition;
                if (tempLen <= 0)
                {
                    _readBufferPosition = 0;

                    _readBuffer = _session.RequestRead(_handle, (ulong) _position, READ_BUFFER_SIZE);


                    if (_readBuffer.Length > 0)
                    {
                        tempLen = _readBuffer.Length;
                    }
                    else
                    {
                        break;
                    }
                }


                // Don't read more than the caller wants.
                if (tempLen > count)
                {
                    tempLen = count;
                }

                // Copy stream data to the caller's buffer.
                Debug.WriteLine("Copy:{0},{1},{2},{3},{4}",_readBuffer,_readBufferPosition,buffer,offset,tempLen);
                Buffer.BlockCopy(_readBuffer, _readBufferPosition, buffer, offset, tempLen);

                // Advance to the next buffer positions.
                readLen += tempLen;
                offset += tempLen;
                count -= tempLen;
                _readBufferPosition += tempLen;
                _position += tempLen;
            }


            // Return the number of bytes that were read to the caller.
            return readLen;
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            lock (this)
            {
                // Lock down the file stream while we do this.

                // Setup this object for writing.
                SetupWrite();

                _attributes.Size = value;

                _session.RequestFSetStat(_handle, _attributes);
            }
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            // Lock down the file stream while we do this.

            // Setup this object for writing.
            SetupWrite();

            // Write data to the file stream.
          // while (count > 0)
         //   {
                // Determine how many bytes we can write to the buffer.
                int tempLen = WRITE_BUFFER_SIZE - _writeBufferPosition;

              /*  if (tempLen <= 0)
                {
                   
                    _session.RequestWrite(_handle, (ulong) (_position - WRITE_BUFFER_SIZE), _writeBuffer);

                    _writeBufferPosition = 0;
                    tempLen = WRITE_BUFFER_SIZE;
                }*/


            if (tempLen >= count) //enought remaining space in writeBuffer
            {
                // No: copy the data to the write buffer first.
                Buffer.BlockCopy(buffer, offset, _writeBuffer, _writeBufferPosition, count);
                _writeBufferPosition += count;
            }
            else //writeBuffer space insufficient
            {
                FlushWriteBuffer();


                if (count > WRITE_BUFFER_SIZE) //writeBuffer size is still lower
                {
                    //solves problem: max writtable count is WRITE_BUFFER_SIZE
                    int remainingcount = count;
                    int suboffset = 0;
                    while (remainingcount >= WRITE_BUFFER_SIZE)//fire whole blocks
                    {
                        int chunkcount = remainingcount <= WRITE_BUFFER_SIZE ? remainingcount : WRITE_BUFFER_SIZE;
                        Buffer.BlockCopy(buffer, offset+suboffset, _writeBuffer, _writeBufferPosition/*always zero*/, chunkcount);
                        _session.RequestWrite(_handle, (ulong)(_position+suboffset), _writeBuffer, _writeBuffer.Length, null, null);
                        remainingcount -= chunkcount;
                        suboffset += chunkcount;
                    }
                    if (remainingcount > 0)//if something remains, do it standard way:
                    {
                        Buffer.BlockCopy(buffer, offset+suboffset, _writeBuffer, _writeBufferPosition/*shoud be 0*/, remainingcount);
                        _writeBufferPosition += remainingcount;
                    }
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, _writeBuffer, _writeBufferPosition, count);
                    _writeBufferPosition += count;
                }
            }
            // Advance the buffer and stream positions.
                _position += count;
               // offset += tempLen;
               // count -= tempLen;
          //  }

            // If the buffer is full, then do a speculative flush now,
            // rather than waiting for the next call to this method.
            if (_writeBufferPosition == WRITE_BUFFER_SIZE)
            {
                
                    _session.RequestWrite(_handle, (ulong) (_position - WRITE_BUFFER_SIZE), _writeBuffer, _writeBuffer.Length, null,null);
                

                _writeBufferPosition = 0;
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);


            if (_handle != null)
            {
                if (_writeMode)
                {
                    FlushWriteBuffer();
                }

                _session.RequestClose(_handle);

                _handle = null;
            }
        }


        private void FlushWriteBuffer()
        {
           // Console.WriteLine("FLUSHHHH the water");
            if (_writeBufferPosition > 0)
            {
               // Console.WriteLine("Written:{0}",_writeBufferPosition);
                var data = new byte[_writeBufferPosition];
                Buffer.BlockCopy(_writeBuffer, 0, data, 0, _writeBufferPosition);


               
                    _session.RequestWrite(_handle, (ulong) (_position - _writeBufferPosition), data, data.Length, null,null);
                

                _writeBufferPosition = 0;
            }
        }

/*
        private void FlushWriteBufferNoPipelining()
        {
            const int maximumDataSize = 1024 * 32 - 38;
            Console.WriteLine("FLUSHHHH the water no pipe");
            if (_writeBufferPosition > 0)
            {
                Console.WriteLine("Written:{0}", _writeBufferPosition);
       
                 int block = ((_writeBufferPosition - 1) / maximumDataSize) + 1;
                 for (int i = 0; i < block; i++)
                 {
                     var blockBufferSize = Math.Min(_writeBufferPosition - maximumDataSize * i, maximumDataSize);
                     var blockBuffer = new byte[blockBufferSize];

                     Buffer.BlockCopy(_writeBuffer, i*maximumDataSize, blockBuffer, 0, blockBufferSize);

                     using (var wait = new AutoResetEvent(false))
                     {
                         _session.RequestWrite(_handle, (ulong) (_position - _writeBufferPosition+i*maximumDataSize), blockBuffer, wait);
                     }
                 }

                _writeBufferPosition = 0;
            }
        }
*/


        private void SetupRead()
        {
            if (_writeMode)
            {
                FlushWriteBuffer();
                _writeMode = false;
            }
        }


        private void SetupWrite()
        {
            if (_writeMode) return;

            /*  if (_bufferPosn < _bufferLen)
            {
                _position -= _bufferPosn;
            }*/
            _readBufferPosition = 0;
            _writeBufferPosition = 0;
            _readBuffer = new byte[0];
            _writeMode = true;
        }
    }
}