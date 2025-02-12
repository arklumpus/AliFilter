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
        // Apply a bitwise operation to one or more alignment masks.
        public static int BitwiseOperation(Arguments arguments, IReadOnlyList<string> maskFiles, TextWriter outputLog)
        {
            if (maskFiles.Count == 0)
            {
                outputLog?.WriteLine();
                outputLog?.WriteLine("ERROR: no mask files supplied for bitwise operation!");
                return 1;
            }

            // The length of the truth table is 2^n.
            int tableLength = 1 << maskFiles.Count;

            string truthTable;

            if (arguments.BitwiseOperation.Equals("and", StringComparison.OrdinalIgnoreCase))
            {
                truthTable = new string('0', tableLength - 1) + "1";
            }
            else if (arguments.BitwiseOperation.Equals("or", StringComparison.OrdinalIgnoreCase))
            {
                truthTable = "0" + new string('1', tableLength - 1);
            }
            else if (arguments.BitwiseOperation.Equals("nor", StringComparison.OrdinalIgnoreCase))
            {
                truthTable = "1" + new string('0', tableLength - 1);
            }
            else if (arguments.BitwiseOperation.Equals("xor", StringComparison.OrdinalIgnoreCase))
            {
                truthTable = new string(Enumerable.Range(0, tableLength).Select(x => Popcnt(x) % 2 == 1 ? '1' : '0').ToArray());
            }
            else if (arguments.BitwiseOperation.Equals("nand", StringComparison.OrdinalIgnoreCase))
            {
                truthTable = new string('1', tableLength - 1) + "0";
            }
            else if (arguments.BitwiseOperation.Equals("xnor", StringComparison.OrdinalIgnoreCase))
            {
                truthTable = new string(Enumerable.Range(0, tableLength).Select(x => Popcnt(x) % 2 == 1 ? '0' : '1').ToArray());
            }
            else if (arguments.BitwiseOperation.Equals("not", StringComparison.OrdinalIgnoreCase))
            {
                if (tableLength == 2)
                {
                    truthTable = "10";
                }
                else
                {
                    outputLog?.WriteLine();
                    outputLog?.WriteLine("ERROR: \"not\" is not a valid operation for " + maskFiles.Count + " alignment masks!");
                    return 1;
                }
            }
            else
            {
                truthTable = new string(arguments.BitwiseOperation.Where(x => x == '0' || x == '1').ToArray());

                if (truthTable.Length != tableLength)
                {
                    outputLog?.WriteLine();
                    outputLog?.WriteLine("ERROR: invalid bitwise operation: \"" + truthTable + "\"!");
                    return 1;
                }
            }

            outputLog?.WriteLine();
            outputLog?.WriteLine("Reading " + maskFiles.Count + " masks...");

            List<Mask> masks = new List<Mask>(maskFiles.Count);

            for (int i = 0; i < maskFiles.Count; i++)
            {
                Alignment _ = null;

                arguments.InputMask = maskFiles[i];
                outputLog?.WriteLine();
                Mask mask = Utilities.ReadMask(ref _, arguments, outputLog);

                if (mask == null)
                {
                    return 1;
                }
                else
                {
                    if (i > 0)
                    {
                        if (mask.Length != masks[i - 1].Length)
                        {
                            outputLog?.WriteLine();
                            outputLog?.WriteLine("ERROR: mask " + maskFiles[i] + " has a different length (" + mask.Length.ToString() + ") than the preceding masks (" + masks[i - 1].Length.ToString() + ")!");
                            return 1;
                        }
                    }

                    masks.Add(mask);
                }
            }

            Mask finalMask = new Mask(Enumerable.Range(0, masks[0].Length).Select(x =>
            {
                int val = 0;

                for (int i = 0; i < masks.Count; i++)
                {
                    if (masks[i].MaskedStates[x])
                    {
                        val |= 1 << (masks.Count - 1 - i);
                    }
                }

                return truthTable[val] == '1';
            }));

            // Number of preserved columns.
            int trues = finalMask.MaskedStates.Count(x => x);

            outputLog?.WriteLine();
            outputLog?.WriteLine("Mask contains " + trues.ToString() + " (" + ((double)trues / finalMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") preserved columns and " + (finalMask.Length - trues).ToString() + " (" + (1 - (double)trues / finalMask.Length).ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + ") deleted columns.");
            outputLog?.WriteLine();

            Console.Out.WriteLine(new string(finalMask.MaskedStates.Select(x => x ? '1' : '0').ToArray()));

            return 0;
        }

        // From https://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel
        private static int Popcnt(int v)
        {
            v = v - ((v >> 1) & 0x55555555);                    // reuse input as temporary
            v = (v & 0x33333333) + ((v >> 2) & 0x33333333);     // temp
            return ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24; // count
        }
    }
}
