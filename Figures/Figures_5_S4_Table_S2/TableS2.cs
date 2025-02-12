/*
    AliFilter: A Machine Learning Approach to Alignment Filtering

    by Giorgio Bianchini, Rui Zhu, Francesco Cicconardi, Edmund RR Moody

    Source code for manuscript figures.

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

using MathNet.Numerics.Statistics;

namespace Figures_5_S4_Table_S2
{
    internal static partial class Program
    {
        static void CreateTableS2(TextWriter output)
        {
            // Read the runtime stats for the phylogenomic analysis.
            Dictionary<string, (int alignmentLength, int distinctPatterns, int ramRequired, List<double> runtimeHours, List<double> mlTreeLength)> results = ReadRuntimeStats();

            Dictionary<string, string> toolNames = new Dictionary<string, string>()
            {
                { "raw", "None" },
                { "alifilter", "AliFilter" },
                { "bmge", "BMGE" },
                { "clipkit", "ClipKIT" },
                { "gblocks", "Gblocks" },
                { "noisy", "Noisy" },
                { "trimal", "trimAl" },
            };

            // Print the header for Table S2.
            output.WriteLine("# Table S2");
            output.WriteLine();
            output.WriteLine("Filtering\tAlignment length\tDistinct patterns\tRAM required\tRuntime \tTree length\tMedian runtime  \tMedian tree length");

            foreach (KeyValuePair<string, (int alignmentLength, int distinctPatterns, int ramRequired, List<double> runtimeHours, List<double> mlTreeLength)> kvp in results)
            {
                double medianRuntime = kvp.Value.runtimeHours.Median();
                double medianTreeLength = kvp.Value.mlTreeLength.Median();

                for (int i = 0; i < kvp.Value.runtimeHours.Count; i++)
                {
                    if (i == 0)
                    {
                        output.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", toolNames[kvp.Key].PadRight(9), kvp.Value.alignmentLength.ToString().PadRight(16), kvp.Value.distinctPatterns.ToString().PadRight(17), (((double)kvp.Value.ramRequired / 1024).ToString("0") + " GB").PadRight(12), (kvp.Value.runtimeHours[i].ToString("0.00") + " h").PadRight(8), kvp.Value.mlTreeLength[i].ToString("0.00").PadRight(11), medianRuntime.ToString("0.00").PadRight(16), medianTreeLength.ToString("0.00").PadRight(18));
                    }
                    else
                    {
                        output.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", "".PadRight(9), "".PadRight(16), "".PadRight(17), "".PadRight(12), (kvp.Value.runtimeHours[i].ToString("0.00") + " h").PadRight(8), kvp.Value.mlTreeLength[i].ToString("0.00").PadRight(11), "".PadRight(16), "".PadRight(18));
                    }
                }
            }
        }
    }
}
