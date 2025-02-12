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
        // Compare two alignments, determining how many columns are present in both of them.
        public static int CompareAlignments(Arguments arguments, TextWriter outputLog)
        {
            // Read the first alignment from a file or from the standard input.
            arguments.InputAlignment = arguments.InputFirstCompare;
            Alignment alignment1 = Utilities.ReadAlignment(arguments, outputLog);

            if (alignment1 == null)
            {
                return 1;
            }

            outputLog?.WriteLine();

            // Read the second alignment from a file or from the standard input.
            arguments.InputAlignment = arguments.InputSecondCompare;
            Alignment alignment2 = Utilities.ReadAlignment(arguments, outputLog);

            if (alignment2 == null)
            {
                return 1;
            }

            outputLog?.WriteLine();

            // Create a mask to compare the two alignments.
            Mask mask = Utilities.CreateMaskFromAlignments(alignment1, alignment2, outputLog, "first", "second", "");

            // Return the comparison mask.
            if (arguments.OutputKind == OutputKind.Mask)
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("Using the second alignment as a mask for the first alignment...");

                // Number of preserved columns.
                int trues = mask.MaskedStates.Count(x => x);

                outputLog?.WriteLine();
                outputLog?.WriteLine("Mask contains " + trues.ToString() + " (" + ((double)trues / mask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") preserved columns and " + (mask.Length - trues).ToString() + " (" + (1 - (double)trues / mask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") deleted columns.");
                outputLog?.WriteLine();

                Console.Out.WriteLine(new string(mask.MaskedStates.Select(x => x ? '1' : '0').ToArray()));
            }
            // Count the columns that are in present in both alignments or only in one of them.
            else
            {
                // Columns in common to the two alignments show up as true in the mask.
                int commonColumns = mask.MaskedStates.Count(x => x);

                // The remaining columns from the original alignments are unique to each alignment.
                int uniqueTo1 = alignment1.AlignmentLength - commonColumns;
                int uniqueTo2 = alignment2.AlignmentLength - commonColumns;

                // Print the results
                outputLog?.WriteLine();

                outputLog?.WriteLine("╔═════════════════════════╦═════════════════════════╦═════════════════════════╗");
                outputLog?.WriteLine("║  Unique to alignment 1  ║  Unique to alignment 2  ║          Common         ║");
                outputLog?.WriteLine("╟─────────────────────────╫─────────────────────────╫─────────────────────────╢");

                string commonColumsString = commonColumns.ToString();
                string uniqueTo1String = uniqueTo1.ToString() + " ";
                string uniqueTo2String = uniqueTo2.ToString() + " ";

                outputLog?.Write("║");

                {
                    int padLeft = (25 - uniqueTo1String.Length) / 2;
                    int padRight = 25 - uniqueTo1String.Length - padLeft;

                    outputLog?.Write(new string(' ', padLeft));
                    outputLog?.Flush();
                    Console.Out.Write(uniqueTo1String);
                    Console.Out.Flush();

                    if (Console.IsOutputRedirected)
                    {
                        outputLog?.Write(uniqueTo1String);
                        outputLog?.Flush();
                    }

                    outputLog?.Write(new string(' ', padRight));
                }

                outputLog?.Write("║");

                {
                    int padLeft = (25 - uniqueTo2String.Length) / 2;
                    int padRight = 25 - uniqueTo2String.Length - padLeft;

                    outputLog?.Write(new string(' ', padLeft));
                    outputLog?.Flush();
                    Console.Out.Write(uniqueTo2String);
                    Console.Out.Flush();

                    if (Console.IsOutputRedirected)
                    {
                        outputLog?.Write(uniqueTo2String);
                        outputLog?.Flush();
                    }

                    outputLog?.Write(new string(' ', padRight));
                }

                outputLog?.Write("║");

                {
                    int padLeft = (25 - commonColumsString.Length) / 2;
                    int padRight = 25 - commonColumsString.Length - padLeft;

                    outputLog?.Write(new string(' ', padLeft));
                    outputLog?.Flush();
                    Console.Out.Write(commonColumsString);
                    Console.Out.Flush();

                    if (Console.IsOutputRedirected)
                    {
                        outputLog?.Write(commonColumsString);
                        outputLog?.Flush();
                    }

                    outputLog?.Write(new string(' ', padRight));
                }

                outputLog?.WriteLine("║");
                outputLog?.WriteLine("╚═════════════════════════╩═════════════════════════╩═════════════════════════╝");
                Console.Out.WriteLine();
            }

            return 0;
        }
    }
}
