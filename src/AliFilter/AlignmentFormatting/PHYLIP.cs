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

using System.Text;

namespace AliFilter.AlignmentFormatting
{
    /// <summary>
    /// Contains methods to read and write PHYLIP alignments.
    /// </summary>
    public class PHYLIP : AlignmentFormatter
    {
        /// <summary>
        /// This class supports alignments in (relaxed) PHYLIP format.
        /// </summary>
        public override AlignmentFileFormat Format => AlignmentFileFormat.PHYLIP;

        /// <summary>
        /// Determines whether an alignment is in PHYLIP format.
        /// </summary>
        /// <param name="input">A <see cref="TextReader"/> from which the alignment is read.</param>
        /// <returns><see langword="true" /> if the alignment is in PHYLIP format, <see langword="false"/> otherwise.</returns>
        public override bool Is(TextReader input)
        {
            char c = ' ';

            if (!SkipWhitespace(input, ref c))
            {
                return false;
            }

            StringBuilder builder = new StringBuilder();

            if (!ReadWord(input, ref c, builder))
            {
                return false;
            }

            int sequenceCount = int.Parse(builder.ToString(), System.Globalization.CultureInfo.InvariantCulture);

            if (!SkipWhitespace(input, ref c))
            {
                return false;
            }
            builder.Clear();
            if (!ReadWord(input, ref c, builder))
            {
                return false;
            }
            int alignmentLength = int.Parse(builder.ToString(), System.Globalization.CultureInfo.InvariantCulture);

            if (!SkipToNextLine(input, ref c) || !SkipWhitespace(input, ref c))
            {
                return false;
            }

            return sequenceCount > 0 && alignmentLength > 0;
        }

        /// <summary>
        /// Skips characters until a non-whitespace character is found.
        /// </summary>
        /// <param name="reader">The text reader from which characters are read.</param>
        /// <param name="c">The current character.</param>
        /// <returns><see langword="true"/> if a non-whitespace character has been read, <see langword="false"/> if the end of the file has been reached without finding a non-whitespace character.</returns>
        private static bool SkipWhitespace(TextReader reader, ref char c)
        {
            while (char.IsWhiteSpace(c))
            {
                int readChar = reader.Read();

                if (readChar >= 0)
                {
                    c = (char)readChar;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reads a series of characters until the next whitespace.
        /// </summary>
        /// <param name="reader">The text reader from which characters are read.</param>
        /// <param name="c">The current character.</param>
        /// <param name="builder">A <see cref="StringBuilder"/> object that will be used to store the word.</param>
        /// <returns><see langword="true"/> if a whitespace character after the word has been read, <see langword="false"/> if the end of the file has been reached without finding a whitespace character.</returns>
        private static bool ReadWord(TextReader reader, ref char c, StringBuilder builder)
        {
            while (!char.IsWhiteSpace(c))
            {
                builder.Append(c);

                int readChar = reader.Read();

                if (readChar >= 0)
                {
                    c = (char)readChar;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Skips characters until a newline character is found.
        /// </summary>
        /// <param name="reader">The text reader from which characters are read.</param>
        /// <param name="c">The current character.</param>
        /// <returns><see langword="true"/> if a newline character has been read, <see langword="false"/> if the end of the file has been reached without finding a newline character.</returns>
        private static bool SkipToNextLine(TextReader reader, ref char c)
        {
            while (c != '\n')
            {
                int readChar = reader.Read();

                if (readChar >= 0)
                {
                    c = (char)readChar;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reads an alignment from a <see cref="TextReader"/> in relaxed PHYLIP format.
        /// </summary>
        /// <param name="input">A <see cref="TextReader"/> from which the alignment is read.</param>
        /// <param name="alignmentType">The alignment type.</param>
        /// <returns>An <see cref="Alignment"/> containing the sequences read from the <see cref="TextReader"/>.</returns>
        public override Alignment Read(TextReader input, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            char c = ' ';

            if (!SkipWhitespace(input, ref c))
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();

            if (!ReadWord(input, ref c, builder))
            {
                return null;
            }

            int sequenceCount = int.Parse(builder.ToString(), System.Globalization.CultureInfo.InvariantCulture);

            if (!SkipWhitespace(input, ref c))
            {
                return null;
            }
            builder.Clear();
            if (!ReadWord(input, ref c, builder))
            {
                return null;
            }
            int alignmentLength = int.Parse(builder.ToString(), System.Globalization.CultureInfo.InvariantCulture);

            if (!SkipToNextLine(input, ref c) || !SkipWhitespace(input, ref c))
            {
                return null;
            }

            string[] names = new string[sequenceCount];
            char[][] sequences = new char[sequenceCount][];

            for (int i = 0; i < sequenceCount; i++)
            {
                builder.Clear();
                if (!ReadWord(input, ref c, builder))
                {
                    return null;
                }

                names[i] = builder.ToString();

                if (!SkipWhitespace(input, ref c))
                {
                    return null;
                }

                sequences[i] = new char[alignmentLength];
                int index = 0;

                while (index < alignmentLength)
                {
                    if (!char.IsWhiteSpace(c))
                    {
                        sequences[i][index] = c;
                        index++;
                    }

                    int readChar = input.Read();

                    if (readChar >= 0)
                    {
                        c = (char)readChar;
                    }
                    else if (i < sequenceCount - 1 || index < alignmentLength)
                    {
                        return null;
                    }
                }

                if (i < sequenceCount - 1 && (!SkipToNextLine(input, ref c) || !SkipWhitespace(input, ref c)))
                {
                    return null;
                }
            }

            return Alignment.Create(sequenceCount, names.Select((x, i) => (x, (IEnumerable<char>)sequences[i])), alignmentType);
        }

        /// <summary>
        /// Writes an <paramref name="alignment"/> in PHYLIP phylip format.
        /// </summary>
        /// <param name="output">A <see cref="TextWriter"/> on which the <paramref name="alignment"/> is written.</param>
        /// <param name="alignment">The <see cref="Alignment"/> to save.</param>
        public override void Write(TextWriter output, Alignment alignment)
        {
            int maxNameLength = alignment.SequenceNames.Select(x => x.Length).Max();

            output.Write(alignment.SequenceCount);
            output.Write("  ");
            output.WriteLine(alignment.AlignmentLength);

            for (int i = 0; i <  alignment.SequenceNames.Length; i++)
            {
                output.Write(alignment.SequenceNames[i]);
                output.Write(new string(' ', maxNameLength - alignment.SequenceNames[i].Length + 1));
                output.WriteLine(new string(alignment.GetSequence(i).ToArray()));
            }
        }
    }
}
