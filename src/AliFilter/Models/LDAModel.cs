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

namespace AliFilter.Models
{
    /// <summary>
    /// Represents the results of a linear discriminant analysis.
    /// </summary>
    internal class LDAModel : IModel<double[]>
    {
        /// <summary>
        /// The coefficients of the transformation.
        /// </summary>
        public double[][] Coefficients { get; set; }

        /// <summary>
        /// The class means.
        /// </summary>
        public double[][] Means { get; set; }

        /// <summary>
        /// The discriminant proportions associated to each component.
        /// </summary>
        public double[] DiscriminantProportions { get; set; }

        /// <summary>
        /// The eigenvalues associated to each component.
        /// </summary>
        public double[] Eigenvalues { get; set; }

        /// <summary>
        /// The standard deviations associated to each component.
        /// </summary>
        public double[] StandardDeviations { get; set; }

        /// <summary>
        /// Create a new <see cref="LDAModel"/>.
        /// </summary>
        public LDAModel()
        {

        }

        /// <summary>
        /// Create a new <see cref="LDAModel"/>.
        /// </summary>
        /// <param name="coefficients">The coefficients of the transformation.</param>
        /// <param name="means">The class means.</param>
        /// <param name="discriminantProportions">The discriminant proportions associated to each component.</param>
        /// <param name="eigenValues">The eigenvalues associated to each component.</param>
        /// <param name="standardDeviations">The standard deviations associated to each component.</param>
        public LDAModel(double[][] coefficients, double[][] means, double[] discriminantProportions, double[] eigenValues, double[] standardDeviations)
        {
            Coefficients = coefficients;
            Means = means;
            DiscriminantProportions = discriminantProportions;
            Eigenvalues = eigenValues;
            StandardDeviations = standardDeviations;
        }

        /// <inheritdoc/>
        public (bool, double) Classify(double[] features)
        {
            double[] projected = Accord.Math.Matrix.Dot(features, Coefficients);
            double d0 = Accord.Math.Distance.Euclidean(projected, Means[0]);
            double d1 = Accord.Math.Distance.Euclidean(projected, Means[1]);

            return (d1 < d0, Math.Max(d0, d1) / (d0 + d1));
        }

        /// <inheritdoc/>
        public double Score(double[] features)
        {
            double[] projected = Accord.Math.Matrix.Dot(features, Coefficients);
            double d0 = Accord.Math.Distance.Euclidean(projected, Means[0]);
            double d1 = Accord.Math.Distance.Euclidean(projected, Means[1]);

            return d0 / (d0 + d1);
        }

        /// <inheritdoc/>
        public Mask Classify(IReadOnlyList<double[]> features, int maxParallelism = -1)
        {
            (bool, double)[] classification = new (bool, double)[features.Count];

            Parallel.For(0, features.Count, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                classification[i] = Classify(features[i]);
            });

            return new Mask(classification);
        }

        /// <inheritdoc/>
        public double[] Score(IReadOnlyList<double[]> features, int maxParallelism = -1)
        {
            double[] scores = new double[features.Count];

            Parallel.For(0, features.Count, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                scores[i] = Score(features[i]);
            });

            return scores;
        }

        /// <summary>
        /// Transforms the alignment features according to the linear discriminant model.
        /// </summary>
        /// <param name="features">The values of the features computed for the alignment column.</param>
        /// <returns>The transformed alignment features.</returns>
        public double[] Transform(double[] features)
        {
            return Accord.Math.Matrix.Dot(features, Coefficients);
        }

        /// <summary>
        /// Transforms the alignment features according to the linear discriminant model.
        /// </summary>
        /// <param name="features">The values of the features computed for the alignment columns.</param>
        /// <param name="maxParallelism">Maximum number of threads to use.</param>
        /// <returns>The transformed alignment features.</returns>
        public double[][] Transform(IReadOnlyList<double[]> features, int maxParallelism = -1)
        {
            double[][] tbr = new double[features.Count][];

            Parallel.For(0, features.Count, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                tbr[i] = this.Transform(features[i]);
            });

            return tbr;
        }
    }
}
