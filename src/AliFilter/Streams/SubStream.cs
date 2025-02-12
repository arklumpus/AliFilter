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
    /// A <see cref="Stream"/> built as a subset of another <see cref="Stream"/>.
    /// </summary>
    internal class SubStream : Stream
    {
        /// <summary>
        /// The base <see cref="Stream"/> from which this <see cref="SubStream"/> was constructed.
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// The offset within the <see cref="BaseStream"/> where this <see cref="SubStream"/> starts.
        /// </summary>
        public long Offset { get; }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => BaseStream.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length { get; }

        /// <inheritdoc/>
        public override long Position
        {
            get => BaseStream.Position - Offset;
            set => BaseStream.Position = value + Offset;
        }

        /// <summary>
        /// Create a new <see cref="SubStream"/>.
        /// </summary>
        /// <param name="baseStream">The base <see cref="Stream"/> from which the <see cref="SubStream"/> will be constructed.</param>
        /// <param name="offset">The offset within the <see cref="BaseStream"/> where the <see cref="SubStream"/> will start.</param>
        /// <param name="length">The length of the <see cref="SubStream"/>.</param>
        public SubStream(Stream baseStream, long offset, long length)
        {
            BaseStream = baseStream;
            Offset = offset;
            Length = length;
            
            BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Length - Position <= 0)
            {
                return 0;
            }
            else
            {
                if (Length - Position < count)
                {
                    count = (int)(Length - Position);
                }

                return BaseStream.Read(buffer, offset, count);
            }
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return BaseStream.Seek(offset + Offset, origin);
                case SeekOrigin.Current:
                    return BaseStream.Seek(offset, origin);
                case SeekOrigin.End:
                    return BaseStream.Seek(Offset + Length - offset, SeekOrigin.Begin);
                default:
                    throw new NotSupportedException();
            }
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
    }
}
