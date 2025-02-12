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

using AliFilter.AlignmentFormatting;

namespace AliFilter
{
    internal static partial class Utilities
    {
        // Read a mask from an alignment sequence or a file.
        internal static Mask ReadMask(ref Alignment alignment, Arguments arguments, TextWriter outputLog)
        {
            Mask mask;

            // Read the mask from a sequence in the alignment.
            if (alignment != null && alignment.TryGetSequence(arguments.InputMask, out IEnumerable<char> maskSequence))
            {
                outputLog?.WriteLine("Reading mask from alignment sequence " + arguments.InputMask + "...");

                alignment = alignment.RemoveSequences(arguments.InputMask);

                try
                {
                    mask = new Mask(new string(maskSequence.ToArray()));
                }
                catch (Exception e)
                {
                    outputLog?.WriteLine();
                    outputLog?.WriteLine("An error occurred while reading the mask!");
                    outputLog?.WriteLine(e.Message);
                    mask = null;
                }
            }
            else
            {
                outputLog?.WriteLine("Reading mask from file " + arguments.InputMask + "...");

                // Try to read the mask file as an alignment file.
                Alignment maskedAlignment = null;


                if (alignment != null)
                {
                    try
                    {
                        AlignmentFileFormat? alignmentFormat = null;

                        maskedAlignment = FormatUtilities.ReadAlignment(arguments.InputMask, ref alignmentFormat, arguments.AlignmentType);

                        if (maskedAlignment != null)
                        {
                            outputLog?.WriteLine();
                            outputLog?.WriteLine("Read " + maskedAlignment.SequenceCount.ToString() + " " + arguments.AlignmentType.ToString() + " sequences in " + alignmentFormat.ToString() + " format.");
                            outputLog?.WriteLine("Masked alignment length: " + maskedAlignment.AlignmentLength.ToString() + " residues.");
                        }
                    }
                    catch
                    {
                        maskedAlignment = null;
                    }
                }

                if (maskedAlignment != null)
                {
                    mask = CreateMaskFromAlignments(alignment, maskedAlignment, outputLog);
                }
                else
                {
                    // Assume that the file contains a plain string of 0s and 1s.
                    try
                    {
                        mask = new Mask(new string(File.ReadAllText(arguments.InputMask).Where(x => !char.IsWhiteSpace(x)).ToArray()));
                    }
                    catch (Exception e)
                    {
                        outputLog?.WriteLine();
                        outputLog?.WriteLine("An error occurred while reading the mask!");
                        outputLog?.WriteLine(e.Message);
                        mask = null;
                    }
                }
            }

            if (mask != null)
            {
                int total = mask.Length;
                int trues = mask.MaskedStates.Count(x => x);

                outputLog?.WriteLine();
                outputLog?.WriteLine("The mask contains " + trues.ToString() + " (" + ((double)trues / total).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") preserved columns and " + (total - trues).ToString() + " (" + (1 - (double)trues / total).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") deleted columns.");
            }

            return mask;
        }

        // Create a mask by comparing two alignments.
        internal static Mask CreateMaskFromAlignments(Alignment alignment, Alignment maskedAlignment, TextWriter outputLog, string alignmentName = "full", string maskedAlignmentName = "masked", string temporarily = " temporarily")
        {
            // Ensure that the full alignment and the masked alignment contain the same sequences in the same order.
            (Alignment fullAlignmentFixed, Alignment maskedAlignmentFixed) = Alignment.MakeConsistent(alignment, maskedAlignment, out string[] removedFrom1, out string[] removedFrom2);

            if (removedFrom1.Length == 0 && removedFrom2.Length == 0)
            {
                outputLog?.WriteLine("The " + alignmentName + " alignment and the " + maskedAlignmentName + " alignment contain the same sequences.");
            }
            else
            {
                if (removedFrom1.Length > 0)
                {
                    outputLog?.WriteLine(removedFrom1.Length.ToString() + " sequences were" + temporarily + " removed from the " + alignmentName + " alignment because they were missing from the " + maskedAlignmentName + " alignment.");
                }

                if (removedFrom2.Length > 0)
                {
                    outputLog?.WriteLine(removedFrom2.Length.ToString() + " sequences were removed from the " + maskedAlignmentName + " alignment because they were missing from the " + alignmentName + " alignment.");
                }
            }

            Mask mask;

            try
            {
                mask = new Mask(fullAlignmentFixed, maskedAlignmentFixed);
            }
            catch (Exception e)
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("An error occurred while computing the mask!");
                outputLog?.WriteLine(e.Message);
                mask = null;
            }

            return mask;
        }
    }
}
