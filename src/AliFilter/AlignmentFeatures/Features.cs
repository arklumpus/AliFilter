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

using System.Collections;

namespace AliFilter.AlignmentFeatures
{
    /// <summary>
    /// Static class containing the default features enabled in AliFilter.
    /// </summary>
    public static class Features
    {
        /// <summary>
        /// Checks whether features computed with a certain version of AliFilter are compatible with the current version.
        /// </summary>
        /// <param name="featureProgramVersion">The version of AliFilter used to compute the features.</param>
        /// <returns>A <see langword="bool"/> indicating whether features computed with the specified program version are compatible with the current version.</returns>
        public static bool FeaturesCompatible(string featureProgramVersion)
        {
            Version version = new Version(featureProgramVersion);

            return version.MajorRevision <= 1;
        }

        /// <summary>
        /// The default features used by AliFilter.
        /// </summary>
        public static readonly FeatureCollection DefaultFeatures = new FeatureCollection
        (
            maxBootstrapReplicates: 1000,
            new GapProportion(),
            new PercentIdentity(),
            new DistanceFromExtremity(),
            new Entropy(),
            new SurroundingGapProportion(1),
            new SurroundingGapProportion(2)
        );

        /// <summary>
        /// Computes the value of all the requested <paramref name="features"/> for the specified <paramref name="alignment"/>.
        /// </summary>
        /// <param name="features">The features to compute for each column in the alignment.</param>
        /// <param name="alignment">The alignment for which the features should be computed.</param>
        /// <param name="maxParallelism">The maximum number of threads to use.</param>
        /// <param name="progressCallback">A progress reporter.</param>
        /// <returns>A <see langword="double"/>[][] containing one element for each alignment column, which itself contains the value of all the features for that column.</returns>
        public static double[][] ComputeAll(this IReadOnlyList<IFeature> features, Alignment alignment, int maxParallelism = -1, IProgress<double> progressCallback = null)
        {
            double[][] computedFeatures = new double[features.Count][];
            progressCallback?.Report(0);

            for (int i = 0; i < features.Count; i++)
            {
                computedFeatures[i] = features[i].Compute(alignment, maxParallelism);

                progressCallback?.Report((i + 1.0) / features.Count);
            }

            double[][] tbr = new double[alignment.AlignmentLength][];

            for (int i = 0; i < tbr.Length; i++)
            {
                tbr[i] = new double[computedFeatures.Length];

                for (int j = 0; j < computedFeatures.Length; j++)
                {
                    tbr[i][j] = computedFeatures[j][i];
                }
            }

            return tbr;
        }

        /// <summary>
        /// Computes the specified number of bootstrap replicates for the specified <paramref name="features"/> for the specified <paramref name="alignment"/>.
        /// </summary>
        /// <param name="features">The features to compute for each column in the alignment.</param>
        /// <param name="alignment">The alignment for which the features should be computed.</param>
        /// <param name="bootstrapReplicates">The number of bootstrap replicates to perform.</param>
        /// <param name="maxParallelism">The maximum number of threads to use.</param>
        /// <param name="progressCallback">A progress reporter.</param>
        /// <returns>A <see langword="double"/>[][,] containing one <see langword="double"/>[,] element E for each alignment column; E[i, j] provides access to the j-th bootstrap replicate of the i-th feature.</returns>
        public static double[][,] ComputeAllBootstrapReplicates(this IReadOnlyList<IFeature> features, Alignment alignment, int bootstrapReplicates, int maxParallelism = -1, IProgress<double> progressCallback = null)
        {
            Random[] randoms = new Random[bootstrapReplicates];

            Random masterRandom = new Random();

            for (int i = 0; i < bootstrapReplicates; i++)
            {
                randoms[i] = new Random(masterRandom.Next());
            }

            double[][,] data = new double[alignment.AlignmentLength][,];

            for (int i = 0; i < alignment.AlignmentLength; i++)
            {
                data[i] = new double[features.Count, bootstrapReplicates];
            }

            object lockObject = new object();
            int progress = 0;

            Parallel.For(0, bootstrapReplicates, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                double[][] tempData;

                Alignment bootstrapReplicate = alignment.Bootstrap(randoms[i]);

                tempData = Utilities.ComputeFeatures(bootstrapReplicate, features, 1, null);

                for (int j = 0; j < tempData.Length; j++)
                {
                    for (int k = 0; k < tempData[j].Length; k++)
                    {
                        data[j][k, i] = tempData[j][k];
                    }
                }

                lock (lockObject)
                {
                    progress++;
                    progressCallback?.Report((double)progress / (bootstrapReplicates + 1));
                }
            });

            return data;
        }
    }

    /// <summary>
    /// Represents a collection of features.
    /// </summary>
    public class FeatureCollection : IReadOnlyList<IFeature>
    {
        /// <summary>
        /// A unique signature for this collection of features.
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// The maximum number of bootstrap replicates to consider.
        /// </summary>
        public int MaxBootstrapReplicates { get; }

        private IFeature[] Features { get; }

        /// <inheritdoc/>
        public IFeature this[int index] => Features[index];

        /// <inheritdoc/>
        public int Count => Features.Length;

        /// <inheritdoc/>
        public IEnumerator<IFeature> GetEnumerator()
        {
            return ((IEnumerable<IFeature>)Features).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Features.GetEnumerator();
        }

        /// <summary>
        /// Create a new <see cref="FeatureCollection"/>.
        /// </summary>
        /// <param name="features">The <see cref="IFeature"/>s to include in the collection.</param>
        /// <param name="maxBootstrapReplicates">The maximum number of bootstrap replicates to consider.</param>
        public FeatureCollection(int maxBootstrapReplicates, IEnumerable<IFeature> features) : this(maxBootstrapReplicates, features.ToArray()) { }

        /// <summary>
        /// Create a new <see cref="FeatureCollection"/>.
        /// </summary>
        /// <param name="features">The <see cref="IFeature"/>s to include in the collection.</param>
        /// <param name="maxBootstrapReplicates">The maximum number of bootstrap replicates to consider.</param>
        public FeatureCollection(int maxBootstrapReplicates, params IFeature[] features)
        {
            this.Features = features;
            this.MaxBootstrapReplicates = maxBootstrapReplicates;
            this.Signature = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(features.Select(x => x.Name).Aggregate((a, b) => a + ";;" + b) + ";;" + maxBootstrapReplicates)));
        }
    }
}
