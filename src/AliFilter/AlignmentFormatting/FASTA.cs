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
using System.Text.RegularExpressions;

namespace AliFilter.AlignmentFormatting
{
    /// <summary>
    /// Contains methods to read and write FASTA alignments.
    /// </summary>
    public partial class FASTA : AlignmentFormatter
    {
        [GeneratedRegex("\\s+")]
        private static partial Regex _WhitespaceRegexGenerator();
        private static readonly Regex WhitespaceRegex = _WhitespaceRegexGenerator();

        /// <summary>
        /// This class supports alignments in FASTA format.
        /// </summary>
        public override AlignmentFileFormat Format => AlignmentFileFormat.FASTA;

        /// <summary>
        /// Determines whether an alignment is in FASTA format.
        /// </summary>
        /// <param name="input">A <see cref="TextReader"/> from which the alignment is read.</param>
        /// <returns><see langword="true" /> if the alignment is in FASTA format, <see langword="false"/> otherwise.</returns>
        public override bool Is(TextReader input)
        {
            int readChar = input.Read();

            while (readChar >= 0 && char.IsWhiteSpace((char)readChar))
            {
                readChar = input.Read();
            }

            return readChar >= 0 && (char)readChar == '>';
        }

        /// <summary>
        /// Reads an alignment from a <see cref="TextReader"/> in FASTA format.
        /// </summary>
        /// <param name="input">A <see cref="TextReader"/> from which the alignment is read.</param>
        /// <param name="alignmentType">The alignment type.</param>
        /// <returns>An <see cref="Alignment"/> containing the sequences read from the <see cref="TextReader"/>.</returns>
        public override Alignment Read(TextReader input, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            List<(string, IEnumerable<char>)> sequences = new List<(string, IEnumerable<char>)>();

            string currSeqName = "";
            StringBuilder currSeq = new StringBuilder();

            string line = input.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                if (line.StartsWith('>'))
                {
                    if (!string.IsNullOrEmpty(currSeqName))
                    {
                        sequences.Add((currSeqName, WhitespaceRegex.Replace(currSeq.ToString(), "")));
                    }
                    currSeqName = line.Substring(1);
                    currSeq.Clear();
                }
                else
                {
                    currSeq.Append(line);
                }

                line = input.ReadLine();
            }

            if (!string.IsNullOrEmpty(currSeqName))
            {
                sequences.Add((currSeqName, WhitespaceRegex.Replace(currSeq.ToString(), "")));
            }

            return Alignment.Create(sequences, alignmentType);
        }

        /// <summary>
        /// Writes an <paramref name="alignment"/> in FASTA format.
        /// </summary>
        /// <param name="output">A <see cref="TextWriter"/> on which the <paramref name="alignment"/> is written.</param>
        /// <param name="alignment">The <see cref="Alignment"/> to save.</param>
        public override void Write(TextWriter output, Alignment alignment)
        {
            for (int i = 0; i < alignment.SequenceCount; i++)
            {
                output.WriteLine(">" + alignment.SequenceNames[i]);
                output.WriteLine(new string(alignment.GetSequence(i).ToArray()));
            }
        }
    }
}
