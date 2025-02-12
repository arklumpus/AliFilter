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

namespace AliFilter.AlignmentFeatures
{
    /// <summary>
    /// Represents the proportion of sequences that have a gap in the column.
    /// </summary>
    public class SurroundingGapProportion : IFeature
    {
        /// <summary>
        /// The number of columns on each side of the subject column to consider.
        /// </summary>
        public int SurroundingAmount { get; }

        /// <summary>
        /// <see cref="AlignmentFeatures.GapProportion"/> instance used to compute the gap proportion for individual columns.
        /// </summary>
        private GapProportion GapProportion { get; } = new GapProportion();

        /// <inheritdoc/>
        public string Name => "Gap proportion (±" + SurroundingAmount.ToString() + ")";

        /// <inheritdoc/>
        public string ShortName => "G~" + SurroundingAmount.ToString() + "~";

        /// <inheritdoc/>
        public string ShortNameForPlot => "G<sub>" + SurroundingAmount.ToString() + "</sub>";

        /// <inheritdoc/>
        public string Description => "Average of the proportion of gaps between the column, " + SurroundingAmount.ToString() + " preceding column(s), and " + SurroundingAmount.ToString() + " subsequent columns.";

        /// <inheritdoc/>
        public double[] Compute(Alignment alignment, int maxParallelism = -1)
        {
            double[] individualGaps = GapProportion.Compute(alignment, maxParallelism);

            double[] tbr = new double[alignment.AlignmentLength];

            Parallel.For(0, alignment.AlignmentLength, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                int count = 0;

                for (int j = Math.Max(0, i - this.SurroundingAmount); j <= Math.Min(alignment.AlignmentLength - 1, i + this.SurroundingAmount); j++)
                {
                    tbr[i] += individualGaps[j];
                    count++;
                }

                tbr[i] /= count;
            });

            return tbr;
        }

        /// <summary>
        /// Create a new <see cref="SurroundingGapProportion"/> instance.
        /// </summary>
        /// <param name="surroundingAmount">The number of columns on each side of the subject column to consider.</param>
        public SurroundingGapProportion(int surroundingAmount)
        {
            this.SurroundingAmount = surroundingAmount;
        }
    }

}
