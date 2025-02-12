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
        // Train a model using the computed features from a file saved on disk and save the model to disk.
        public static int TrainModel(Arguments arguments, TextWriter outputLog)
        {
            //Deserialize alignment features.
            (double[][] features, IReadOnlyList<double[,]> bootstrapReplicates, Mask mask) = Utilities.ReadFeatures(arguments, outputLog);

            if (bootstrapReplicates.Count > 0)
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("WARNING: The training data contains bootstrap replicates. These will be ignored while training the model.");
                outputLog?.WriteLine();
            }

            outputLog?.WriteLine();
            outputLog?.WriteLine("Training model...");

            ProgressBar bar = null;

            if (outputLog != null)
            {
                bar = new ProgressBar(outputLog);
            }

            bar?.Start();
            // Train the model.
            Models.FullModel model = Models.FullModel.Train(features, arguments.Features.Signature, mask.MaskedStates, arguments.MaxParallelism, bar != null ? new Progress<double>(bar.Progress) : null);
            bar?.Finish();

            outputLog?.WriteLine();
            outputLog?.WriteLine("Saving model to file " + Path.GetFullPath(arguments.OutputFile) + "...");

            // Save the model to a file in JSON format.
            using (FileStream outputStream = File.Create(arguments.OutputFile))
            {
                JsonSerializer.Serialize(outputStream, model, ModelSerializerContext.Default.FullModel);
            }

            return 0;
        }
    }
}
