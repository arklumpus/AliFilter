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

using AliFilter.Streams;
using System.IO.Compression;

namespace AliFilter
{
    internal static partial class Utilities
    {
        // Deserialise a feature file.
        internal static (double[][] features, IReadOnlyList<double[,]> bootstrapReplicates, Mask mask) ReadFeatures(Arguments arguments, TextWriter outputLog)
        {
            // Necessary for the infinite parallel loop.
            static IEnumerable<bool> InfiniteLoop()
            {
                while (true)
                {
                    yield return true;
                }
            }

            List<double[]> data = new List<double[]>();
            List<double[,]> bootstrapReplicates = new List<double[,]>();
            List<bool> mask = new List<bool>();

            object listLock = new object();
            object IOLock = new object();
            long nextStartingPoint = 0;

            outputLog?.WriteLine("Reading features from file " + Path.GetFullPath(arguments.InputFeatures) + "...");


            long processed = 0;
            ProgressBar bar = null;

            if (outputLog != null)
            {
                bar = new ProgressBar(outputLog);
            }

            bar?.Start();

            using (FileStream inputStream = new FileStream(arguments.InputFeatures, FileMode.Open, FileAccess.Read))
            {
                long streamLength = inputStream.Length;

                // There are two bottlenecks here: I/O bottleneck (reading the compressed data from disk) and CPU bottleneck (decompressing the compressed data).
                // With the parallel loop, we can keep busy decompressing data while other threads are waiting on I/O.
                Parallel.ForEach(InfiniteLoop(), new ParallelOptions() { MaxDegreeOfParallelism = arguments.MaxParallelism }, (_, loopState) =>
                {
                    // Extract a chunk of data from the file.
                    MemoryStream streamToBeDecompressed = null;

                    lock (IOLock)
                    {
                        if (nextStartingPoint < streamLength)
                        {
                            inputStream.Seek(nextStartingPoint, SeekOrigin.Begin);

                            byte[] gzipStreamLength = new byte[8];
                            inputStream.ReadExactly(gzipStreamLength, 0, 8);

                            long length = BitConverter.ToInt64(gzipStreamLength);
                            long startPosition = inputStream.Position;

                            streamToBeDecompressed = new MemoryStream();

                            using (SubStream subs = new SubStream(inputStream, startPosition, length))
                            {
                                subs.CopyTo(streamToBeDecompressed);
                            }

                            streamToBeDecompressed.Seek(0, SeekOrigin.Begin);

                            nextStartingPoint = startPosition + length;
                        }
                        else
                        {
                            loopState.Break();
                        }
                    }

                    // Decompress and read the chunk of data.
                    if (streamToBeDecompressed != null)
                    {
                        using (GZipStream decompressedStream = new GZipStream(streamToBeDecompressed, CompressionMode.Decompress, leaveOpen: false))
                        using (BufferedStream bufferedStream = new BufferedStream(decompressedStream, 16384))
                        {
                            using (BinaryReader reader = new BinaryReader(bufferedStream))
                            {
                                List<double[]> currData = new List<double[]>();
                                List<double[,]> currBootstrapReplicates = new List<double[,]>();
                                List<bool> currMask = new List<bool>();

                                string programVersion = reader.ReadString();

                                if (!AliFilter.AlignmentFeatures.Features.FeaturesCompatible(programVersion))
                                {
                                    throw new Exception("The training features have been computed using an incompatible version of the program (" + programVersion + " instead of " + Program.Version + ")!");
                                }

                                string featureSignature = reader.ReadString();
                                if (featureSignature != arguments.Features.Signature)
                                {
                                    throw new Exception("The training data contain a different set of features than the current program (" + featureSignature + " instead of " + arguments.Features.Signature + ")!");
                                }

                                int bootstrapReplicateCount = reader.ReadInt32();

                                while (true)
                                {
                                    bool maskValue;

                                    try
                                    {
                                        maskValue = reader.ReadBoolean();
                                    }
                                    catch (EndOfStreamException)
                                    {
                                        break;
                                    }

                                    double[] columnFeatures = new double[arguments.Features.Count];
                                    double[,] columnReplicates = new double[arguments.Features.Count, bootstrapReplicateCount];

                                    bool anyNan = false;

                                    for (int i = 0; i < arguments.Features.Count; i++)
                                    {
                                        for (int j = 0; j < bootstrapReplicateCount + 1; j++)
                                        {
                                            if (j == 0)
                                            {
                                                columnFeatures[i] = reader.ReadDouble();
                                                if (!double.IsFinite(columnFeatures[i]))
                                                {
                                                    anyNan = true;
                                                }
                                            }
                                            else
                                            {
                                                columnReplicates[i, j - 1] = reader.ReadDouble();
                                                if (!double.IsFinite(columnReplicates[i, j - 1]))
                                                {
                                                    anyNan = true;
                                                }
                                            }
                                            
                                        }
                                    }

                                    if (!anyNan)
                                    {
                                        currData.Add(columnFeatures);
                                        if (bootstrapReplicateCount > 0)
                                        {
                                            currBootstrapReplicates.Add(columnReplicates);
                                        }
                                        currMask.Add(maskValue);
                                    }
                                }

                                lock (listLock)
                                {
                                    processed += streamToBeDecompressed.Length;

                                    bar?.Progress((double)processed / streamLength);

                                    data.AddRange(currData);
                                    bootstrapReplicates.AddRange(currBootstrapReplicates);
                                    mask.AddRange(currMask);
                                }
                            }
                        }

                        streamToBeDecompressed.Dispose();
                    }
                });
            }

            bar?.Finish();

            outputLog?.WriteLine("Read features for " + data.Count + " alignment columns.");
            int trues = mask.Count(x => x);

            outputLog?.WriteLine("Mask contains " + trues.ToString() + " (" + ((double)trues / mask.Count).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") preserved columns and " + (mask.Count - trues).ToString() + " (" + (1 - (double)trues / mask.Count).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") deleted columns.");
            outputLog?.WriteLine();

            if (arguments.Mistakes > 0)
            {
                int countMistakes = Math.Min((int)Math.Round(arguments.Mistakes * mask.Count), mask.Count);

                outputLog?.WriteLine("Randomly altering " + countMistakes.ToString() + " column assignments...");

                List<int> candidates = Enumerable.Range(0, mask.Count).ToList();
                int countToT = 0;
                int countToF = 0;

                Random rnd = new Random();

                for (int i = 0; i < countMistakes; i++)
                {
                    int ind = rnd.Next(0, candidates.Count);
                    int j = candidates[ind];
                    candidates.RemoveAt(ind);

                    if (mask[j])
                    {
                        countToF++;
                    }
                    else
                    {
                        countToT++;
                    }

                    mask[j] = !mask[j];
                }

                outputLog?.WriteLine(countToT.ToString() + " columns have been incorrectly preserved.");
                outputLog?.WriteLine(countToF.ToString() + " columns have been incorrectly deleted.");
                outputLog?.WriteLine();
            }

            return (data.ToArray(), bootstrapReplicates, new Mask(mask));
        }
    }
}
