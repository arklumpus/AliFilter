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
    /// Represents the results of a logistic regression analysis.
    /// </summary>
    public class LogisticModel : IModel<double[]>
    {
        /// <summary>
        /// The coefficients of the logistic transformation.
        /// </summary>
        public double[] Coefficients { get; set; }

        /// <summary>
        /// The intercept of the logistic transformation.
        /// </summary>
        public double Intercept { get; set; }

        /// <summary>
        /// Create a new <see cref="LogisticModel"/>.
        /// </summary>
        public LogisticModel()
        {

        }

        /// <summary>
        /// Create a new <see cref="LogisticModel"/>.
        /// </summary>
        /// <param name="coefficients">The coefficients of the logistic transformation.</param>
        /// <param name="intercept">The intercept of the logistic transformation.</param>
        public LogisticModel(double[] coefficients, double intercept)
        {
            this.Coefficients = coefficients;
            this.Intercept = intercept;
        }

        /// <inheritdoc/>
        public (bool, double) Classify(double[] features)
        {
            double projected = Accord.Math.Matrix.Dot(Coefficients, features) + Intercept;

            double p = 1 / (1 + Math.Exp(-projected));

            return (p > 0.5, Math.Max(p, 1 - p));
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
        public double Score(double[] features)
        {
            return 1 / (1 + Math.Exp(-(Accord.Math.Matrix.Dot(Coefficients, features) + Intercept)));
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
    }
}
