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
using System.Threading;
using Renci.SshNet.Sftp;
using Renci.SshNet.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sshfs
{
    internal sealed class SftpContextStream : Stream
    {
        /// <summary>
        ///   Effective size of readRequest. 
        /// </summary>
        private int optimalReadRequestSize;
        private byte[] readBuffer;
        /// <summary>
        ///  Position of readBuffer data in source stream
        /// </summary>
        private long readBufferPosition;
        /// <summary>
        ///  Valid count of data in read buffer
        /// </summary>
        private int readBufferCount;
        private bool readBufferIsAtEOF;

        private int WRITE_BUFFER_SIZE;
        

        private readonly byte[] _writeBuffer;

        private readonly ISftpSession _session;
        private SftpFileAttributes _attributes;
        private byte[] _handle;

        private bool _writeMode;
        private int _writeBufferPosition;
        private long _position;

        internal SftpContextStream(ISftpSession session, string path, FileMode mode, FileAccess access,
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

            this.optimalReadRequestSize = checked((int)this._session.CalculateOptimalReadLength(uint.MaxValue));
            this.readBuffer = new byte[this.optimalReadRequestSize];
            this.readBufferCount = 0;

            WRITE_BUFFER_SIZE = (int)_session.CalculateOptimalWriteLength(65536, _handle);

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
                }
            }
        }

        /// <summary>
        ///  Asynchronous read, will copy data to buffer and call Received.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="count"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferOffset"></param>
        /// <param name="Received"></param>
        private void ReadAsync(long position, int count, byte[] buffer, int bufferOffset, Action<int> Received)
        {
#if DEBUG
            Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + " Sending Async read for offset " + bufferOffset);
