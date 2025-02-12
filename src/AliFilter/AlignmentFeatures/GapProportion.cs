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
    public class GapProportion : IFeature
    {
        /// <inheritdoc/>
        public string Name => "Gap proportion";

        /// <inheritdoc/>
        public string ShortName => "G~0~";

        /// <inheritdoc/>
        public string ShortNameForPlot => "G<sub>0</sub>";

        /// <inheritdoc/>
        public string Description => "Proportion of sequences that have a gap in the column.";

        /// <inheritdoc/>
        public double[] Compute(Alignment alignment, int maxParallelism = -1)
        {
            double[] tbr = new double[alignment.AlignmentLength];

            Parallel.For(0, alignment.AlignmentLength, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                tbr[i] = alignment.GetColumn(i).Count(x => x == '-') / (double)alignment.SequenceCount;
            });

            return tbr;
        }

        /// <summary>
        /// Create a new <see cref="GapProportion"/> instance.
        /// </summary>
        public GapProportion() { }
    }
}
