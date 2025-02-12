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
    internal static partial class Utilities
    {
        /// <summary>
        /// Compute the accuracy metric.
        /// </summary>
        /// <param name="tp">Number of true positive assignments.</param>
        /// <param name="tn">Number of true negative assignments.</param>
        /// <param name="fp">Number of false positive assignments.</param>
        /// <param name="fn">Number of false negative assignments.</param>
        /// <returns>The accuracy metric.</returns>
        public static double ComputeAccuracy(int tp, int tn, int fp, int fn)
        {
            return ((double)tp + tn) / ((double)tp + tn + fp + fn);
        }

        /// <summary>
        /// Compute the Matthews correlation coefficient.
        /// </summary>
        /// <param name="tp">Number of true positive assignments.</param>
        /// <param name="tn">Number of true negative assignments.</param>
        /// <param name="fp">Number of false positive assignments.</param>
        /// <param name="fn">Number of false negative assignments.</param>
        /// <returns>The MCC.</returns>
        public static double ComputeMCC(int tp, int tn, int fp, int fn)
        {
            if (tn + fp + fn == 0 || tp + fp + fn == 0)
            {
                return 1;
            }
            else if (tp + tn + fn == 0 || tp + tn + fp == 0)
            {
                return 0;
            }
            else if (tp + fp == 0 || tp + fn == 0 || tn + fp == 0 || tn + fn == 0)
            {
                return 0;
            }
            else
            {
                return ((double)tp * tn - (double)fp * fn) / Math.Sqrt(((double)tp + fp) * ((double)tp + fn) * ((double)tn + fp) * ((double)tn + fn));
            }
        }

        /// <summary>
        /// Compute the F_beta score.
        /// </summary>
        /// <param name="beta">Beta parameter, which determines the balance between false positives and false negatives.</param>
        /// <param name="tp">Number of true positive assignments.</param>
        /// <param name="fp">Number of false positive assignments.</param>
        /// <param name="fn">Number of false negative assignments.</param>
        /// <returns>The F_beta score.</returns>
        public static double ComputeFBeta(double beta, int tp, int fp, int fn)
        {
            return (1 + beta * beta) * tp / ((1 + beta * beta) * tp + beta * beta * fn + fp);
        }
    }
}
