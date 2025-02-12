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

namespace AliFilter
{
    internal static partial class Tasks
    {
        // Read a set of alignments from a folder, and suggest subsets to use for training, test and validation.
        public static int SuggestSets(Arguments arguments, TextWriter outputLog)
        {
            outputLog?.WriteLine();
            outputLog?.WriteLine("Reading alignments from " + Path.GetFullPath(arguments.SuggestFolder) + "...");

            string[] alignmentFiles = Directory.GetFiles(arguments.SuggestFolder);

            if (alignmentFiles.Length < 3)
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("The specified folder " + arguments.SuggestFolder + " contains fewer than 3 alignments!");
                return 1;
            }

            List<(string, int)> alignmentsWithLength = new List<(string, int)>();

            for (int i = 0; i < alignmentFiles.Length; i++)
            {
                // Read each alignment from a file in the specified folder.
                arguments.InputAlignment = alignmentFiles[i];
                Alignment alignment = Utilities.ReadAlignment(arguments, outputLog);

                if (alignment == null)
                {
                    return 1;
                }

                alignmentsWithLength.Add((Path.GetFileName(alignmentFiles[i]), alignment.AlignmentLength));
            }

            // Sort the alignments based on their length.
            alignmentsWithLength.Sort((a, b) => a.Item2 - b.Item2);

            // Determine the size of the training, validation and test sets.
            int totalCount = Math.Min(arguments.SuggestCount, alignmentsWithLength.Count);

            int testSetSize = (int)(arguments.SuggestSplit[2] * totalCount / arguments.SuggestSplit.Sum());
            int validationSetSize = (int)(arguments.SuggestSplit[1] * totalCount / arguments.SuggestSplit.Sum());
            int trainingSetSize = totalCount - testSetSize - validationSetSize;

            // Select equispaced alignments for the training set.
            (string, int)[] trainingSet = new (string, int)[trainingSetSize];
            int[] trainingSetIndices = new int[trainingSetSize];

            if (trainingSetSize == 1)
            {
                int index = alignmentsWithLength.Count / 2;
                trainingSetIndices[0] = index;
                trainingSet[0] = alignmentsWithLength[index];
            }
            else
            {
                double factor = (double)(alignmentsWithLength.Count - 1) / (trainingSetSize - 1);
                for (int i = 0; i < trainingSetSize; i++)
                {
                    int index = (int)(i * factor);

                    trainingSetIndices[i] = index;
                    trainingSet[i] = alignmentsWithLength[index];
                }
            }

            for (int i = trainingSetSize - 1; i >= 0; i--)
            {
                alignmentsWithLength.RemoveAt(trainingSetIndices[i]);
            }

            // Select equispaced alignments for the validation set.
            (string, int)[] validationSet = new (string, int)[validationSetSize];
            int[] validationSetIndices = new int[validationSetSize];
            if (validationSetSize == 1)
            {
                int index = alignmentsWithLength.Count / 2;
                validationSetIndices[0] = index;
                validationSet[0] = alignmentsWithLength[index];
            }
            else
            {
                double factor = (double)(alignmentsWithLength.Count - 1) / (validationSetSize - 1);
                for (int i = 0; i < validationSetSize; i++)
                {
                    int index = (int)(i * factor);
                    validationSetIndices[i] = index;
                    validationSet[i] = alignmentsWithLength[index];
                }
            }

            for (int i = validationSetSize - 1; i >= 0; i--)
            {
                alignmentsWithLength.RemoveAt(validationSetIndices[i]);
            }

            // Select equispaced alignments for the test set.
            (string, int)[] testSet = new (string, int)[testSetSize];
            int[] testSetIndices = new int[testSetSize];
            if (testSetSize == 1)
            {
                int index = alignmentsWithLength.Count / 2;
                testSetIndices[0] = index;
                testSet[0] = alignmentsWithLength[index];
            }
            else
            {
                double factor = (double)(alignmentsWithLength.Count - 1) / (testSetSize - 1);
                for (int i = 0; i < testSetSize; i++)
                {
                    int index = (int)(i * factor);
                    testSetIndices[i] = index;
                    testSet[i] = alignmentsWithLength[index];
                }
            }

            for (int i = testSetSize - 1; i >= 0; i--)
            {
                alignmentsWithLength.RemoveAt(testSetIndices[i]);
            }

            outputLog?.WriteLine();
            Console.Out.WriteLine("Suggested training set:");

            for (int i = 0; i < trainingSet.Length; i++)
            {
                outputLog?.Write("    ");
                outputLog?.Flush();
                Console.Out.Write(trainingSet[i].Item1);
                outputLog?.Write(" [" + trainingSet[i].Item2.ToString() + "]");
                outputLog?.Flush();
                Console.Out.WriteLine();
            }

            outputLog?.WriteLine();
            Console.Out.WriteLine("Suggested validation set:");

            for (int i = 0; i < validationSet.Length; i++)
            {
                outputLog?.Write("    ");
                outputLog?.Flush();
                Console.Out.Write(validationSet[i].Item1);
                outputLog?.Write(" [" + validationSet[i].Item2.ToString() + "]");
                outputLog?.Flush();
                Console.Out.WriteLine();
            }

            outputLog?.WriteLine();
            Console.Out.WriteLine("Suggested test set:");

            for (int i = 0; i < testSet.Length; i++)
            {
                outputLog?.Write("    ");
                outputLog?.Flush();
                Console.Out.Write(testSet[i].Item1);
                outputLog?.Write(" [" + testSet[i].Item2.ToString() + "]");
                outputLog?.Flush();
                Console.Out.WriteLine();
            }

            if (!string.IsNullOrEmpty(arguments.SuggestTrainingOut))
            {
                using (StreamWriter sw = new StreamWriter(arguments.SuggestTrainingOut))
                {
                    sw.NewLine = "\n";
                    for (int i = 0; i < trainingSet.Length; i++)
                    {
                        sw.WriteLine(trainingSet[i].Item1);
                    }
                }
            }

            if (!string.IsNullOrEmpty(arguments.SuggestValidationOut))
            {
                using (StreamWriter sw = new StreamWriter(arguments.SuggestValidationOut))
                {
                    sw.NewLine = "\n";
                    for (int i = 0; i < validationSet.Length; i++)
                    {
                        sw.WriteLine(validationSet[i].Item1);
                    }
                }
            }

            if (!string.IsNullOrEmpty(arguments.SuggestTestOut))
            {
                using (StreamWriter sw = new StreamWriter(arguments.SuggestTestOut))
                {
                    sw.NewLine = "\n";
                    for (int i = 0; i < testSet.Length; i++)
                    {
                        sw.WriteLine(testSet[i].Item1);
                    }
                }
            }

            return 0;
        }
    }
}
