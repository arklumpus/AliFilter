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

using Accord.Statistics.Analysis;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using AliFilter.AlignmentFeatures;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AliFilter.Models
{
    /// <summary>
    /// Represents a model that can classify alignment columns.
    /// </summary>
    public class FullModel : IModel<double[]>
    {
        [JsonInclude]
        internal LDAModel LdaModel { get; set; }

        /// <summary>
        /// The logistic model used for alignment column classification.
        /// </summary>
        [JsonInclude]
        public LogisticModel LogisticModel { get; internal set; }

        /// <summary>
        /// The signature of the features that have been used to train the model.
        /// </summary>
        [JsonInclude]
        public string FeatureSignature { get; internal set; }

        /// <summary>
        /// Constructor for JSON deserialisation.
        /// </summary>
        [JsonConstructor]
        protected internal FullModel()
        {

        }

        private protected FullModel(LDAModel ldaModel, LogisticModel logisticModel, string featureSignature)
        {
            this.LdaModel = ldaModel;
            this.LogisticModel = logisticModel;
            this.FeatureSignature = featureSignature;
        }

        /// <summary>
        /// Train a new model from the provided features.
        /// </summary>
        /// <param name="features">The features that have been computed for the alignment columns.</param>
        /// <param name="featureSignature">The feature signature, useful to ensure that the model is applied to the same features on which it was trained.</param>
        /// <param name="mask">The mask for each alignment column.</param>
        /// <param name="maxParallelism">The maximum number of threads to use.</param>
        /// <param name="progress">A progress reporter.</param>
        /// <returns>A <see cref="FullModel"/> that has been trained on the supplied data.</returns>
        public static FullModel Train(double[][] features, string featureSignature, bool[] mask, int maxParallelism = -1, IProgress<double> progress = null)
        {
            LinearDiscriminantAnalysis ldaModel = null;
            LogisticRegression logisticModel = null;

            object progressLock = new object();
            int progressCount = 0;

            progress?.Report(0);

            Parallel.For(0, 2, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                if (i == 0)
                {
                    logisticModel = TrainLogisticModel(features, mask);

                    lock (progressLock)
                    {
                        progressCount++;
                        progress?.Report(0.5 * progressCount);
                    }
                }
                else if (i == 1)
                {
                    int[] classes = (from el in mask select el ? 1 : 0).ToArray();

                    ldaModel = new LinearDiscriminantAnalysis();
                    ldaModel.Learn(features, classes);

                    lock (progressLock)
                    {
                        progressCount++;
                        progress?.Report(0.5 * progressCount);
                    }
                }
            });

            progress?.Report(1);

            return new FullModel(new LDAModel(ldaModel.Classifier.First.Weights, ldaModel.Classifier.Second.Means, ldaModel.DiscriminantProportions, ldaModel.Eigenvalues, ldaModel.StandardDeviations), new LogisticModel(logisticModel.Weights, logisticModel.Intercept), featureSignature);
        }


        private static LogisticRegression TrainLogisticModel(double[][] data, bool[] mask)
        {
            IterativeReweightedLeastSquares<LogisticRegression> learner = new IterativeReweightedLeastSquares<LogisticRegression>()
            {
                Tolerance = 1e-4,
                MaxIterations = 1000,
                Regularization = 0
            };

            LogisticRegression regression = learner.Learn(data, mask);

            return regression;
        }

        /// <inheritdoc/>
        public (bool, double) Classify(double[] features)
        {
            return this.LogisticModel.Classify(features);
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
            return this.LogisticModel.Score(features);
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
        /// Classify all columns in the alignment, using the specified features, thresholds, and number of bootstrap replicates.
        /// </summary>
        /// <param name="alignment">The alignment whose columns should be classified.</param>
        /// <param name="features">The features to use when classifying the columns.</param>
        /// <param name="computedFeatures">When this method returns, this variable will contain the alignment features computed for the alignment.</param>
        /// <param name="threshold">The logistic model threshold.</param>
        /// <param name="bootstrapReplicateCount">The number of bootstrap replicates.</param>
        /// <param name="bootstrapThreshold">The bootstrap threshold.</param>
        /// <param name="maxParallelism">The maximum degree of parallelism.</param>
        /// <param name="progressCallback">A progress callback.</param>
        /// <returns>A <see cref="Mask"/> that contains the results of the model for the specified alignment.</returns>
        public Mask Classify(Alignment alignment, FeatureCollection features, out double[][] computedFeatures, double threshold = 0.5, int bootstrapReplicateCount = 0, double bootstrapThreshold = 0.5, int maxParallelism = -1, IProgress<double> progressCallback = null)
        {
            if (features.Signature != this.FeatureSignature)
            {
                throw new ArgumentException("The model contains a different set of features than the current program (" + this.FeatureSignature + " instead of " + features.Signature + ")!");
            }

            computedFeatures = features.ComputeAll(alignment, maxParallelism, progressCallback);

            if (bootstrapReplicateCount == 0)
            {
                Mask mask = this.Classify(computedFeatures, maxParallelism);

                for (int i = 0; i < mask.Length; i++)
                {
                    double val = mask.MaskedStates[i] ? mask.Confidence[i] : (1 - mask.Confidence[i]);

                    if (val > threshold)
                    {
                        mask.MaskedStates[i] = true;
                        mask.Confidence[i] = val;
                    }
                    else
                    {
                        mask.MaskedStates[i] = false;
                        mask.Confidence[i] = 1 - val;
                    }
                }

                return mask;
            }
            else
            {
                Mask[] masks = new Mask[bootstrapReplicateCount];

                Random[] randoms = new Random[bootstrapReplicateCount];

                Random masterRandom = new Random();

                for (int i = 0; i < bootstrapReplicateCount; i++)
                {
                    randoms[i] = new Random(masterRandom.Next());
                }

                object progressLock = new object();
                int completed = 0;

                Parallel.For(0, bootstrapReplicateCount, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
                {
                    Alignment replicate = alignment.Bootstrap(randoms[i]);
                    masks[i] = this.Classify(replicate, features, out _, threshold, 0, 0.5, 1, null);

                    if (progressCallback != null)
                    {
                        lock (progressLock)
                        {
                            completed++;
                            progressCallback.Report((double)completed / bootstrapReplicateCount);
                        }
                    }
                });

                (bool, double)[] mask = new (bool, double)[alignment.AlignmentLength];

                for (int i = 0; i < mask.Length; i++)
                {
                    int count = 0;
                    for (int j = 0; j < bootstrapReplicateCount; j++)
                    {
                        if (masks[j].MaskedStates[i])
                        {
                            count++;
                        }
                    }

                    if ((double)count / bootstrapReplicateCount > bootstrapThreshold)
                    {
                        mask[i] = (true, (double)count / bootstrapReplicateCount);
                    }
                    else
                    {
                        mask[i] = (false, (1 - (double)count / bootstrapReplicateCount));
                    }
                }

                return new Mask(mask);
            }
        }


        /// <summary>
        /// Classify all the columns in an alignment for which features and bootstrap replicates have already been computed.
        /// </summary>
        /// <param name="alignmentFeatures">The features that have been computed for the alignment being analysed.</param>
        /// <param name="bootstrapReplicates">The features that have been computed for the bootstrap replicates of the alignment being analysed.</param>
        /// <param name="threshold">The logistic model threshold.</param>
        /// <param name="bootstrapReplicateCount">The number of bootstrap replicates. The <paramref name="bootstrapReplicates"/> must contain at least this number of replicates.</param>
        /// <param name="bootstrapThreshold">The bootstrap threshold.</param>
        /// <param name="maxParallelism">The maximum degree of parallelism.</param>
        /// <param name="progressCallback">A progress callback.</param>
        /// <returns>A <see cref="Mask"/> that contains the results of the model for the specified alignment.</returns>
        public Mask Classify(double[][] alignmentFeatures, IReadOnlyList<double[,]> bootstrapReplicates, double threshold = 0.5, int bootstrapReplicateCount = 0, double bootstrapThreshold = 0.5, int maxParallelism = -1, IProgress<double> progressCallback = null)
        {
            if (bootstrapReplicateCount == 0)
            {
                Mask mask = this.Classify(alignmentFeatures, maxParallelism);

                for (int i = 0; i < mask.Length; i++)
                {
                    double val = mask.MaskedStates[i] ? mask.Confidence[i] : (1 - mask.Confidence[i]);

                    if (val > threshold)
                    {
                        mask.MaskedStates[i] = true;
                        mask.Confidence[i] = val;
                    }
                    else
                    {
                        mask.MaskedStates[i] = false;
                        mask.Confidence[i] = 1 - val;
                    }
                }

                return mask;
            }
            else
            {
                Mask[] masks = new Mask[bootstrapReplicateCount];

                object progressLock = new object();
                int completed = 0;

                Parallel.For(0, bootstrapReplicateCount, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
                {
                    double[][] features = new double[alignmentFeatures.Length][];

                    for (int j = 0; j < alignmentFeatures.Length; j++)
                    {
                        features[j] = new double[bootstrapReplicates[j].GetLength(0)];
                        for (int k = 0; k < features[j].Length; k++)
                        {
                            features[j][k] = bootstrapReplicates[j][k, i];
                        }
                    }

                    masks[i] = this.Classify(features, null, threshold, 0, 0.5, 1, null);

                    if (progressCallback != null)
                    {
                        lock (progressLock)
                        {
                            completed++;
                            progressCallback.Report((double)completed / bootstrapReplicateCount);
                        }
                    }
                });

                (bool, double)[] mask = new (bool, double)[alignmentFeatures.Length];

                for (int i = 0; i < mask.Length; i++)
                {
                    int count = 0;
                    for (int j = 0; j < bootstrapReplicateCount; j++)
                    {
                        if (masks[j].MaskedStates[i])
                        {
                            count++;
                        }
                    }

                    if ((double)count / bootstrapReplicateCount > bootstrapThreshold)
                    {
                        mask[i] = (true, (double)count / bootstrapReplicateCount);
                    }
                    else
                    {
                        mask[i] = (false, (1 - (double)count / bootstrapReplicateCount));
                    }
                }

                return new Mask(mask);
            }
        }

        /// <summary>
        /// Deserialise a <see cref="FullModel"/> from a stream.
        /// </summary>
        /// <param name="modelStream">The <see cref="Stream"/> from which the model should be deserialised.</param>
        /// <returns>The deserialised <see cref="FullModel"/>.</returns>
        public static FullModel FromStream(Stream modelStream) => Utilities.ReadModel<FullModel>(modelStream);

        /// <summary>
        /// Deserialise a <see cref="FullModel"/> from a file.
        /// </summary>
        /// <param name="modelFile">The file from which the model should be deserialised.</param>
        /// <returns>The deserialised <see cref="FullModel"/>.</returns>
        public static FullModel FromFile(string modelFile) => Utilities.ReadModel<FullModel>(modelFile);

        /// <summary>
        /// Save the model in JSON format to the specified output <see cref="Stream"/>.
        /// </summary>
        /// <param name="outputStream">The output <see cref="Stream"/>.</param>
        public virtual void Save(Stream outputStream)
        {
            JsonSerializer.Serialize(outputStream, this, ModelSerializerContext.Default.FullModel);
        }

        /// <summary>
        /// Save the model in JSON format to the specified output file.
        /// </summary>
        /// <param name="outputFile">The output file.</param>
        public virtual void Save(string outputFile)
        {
            using (FileStream outputStream = File.Create(outputFile))
            {
                this.Save(outputStream);
            }
        }

        /// <summary>
        /// Compute an alignment <see cref="Mask"/> for the specified alignment.
        /// </summary>
        /// <param name="alignment">The alignment for which a mask should be computed.</param>
        /// <param name="features">The set of features to use. These must correspond to the features used to train the model.</param>
        /// <param name="threshold">Custom override value for the logistic model threshold.</param>
        /// <param name="bootstrapReplicateCount">Custom override value for the number of bootstrap replicates.</param>
        /// <param name="bootstrapThreshold">Custom override value for the bootstrap threshold.</param>
        /// <param name="maxParallelism">The maximum degree of parallelism.</param>
        /// <param name="progressCallback">A progress callback.</param>
        /// <returns>A <see cref="Mask"/> that contains the results of the model for the specified alignment.</returns>
        /// <exception cref="ArgumentException">Thrown if the <see cref="FeatureCollection"/> used to train this model is different from the specified <paramref name="features"/>.</exception>
        public Mask GetMask(Alignment alignment, FeatureCollection features = default, double threshold = 0.5, int bootstrapReplicateCount = 0, double bootstrapThreshold = 0.5, int maxParallelism = -1, IProgress<double> progressCallback = null)
        {
            FeatureCollection requestedFeatures = features ?? Features.DefaultFeatures;

            if (requestedFeatures.Signature != this.FeatureSignature)
            {
                throw new ArgumentException("The signature of the specified feature collection is different from the signature of the features used to train this model!");
            }

            return this.Classify(alignment, requestedFeatures, out _, threshold, bootstrapReplicateCount, bootstrapThreshold, maxParallelism, progressCallback);
        }

        /// <summary>
        /// Compute an alignment <see cref="Mask"/> from pre-computed alignment features.
        /// </summary>
        /// <param name="bootstrapReplicates">Pre-computed bootstrap replicate features.</param>
        /// <param name="alignmentFeatures">The pre-computed alignment features for which a mask should be computed.</param>
        /// <param name="threshold">Custom override value for the logistic model threshold.</param>
        /// <param name="bootstrapReplicateCount">Custom override value for the number of bootstrap replicates.</param>
        /// <param name="bootstrapThreshold">Custom override value for the bootstrap threshold.</param>
        /// <param name="maxParallelism">The maximum degree of parallelism.</param>
        /// <param name="progressCallback">A progress callback.</param>
        /// <returns>A <see cref="Mask"/> that contains the results of the model for the specified alignment.</returns>
        /// <exception cref="ArgumentException">Thrown if more bootstrap replicates are requested than are provided in the <paramref name="bootstrapReplicates"/> parameter.</exception>
        public Mask GetMask(double[][] alignmentFeatures, IReadOnlyList<double[,]> bootstrapReplicates = null, double threshold = 0.5, int bootstrapReplicateCount = 0, double bootstrapThreshold = 0.5, int maxParallelism = -1, IProgress<double> progressCallback = null)
        {
            if (!(bootstrapReplicateCount == 0 || bootstrapReplicates?[0]?.GetLength(1) >= bootstrapReplicateCount))
            {
                throw new ArgumentException(bootstrapReplicateCount.ToString() + " need to be analysed, but " + ((bootstrapReplicates?[0]?.GetLength(1))?.ToString() ?? "none") + " have been provided!");
            }

            return this.Classify(alignmentFeatures, bootstrapReplicates, threshold, bootstrapReplicateCount, bootstrapThreshold, maxParallelism, progressCallback);
        }

        /// <summary>
        /// Validate a model using the provided validation data.
        /// </summary>
        /// <param name="validationMask">The validation mask.</param>
        /// <param name="validationFeatures">The validation features.</param>
        /// <param name="bootstrapReplicates">The validation bootstrap replicates. At least <paramref name="maxBootstrapReplicates"/> must have been computed.</param>
        /// <param name="maxBootstrapReplicates">Maximum number of bootstrap replicates to consider. This must be a multiple of 100. If this parameter is not provided, the default value is to use all the provided <paramref name="bootstrapReplicates"/>.</param>
        /// <param name="targetMetric">The target metric being optimised (default: MCC).</param>
        /// <param name="effortPenalty">Parameter used to penalise results obtained with higher numbers of bootstrap replicates.</param>
        /// <param name="maxParallelism">The maximum degree of parallelism.</param>
        /// <param name="progressCallback">A progress callback.</param>
        /// <returns>The validated model.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="maxBootstrapReplicates"/> is greater than the number of bootstrap replicates provided in the <paramref name="bootstrapReplicates"/> parameter.</exception>
        public ValidatedModel Validate(Mask validationMask, double[][] validationFeatures, IReadOnlyList<double[,]> bootstrapReplicates, int maxBootstrapReplicates = -1, Func<ConfusionMatrix, double> targetMetric = default, double effortPenalty = 0.005, int maxParallelism = -1, IProgress<double> progressCallback = null)
        {
            if (maxBootstrapReplicates < 0)
            {
                maxBootstrapReplicates = bootstrapReplicates[0].GetLength(1);
            }

            if (maxBootstrapReplicates > bootstrapReplicates[0].GetLength(1))
            {
                throw new ArgumentException("Up to " + maxBootstrapReplicates.ToString() + " should be used, but " + bootstrapReplicates[0].GetLength(1).ToString() + " have been provided!");
            }

            Func<int, int, int, int, double> targetMetricInts;
            
            if (targetMetric == null)
            {
                targetMetricInts = Utilities.ComputeMCC;
            }
            else
            {
                targetMetricInts = (tp, tn, fp, fn) => targetMetric(new ConfusionMatrix(tp, fp, tn, fn));
            }

            double[][] columnScores = new double[maxBootstrapReplicates + 1][];

            // Use the model to classify the alignment columns for each bootstrap replicate.
            Parallel.For(0, maxBootstrapReplicates + 1, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                if (i == 0)
                {
                    columnScores[i] = this.Score(validationFeatures, 1);
                }
                else
                {
                    double[][] currReplicate = new double[validationFeatures.Length][];

                    for (int j = 0; j < validationFeatures.Length; j++)
                    {
                        currReplicate[j] = new double[validationFeatures[j].Length];

                        for (int k = 0; k < validationFeatures[j].Length; k++)
                        {
                            currReplicate[j][k] = bootstrapReplicates[j][k, i - 1];
                        }
                    }

                    columnScores[i] = this.Score(currReplicate, 1);
                }
            });

            // Number of threshold values between 0 and 1 to test.
            int thresholdRangeSteps = 101;

            // Set up the array to hold the F_β-score values.
            double[][][] scores = new double[thresholdRangeSteps][][];

            for (int i = 0; i < scores.Length; i++)
            {
                scores[i] = new double[(maxBootstrapReplicates - 100) / 100 + 2][];
                scores[i][0] = new double[1];

                for (int j = 1; j < scores[i].Length; j++)
                {
                    scores[i][j] = new double[thresholdRangeSteps];
                }
            }

            object progressLock = new object();
            int completed = 0;

            // Compute the target score for each combination of thresholds and number of bootstrap replicates.
            Parallel.For(0, scores.Length, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
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
                            if (columnScores[0][m] > threshold && validationMask.MaskedStates[m])
                            {
                                tp++;
                            }
                            else if (columnScores[0][m] > threshold && !validationMask.MaskedStates[m])
                            {
                                fp++;
                            }
                            else if (columnScores[0][m] <= threshold && !validationMask.MaskedStates[m])
                            {
                                tn++;
                            }
                            else if (columnScores[0][m] <= threshold && validationMask.MaskedStates[m])
                            {
                                fn++;
                            }
                        }

                        // Target score.
                        scores[i][j][0] = targetMetricInts(tp, tn, fp, fn);
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
                                if (proportionAboveThreshold[m] > bootstrapThreshold && validationMask.MaskedStates[m])
                                {
                                    tp++;
                                }
                                else if (proportionAboveThreshold[m] > bootstrapThreshold && !validationMask.MaskedStates[m])
                                {
                                    fp++;
                                }
                                else if (proportionAboveThreshold[m] <= bootstrapThreshold && !validationMask.MaskedStates[m])
                                {
                                    tn++;
                                }
                                else if (proportionAboveThreshold[m] <= bootstrapThreshold && validationMask.MaskedStates[m])
                                {
                                    fn++;
                                }
                            }

                            // Target score.
                            scores[i][j][k] = targetMetricInts(tp, tn, fp, fn);
                        }
                    }

                    // Report progress.
                    lock (progressLock)
                    {
                        completed++;
                        progressCallback?.Report((double)completed / (scores.Length * ((maxBootstrapReplicates - 100) / 100 + 2)));
                    }
                }
            });

            progressCallback?.Report(1);

            // Maximum scores by number of replicates.
            (double maxTargetScore, double threshold, double bootstrapThreshold, double finalScore)[] maximumScoresByBootstrapReplicates = new (double maxTargetScore, double threshold, double bootstrapThreshold, double finalScore)[(maxBootstrapReplicates - 100) / 100 + 2];

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
                                maximumScoresByBootstrapReplicates[j] = (scores[i][j][k], (double)i / (thresholdRangeSteps - 1), (double)k / (thresholdRangeSteps - 1), scores[i][j][k] - bootstrapReplicateCount * effortPenalty * 0.01);
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

            // Create the validated model
            return new ValidatedModel(this, maximumScoresByBootstrapReplicates[0].threshold, maximumScoresByBootstrapReplicates[bestScoreIndex].threshold, (bestScoreIndex - 1) * 100 + 100, maximumScoresByBootstrapReplicates[bestScoreIndex].bootstrapThreshold, maximumScoresByBootstrapReplicates[bestTargetScoreIndex].threshold, (bestTargetScoreIndex - 1) * 100 + 100, maximumScoresByBootstrapReplicates[bestTargetScoreIndex].bootstrapThreshold);
        }
    }
}
