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
    /// Represents the number of residues between the column and the closest extremity (start or end) of the alignment.
    /// </summary>
    public class DistanceFromExtremity : IFeature
    {
        /// <inheritdoc/>
        public string Name => "Distance from extremity";

        /// <inheritdoc/>
        public string ShortName => "D";

        /// <inheritdoc/>
        public string ShortNameForPlot => "D";

        /// <inheritdoc/>
        public string Description => "Number of residues between the column and the closest extremity (start or end) of the alignment.";

        /// <inheritdoc/>
        public double[] Compute(Alignment alignment, int maxParallelism = -1)
        {
            double[] tbr = new double[alignment.AlignmentLength];

            Parallel.For(0, alignment.AlignmentLength, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                tbr[i] = Math.Min(i, alignment.AlignmentLength - 1 - i);
            });

            return tbr;
        }

        /// <summary>
        /// Create a new <see cref="DistanceFromExtremity"/> instance.
        /// </summary>
        public DistanceFromExtremity() { }
    }
}
