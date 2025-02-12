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
    /// Represents the Shannon entropy for the residue frequencies in the column, excluding gaps.
    /// </summary>
    public class Entropy : IFeature
    {
        /// <inheritdoc/>
        public string Name => "Entropy";

        /// <inheritdoc/>
        public string ShortName => "E";

        /// <inheritdoc/>
        public string ShortNameForPlot => "E";

        /// <inheritdoc/>
        public string Description => "Shannon entropy for the residue frequencies in the column, excluding gaps.";

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
                    double tot = counts.Values.Sum();

                    tbr[i] = 0;
                    foreach (double d in counts.Values)
                    {
                        tbr[i] += -d / tot * Math.Log(d / tot);
                    }
                }
            });

            return tbr;
        }

        /// <summary>
        /// Create a new <see cref="Entropy"/> instance.
        /// </summary>
        public Entropy() { }
    }
}
