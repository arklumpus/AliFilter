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

namespace AliFilter
{
    /// <summary>
    /// Describes a type of alignment mask.
    /// </summary>
    public enum MaskType
    {
        /// <summary>
        /// A mask sequence containing 0s and 1s.
        /// </summary>
        Binary,

        /// <summary>
        /// A mask sequence where each character is determined using the Sanger convention for storing the log-transformed preservation score.
        /// For a preservation score of p (ranging from 0 - the column should be deleted - to 1 - the column should be preserved), an ASCII character with value 126 + 10 * log10(p) is used. 
        /// </summary>
        Fuzzy,

        /// <summary>
        /// A list of floating point numbers, separated by spaces, where each number represents the preservation score of an alignment column.
        /// </summary>
        Float
    }

    /// <summary>
    /// Represents an alignment mask.
    /// </summary>
    public class Mask
    {
        /// <summary>
        /// The length of the mask.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// The actual mask.
        /// </summary>
        public bool[] MaskedStates { get; }

        /// <summary>
        /// Confidence score assigned to each mask state.
        /// </summary>
        public double[] Confidence { get; }

        /// <summary>
        /// Create a new <see cref="Mask"/> from a mask sequence containing 0s and 1s.
        /// </summary>
        /// <param name="maskSequence">The mask sequence.</param>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="maskSequence"/> contains characters other than 0 or 1.</exception>
        public Mask(string maskSequence)
        {
            this.Length = maskSequence.Length;
            this.MaskedStates = new bool[this.Length];
            this.Confidence = new double[this.Length];

            for (int i = 0; i < maskSequence.Length; i++)
            {
                if (maskSequence[i] == '0')
                {
                    this.MaskedStates[i] = false;
                    this.Confidence[i] = 1;
                }
                else if (maskSequence[i] == '1')
                {
                    this.MaskedStates[i] = true;
                    this.Confidence[i] = 1;
                }
                else
                {
                    throw new ArgumentException("Invalid character '" + maskSequence[i] + "' in mask sequence!", nameof(maskSequence));
                }
            }
        }

        /// <summary>
        /// Create a new <see cref="Mask"/> by comparing a full alignment with an alignment where some columns have been removed.
        /// </summary>
        /// <param name="fullAlignment">The full alignment.</param>
        /// <param name="maskedAlignment">The alignment where some columns have been removed.</param>
        public Mask(Alignment fullAlignment, Alignment maskedAlignment)
        {
            HashSet<string> maskedColumns = new HashSet<string>(maskedAlignment.AlignmentLength);

            for (int i = 0; i < maskedAlignment.AlignmentLength; i++)
            {
                maskedColumns.Add(new string(maskedAlignment.GetColumn(i).ToArray()).ToUpperInvariant());
            }

            this.Length = fullAlignment.AlignmentLength;
            this.MaskedStates = new bool[this.Length];
            this.Confidence = new double[this.Length];

            for (int i = 0; i < fullAlignment.AlignmentLength; i++)
            {
                string column = new string(fullAlignment.GetColumn(i).ToArray()).ToUpperInvariant();

                if (maskedColumns.Contains(column))
                {
                    this.MaskedStates[i] = true;
                    this.Confidence[i] = 1;
                }
                else
                {
                    this.MaskedStates[i] = false;
                    this.Confidence[i] = 1;
                }
            }
        }

        /// <summary>
        /// Create a new <see cref="Mask"/> from masked states.
        /// </summary>
        /// <param name="maskedStates">The masked states.</param>
        public Mask(IEnumerable<bool> maskedStates)
        {
            this.MaskedStates = maskedStates.ToArray();
            this.Length = this.MaskedStates.Length;
            this.Confidence = new double[this.MaskedStates.Length];
            for (int i = 0; i < this.MaskedStates.Length; i++)
            {
                this.Confidence[i] = 1;
            }
        }

        /// <summary>
        /// Create a new <see cref="Mask"/> from masked states and confidence scores.
        /// </summary>
        /// <param name="maskAndScores">The masked states and scores.</param>
        public Mask(IReadOnlyList<(bool, double)> maskAndScores)
        {
            this.Length = maskAndScores.Count;
            this.MaskedStates = new bool[this.Length];
            this.Confidence = new double[this.Length];

            for (int i = 0; i < this.Length; i++)
            {
                MaskedStates[i] = maskAndScores[i].Item1;
                Confidence[i] = maskAndScores[i].Item2;
            }
        }

        /// <summary>
        /// Create a string representation of the mask, where columns to be preserved are represented by a '1' and columns to be discarded are represented by a '0'.
        /// </summary>
        /// <returns>A string representation of the mask.</returns>
        public override string ToString() => this.ToString(MaskType.Binary);

        /// <summary>
        /// Create a string representation of the mask in the specified format.
        /// </summary>
        /// <param name="maskType">The kind of output to produce.</param>
        /// <returns>A string representation of the mask.</returns>
        public string ToString(MaskType maskType = MaskType.Binary)
        {
            StringBuilder sb = new StringBuilder(this.Length);
            using (StringWriter sw = new StringWriter(sb))
            {
                this.Save(sw, maskType);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Saves the mask to the specified output file.
        /// </summary>
        /// <param name="outputFile">The output file.</param>
        /// <param name="maskType">The kind of output to produce.</param>
        public void Save(string outputFile, MaskType maskType = MaskType.Binary) => Utilities.SaveMask(outputFile, this, maskType);

        /// <summary>
        /// Saves the mask to the specified output <see cref="Stream"/>.
        /// </summary>
        /// <param name="outputStream">The output <see cref="Stream"/>.</param>
        /// <param name="maskType">The kind of output to produce.</param>
        /// <param name="leaveOpen">Whether the <see cref="Stream"/> should be left open after the mask has been written.</param>
        public void Save(Stream outputStream, MaskType maskType = MaskType.Binary, bool leaveOpen = false) => Utilities.SaveMask(outputStream, this, maskType);

        /// <summary>
        /// Saves the mask to the specified output <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="outputWriter">The output <see cref="TextWriter"/>.</param>
        /// <param name="maskType">The kind of output to produce.</param>
        public void Save(TextWriter outputWriter, MaskType maskType = MaskType.Binary) => Utilities.SaveMask(outputWriter, this, maskType);
    }
}
