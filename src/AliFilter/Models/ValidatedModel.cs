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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AliFilter.Models
{
    /// <summary>
    /// Represents a <see cref="FullModel"/> that has been cross-validated.
    /// </summary>
    public sealed class ValidatedModel : FullModel
    {
        /// <summary>
        /// Whether this model has actually been tuned by cross-validation or not.
        /// </summary>
        public bool Validated { get; set; } = false;

        /// <summary>
        /// Threshold used for classification. Best (most efficient) value.
        /// </summary>
        public double BestThreshold { get; set; } = 0.5;

        /// <summary>
        /// Bootstrap threshold used for classification. Best (most efficient) value.
        /// </summary>
        public double BestBootstrapThreshold { get; set; } = 0.5;

        /// <summary>
        /// Number of bootstrap replicates to use. Best (most efficient) value.
        /// </summary>
        public int BestBootstrapReplicates { get; set; } = 0;

        /// <summary>
        /// Threshold used for classification. Most accurate value.
        /// </summary>
        public double AccurateThreshold { get; set; } = 0.5;

        /// <summary>
        /// Bootstrap threshold used for classification. Most accurate value.
        /// </summary>
        public double AccurateBootstrapThreshold { get; set; } = 0.5;

        /// <summary>
        /// Number of bootstrap replicates to use. Most accurate value.
        /// </summary>
        public int AccurateBootstrapReplicates { get; set; } = 0;

        /// <summary>
        /// Threshold used for classification. Best value to use when no bootstrap replicates are performed.
        /// </summary>
        public double FastThreshold { get; set; } = 0.5;

        [JsonConstructor]
        internal ValidatedModel() : base() { }

        /// <summary>
        /// Create a new <see cref="ValidatedModel"/> from the specified <see cref="FullModel"/>, with the provided validated model parameters.
        /// </summary>
        /// <param name="model">The trained logistic model.</param>
        /// <param name="fastThreshold">The logistic model threshold to use when no bootstrap replicates are performed.</param>
        /// <param name="bestThreshold">The logistic model threshold that offers the best balance between performance and accuracy.</param>
        /// <param name="bestBootstrapReplicates">The number of bootstrap replicates that offers the best balance between performance and accuracy.</param>
        /// <param name="bestBootstrapThreshold">The bootstrap threshold that offers the best balance between performance and accuracy.</param>
        /// <param name="accurateThreshold">The logistic model threshold that provides the highest accuracy.</param>
        /// <param name="accurateBootstrapReplicates">The number of bootstrap replicates that provides the highest accuracy.</param>
        /// <param name="accurateBootstrapThreshold">The bootstrap threshold that provides the highest accuracy.</param>
        internal ValidatedModel(FullModel model, double fastThreshold, double bestThreshold, int bestBootstrapReplicates, double bestBootstrapThreshold, double accurateThreshold, int accurateBootstrapReplicates, double accurateBootstrapThreshold) : base(model.LdaModel, model.LogisticModel, model.FeatureSignature)
        {
            this.Validated = true;
            this.FastThreshold = fastThreshold;
            this.BestThreshold = bestThreshold;
            this.BestBootstrapReplicates = bestBootstrapReplicates;
            this.BestBootstrapThreshold = bestBootstrapThreshold;
            this.AccurateThreshold = accurateThreshold;
            this.AccurateBootstrapReplicates = accurateBootstrapReplicates;
            this.AccurateBootstrapThreshold = accurateBootstrapThreshold;
        }

        /// <summary>
        /// Deserialise a <see cref="ValidatedModel"/> from a stream.
        /// </summary>
        /// <param name="modelStream">The <see cref="Stream"/> from which the model should be deserialised.</param>
        /// <returns>The deserialised <see cref="ValidatedModel"/>. If the stream contains an unvalidated model, the <see cref="Validated"/> property of the returned instance will be <see langword="false"/>.</returns>
        public static new ValidatedModel FromStream(Stream modelStream) => Utilities.ReadModel<ValidatedModel>(modelStream);

        /// <summary>
        /// Deserialise a <see cref="ValidatedModel"/> from a file.
        /// </summary>
        /// <param name="modelFile">The file from which the model should be deserialised.</param>
        /// <returns>The deserialised <see cref="ValidatedModel"/>. If the file contains an unvalidated model, the <see cref="Validated"/> property of the returned instance will be <see langword="false"/>.</returns>
        public static new ValidatedModel FromFile(string modelFile) => Utilities.ReadModel<ValidatedModel>(modelFile);

        /// <summary>
        /// Save the model in JSON format to the specified output <see cref="Stream"/>.
        /// </summary>
        /// <param name="outputStream">The output <see cref="Stream"/>.</param>
        public override void Save(Stream outputStream)
        {
            JsonSerializer.Serialize(outputStream, this, ModelSerializerContext.Default.ValidatedModel);
        }

        /// <summary>
        /// Save the model in JSON format to the specified output file.
        /// </summary>
        /// <param name="outputFile">The output file.</param>
        public override void Save(string outputFile)
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
        /// <param name="defaultParameters">The set of validated parameters to use.</param>
        /// <param name="features">The set of features to use. These must correspond to the features used to train the model.</param>
        /// <param name="threshold">Custom override value for the logistic model threshold.</param>
        /// <param name="bootstrapReplicateCount">Custom override value for the number of bootstrap replicates.</param>
        /// <param name="bootstrapThreshold">Custom override value for the bootstrap threshold.</param>
        /// <param name="maxParallelism">The maximum degree of parallelism.</param>
        /// <param name="progressCallback">A progress callback.</param>
        /// <returns>A <see cref="Mask"/> that contains the results of the model for the specified alignment.</returns>
        /// <exception cref="ArgumentException">Thrown if the <see cref="FeatureCollection"/> used to train this model is different from the specified <paramref name="features"/>.</exception>
        public Mask GetMask(Alignment alignment, DefaultParameters defaultParameters = DefaultParameters.Best, FeatureCollection features = default, double? threshold = default, int? bootstrapReplicateCount = default, double? bootstrapThreshold = default, int maxParallelism = -1, IProgress<double> progressCallback = null)
        {
            double defaultThreshold = defaultParameters switch { DefaultParameters.Accurate => this.AccurateThreshold, DefaultParameters.Best => this.BestThreshold, DefaultParameters.Fast => this.FastThreshold, _ => 0.5 };
            double defaultBootstrapThreshold = defaultParameters switch { DefaultParameters.Accurate => this.AccurateBootstrapThreshold, DefaultParameters.Best => this.BestBootstrapThreshold, _ => 0.5 };
            int defaultBootstrapReplicates = defaultParameters switch { DefaultParameters.Accurate => this.AccurateBootstrapReplicates, DefaultParameters.Best => this.BestBootstrapReplicates, _ => 0 };

            FeatureCollection requestedFeatures = features ?? Features.DefaultFeatures;

            if (requestedFeatures.Signature != this.FeatureSignature)
            {
                throw new ArgumentException("The signature of the specified feature collection is different from the signature of the features used to train this model!");
            }

            return this.Classify(alignment, requestedFeatures, out _, threshold ?? defaultThreshold, bootstrapReplicateCount ?? defaultBootstrapReplicates, bootstrapThreshold ?? defaultBootstrapThreshold, maxParallelism, progressCallback);
        }

        /// <summary>
        /// Compute an alignment <see cref="Mask"/> from pre-computed alignment features.
        /// </summary>
        /// <param name="bootstrapReplicates">Pre-computed bootstrap replicate features.</param>
        /// <param name="alignmentFeatures">The pre-computed alignment features for which a mask should be computed.</param>
        /// <param name="defaultParameters">The set of validated parameters to use.</param>
        /// <param name="threshold">Custom override value for the logistic model threshold.</param>
        /// <param name="bootstrapReplicateCount">Custom override value for the number of bootstrap replicates.</param>
        /// <param name="bootstrapThreshold">Custom override value for the bootstrap threshold.</param>
        /// <param name="maxParallelism">The maximum degree of parallelism.</param>
        /// <param name="progressCallback">A progress callback.</param>
        /// <returns>A <see cref="Mask"/> that contains the results of the model for the specified alignment.</returns>
        /// <exception cref="ArgumentException">Thrown if more bootstrap replicates are requested than are provided in the <paramref name="bootstrapReplicates"/> parameter.</exception>
        public Mask GetMask(double[][] alignmentFeatures, IReadOnlyList<double[,]> bootstrapReplicates = null, DefaultParameters defaultParameters = DefaultParameters.Best, double? threshold = default, int? bootstrapReplicateCount = default, double? bootstrapThreshold = default, int maxParallelism = -1, IProgress<double> progressCallback = null)
        {
            double defaultThreshold = defaultParameters switch { DefaultParameters.Accurate => this.AccurateThreshold, DefaultParameters.Best => this.BestThreshold, DefaultParameters.Fast => this.FastThreshold, _ => 0.5 };
            double defaultBootstrapThreshold = defaultParameters switch { DefaultParameters.Accurate => this.AccurateBootstrapThreshold, DefaultParameters.Best => this.BestBootstrapThreshold, _ => 0.5 };
            int defaultBootstrapReplicates = defaultParameters switch { DefaultParameters.Accurate => this.AccurateBootstrapReplicates, DefaultParameters.Best => this.BestBootstrapReplicates, _ => 0 };

            int requestedBootstrapReplicates = bootstrapReplicateCount ?? defaultBootstrapReplicates;

            if (!(requestedBootstrapReplicates == 0 || bootstrapReplicates?[0]?.GetLength(1) >= requestedBootstrapReplicates))
            {
                throw new ArgumentException(requestedBootstrapReplicates.ToString() + " need to be analysed, but " + ((bootstrapReplicates?.Count)?.ToString() ?? "none") + " have been provided!");
            }

            return this.Classify(alignmentFeatures, bootstrapReplicates, threshold ?? defaultThreshold, requestedBootstrapReplicates, bootstrapThreshold ?? defaultBootstrapThreshold, maxParallelism, progressCallback);
        }
    }
}
