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

using System.Text.Json.Serialization;

namespace AliFilter.Models
{
    /// <summary>
    /// Represents a model that can classify alignment columns.
    /// </summary>
    public interface IModel<T>
    {
        /// <summary>
        /// Classifies a single alignment column.
        /// </summary>
        /// <param name="features">The values of the features computed for the alignment column.</param>
        /// <returns>The first element of the tuple is <see langword="true"/> if the column should be preserved, <see langword="false"/> if it should be deleted. The second element of the tuple represents the model confidence (1 for high confidence, 0.5 for low confidence).</returns>
        public (bool, double) Classify(T features);

        /// <summary>
        /// Classifies multiple alignment columns.
        /// </summary>
        /// <param name="features">The values of the features computed for the alignment columns.</param>
        /// <param name="maxParallelism">Maximum number of threads to use.</param>
        /// <returns>A <see cref="Mask"/> containing the results of the classification.</returns>
        public Mask Classify(IReadOnlyList<T> features, int maxParallelism = -1);


        /// <summary>
        /// Computes the preservation score for a single alignment column.
        /// </summary>
        /// <param name="features">The values of the features computed for the alignment column.</param>
        /// <returns>The preservation score of the column.</returns>
        public double Score(T features);

        /// <summary>
        /// Computes the preservation score for multiple alignment columns.
        /// </summary>
        /// <param name="features">The values of the features computed for the alignment columns.</param>
        /// <param name="maxParallelism">Maximum number of threads to use.</param>
        /// <returns>An array of <see cref="double"/> values corresponding to the preservation score of each column.</returns>
        public double[] Score(IReadOnlyList<T> features, int maxParallelism = -1);
    }

    /// <summary>
    /// Handles model serialization and deserialization.
    /// </summary>
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(FullModel))]
    [JsonSerializable(typeof(ValidatedModel))]
    internal partial class ModelSerializerContext : JsonSerializerContext { }
}
