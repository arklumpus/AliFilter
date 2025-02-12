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

using System.IO.Compression;

namespace AliFilter
{
    internal static partial class Tasks
    {
        // Read an alignment, compute the features, and save them to an output file.
        public static int ComputeFeatures(Arguments arguments, TextWriter outputLog)
        {
            int bootstrapReplicates = arguments.FeaturesForValidation ? 1000 : 0;

            (Mask mask, double[,,] data)? computedFeatures = ComputeFeaturesCommon(arguments, outputLog, bootstrapReplicates);

            if (computedFeatures == null)
            {
                return 1;
            }
            else
            {
                return SaveFeatures(arguments, outputLog, computedFeatures.Value.mask, bootstrapReplicates, computedFeatures.Value.data);
            }
        }

        // Read an alignment and a mask and compute the features.
        private static (Mask mask, double[,,] data)? ComputeFeaturesCommon(Arguments arguments, TextWriter outputLog, int bootstrapReplicates)
        {
            // Read the alignment from a file or from the standard input.
            Alignment alignment = Utilities.ReadAlignment(arguments, outputLog);

            if (alignment == null)
            {
                return null;
            }

            // Read the mask from another file or from a sequence in the alignment.
            Mask mask = Utilities.ReadMask(ref alignment, arguments, outputLog);

            if (mask == null)
            {
                return null;
            }

            Random[] randoms = new Random[bootstrapReplicates];

            Random masterRandom = new Random();

            for (int i = 0; i < bootstrapReplicates; i++)
            {
                randoms[i] = new Random(masterRandom.Next());
            }

            double[,,] data = new double[alignment.AlignmentLength, arguments.Features.Count, bootstrapReplicates + 1];

            // Set up the progress bar.
            ProgressBar bar = null;

            if (bootstrapReplicates > 0 && outputLog != null)
            {
                bar = new ProgressBar(outputLog);
            }

            bar?.Start();

            object lockObject = new object();
            int progress = 0;

            Parallel.For(0, bootstrapReplicates + 1, new ParallelOptions() { MaxDegreeOfParallelism = arguments.MaxParallelism }, i =>
            {
                double[][] tempData;

                if (i == 0)
                {
                    tempData = Utilities.ComputeFeatures(alignment, arguments.Features, bootstrapReplicates == 0 ? arguments.MaxParallelism : 1, bootstrapReplicates == 0 ? outputLog : null);
                }
                else
                {
                    Alignment bootstrapReplicate = alignment.Bootstrap(randoms[i - 1]);

                    tempData = Utilities.ComputeFeatures(bootstrapReplicate, arguments.Features, 1, null);
                }

                for (int j = 0; j < tempData.Length; j++)
                {
                    for (int k = 0; k < tempData[j].Length; k++)
                    {
                        data[j, k, i] = tempData[j][k];
                    }
                }

                if (bootstrapReplicates > 0)
                {
                    lock (lockObject)
                    {
                        progress++;
                        bar?.Progress((double)progress / (bootstrapReplicates + 1));
                    }
                }
            });

            bar?.Finish();

            return (mask, data);
        }

        // Save features to an output file.
        private static int SaveFeatures(Arguments arguments, TextWriter outputLog, Mask mask, int bootstrapReplicates, double[,,] data)
        {
            outputLog?.WriteLine((arguments.Append ? "Appending" : "Writing") + " features to file " + Path.GetFullPath(arguments.OutputFile) + "...");

            // Save the features to the output file.
            using FileStream outputStream = new FileStream(arguments.OutputFile, arguments.Append ? FileMode.OpenOrCreate : FileMode.Create);

            // If we are appending, move to the end of the file.
            outputStream.Seek(0, SeekOrigin.End);
            long startPosition = outputStream.Position;

            // The size of the compressed data cannot be predicted until the actual compression takes place;
            // thus, leave some space here for the data size that will be filled in later.
            outputStream.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });

            // Compress the feature data.
            using (GZipStream compressedStream = new GZipStream(outputStream, CompressionLevel.SmallestSize, leaveOpen: true))
            using (BinaryWriter bw = new BinaryWriter(compressedStream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                bw.Write(Program.Version);
                bw.Write(arguments.Features.Signature);
                bw.Write(bootstrapReplicates);

                for (int i = 0; i < data.GetLength(0); i++)
                {
                    bw.Write(mask.MaskedStates[i]);

                    for (int j = 0; j < data.GetLength(1); j++)
                    {
                        for (int k = 0; k < data.GetLength(2); k++)
                        {
                            bw.Write(data[i, j, k]);
                        }
                    }
                }
            }

            long dataSize = outputStream.Position - startPosition - 8;

            outputStream.Seek(startPosition, SeekOrigin.Begin);

            using (BinaryWriter bw = new BinaryWriter(outputStream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                bw.Write(dataSize);
            }

            return 0;
        }
    }
}
