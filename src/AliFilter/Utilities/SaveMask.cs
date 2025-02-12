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
    internal static partial class Utilities
    {
        /// <summary>
        /// Save a mask to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> on which the <paramref name="mask"/> will be saved.</param>
        /// <param name="mask">The <see cref="Mask"/> to save.</param>
        /// <param name="type">The type of mask to write.</param>
        internal static void SaveMask(TextWriter writer, Mask mask, MaskType type)
        {
            if (type == MaskType.Binary)
            {
                for (int i = 0; i < mask.Length; i++)
                {
                    writer.Write(mask.MaskedStates[i] ? "1" : "0");
                }
            }
            else if (type == MaskType.Fuzzy)
            {
                for (int i = 0; i < mask.Length; i++)
                {
                    char c = (char)(int)Math.Round(Math.Max(126 + 10 * Math.Log10(mask.MaskedStates[i] ? mask.Confidence[i] : (1 - mask.Confidence[i])), 33));
                    writer.Write(c);
                }
            }
            else if (type == MaskType.Float)
            {
                for (int i = 0; i < mask.Length; i++)
                {
                    double val = mask.MaskedStates[i] ? mask.Confidence[i] : (1 - mask.Confidence[i]);

                    writer.Write(val.ToString(System.Globalization.CultureInfo.InvariantCulture));

                    if (i < mask.Length - 1)
                    {
                        writer.Write(" ");
                    }
                }
            }

            writer.WriteLine();
        }

        /// <summary>
        /// Save a mask to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> on which the <paramref name="mask"/> will be saved.</param>
        /// <param name="mask">The <see cref="Mask"/> to save.</param>
        /// <param name="type">The type of mask to write.</param>
        /// <param name="leaveOpen">Whether the stream should be left open after the mask has been written.</param>
        internal static void SaveMask(Stream stream, Mask mask, MaskType type, bool leaveOpen = true)
        {
            using (TextWriter sw = new StreamWriter(stream, leaveOpen: leaveOpen))
            {
                SaveMask(sw, mask, type);
            }
        }

        /// <summary>
        /// Save a mask to a file.
        /// </summary>
        /// <param name="fileName">The file to which the <paramref name="mask"/> will be saved.</param>
        /// <param name="mask">The <see cref="Mask"/> to save.</param>
        /// <param name="type">The type of mask to write.</param>
        internal static void SaveMask(string fileName, Mask mask, MaskType type)
        {
            using (FileStream fs = File.Create(fileName))
            {
                SaveMask(fs, mask, type);
            }
        }

        // Save a mask to a file or to the standard output.
        internal static void SaveMask(Arguments arguments, Mask mask, TextWriter outputLog)
        {
            MaskType maskType = arguments.OutputKind switch { OutputKind.Mask => MaskType.Binary, OutputKind.FuzzyMask => MaskType.Fuzzy, OutputKind.FloatMask => MaskType.Float, _ => MaskType.Binary };

            if (!string.IsNullOrEmpty(arguments.OutputFile) && arguments.OutputFile != "stdout")
            {
                outputLog?.WriteLine("Exporting the mask to file " + Path.GetFullPath(arguments.OutputFile) + "...");

                SaveMask(arguments.OutputFile, mask, maskType);
            }
            else
            {
                outputLog?.WriteLine("Writing the mask to the standard output...");
                outputLog?.WriteLine();

                SaveMask(Console.Out, mask, maskType);
            }
        }
    }
}
