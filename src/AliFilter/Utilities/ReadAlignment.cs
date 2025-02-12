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

using Accord.Math;
using AliFilter.AlignmentFormatting;

namespace AliFilter
{
    internal static partial class Utilities
    {
        // Read an alignment from a file or from the standard input.
        internal static Alignment ReadAlignment(Arguments arguments, TextWriter outputLog)
        {
            Alignment alignment;
            AlignmentFileFormat? alignmentFormat = arguments.InputFormat;

            try
            {
                if (arguments.InputAlignment == "stdin" || arguments.InputAlignment == "-" || arguments.InputAlignment == "--" || string.IsNullOrEmpty(arguments.InputAlignment))
                {
                    outputLog?.WriteLine("Reading alignment from the standard input...");
                    Stream stdin = Console.OpenStandardInput();
                    alignment = FormatUtilities.ReadAlignment(stdin, ref alignmentFormat, arguments.AlignmentType, false);
                }
                else
                {
                    outputLog?.WriteLine("Reading alignment from file " + Path.GetFullPath(arguments.InputAlignment) + "...");
                    alignment = FormatUtilities.ReadAlignment(arguments.InputAlignment, ref alignmentFormat, arguments.AlignmentType);
                }
            }
            catch (Exception e)
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("An error occurred while reading the alignment!");
                outputLog?.WriteLine(e.Message);
                alignment = null;
                alignmentFormat = null;
            }

            if (alignment == null)
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("The alignment format could not be determined, or an error occurred while reading the alignment!");

                return null;
            }
            else
            {
                arguments.AlignmentType = (AlignmentType)alignment.Tags["AlignmentType"];

                outputLog?.WriteLine();
                outputLog?.WriteLine("Read " + alignment.SequenceCount.ToString() + " " + arguments.AlignmentType.ToString() + " sequences in " + alignmentFormat.ToString() + " format.");
                outputLog?.WriteLine("Alignment length: " + alignment.AlignmentLength.ToString() + " residues.");

                arguments.InputFormat = alignmentFormat;

                if (!string.IsNullOrEmpty(arguments.Remove))
                {
                    if (alignment.TryGetSequence(arguments.Remove, out _))
                    {
                        outputLog?.WriteLine();
                        outputLog?.WriteLine("Removing sequence " + arguments.Remove + ".");

                        alignment = alignment.RemoveSequences(arguments.Remove);
                    }
                    else
                    {
                        outputLog?.WriteLine();
                        outputLog?.WriteLine("Reading list of sequences to remove from file " + Path.GetFullPath(arguments.Remove));
                        
                        string[] sequenceNamesInFile = File.ReadLines(arguments.Remove).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        outputLog?.WriteLine("Read " + sequenceNamesInFile.Length.ToString() + " sequence names.");
                        
                        string[] sequencesToRemove = sequenceNamesInFile.Where(x => alignment.TryGetSequence(x, out _)).ToArray();
                        outputLog?.WriteLine("Removing " + sequencesToRemove.Length.ToString() + " sequences.");

                        alignment = alignment.RemoveSequences(sequencesToRemove);
                    }
                }

                if (!string.IsNullOrEmpty(arguments.Keep))
                {
                    outputLog?.WriteLine();
                    outputLog?.WriteLine("Reading list of sequences to keep from file " + Path.GetFullPath(arguments.Keep));

                    string[] sequenceNamesInFile = File.ReadLines(arguments.Keep).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    outputLog?.WriteLine("Read " + sequenceNamesInFile.Length.ToString() + " sequence names.");

                    string[] sequencesToRemove = alignment.SequenceNames.Where(x => !sequenceNamesInFile.Contains(x)).ToArray();
                    outputLog?.WriteLine("Keeping " + (alignment.SequenceCount - sequencesToRemove.Length).ToString() + " sequences.");

                    alignment = alignment.RemoveSequences(sequencesToRemove);
                }

                return alignment;
            }
        }

    }
}
