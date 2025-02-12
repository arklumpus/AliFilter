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
    /// Describes an alignment column feature.
    /// </summary>
    public interface IFeature
    {
        /// <summary>
        /// The full name of the feature.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The short name of the feature (to be used in Markdown text).
        /// </summary>
        public abstract string ShortName { get; }

        /// <summary>
        /// The short name of the feature (to be used in plots).
        /// </summary>
        public abstract string ShortNameForPlot { get; }

        /// <summary>
        /// A description of the feature.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Compute the value of the feature for all columns in the <paramref name="alignment"/>, using at most <paramref name="maxParallelism"/> threads.
        /// </summary>
        /// <param name="alignment">The alignment for which the feature should be computed.</param>
        /// <param name="maxParallelism">The maximum number of threads to use.</param>
        /// <returns>A <see langword="double"/>[] containing the value of the feature for each column in the alignment.</returns>
        public abstract double[] Compute(Alignment alignment, int maxParallelism = -1);
    }
}
