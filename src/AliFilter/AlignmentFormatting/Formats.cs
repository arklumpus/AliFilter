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

using AliFilter.Streams;

namespace AliFilter.AlignmentFormatting
{
    /// <summary>
    /// Alignment file formats supported by this program.
    /// </summary>
    public enum AlignmentFileFormat
    {
        /// <summary>
        /// FASTA format.
        /// </summary>
        FASTA,

        /// <summary>
        /// Relaxed PHYLIP format
        /// </summary>
        PHYLIP
    }

    /// <summary>
    /// Contains utilities to deal with alignment formats.
    /// </summary>
    public static class FormatUtilities
    {
        internal static readonly AlignmentFormatter[] AlignmentFormatters = new AlignmentFormatter[]
        {
            new FASTA(),
            new PHYLIP(),
        };

        /// <summary>
        /// Parses an input format.
        /// </summary>
        /// <param name="format">The input format.</param>
        /// <returns>A <see cref="AlignmentFileFormat"/> enumeration, or <see langword="null"/> to autodetect.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="format"/> string does not correspond to any known format.</exception>
        public static AlignmentFileFormat? ParseFormat(string format)
        {
            if (string.IsNullOrEmpty(format) || format.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            else
            {
                foreach (AlignmentFileFormat fmt in Enum.GetValues<AlignmentFileFormat>())
                {
                    if (fmt.ToString().Equals(format, StringComparison.OrdinalIgnoreCase))
                    {
                        return fmt;
                    }
                }

                throw new ArgumentException("Invalid alignment format specified: " + format + "!");
            }
        }

        /// <summary>
        /// Gets the alignment formatter corresponding to the specified alignment <paramref name="format"/>.
        /// </summary>
        /// <param name="format">The alignment format.</param>
        /// <returns>An <see cref="AlignmentFormatter"/> that can read and write alignments in the specified format.</returns>
        public static AlignmentFormatter GetAlignmentFormat(AlignmentFileFormat format)
        {
            foreach (AlignmentFormatter formatter in AlignmentFormatters)
            {
                if (format == formatter.Format)
                {
                    return formatter;
                }
            }

            return null;
        }

        /// <summary>
        /// Reads an alignment from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The stream from which the alignment is read.</param>
        /// <param name="alignmentFormat">When this method returns, this variable will contain the format in which the alignment was read.</param>
        /// <param name="alignmentType">The alignment type.</param>
        /// <param name="leaveOpen">Whether the stream should be disposed after reading the alignment.</param>
        /// <returns>The parsed alignment.</returns>
        public static Alignment ReadAlignment(Stream stream, ref AlignmentFileFormat? alignmentFormat, AlignmentType alignmentType = AlignmentType.Autodetect, bool leaveOpen = false)
        {
            using (SeekableStream st = new SeekableStream(stream, leaveOpen))
            {
                if (alignmentFormat != null)
                {
                    AlignmentFormatter formatter = GetAlignmentFormat(alignmentFormat.Value);
                    return formatter.Read(st, alignmentType);
                }
                else
                {
                    foreach (AlignmentFormatter formatter in AlignmentFormatters)
                    {
                        st.Seek(0, SeekOrigin.Begin);

                        if (formatter.Is(st))
                        {
                            st.Seek(0, SeekOrigin.Begin);
                            alignmentFormat = formatter.Format;
                            return formatter.Read(st, alignmentType);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Reads an alignment from a file.
        /// </summary>
        /// <param name="fileName">The file from which the alignment is read.</param>
        /// <param name="alignmentFormat">When this method returns, this variable will contain the format in which the alignment was read.</param>
        /// <param name="alignmentType">The alignment type.</param>
        /// <returns>The parsed alignment.</returns>
        public static Alignment ReadAlignment(string fileName, ref AlignmentFileFormat? alignmentFormat, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                return ReadAlignment(fs, ref alignmentFormat, alignmentType, false);
            }
        }
    }
}
