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

using AliFilter.Models;

namespace AliFilter
{
    internal static partial class Tasks
    {
        // Test a model using the specified test data.
        public static int TestModel(Arguments arguments, TextWriter outputLog)
        {
            // Deserialize model.
            ValidatedModel model = Utilities.ReadModel<ValidatedModel>(arguments, outputLog);
            outputLog?.WriteLine();

            // Deserialize alignment features.
            (double[][] features, IReadOnlyList<double[,]> bootstrapReplicates, Mask mask) = Utilities.ReadFeatures(arguments, outputLog);

            // Model parameters.
            double threshold = arguments.Threshold ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateThreshold, DefaultParameters.Best => model.BestThreshold, DefaultParameters.Fast => model.FastThreshold, _ => model.FastThreshold };
            int bootstrapReplicateCount = arguments.BootstrapReplicates ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateBootstrapReplicates, DefaultParameters.Best => model.BestBootstrapReplicates, DefaultParameters.Fast => 0, _ => 0 };
            double bootstrapThreshold = arguments.BootstrapThreshold ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateBootstrapThreshold, DefaultParameters.Best => model.BestBootstrapThreshold, DefaultParameters.Fast => 0.5, _ => 0.5 };

            return TestModelCommon(arguments, outputLog, features, bootstrapReplicates, mask, model, threshold, bootstrapReplicateCount, bootstrapThreshold);
        }

        // Test a model using a single alignment + mask.
        public static int TestModelOnAlignment(Arguments arguments, TextWriter outputLog)
        {
            // Deserialize model.
            ValidatedModel model = Utilities.ReadModel<ValidatedModel>(arguments, outputLog);

            outputLog?.WriteLine();

            // Model parameters.
            double threshold = arguments.Threshold ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateThreshold, DefaultParameters.Best => model.BestThreshold, DefaultParameters.Fast => model.FastThreshold, _ => model.FastThreshold };
            int bootstrapReplicateCount = arguments.BootstrapReplicates ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateBootstrapReplicates, DefaultParameters.Best => model.BestBootstrapReplicates, DefaultParameters.Fast => 0, _ => 0 };
            double bootstrapThreshold = arguments.BootstrapThreshold ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateBootstrapThreshold, DefaultParameters.Best => model.BestBootstrapThreshold, DefaultParameters.Fast => 0.5, _ => 0.5 };

            (Mask mask, double[,,] data)? computedFeatures = Tasks.ComputeFeaturesCommon(arguments, outputLog, bootstrapReplicateCount);

            if (computedFeatures == null)
            {
                return 1;
            }

            double[][] features = new double[computedFeatures.Value.mask.Length][];
            double[][,] bootstrapReplicates = new double[computedFeatures.Value.mask.Length][,];

            for (int i = 0; i < features.Length; i++)
            {
                features[i] = new double[computedFeatures.Value.data.GetLength(1)];
                for (int j = 0; j < features[i].Length; j++)
                {
                    features[i][j] = computedFeatures.Value.data[i, j, 0];
                }
            }

            for (int i = 0; i < computedFeatures.Value.mask.Length; i++)
            {
                bootstrapReplicates[i] = new double[computedFeatures.Value.data.GetLength(1), bootstrapReplicateCount];
                for (int j = 0; j < computedFeatures.Value.data.GetLength(1); j++)
                {
                    for (int k = 0; k < bootstrapReplicateCount; k++)
                    {
                        bootstrapReplicates[i][j, k] = computedFeatures.Value.data[i, j, k + 1];
                    }
                }
            }

            return TestModelCommon(arguments, outputLog, features, bootstrapReplicates, computedFeatures.Value.mask, model, threshold, bootstrapReplicateCount, bootstrapThreshold);
        }
        
        // Common parts of model testing.
        private static int TestModelCommon(Arguments arguments, TextWriter outputLog, double[][] features, IReadOnlyList<double[,]> bootstrapReplicates, Mask mask, ValidatedModel model, double threshold, int bootstrapReplicateCount, double bootstrapThreshold)
        {
            outputLog?.WriteLine("Applying model to " + features.Length + " alignment columns...");

            // Use the model to classify the alignment columns.
            Mask predictedMask = model.Classify(features, bootstrapReplicates, threshold, bootstrapReplicateCount, bootstrapThreshold, arguments.MaxParallelism);

            // Number of preserved columns.
            int trues = predictedMask.MaskedStates.Count(x => x);

            outputLog?.WriteLine("Mask contains " + trues.ToString() + " (" + ((double)trues / predictedMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") preserved columns and " + (predictedMask.Length - trues).ToString() + " (" + (1 - (double)trues / predictedMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") deleted columns.");
            outputLog?.WriteLine();

            // Compute number of true and false positives and negatives.
            int truePositives = 0;
            int trueNegatives = 0;
            int falsePositives = 0;
            int falseNegatives = 0;

            for (int i = 0; i < predictedMask.Length; i++)
            {
                if (predictedMask.MaskedStates[i] && mask.MaskedStates[i])
                {
                    truePositives++;
                }
                else if (predictedMask.MaskedStates[i] && !mask.MaskedStates[i])
                {
                    falsePositives++;
                }
                else if (!predictedMask.MaskedStates[i] && mask.MaskedStates[i])
                {
                    falseNegatives++;
                }
                else
                {
                    trueNegatives++;
                }
            }

            // Maximum cell size for the table.
            int maxLength = Math.Max(Math.Max(Math.Max(truePositives, trueNegatives), Math.Max(falsePositives, falseNegatives)).ToString().Length, 8);

            // Display the table.

            outputLog?.Write(new string(' ', 13));
            outputLog?.Write('╔');
            outputLog?.Write(new string('═', (maxLength + 5) * 2 - 1));
            outputLog?.Write('╦');
            outputLog?.Write(new string('═', (maxLength + 5) * 2 - 1));
            outputLog?.WriteLine('╗');

            outputLog?.Write(new string(' ', 13));
            outputLog?.Write('║');
            outputLog?.Write("True".PadCenter((maxLength + 5) * 2 - 1));
            outputLog?.Write('║');
            outputLog?.Write("False".PadCenter((maxLength + 5) * 2 - 1));
            outputLog?.WriteLine('║');

            outputLog?.Write(new string(' ', 13));
            outputLog?.Write('╟');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.Write('┬');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.Write('╫');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.Write('┬');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.WriteLine('╢');

            outputLog?.Write(new string(' ', 13));
            outputLog?.Write('║');
            outputLog?.Write("Positive".PadCenter(maxLength + 4));
            outputLog?.Write('│');
            outputLog?.Write("Negative".PadCenter(maxLength + 4));
            outputLog?.Write('║');
            outputLog?.Write("Positive".PadCenter(maxLength + 4));
            outputLog?.Write('│');
            outputLog?.Write("Negative".PadCenter(maxLength + 4));
            outputLog?.WriteLine('║');

            outputLog?.Write('╔');
            outputLog?.Write(new string('═', 12));
            outputLog?.Write('╬');
            outputLog?.Write(new string('═', maxLength + 4));
            outputLog?.Write('╪');
            outputLog?.Write(new string('═', maxLength + 4));
            outputLog?.Write('╬');
            outputLog?.Write(new string('═', maxLength + 4));
            outputLog?.Write('╪');
            outputLog?.Write(new string('═', maxLength + 4));
            outputLog?.WriteLine('╣');

            outputLog?.Write("║  Absolute  ║  ");
            outputLog?.Write(truePositives.ToString().PadLeft(maxLength, ' '));
            outputLog?.Write("  │  ");
            outputLog?.Write(trueNegatives.ToString().PadLeft(maxLength, ' '));
            outputLog?.Write("  ║  ");
            outputLog?.Write(falsePositives.ToString().PadLeft(maxLength, ' '));
            outputLog?.Write("  │  ");
            outputLog?.Write(falseNegatives.ToString().PadLeft(maxLength, ' '));
            outputLog?.WriteLine("  ║");

            outputLog?.Write('╟');
            outputLog?.Write(new string('─', 12));
            outputLog?.Write('╫');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.Write('┼');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.Write('╫');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.Write('┼');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.WriteLine('╢');

            outputLog?.Write("║  Relative  ║  ");
            outputLog?.Write(((double)truePositives / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture).PadLeft(maxLength, ' '));
            outputLog?.Write("  │  ");
            outputLog?.Write(((double)trueNegatives / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture).PadLeft(maxLength, ' '));
            outputLog?.Write("  ║  ");
            outputLog?.Write(((double)falsePositives / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture).PadLeft(maxLength, ' '));
            outputLog?.Write("  │  ");
            outputLog?.Write(((double)falseNegatives / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture).PadLeft(maxLength, ' '));
            outputLog?.WriteLine("  ║");

            outputLog?.Write('╟');
            outputLog?.Write(new string('─', 12));
            outputLog?.Write('╫');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.Write('┼');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.Write('╫');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.Write('┼');
            outputLog?.Write(new string('─', maxLength + 4));
            outputLog?.WriteLine('╢');

            outputLog?.Write("║      Rate  ║  ");
            outputLog?.Write(((double)truePositives / (truePositives + falseNegatives)).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture).PadLeft(maxLength, ' '));
            outputLog?.Write("  │  ");
            outputLog?.Write((trueNegatives + falsePositives == 0 ? 0 : (double)trueNegatives / (trueNegatives + falsePositives)).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture).PadLeft(maxLength, ' '));
            outputLog?.Write("  ║  ");
            outputLog?.Write((trueNegatives + falsePositives == 0 ? 0 : (double)falsePositives / (falsePositives + trueNegatives)).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture).PadLeft(maxLength, ' '));
            outputLog?.Write("  │  ");
            outputLog?.Write(((double)falseNegatives / (falseNegatives + truePositives)).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture).PadLeft(maxLength, ' '));
            outputLog?.WriteLine("  ║");

            outputLog?.Write('╚');
            outputLog?.Write(new string('═', 12));
            outputLog?.Write('╩');
            outputLog?.Write(new string('═', maxLength + 4));
            outputLog?.Write('╧');
            outputLog?.Write(new string('═', maxLength + 4));
            outputLog?.Write('╩');
            outputLog?.Write(new string('═', maxLength + 4));
            outputLog?.Write('╧');
            outputLog?.Write(new string('═', maxLength + 4));
            outputLog?.WriteLine('╝');

            // Compute the ROC curve.
            int steps = 1000;

            double minDistance = double.MaxValue;
            double thresholdFpr = -1;
            double thresholdTpr = -1;
            double thresholdPrecision = -1;

            double targetThreshold = bootstrapReplicateCount == 0 ? threshold : bootstrapThreshold;

            List<(double fpr, double tpr)> rocCurve = new List<(double fpr, double tpr)>();

            for (int i = 0; i <= steps; i++)
            {
                double thr = (double)i / steps;

                int tn = 0;
                int tp = 0;
                int fn = 0;
                int fp = 0;

                for (int j = 0; j < mask.Length; j++)
                {
                    bool label = mask.MaskedStates[j];
                    double score = predictedMask.MaskedStates[j] ? predictedMask.Confidence[j] : (1 - predictedMask.Confidence[j]);

                    if (label && score > thr)
                    {
                        tp++;
                    }
                    else if (!label && score > thr)
                    {
                        fp++;
                    }
                    else if (label && score <= thr)
                    {
                        fn++;
                    }
                    else
                    {
                        tn++;
                    }
                }

                double fpr = (double)fp / (fp + tn);
                double tpr = (double)tp / (tp + fn);

                if (fp + tn == 0)
                {
                    fpr = 0;
                }

                if (tp + fn == 0)
                {
                    tpr = 0;
                }

                if (Math.Abs(thr - targetThreshold) < minDistance)
                {
                    thresholdFpr = fpr;
                    thresholdTpr = tpr;
                    thresholdPrecision = (double)tp / (tp + fp);
                    minDistance = Math.Abs(thr - targetThreshold);
                }

                rocCurve.Add((fpr, tpr));
            }

            rocCurve.Add((0, 0));
            rocCurve.Add((1, 1));

            rocCurve.Sort((a, b) =>
            {
                int sign1 = Math.Sign(a.fpr - b.fpr);

                if (sign1 != 0)
                {
                    return sign1;
                }
                else
                {
                    return Math.Sign(a.tpr - b.tpr);
                }

            });

            // Compute AUC.
            double auc = 0;
            for (int i = 0; i < rocCurve.Count - 1; i++)
            {
                auc += (rocCurve[i + 1].fpr - rocCurve[i].fpr) * (rocCurve[i + 1].tpr + rocCurve[i].tpr) * 0.5;
            }

            outputLog?.WriteLine();
            outputLog?.Flush();

            Console.Out.WriteLine("# Confusion matrix");
            Console.Out.WriteLine("TP\t{0}", truePositives);
            Console.Out.WriteLine("TN\t{0}", trueNegatives);
            Console.Out.WriteLine("FP\t{0}", falsePositives);
            Console.Out.WriteLine("FN\t{0}", falseNegatives);
            Console.Out.Flush();

            if (Console.IsOutputRedirected)
            {
                outputLog?.WriteLine("# Confusion matrix");
                outputLog?.WriteLine("TP\t{0}", truePositives);
                outputLog?.WriteLine("TN\t{0}", trueNegatives);
                outputLog?.WriteLine("FP\t{0}", falsePositives);
                outputLog?.WriteLine("FN\t{0}", falseNegatives);
            }

            outputLog?.WriteLine();
            outputLog?.Flush();

            // Compute F_beta scores.
            double f05 = Utilities.ComputeFBeta(0.5, truePositives, falsePositives, falseNegatives);
            double f1 = Utilities.ComputeFBeta(1, truePositives, falsePositives, falseNegatives);
            double f2 = Utilities.ComputeFBeta(2, truePositives, falsePositives, falseNegatives);

            // Compute accuracy.
            double accuracy = Utilities.ComputeAccuracy(truePositives, trueNegatives, falsePositives, falseNegatives);

            // Compute MCC.
            double mcc = Utilities.ComputeMCC(truePositives, trueNegatives, falsePositives, falseNegatives);

            // Compute the model confidence score.
            double modelConfidence = 1 - predictedMask.Confidence.Select(x => x * (1 - x) * 4).Sum() / predictedMask.Length;

            // Compute rates.
            double modelTpr = truePositives == 0 ? 0 : ((double)truePositives / (truePositives + falseNegatives));
            double modelPpv = truePositives == 0 ? 0 : ((double)truePositives / (truePositives + falsePositives));
            double modelFpr = falsePositives == 0 ? 0 : ((double)falsePositives / (falsePositives + trueNegatives));

            Console.Out.WriteLine("# Performance metrics");
            Console.Out.WriteLine("A\t{0}", accuracy.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            Console.Out.WriteLine("MCC\t{0}", mcc.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            Console.Out.WriteLine("TPR\t{0}", modelTpr.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            Console.Out.WriteLine("PPV\t{0}", modelPpv.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            Console.Out.WriteLine("FPR\t{0}", modelFpr.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            Console.Out.WriteLine("F_0.5\t{0}", f05.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            Console.Out.WriteLine("F_1\t{0}", f1.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            Console.Out.WriteLine("F_2\t{0}", f2.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            Console.Out.WriteLine("AUC\t{0}", auc.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            Console.Out.WriteLine("C\t{0}", modelConfidence.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            Console.Out.Flush();

            if (Console.IsOutputRedirected)
            {
                outputLog?.WriteLine("# Performance metrics");
                outputLog?.WriteLine("A\t{0}", accuracy.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                outputLog?.WriteLine("MCC\t{0}", mcc.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                outputLog?.WriteLine("TPR\t{0}", modelTpr.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                outputLog?.WriteLine("PPV\t{0}", modelPpv.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                outputLog?.WriteLine("FPR\t{0}", modelFpr.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                outputLog?.WriteLine("F_0.5\t{0}", f05.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                outputLog?.WriteLine("F_1\t{0}", f1.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                outputLog?.WriteLine("F_2\t{0}", f2.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                outputLog?.WriteLine("AUC\t{0}", auc.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                outputLog?.WriteLine("C\t{0}", modelConfidence.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            }

            outputLog?.WriteLine();

            if (arguments.ReportFile != null)
            {
                return Utilities.CreateModelTestReport(features, mask, model, predictedMask, threshold, bootstrapThreshold, bootstrapReplicateCount, rocCurve, thresholdFpr, thresholdTpr, thresholdPrecision, auc, arguments, outputLog);
            }
            else
            {
                return 0;
            }
        }
    }
}