#endif
            this._session.RequestReadAsync(_handle, (ulong)(position), (uint)count,
                    response => {
#if DEBUG
                        Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + " Got data for offset " + bufferOffset);
#endif
                        if (response.Data != null)
                        {
                            lock (buffer)
                            {
                                Buffer.BlockCopy(response.Data, 0, buffer, bufferOffset, response.Data.Length);
                                Received(response.Data.Length);
                            }
                        }
                        else
                        {
                            Received(0);
                        }
                    }
            );
        }

        /// <summary>
        ///   Normal read
        /// </summary>
        /// <param name="position"></param>
        /// <param name="count"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferOffset"></param>
        /// <returns>Length of received data</returns>
        private int ReadSync(long position, int count, byte[] buffer, int bufferOffset)
        {
            byte[] data = this._session.RequestRead(this._handle, (ulong)position, (uint)count);
            Buffer.BlockCopy(data, 0, buffer, bufferOffset, data.Length);
            return data.Length;
        }

        /// <summary>
        ///   Update read buffer state with data at position
        /// </summary>
        /// <param name="position"></param>
        private void ReadDataToBufferSync(long position)
        {
            this.readBufferCount = this.ReadSync(position, this.optimalReadRequestSize, this.readBuffer, 0);
            this.readBufferPosition = position;
            this.readBufferIsAtEOF = this.readBufferCount != this.optimalReadRequestSize || this.readBufferCount == 0;
        }

        private void ReadDataToBufferASync(long position)
        {
            this.ReadAsync(position, this.readBuffer.Length, this.readBuffer, 0, 
                received => {
                    this.readBufferPosition = position;
                    this.readBufferCount = received;
                });
        }

        /// <summary>
        ///   Tryes to satisfy request from readBuffer. Returns count of bytes that hits the buffer.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="count"></param>
        /// <param name="dst"></param>
        /// <param name="dstOffset"></param>
        /// <param name="isOEF">true if buffer war read to the end and its also EOF</param>
        /// <returns></returns>
        private int getDataFromBuffer(long position, int count, byte[] dst, int dstOffset, ref bool isEOF)
        {
            if ((position >= this.readBufferPosition) &&
                (position < this.readBufferPosition + this.readBufferCount)) //atleast partial hit
            {
                int hitPosition = (int)(position - this.readBufferPosition);
                int hitLength = Math.Min(count, this.readBufferCount - hitPosition);
                Buffer.BlockCopy(this.readBuffer, hitPosition, dst, dstOffset, hitLength);

                isEOF = (this.readBufferIsAtEOF) && (hitPosition + hitLength == this.readBufferCount);
                return hitLength;
            }
            return 0;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="position">Position to read from</param>
        /// <param name="count">Count of bytes, unlimited</param>
        /// <param name="dst">Destination buffer</param>
        /// <param name="dstOffset">Offset in destination buffer to write from</param>
        /// <returns>Count of bytes readed to dst</returns>
        public int getDataAt(long position, int count, byte[] dst, int dstOffset = 0)
        {
            int received = 0;

            bool isEOF = false;
            int hitLength = this.getDataFromBuffer(position, count, dst, dstOffset, ref isEOF);
            if (hitLength > 0)
            {
#if DEBUG
                Console.WriteLine("Readbuffer hit " + hitLength);
#endif
                received    += hitLength;
                count       -= hitLength;
                position    += hitLength;
                dstOffset   += hitLength;
                if (isEOF || count == 0)  //nothing remains
                {
                    _position += received;
                    return received;
                }
            }

            //small request, we want to load more data than requested and store them in buffer:
            if (count < this.optimalReadRequestSize) 
            {
                this.ReadDataToBufferSync(position);
                hitLength = this.getDataFromBuffer(position, count, dst, dstOffset, ref isEOF);

                _position = position + received + hitLength;
                return received + hitLength;
            }


            //request with big buffers remains:

            List<EventWaitHandle> waits = new List<EventWaitHandle>();

            int readCount = count;
            int winOffset = 0;
            int receivedTotal = 0;
#if DEBUG
            DateTime startTime = DateTime.Now;
#endif
            while (readCount > 0)
            {
                int winSize = readCount > this.optimalReadRequestSize ? this.optimalReadRequestSize : readCount;

                EventWaitHandle wait = new AutoResetEvent(false);
                wait.Reset();
                waits.Add(wait);

                this.ReadAsync(
                            position + winOffset, winSize,
                            dst, dstOffset + winOffset,
                            receivedStatus => {
                                Interlocked.Add(ref receivedTotal, receivedStatus);
                                wait.Set();
                            }
                );

                winOffset += winSize;
                readCount -= winSize;
            }

            if (!WaitHandle.WaitAll(waits.ToArray(), 20000))
            {
                throw new SshOperationTimeoutException("Timeout on wait");
            }
#if DEBUG
            int rrTime = (DateTime.Now - startTime).Milliseconds;
            Console.WriteLine(rrTime+" with "+waits.Count.ToString());
#endif
            _position = _position + received + receivedTotal;
            return received + receivedTotal;
        }


        public override int Read(byte[] buffer, int bufferOffset, int bufferCount)
        {
            // Set up for the read operation.
            SetupRead();
            return this.getDataAt(this._position, bufferCount, buffer, bufferOffset);
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
                        _session.RequestWrite(
                            _handle, 
                            (ulong)(_position+suboffset), 
                            _writeBuffer, 
                            0,
                            chunkcount, 
                            null, 
                            null
                        );
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
                
                    _session.RequestWrite(
                        _handle, 
                        (ulong) (_position - WRITE_BUFFER_SIZE), 
                        _writeBuffer, 
                        0,
                        _writeBufferPosition, 
                        null,
                        null
                    );
                

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


               
                    _session.RequestWrite(
                        _handle, 
                        (ulong) (_position - _writeBufferPosition), 
                        data, 
                        0, 
                        _writeBufferPosition, 
                        null, 
                        null
                    );
                

                _writeBufferPosition = 0;
            }
        }

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

            _writeBufferPosition = 0;
            _writeMode = true;
        }
    }
}