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

namespace AliFilter
{
    /// <summary>
    /// Represents a confusion matrix.
    /// </summary>
    public class ConfusionMatrix
    {
        /// <summary>
        /// Number of true positives.
        /// </summary>
        public int TruePositives { get; }

        /// <summary>
        /// Number of false positives.
        /// </summary>
        public int FalsePositives { get; }

        /// <summary>
        /// Number of true negatives.
        /// </summary>
        public int TrueNegatives { get; }

        /// <summary>
        /// Number of false negatives.
        /// </summary>
        public int FalseNegatives { get; }

        /// <summary>
        /// Accuracy metric.
        /// </summary>
        public double Accuracy => Utilities.ComputeAccuracy(TruePositives, TrueNegatives, FalsePositives, FalseNegatives);

        /// <summary>
        /// Matthews correlation coefficient.
        /// </summary>
        public double MCC => Utilities.ComputeMCC(TruePositives, TrueNegatives, FalsePositives, FalseNegatives);

        /// <summary>
        /// F_beta score.
        /// </summary>
        /// <param name="beta">The value of the beta parameter.</param>
        /// <returns>The F_beta score.</returns>
        public double F_beta(double beta) => Utilities.ComputeFBeta(beta, TruePositives, FalsePositives, FalseNegatives);

        /// <summary>
        /// F_1 score (see <see cref="F_beta(double)"/> for the F_beta score with other values of beta.
        /// </summary>
        public double F_1 => F_beta(1);

        /// <summary>
        /// True positive rate.
        /// </summary>
        public double TPR => TruePositives == 0 ? 0 : ((double)TruePositives / (TruePositives + FalseNegatives));

        /// <summary>
        /// Positive predictive value.
        /// </summary>
        public double PPV => TruePositives == 0 ? 0 : ((double)TruePositives / (TruePositives + FalsePositives));

        /// <summary>
        /// False positive rate.
        /// </summary>
        public double FPR => TruePositives == 0 ? 0 : ((double)FalsePositives / (FalsePositives + TrueNegatives));

        /// <summary>
        /// Create a new <see cref="ConfusionMatrix"/> from the specified number of false and true positives and negatives.
        /// </summary>
        /// <param name="truePositives">The number of true positives.</param>
        /// <param name="falsePositives">The number of false positives.</param>
        /// <param name="trueNegatives">The number of true negatives.</param>
        /// <param name="falseNegatives">The number of false negatives.</param>
        public ConfusionMatrix(int truePositives, int falsePositives, int trueNegatives, int falseNegatives)
        {
            TruePositives = truePositives;
            FalsePositives = falsePositives;
            TrueNegatives = trueNegatives;
            FalseNegatives = falseNegatives;
        }


        /// <summary>
        /// Compute a confusion matrix by comparing two <see cref="Mask"/>s.
        /// </summary>
        /// <param name="predictedAssignments">The <see cref="Mask"/> containing the predicted assignments.</param>
        /// <param name="trueAssignments">The <see cref="Mask"/> containing the "true" assignments.</param>
        /// <exception cref="ArgumentException">Thrown if the two <see cref="Mask"/>s have different lengths.</exception>
        public ConfusionMatrix(Mask predictedAssignments, Mask trueAssignments)
        {
            if (predictedAssignments.Length != trueAssignments.Length)
            {
                throw new ArgumentException("The two masks must have the same length!");
            }

            this.TruePositives = 0;
            this.FalsePositives = 0;
            this.TrueNegatives = 0;
            this.FalseNegatives = 0;

            for (int i = 0; i < predictedAssignments.Length; i++)
            {
                switch ((predictedAssignments.MaskedStates[i], trueAssignments.MaskedStates[i]))
                {
                    case (false, false):
                        this.TrueNegatives++;
                        break;
                    case (false, true):
                        this.FalseNegatives++;
                        break;
                    case (true, false):
                        this.FalsePositives++;
                        break;
                    case (true, true):
                        this.TruePositives++;
                        break;
                }
            }
        }
    }
}
