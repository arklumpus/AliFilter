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
        // Compare two masks, determining for how many columns they agree or disagree.
        public static int CompareMasks(Arguments arguments, TextWriter outputLog)
        {
            // Read the alignment from a file or from the standard input.
            Alignment alignment = Utilities.ReadAlignment(arguments, outputLog);

            if (alignment == null)
            {
                return 1;
            }

            // Create a clone of the alignment (in case the masks contain different subsets of sequences).
            Alignment clone = alignment.Clone();

            outputLog?.WriteLine();

            // Read the first mask from another file or from a sequence in the alignment.
            arguments.InputMask = arguments.InputFirstCompare;
            Mask mask1 = Utilities.ReadMask(ref clone, arguments, outputLog);

            if (mask1 == null)
            {
                return 1;
            }

            outputLog?.WriteLine();

            // Read the second mask from another file or from a sequence in the alignment.
            arguments.InputMask = arguments.InputSecondCompare;
            Mask mask2 = Utilities.ReadMask(ref alignment, arguments, outputLog);

            if (mask2 == null)
            {
                return 1;
            }

            outputLog?.WriteLine();

            // Both masks preserve the column.
            int tt = 0;

            // The first mask preserves the column and the second mask deletes it.
            int tf = 0;

            // The first mask deletes the column and the second mask preserves it.
            int ft = 0;

            // Both masks delete the column.
            int ff = 0;

            for (int i = 0; i < alignment.AlignmentLength; i++)
            {
                if (mask1.MaskedStates[i] && mask2.MaskedStates[i])
                {
                    tt++;
                }
                else if (mask1.MaskedStates[i] && !mask2.MaskedStates[i])
                {
                    tf++;
                }
                else if (!mask1.MaskedStates[i] && mask2.MaskedStates[i])
                {
                    ft++;
                }
                else if (!mask1.MaskedStates[i] && !mask2.MaskedStates[i])
                {
                    ff++;
                }
            }

            string ttString = tt.ToString() + " ";
            string tfString = tf.ToString();
            string ftString = ft.ToString() + " ";
            string ffString = ff.ToString();

            // Print the results
            outputLog?.WriteLine();

            outputLog?.WriteLine("                      ╔═════════════════════╗");
            outputLog?.WriteLine("                      ║       Mask 2        ║");
            outputLog?.WriteLine("                      ╟──────────╥──────────╢");
            outputLog?.WriteLine("                      ║   Keep   ║  Delete  ║");
            outputLog?.WriteLine("╔══════════╤══════════╬══════════╬══════════╣");
            outputLog?.Write("║          │   Keep   ║");

            {
                int padLeft = (10 - ttString.Length) / 2;
                int padRight = 10 - ttString.Length - padLeft;

                outputLog?.Write(new string(' ', padLeft));
                outputLog?.Flush();
                Console.Out.Write(ttString);
                Console.Out.Flush();

                if (Console.IsOutputRedirected)
                {
                    outputLog?.Write(ttString);
                    outputLog?.Flush();
                }

                outputLog?.Write(new string(' ', padRight));
            }

            outputLog?.Write("║");

            {
                int padLeft = (10 - tfString.Length) / 2;
                int padRight = 10 - tfString.Length - padLeft;

                outputLog?.Write(new string(' ', padLeft));
                outputLog?.Flush();
                Console.Out.Write(tfString);
                Console.Out.Flush();

                if (Console.IsOutputRedirected)
                {
                    outputLog?.Write(tfString);
                    outputLog?.Flush();
                }

                outputLog?.Write(new string(' ', padRight));
            }

            outputLog?.Write("║");
            outputLog?.Flush();
            Console.Out.WriteLine();
            Console.Out.Flush();
            if (Console.IsOutputRedirected)
            {
                outputLog?.WriteLine();
                outputLog?.Flush();
            }

            outputLog?.WriteLine("║  Mask 1  ╞══════════╬══════════╬══════════╣");
            outputLog?.Write("║          │  Delete  ║");

            {
                int padLeft = (10 - ftString.Length) / 2;
                int padRight = 10 - ftString.Length - padLeft;

                outputLog?.Write(new string(' ', padLeft));
                outputLog?.Flush();
                Console.Out.Write(ftString);
                Console.Out.Flush();

                if (Console.IsOutputRedirected)
                {
                    outputLog?.Write(ftString);
                    outputLog?.Flush();
                }

                outputLog?.Write(new string(' ', padRight));
            }

            outputLog?.Write("║");

            {
                int padLeft = (10 - ffString.Length) / 2;
                int padRight = 10 - ffString.Length - padLeft;

                outputLog?.Write(new string(' ', padLeft));
                outputLog?.Flush();
                Console.Out.Write(ffString);
                Console.Out.Flush();

                if (Console.IsOutputRedirected)
                {
                    outputLog?.Write(ffString);
                    outputLog?.Flush();
                }

                outputLog?.Write(new string(' ', padRight));
            }

            outputLog?.Write("║");
            outputLog?.Flush();
            Console.Out.WriteLine();
            Console.Out.Flush();
            if (Console.IsOutputRedirected)
            {
                outputLog?.WriteLine();
                outputLog?.Flush();
            }

            outputLog?.WriteLine("╚══════════╧══════════╩══════════╩══════════╝");
            outputLog?.WriteLine();

            outputLog?.Write("Agreement: ");
            outputLog?.Flush();
            Console.Out.WriteLine((tt + ff).ToString());
            Console.Out.Flush();
            if (Console.IsOutputRedirected)
            {
                outputLog?.WriteLine((tt + ff).ToString());
                outputLog?.Flush();
            }

            outputLog?.Write("Disagreement: ");
            outputLog?.Flush();
            Console.Out.WriteLine((tf + ft).ToString());
            Console.Out.Flush();
            if (Console.IsOutputRedirected)
            {
                outputLog?.WriteLine((tf + ft).ToString());
                outputLog?.Flush();
            }

            return 0;
        }
    }
}
