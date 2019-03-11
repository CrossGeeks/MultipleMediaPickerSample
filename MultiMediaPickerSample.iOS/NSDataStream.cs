﻿using System;
using System.IO;
using UIKit;
using Foundation;
using System.Runtime.InteropServices;
namespace MultiMediaPickerSample.iOS
{


        class NSDataStream : Stream
        {
            NSData theData;
            uint pos;

            public NSDataStream(NSData data)
            {
                this.theData = data;
            }

            protected override void Dispose(bool disposing)
            {
                if (theData != null)
                {
                    theData.Dispose();
                    theData = null;
                }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (pos >= theData.Length)
                {
                    return 0;
                }
                else
                {

                    var len = (int)Math.Min(count, (double)(theData.Length - pos));

                    Marshal.Copy(new IntPtr(theData.Bytes.ToInt64() + pos), buffer, offset, len);
                    pos += (uint)len;
                    return len;
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }

            public override long Length
            {
                get
                {

                    return (long)theData.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return pos;
                }
                set
                {
                }
            }
        }
}
