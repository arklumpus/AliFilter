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

using System.Text;

namespace AliFilter
{
    internal class Program
    {
        /// <summary>
        /// Returns the version of the program.
        /// </summary>
        public static string Version
        {
            get
            {
                return typeof(Program).Assembly.GetName().Version.ToString(3);
            }
        }

        /// <summary>
        /// Program tasks.
        /// </summary>
        internal enum ProgramTask
        {
            /// <summary>
            /// Compute alignment features.
            /// </summary>
            ComputeFeatures,

            /// <summary>
            /// Train a model.
            /// </summary>
            TrainModel,

            /// <summary>
            /// Validate a model.
            /// </summary>
            ValidateModel,

            /// <summary>
            /// Test a model.
            /// </summary>
            TestModel,

            /// <summary>
            /// Test a model given an alignment and a mask.
            /// </summary>
            TestModelOnAlignment,

            /// <summary>
            /// Filter an alignment.
            /// </summary>
            FilterAlignment,

            /// <summary>
            /// Compare two alignments.
            /// </summary>
            CompareAlignments,

            /// <summary>
            /// Compare two alignment masks.
            /// </summary>
            CompareMasks,

            /// <summary>
            /// Apply a bitwise operation to one or more alignment masks.
            /// </summary>
            BitwiseOperation,

            /// <summary>
            /// Suggest which alignments to use for training, validation and test.
            /// </summary>
            SuggestSets
        }

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>0 if no error occured, or a different value if an error occurs.</returns>
        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            ArgumentParser argParser = new ArgumentParser();
            ProgramTask? task = argParser.ParseArguments(args);

            Arguments arguments = argParser.Arguments;
            TextWriter outputLog = argParser.OutputLog;

            if (argParser.ShowHelp)
            {
                return 0;
            }
            
            if (argParser.ShowUsage)
            {
                return 64;
            }

            if (args.Length > 0)
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("AliFilter was called with the following arguments:");
                outputLog?.WriteLine("    " + args.Aggregate((a, b) => a + " " + b));
                outputLog?.WriteLine();
            }

            if (task != null)
            {
                switch (task.Value)
                {
                    case ProgramTask.ComputeFeatures:
                        outputLog?.WriteLine("Task: compute alignment features.");
                        outputLog?.WriteLine();
                        break;

                    case ProgramTask.TrainModel:
                        outputLog?.WriteLine("Task: train model.");
                        outputLog?.WriteLine();
                        break;

                    case ProgramTask.ValidateModel:
                        outputLog?.WriteLine("Task: validate model.");
                        outputLog?.WriteLine();
                        break;

                    case ProgramTask.TestModel:
                    case ProgramTask.TestModelOnAlignment:
                        outputLog?.WriteLine("Task: test model.");
                        outputLog?.WriteLine();
                        break;

                    case ProgramTask.CompareAlignments:
                        outputLog?.WriteLine("Task: compare alignments.");
                        outputLog?.WriteLine();
                        break;

                    case ProgramTask.CompareMasks:
                        outputLog?.WriteLine("Task: compare alignment masks.");
                        outputLog?.WriteLine();
                        break;

                    case ProgramTask.FilterAlignment:
                        outputLog?.WriteLine("Task: filter alignment.");
                        outputLog?.WriteLine();
                        break;

                    case ProgramTask.SuggestSets:
                        outputLog?.WriteLine("Task: suggest training, validation, and test sets.");
                        outputLog?.WriteLine();
                        break;
                }

                if (argParser.SpecifiedArguments.Count > 0)
                {
                    outputLog?.WriteLine("WARNING: Arguments " + argParser.SpecifiedArguments.Select(x => x.Replace("{,}", "").TrimEnd('=', ':')).Aggregate((a, b) => a + ", " + b) + " will be ignored for this task!");
                    outputLog?.WriteLine();
                }

                switch (task.Value)
                {
                    case ProgramTask.ComputeFeatures:
                        return Tasks.ComputeFeatures(arguments, outputLog);

                    case ProgramTask.TrainModel:
                        return Tasks.TrainModel(arguments, outputLog);

                    case ProgramTask.ValidateModel:
                        return Tasks.ValidateModel(arguments, outputLog);

                    case ProgramTask.TestModel:
                        return Tasks.TestModel(arguments, outputLog);

                    case ProgramTask.TestModelOnAlignment:
                        return Tasks.TestModelOnAlignment(arguments, outputLog);

                    case ProgramTask.CompareAlignments:
                        return Tasks.CompareAlignments(arguments, outputLog);

                    case ProgramTask.CompareMasks:
                        return Tasks.CompareMasks(arguments, outputLog);

                    case ProgramTask.FilterAlignment:
                        return Tasks.FilterAlignment(arguments, outputLog);

                    case ProgramTask.BitwiseOperation:
                        return Tasks.BitwiseOperation(arguments, argParser.UnrecognisedArguments, outputLog);

                    case ProgramTask.SuggestSets:
                        return Tasks.SuggestSets(arguments, outputLog);
                }
            }

            outputLog?.WriteLine();
            outputLog?.WriteLine("ERROR: Unknown task!");
            return 1;
        }
    }
}
