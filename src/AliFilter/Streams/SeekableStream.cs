/*
    AliFilter: A Machine Learning Approach to Alignment Filtering

    by Giorgio Bianchini, Rui Zhu, Francesco Cicconardi, Edmund RR Moody

    Copyright (C) 2024  Giorgio Bianchini
 
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, version 3.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace AliFilter.Streams
{
    /// <summary>
    /// A wrapper for a <see cref="Stream"/> that supports seek operations using a cache.
    /// </summary>
    internal class SeekableStream : Stream
    {
        private readonly Stream BaseStream;
        private readonly MemoryStream CachedStream;

        private bool isCaching = true;

        /// <summary>
        /// Determines whether the stream is caching the read data or not. Seekability is lost once this is set to false.
        /// </summary>
        public bool IsCaching
        {
            get
            {
                return isCaching;
            }

            set
            {
                if (isCaching)
                {
                    isCaching = value;
                }
                else
                {
                    throw new InvalidOperationException("Caching cannot be resumed once it has been stopped!");
                }
            }
        }

        /// <summary>
        /// Determines whether the underlying <see cref="Stream"/> is disposed when this object is disposed.
        /// </summary>
        public bool LeaveOpen { get; set; }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => BaseStream.CanSeek || IsCaching;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => BaseStream.Length;

        private long position = 0;

        /// <inheritdoc/>
        public override long Position { get => position; set => Seek(value, SeekOrigin.Begin); }

        /// <inheritdoc/>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (CachedStream == null || this.Position >= CachedStream.Length)
            {
                int readBytes = BaseStream.Read(buffer, offset, count);

                if (IsCaching)
                {
                    CachedStream.Write(buffer, offset, readBytes);
                }

                this.position += readBytes;

                return readBytes;
            }
            else
            {
                int readBytes = CachedStream.Read(buffer, offset, Math.Min(count, (int)(CachedStream.Length - this.Position)));

                this.position += readBytes;

                return readBytes;
            }
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (this.CanSeek)
            {
                if (BaseStream.CanSeek)
                {
                    BaseStream.Seek(offset, origin);
                    this.position = BaseStream.Position;
                    return this.Position;
                }
                else
                {
                    long actualPosition = offset;

                    switch (origin)
                    {
                        case SeekOrigin.Begin:
                            break;
                        case SeekOrigin.Current:
                            actualPosition = this.Position + offset;
                            break;
                        case SeekOrigin.End:
                            actualPosition = this.Length + offset;
                            break;
                    }

                    if (actualPosition <= CachedStream.Length)
                    {
                        this.position = actualPosition;
                        CachedStream.Position = actualPosition;
                        return actualPosition;
                    }
                    else
                    {
                        this.position = CachedStream.Length;
                        CachedStream.Position = this.Position;

                        byte[] buffer = new byte[4096];

                        while (this.Position < actualPosition)
                        {
                            this.Read(buffer, 0, (int)Math.Min(4096, actualPosition - this.Position));
                        }

                        return actualPosition;
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Create a new <see cref="SeekableStream"/> from the supplied <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> from which the <see cref="SeekableStream"/> is constructed.</param>
        /// <param name="leaveOpen">If this is <see langword="false"/>, this instance takes ownership of the <paramref name="stream"/> and will dispose of it at the end of its lifecycle.</param>
        public SeekableStream(Stream stream, bool leaveOpen = false)
        {
            this.BaseStream = stream;
            this.LeaveOpen = leaveOpen;

            if (stream.CanSeek)
            {
                this.IsCaching = false;
                this.CachedStream = null;
            }
            else
            {
                this.CachedStream = new MemoryStream();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!LeaveOpen)
                {
                    this.BaseStream?.Dispose();
                }
                this.CachedStream?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
