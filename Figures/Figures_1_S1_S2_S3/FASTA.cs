/*
    AliFilter: A Machine Learning Approach to Alignment Filtering

    by Giorgio Bianchini, Rui Zhu, Francesco Cicconardi, Edmund RR Moody

    Source code for manuscript figures.

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

namespace Figures_1_S1_S2_S3
{
    /// <summary>
    /// Contains methods to read and write FASTA alignments.
    /// </summary>
    public static class FASTA
    {
        /// <summary>
        /// Reads an alignment from a <see cref="TextReader"/> in FASTA format.
        /// </summary>
        /// <param name="input">A <see cref="TextReader"/> from which the alignment is read.</param>
        /// <param name="alignmentType">The alignment type.</param>
        /// <returns>An <see cref="Alignment"/> containing the sequences read from the <see cref="TextReader"/>.</returns>
        public static Dictionary<string, string> Read(TextReader input)
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
                        sequences.Add((currSeqName, currSeq.ToString()));
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
                sequences.Add((currSeqName, currSeq.ToString()));
            }

            return new Dictionary<string, string>(sequences.Select(x => new KeyValuePair<string, string>(x.Item1, new string(x.Item2.ToArray()))));
        }

        public static Dictionary<string, string> Read(string inputFile)
        {
            using (StreamReader sr = new StreamReader(inputFile))
            {
                return Read(sr);
            }
        }
    }
}
