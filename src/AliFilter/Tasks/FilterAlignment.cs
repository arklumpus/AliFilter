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
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;

namespace AliFilter
{
    internal static partial class Tasks
    {
        // Apply an AliFilter model to filter an alignment.
        public static int FilterAlignment(Arguments arguments, TextWriter outputLog)
        {
            // Read the alignment from a file or from the standard input.
            Alignment alignment = Utilities.ReadAlignment(arguments, outputLog);

            if (alignment == null)
            {
                return 1;
            }

            // Declare variables.
            Mask predictedMask;
            double threshold = -1;
            int bootstrapReplicateCount = -1;
            double bootstrapThreshold = -1;
            ValidatedModel model = null;
            double[][] features = null;

            // No filtering is to be applied.
            if (!string.IsNullOrEmpty(arguments.InputModel) && arguments.InputModel.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("Keeping all columns...");
                predictedMask = new Mask(Enumerable.Repeat(true, alignment.AlignmentLength));
                outputLog?.WriteLine();
            }
            // Simply remove columns with more than a certain proportion of gaps.
            else if (!string.IsNullOrEmpty(arguments.InputModel) && arguments.InputModel.StartsWith("gap_", StringComparison.OrdinalIgnoreCase) && double.TryParse(arguments.InputModel.Substring(4), out double gapThreshold) && gapThreshold >= 0 && gapThreshold <= 1)
            {
                outputLog?.WriteLine();
                if (gapThreshold < 1)
                {
                    outputLog?.WriteLine("Removing columns with more than " + gapThreshold.ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + " gaps...");
                }
                else
                {
                    outputLog?.WriteLine("Removing columns consisting only of gaps...");
                }

                predictedMask = new Mask(Enumerable.Range(0, alignment.AlignmentLength).Select(x => alignment.GetColumn(x).Count(x => x == '-') <= (gapThreshold < 1 ? (gapThreshold * alignment.SequenceCount) : (alignment.SequenceCount - 1))));

                // Number of preserved columns.
                int trues = predictedMask.MaskedStates.Count(x => x);

                outputLog?.WriteLine();
                outputLog?.WriteLine("Mask contains " + trues.ToString() + " (" + ((double)trues / predictedMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") preserved columns and " + (predictedMask.Length - trues).ToString() + " (" + (1 - (double)trues / predictedMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") deleted columns.");
                outputLog?.WriteLine();
            }
            // Use the default model.
            else if (string.IsNullOrEmpty(arguments.InputModel) || arguments.InputModel.Equals("alifilter", StringComparison.OrdinalIgnoreCase))
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("Using the default AliFilter model...");

                using (Stream modelStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AliFilter.Models.alifilter.validated.json"))
                {
                    model = JsonSerializer.Deserialize<ValidatedModel>(modelStream, (JsonTypeInfo<ValidatedModel>)ModelSerializerContext.Default.GetTypeInfo(typeof(ValidatedModel)));
                }

                outputLog?.WriteLine();

                // Model parameters.
                threshold = arguments.Threshold ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateThreshold, DefaultParameters.Best => model.BestThreshold, DefaultParameters.Fast => model.FastThreshold, _ => model.FastThreshold };
                bootstrapReplicateCount = arguments.BootstrapReplicates ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateBootstrapReplicates, DefaultParameters.Best => model.BestBootstrapReplicates, DefaultParameters.Fast => 0, _ => 0 };
                bootstrapThreshold = arguments.BootstrapThreshold ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateBootstrapThreshold, DefaultParameters.Best => model.BestBootstrapThreshold, DefaultParameters.Fast => 0.5, _ => 0.5 };

                outputLog?.WriteLine("Applying model to " + alignment.AlignmentLength + " alignment columns...");

                ProgressBar bar = null;
                if (outputLog != null)
                {
                    bar = new ProgressBar(outputLog);
                }
                bar?.Start();

                // Use the model to classify the alignment columns.
                predictedMask = model.Classify(alignment, arguments.Features, out features, threshold, bootstrapReplicateCount, bootstrapThreshold, arguments.MaxParallelism, bar != null ? new Progress<double>(bar.Progress) : null);

                bar?.Finish();

                // Number of preserved columns.
                int trues = predictedMask.MaskedStates.Count(x => x);

                outputLog?.WriteLine();
                outputLog?.WriteLine("Mask contains " + trues.ToString() + " (" + ((double)trues / predictedMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") preserved columns and " + (predictedMask.Length - trues).ToString() + " (" + (1 - (double)trues / predictedMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") deleted columns.");
                outputLog?.WriteLine();

                // Compute the model confidence score.
                double modelScore = 1 - predictedMask.Confidence.Select(x => x * (1 - x) * 4).Sum() / predictedMask.Length;

                outputLog?.WriteLine("Model confidence score: " + modelScore.ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture));
                outputLog?.WriteLine();
            }
            // The model input points to a file, containing either a pre-computed mask, or a serialized model.
            else
            {
                object modelOrMask = Utilities.ReadModelOrMask<ValidatedModel>(arguments, outputLog);

                // The file contains a pre-computed mask.
                if (modelOrMask is Mask inputMask)
                {
                    if (inputMask.Length != alignment.AlignmentLength)
                    {
                        outputLog?.WriteLine();
                        outputLog?.WriteLine("ERROR: The input mask has a different length (" + inputMask.Length.ToString() + ") than the alignment (" + alignment.AlignmentLength.ToString() + ")!");
                        return 1;
                    }

                    outputLog?.WriteLine();
                    outputLog?.WriteLine("Applying pre-computed mask...");

                    predictedMask = inputMask;

                    // Number of preserved columns.
                    int trues = predictedMask.MaskedStates.Count(x => x);

                    outputLog?.WriteLine();
                    outputLog?.WriteLine("Mask contains " + trues.ToString() + " (" + ((double)trues / predictedMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") preserved columns and " + (predictedMask.Length - trues).ToString() + " (" + (1 - (double)trues / predictedMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") deleted columns.");
                    outputLog?.WriteLine();
                }
                // The file contains a serialized model.
                else if (modelOrMask is ValidatedModel inputModel)
                {
                    model = inputModel;

                    outputLog?.WriteLine();

                    if (!model.Validated)
                    {
                        outputLog?.WriteLine("WARNING: The model has not been validated!");
                    }

                    // Model parameters.
                    threshold = arguments.Threshold ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateThreshold, DefaultParameters.Best => model.BestThreshold, DefaultParameters.Fast => model.FastThreshold, _ => model.FastThreshold };
                    bootstrapReplicateCount = arguments.BootstrapReplicates ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateBootstrapReplicates, DefaultParameters.Best => model.BestBootstrapReplicates, DefaultParameters.Fast => 0, _ => 0 };
                    bootstrapThreshold = arguments.BootstrapThreshold ?? arguments.DefaultParameters switch { DefaultParameters.Accurate => model.AccurateBootstrapThreshold, DefaultParameters.Best => model.BestBootstrapReplicates, DefaultParameters.Fast => 0.5, _ => 0.5 };

                    outputLog?.WriteLine("Applying model to " + alignment.AlignmentLength + " alignment columns...");

                    ProgressBar bar = null;
                    if (outputLog != null)
                    {
                        bar = new ProgressBar(outputLog);
                    }
                    bar?.Start();

                    // Use the model to classify the alignment columns.
                    predictedMask = model.Classify(alignment, arguments.Features, out features, threshold, bootstrapReplicateCount, bootstrapThreshold, arguments.MaxParallelism, bar != null ? new Progress<double>(bar.Progress) : null);

                    bar?.Finish();

                    // Number of preserved columns.
                    int trues = predictedMask.MaskedStates.Count(x => x);

                    outputLog?.WriteLine();
                    outputLog?.WriteLine("Mask contains " + trues.ToString() + " (" + ((double)trues / predictedMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") preserved columns and " + (predictedMask.Length - trues).ToString() + " (" + (1 - (double)trues / predictedMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") deleted columns.");
                    outputLog?.WriteLine();

                    // Compute the model confidence score.
                    double modelScore = 1 - predictedMask.Confidence.Select(x => x * (1 - x) * 4).Sum() / predictedMask.Length;

                    outputLog?.WriteLine("Model confidence score: " + modelScore.ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture));
                    outputLog?.WriteLine();
                }
                // The file does not contain a valid model or mask.
                else
                {
                    outputLog?.WriteLine();
                    outputLog?.WriteLine("ERROR: The model file " + arguments.InputModel + " does not contain a valid model or mask!");
                    outputLog?.WriteLine();
                    return 1;
                }
            }

            if (arguments.OutputKind == OutputKind.FilteredAlignment)
            {
                // Filter the alignment using the mask.
                Alignment filteredAlignment = alignment.Filter(predictedMask);

                if (arguments.Clean != null)
                {
                    outputLog?.WriteLine("Cleaning alignment...");

                    List<int> sequencesToRemove = new List<int>();

                    for (int i = 0; i < filteredAlignment.SequenceCount; i++)
                    {
                        int gaps = filteredAlignment.GetSequence(i).Count(x => x == '-');

                        if ((double)gaps / filteredAlignment.AlignmentLength >= arguments.Clean.Value)
                        {
                            sequencesToRemove.Add(i);
                        }
                    }

                    filteredAlignment = filteredAlignment.RemoveSequences(sequencesToRemove);

                    outputLog?.WriteLine(sequencesToRemove.Count + " sequences have been removed because they contained more than " + arguments.Clean.Value.ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + " gaps");
                    outputLog?.WriteLine();
                }

                // Save the alignment.
                Utilities.SaveAlignment(arguments, filteredAlignment, outputLog);
            }
            else
            {
                // Save the mask.
                Utilities.SaveMask(arguments, predictedMask, outputLog);
            }

            if (arguments.ReportFile != null && model != null)
            {
                return Utilities.CreateAlignmentReport(features, alignment is ProteinAlignment, model, predictedMask, threshold, bootstrapThreshold, bootstrapReplicateCount, arguments, outputLog);
            }
            else
            {
                return 0;
            }
        }
    }
}
