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

using AliFilter.AlignmentFeatures;

namespace AliFilter
{
    /// <summary>
    /// Contains utility methods to perform various tasks.
    /// </summary>
    internal static partial class Utilities
    {
        /// <summary>
        /// Compute column features for an alignment.
        /// </summary>
        /// <param name="alignment">The alignment for whose columns the features will be computed.</param>
        /// <param name="features">The features to be computed.</param>
        /// <param name="maxParallelism">The maximum degree of parallelism to use when computing the features.</param>
        /// <param name="outputLog">A <see cref="TextWriter"/> on which output is written, or <see langword="null"/> to disable output.</param>
        /// <returns>The features for each column of the <paramref name="alignment"/>.</returns>
        internal static double[][] ComputeFeatures(Alignment alignment, IReadOnlyList<IFeature> features, int maxParallelism, TextWriter outputLog)
        {
            outputLog?.WriteLine("Computing " + features.Count + " features for " + alignment.AlignmentLength + " alignment columns...");

            // Set up the progress bar.
            ProgressBar bar = null;
            if (outputLog != null)
            {
                bar = new ProgressBar(outputLog);
            }

            bar?.Start();
            // Compute the features.
            double[][] data = Features.ComputeAll(features, alignment, maxParallelism, new Progress<double>(x => bar?.Progress(x)));
            bar?.Finish();

            return data;
        }
    }
}
