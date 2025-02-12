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
        // Save an alignment to a file or to the standard output.
        internal static void SaveAlignment(Arguments arguments, Alignment alignment, TextWriter outputLog)
        {
            AlignmentFileFormat format = arguments.OutputFormat ?? arguments.InputFormat ?? AlignmentFileFormat.FASTA;

            if (!string.IsNullOrEmpty(arguments.OutputFile) && arguments.OutputFile != "stdout")
            {
                outputLog?.WriteLine("Exporting the filtered alignment in " + format.ToString() + " format to file " + Path.GetFullPath(arguments.OutputFile) + "...");

                FormatUtilities.GetAlignmentFormat(format).Write(arguments.OutputFile, alignment);
            }
            else
            {
                outputLog?.WriteLine("Writing the filtered alignment in " + format.ToString() + " format to the standard output...");
                outputLog?.WriteLine();

                FormatUtilities.GetAlignmentFormat(format).Write(Console.Out, alignment);
            }
        }
    }
}
