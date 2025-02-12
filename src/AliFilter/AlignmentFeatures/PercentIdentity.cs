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
    /// Represents the frequency of the most common residue in the alignment column, excluding gaps.
    /// </summary>
    public class PercentIdentity : IFeature
    {
        /// <inheritdoc/>
        public string Name => "Percent identity";

        /// <inheritdoc/>
        public string ShortName => "P";

        /// <inheritdoc/>
        public string ShortNameForPlot => "P";

        /// <inheritdoc/>
        public string Description => "Frequency of the most common residue in the alignment column, excluding gaps.";

        /// <inheritdoc/>
        public double[] Compute(Alignment alignment, int maxParallelism = -1)
        {
            double[] tbr = new double[alignment.AlignmentLength];

            Parallel.For(0, alignment.AlignmentLength, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                Dictionary<char, int> counts = new Dictionary<char, int>();

                foreach (char c in alignment.GetColumn(i))
                {
                    if (c != '-')
                    {
                        if (!counts.TryGetValue(c, out int ct))
                        {
                            ct = 0;
                        }

                        counts[c] = ct + 1;
                    }
                }

                if (counts.Count == 0)
                {
                    tbr[i] = 0;
                }
                else
                {
                    tbr[i] = counts.Values.Max() / (double)alignment.SequenceCount;
                }
            });

            return tbr;
        }

        /// <summary>
        /// Create a new <see cref="PercentIdentity"/> instance.
        /// </summary>
        public PercentIdentity() { }
    }
}
