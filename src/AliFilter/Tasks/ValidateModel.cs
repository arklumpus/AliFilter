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
using System.Text.Json;

namespace AliFilter
{
    internal static partial class Tasks
    {
        // Validate a model using the specified validation data.
        public static int ValidateModel(Arguments arguments, TextWriter outputLog)
        {
            // Deserialize model.
            FullModel model = Utilities.ReadModel<FullModel>(arguments, outputLog);

            outputLog?.WriteLine();

            // Deserialize alignment features.
            (double[][] features, IReadOnlyList<double[,]> bootstrapReplicates, Mask mask) = Utilities.ReadFeatures(arguments, outputLog);

            if (bootstrapReplicates.Count != features.Length)
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("The validation data does not contain bootstrap replicates for all alignment columns!");
                outputLog?.WriteLine();
                return 1;
            }

            outputLog?.Write("Optimisation target: ");
            Func<int, int, int, int, double> targetMetric;
            string targetScoreHeader = "";

            switch (arguments.OptimisationTarget)
            {
                case OptimisationTarget.Accuracy:
                    outputLog?.WriteLine("Accuracy.");
                    targetMetric = Utilities.ComputeAccuracy;
                    targetScoreHeader = "Accuracy";
                    break;
                case OptimisationTarget.MCC:
                    outputLog?.WriteLine("Matthews Correlation Coefficient.");
                    targetMetric = Utilities.ComputeMCC;
                    targetScoreHeader = "   MCC  ";
                    break;
                case OptimisationTarget.FBeta:
                    outputLog?.WriteLine("F_" + arguments.Beta.ToString(System.Globalization.CultureInfo.InvariantCulture) + " score.");
                    targetMetric = (tp, tn, fp, fn) => Utilities.ComputeFBeta(arguments.Beta, tp, fp, fn);
                    targetScoreHeader = "F_" + arguments.Beta.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture).Substring(0, Math.Min(arguments.Beta.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture).Length, 6));
                    if (targetScoreHeader.Length < 8)
                    {
                        targetScoreHeader = new string(' ', 8 - targetScoreHeader.Length - (8 - targetScoreHeader.Length) / 2) + targetScoreHeader + new string(' ', (8 - targetScoreHeader.Length) / 2);
                    }
                    break;
                default:
                    outputLog?.WriteLine("Unknown!");
                    return 1;
            }

            outputLog?.WriteLine();

            outputLog?.WriteLine("Validating model and tuning hyperparameters...");
            outputLog?.WriteLine();
            outputLog?.WriteLine("The following parameters will be tuned:");
            outputLog?.WriteLine("  • Logistic model threshold (range: 0.0 - 1.0)");
            outputLog?.WriteLine("  • Number of bootstrap replicates (range: 0, 100 - " + arguments.Features.MaxBootstrapReplicates + ")");
            outputLog?.WriteLine("  • Bootstrap threshold (range: 0.0 - 1.0)");

            outputLog?.WriteLine();

            outputLog?.WriteLine("Applying model to " + features.Length + " alignment columns, with up to " + arguments.Features.MaxBootstrapReplicates + " bootstrap replicates.");

            outputLog?.WriteLine();

            double[][] columnScores = new double[arguments.Features.MaxBootstrapReplicates + 1][];

            // Use the model to classify the alignment columns for each bootstrap replicate.
            Parallel.For(0, arguments.Features.MaxBootstrapReplicates + 1, new ParallelOptions() { MaxDegreeOfParallelism = arguments.MaxParallelism }, i =>
            {
                if (i == 0)
                {
                    columnScores[i] = model.Score(features, 1);
                }
                else
                {
                    double[][] currReplicate = new double[features.Length][];

                    for (int j = 0; j < features.Length; j++)
                    {
                        currReplicate[j] = new double[features[j].Length];

                        for (int k = 0; k < features[j].Length; k++)
                        {
                            currReplicate[j][k] = bootstrapReplicates[j][k, i - 1];
                        }
                    }

                    columnScores[i] = model.Score(currReplicate, 1);
                }
            });

            outputLog?.WriteLine("Analysing results...");

            // Number of threshold values between 0 and 1 to test.
            int thresholdRangeSteps = 101;

            // Set up the array to hold the F_β-score values.
            double[][][] scores = new double[thresholdRangeSteps][][];

            for (int i = 0; i < scores.Length; i++)
            {
                scores[i] = new double[(arguments.Features.MaxBootstrapReplicates - 100) / 100 + 2][];
                scores[i][0] = new double[1];

                for (int j = 1; j < scores[i].Length; j++)
                {
                    scores[i][j] = new double[thresholdRangeSteps];
                }
            }

            // Set up the progress bar.
            ProgressBar bar = null;
            if (outputLog != null)
            {
                bar = new ProgressBar(outputLog);
            }
            bar?.Start();
            object progressLock = new object();
            int completed = 0;

            // Compute the target score for each combination of thresholds and number of bootstrap replicates.
            Parallel.For(0, scores.Length, new ParallelOptions() { MaxDegreeOfParallelism = arguments.MaxParallelism }, i =>
            {
                // Threshold for the logistic model.
                double threshold = (double)i / (thresholdRangeSteps - 1);

                for (int j = 0; j < scores[i].Length; j++)
                {
                    // First element, no bootstrap replicates.
                    if (j == 0)
                    {
                        int tp = 0; // True positives
                        int fp = 0; // False positive
                        int tn = 0; // True negatives
                        int fn = 0; // False negatives

                        for (int m = 0; m < columnScores[0].Length; m++)
                        {
                            if (columnScores[0][m] > threshold && mask.MaskedStates[m])
                            {
                                tp++;
                            }
                            else if (columnScores[0][m] > threshold && !mask.MaskedStates[m])
                            {
                                fp++;
                            }
                            else if (columnScores[0][m] <= threshold && !mask.MaskedStates[m])
                            {
                                tn++;
                            }
                            else if (columnScores[0][m] <= threshold && mask.MaskedStates[m])
                            {
                                fn++;
                            }
                        }

                        // Target score.
                        scores[i][j][0] = targetMetric(tp, tn, fp, fn);
                    }
                    else
                    {
                        // Number of bootstrap replicates.
                        int bootstrapReplicateCount = (j - 1) * 100 + 100;

                        // Number of bootstrap replicates above the threshold.
                        double[] proportionAboveThreshold = new double[columnScores[0].Length];

                        for (int m = 0; m < columnScores[0].Length; m++)
                        {
                            for (int l = 0; l < bootstrapReplicateCount; l++)
                            {
                                if (columnScores[l + 1][m] > threshold)
                                {
                                    proportionAboveThreshold[m]++;
                                }
                            }
                        }

                        for (int m = 0; m < columnScores[0].Length; m++)
                        {
                            proportionAboveThreshold[m] /= bootstrapReplicateCount;
                        }

                        for (int k = 0; k < scores[i][j].Length; k++)
                        {
                            // Threshold for the bootstrap replicates.
                            double bootstrapThreshold = (double)k / (thresholdRangeSteps - 1);

                            int tp = 0; // True positives
                            int fp = 0; // False positive
                            int tn = 0; // True negatives
                            int fn = 0; // False negatives

                            for (int m = 0; m < columnScores[0].Length; m++)
                            {
                                if (proportionAboveThreshold[m] > bootstrapThreshold && mask.MaskedStates[m])
                                {
                                    tp++;
                                }
                                else if (proportionAboveThreshold[m] > bootstrapThreshold && !mask.MaskedStates[m])
                                {
                                    fp++;
                                }
                                else if (proportionAboveThreshold[m] <= bootstrapThreshold && !mask.MaskedStates[m])
                                {
                                    tn++;
                                }
                                else if (proportionAboveThreshold[m] <= bootstrapThreshold && mask.MaskedStates[m])
                                {
                                    fn++;
                                }
                            }

                            // Target score.
                            scores[i][j][k] = targetMetric(tp, tn, fp, fn);
                        }
                    }

                    // Report progress.
                    lock (progressLock)
                    {
                        completed++;
                        bar?.Progress((double)completed / (scores.Length * ((arguments.Features.MaxBootstrapReplicates - 100) / 100 + 2)));
                    }
                }
            });

            bar?.Finish();

            // Maximum scores by number of replicates.
            (double maxTargetScore, double threshold, double bootstrapThreshold, double finalScore)[] maximumScoresByBootstrapReplicates = new (double maxTargetScore, double threshold, double bootstrapThreshold, double finalScore)[(arguments.Features.MaxBootstrapReplicates - 100) / 100 + 2];

            // Overall best score, after considering computational effort.
            double bestOverallScore = double.MinValue;
            int bestScoreIndex = -1;

            // Overall best target score, regardless of computational effort.
            double bestOverallTargetScore = double.MinValue;
            int bestTargetScoreIndex = -1;

            // Find the best scores.
            for (int j = 0; j < maximumScoresByBootstrapReplicates.Length; j++)
            {
                maximumScoresByBootstrapReplicates[j] = (double.MinValue, 0.5, 0.5, double.MinValue);

                for (int i = 1; i < thresholdRangeSteps - 1; i++)
                {
                    if (j == 0)
                    {
                        if (scores[i][j][0] > maximumScoresByBootstrapReplicates[j].maxTargetScore)
                        {
                            maximumScoresByBootstrapReplicates[j] = (scores[i][j][0], (double)i / (thresholdRangeSteps - 1), 0.5, scores[i][j][0]);
                        }
                    }
                    else
                    {
                        int bootstrapReplicateCount = (j - 1) * 100 + 100;

                        for (int k = 1; k < thresholdRangeSteps - 1; k++)
                        {
                            if (scores[i][j][k] > maximumScoresByBootstrapReplicates[j].maxTargetScore)
                            {
                                maximumScoresByBootstrapReplicates[j] = (scores[i][j][k], (double)i / (thresholdRangeSteps - 1), (double)k / (thresholdRangeSteps - 1), scores[i][j][k] - bootstrapReplicateCount * arguments.EffortPenalty * 0.01);
                            }
                        }
                    }
                }

                if (maximumScoresByBootstrapReplicates[j].maxTargetScore > bestOverallTargetScore)
                {
                    bestTargetScoreIndex = j;
                    bestOverallTargetScore = maximumScoresByBootstrapReplicates[j].maxTargetScore;
                }

                if (maximumScoresByBootstrapReplicates[j].finalScore > bestOverallScore)
                {
                    bestScoreIndex = j;
                    bestOverallScore = maximumScoresByBootstrapReplicates[j].finalScore;
                }
            }

            // Print the table with the best scores.
            outputLog?.WriteLine("╔═════════════════╦══════════╦══════════╦═══════════╦══════════════╗");
            outputLog?.WriteLine("║ # BS replicates ║  Score   ║ " + targetScoreHeader + " ║ Threshold ║ BS threshold ║");
            outputLog?.WriteLine("╟─────────────────╫──────────╫──────────╫───────────╫──────────────╢");

            for (int j = 0; j < maximumScoresByBootstrapReplicates.Length; j++)
            {
                int bootstrapReplicateCount = (j - 1) * 100 + 100;

                outputLog?.Write("║");
                outputLog?.Write(bootstrapReplicateCount.ToString().PadCenter(17));
                outputLog?.Write("║");
                outputLog?.Write(maximumScoresByBootstrapReplicates[j].finalScore.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture).PadLeft(9));
                outputLog?.Write(" ║");
                outputLog?.Write(maximumScoresByBootstrapReplicates[j].maxTargetScore.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture).PadLeft(9));
                outputLog?.Write(" ║");
                outputLog?.Write(maximumScoresByBootstrapReplicates[j].threshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).PadLeft(10));
                outputLog?.Write(" ║");
                outputLog?.Write(maximumScoresByBootstrapReplicates[j].bootstrapThreshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).PadLeft(13));
                outputLog?.WriteLine(" ║");
            }

            outputLog?.WriteLine("╚═════════════════╩══════════╩══════════╩═══════════╩══════════════╝");

            outputLog?.WriteLine();
            outputLog?.WriteLine("Best " + targetScoreHeader.Trim() + ": " + bestOverallTargetScore.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + " (" + ((bestTargetScoreIndex - 1) * 100 + 100).ToString() + " bootstrap replicates, " + maximumScoresByBootstrapReplicates[bestTargetScoreIndex].threshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " threshold, " + maximumScoresByBootstrapReplicates[bestTargetScoreIndex].bootstrapThreshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " bootstrap threshold).");
            outputLog?.WriteLine();
            outputLog?.WriteLine("Best overall score: " + bestOverallScore.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + " (" + ((bestScoreIndex - 1) * 100 + 100).ToString() + " bootstrap replicates, " + maximumScoresByBootstrapReplicates[bestScoreIndex].threshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " threshold, " + maximumScoresByBootstrapReplicates[bestScoreIndex].bootstrapThreshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " bootstrap threshold).");

            // Create the validated model
            ValidatedModel validatedModel = new ValidatedModel(model, maximumScoresByBootstrapReplicates[0].threshold, maximumScoresByBootstrapReplicates[bestScoreIndex].threshold, (bestScoreIndex - 1) * 100 + 100, maximumScoresByBootstrapReplicates[bestScoreIndex].bootstrapThreshold, maximumScoresByBootstrapReplicates[bestTargetScoreIndex].threshold, (bestTargetScoreIndex - 1) * 100 + 100, maximumScoresByBootstrapReplicates[bestTargetScoreIndex].bootstrapThreshold);

            outputLog?.WriteLine();
            outputLog?.WriteLine("Saving validated model to file " + Path.GetFullPath(arguments.OutputFile) + "...");

            // Save the model to a file in JSON format.
            using (FileStream outputStream = File.Create(arguments.OutputFile))
            {
                JsonSerializer.Serialize(outputStream, validatedModel, ModelSerializerContext.Default.ValidatedModel);
            }

            if (arguments.ReportFile != null)
            {
                return Utilities.CreateModelValidationReport(features, mask, validatedModel, arguments.OptimisationTarget, arguments.Beta, arguments.EffortPenalty, scores, maximumScoresByBootstrapReplicates, bestScoreIndex, bestTargetScoreIndex, arguments, outputLog);
            }
            else
            {
                return 0;
            }
        }
    }
}
