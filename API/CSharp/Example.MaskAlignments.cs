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
using AliFilter.Models;
using AliFilter;
using AliFilter.AlignmentFeatures;
using System.Diagnostics;

namespace AliFilterExample
{
    internal partial class Example
    {
        /// <summary>
        /// Example 1: create an alignment mask and filter an alignment using the "best" preset.
        /// </summary>
        static void CreateAlignmentMaskAndFilter()
        {
            // Load an alignment in PHYLIP or FASTA format.
            Alignment alignment = Alignment.FromFile("../../../Data/example.phy");

            // Load a validated AliFilter model.
            ValidatedModel model = ValidatedModel.FromFile("../../../Data/alifilter.validated.json");

            // Compute an alignment mask according to the model.
            Mask mask = model.GetMask(alignment);

            // Create an alignment that has been filtered with the specified mask. 
            Alignment filteredAlignment = alignment.Filter(mask);

            // Save the alignment in PHYLIP format.
            filteredAlignment.Save("test.phy", AlignmentFileFormat.PHYLIP);
        }

        /// <summary>
        /// Example 2: compute alignment features, then create multiple masks with different thresholds.
        /// </summary>
        static void ComputeFeaturesAndCreateMasks()
        {
            // Load an alignment in PHYLIP or FASTA format.
            Alignment alignment = Alignment.FromFile("../../../Data/example.phy");

            // Compute alignment features.
            double[][] features = Features.DefaultFeatures.ComputeAll(alignment);

            // Load a validated AliFilter model.
            ValidatedModel model = ValidatedModel.FromFile("../../../Data/alifilter.validated.json");

            // Create an array to store the masks.
            Mask[] masks = new Mask[101];

            // Compute the alignment masks.
            for (int i = 0; i < masks.Length; i++)
            {
                double maskThreshold = i * 0.01; // Threshold value

                // Compute the mask.
                masks[i] = model.GetMask(features, threshold: maskThreshold);
            }

            // Create the output file.
            using (StreamWriter sw = new StreamWriter("masks.txt"))
            {
                for (int i = 0; i < masks.Length; i++)
                {
                    // Convert each mask to a string and write it.
                    sw.WriteLine(masks[i].ToString());

                    // You may also want to look at the Mask.Save overloads.
                }
            }
        }

        /// <summary>
        /// Example 3: Compute bootstrap replicates and create a mask using the "accurate" preset.
        /// </summary>
        static void ComputeBootstrapReplicatesAndGetAccurateMask()
        {
            // Load an alignment in PHYLIP or FASTA format.
            Alignment alignment = Alignment.FromFile("../../../Data/example.phy");

            // Compute alignment features.
            double[][] features = Features.DefaultFeatures.ComputeAll(alignment);

            // Load a validated AliFilter model.
            ValidatedModel model = ValidatedModel.FromFile("../../../Data/alifilter.validated.json");

            // Number of bootstrap replicates.
            int bootstrapReplicatesCount = model.AccurateBootstrapReplicates;

            // Compute the bootstrap replicate features. This will take a while.
            double[][,] bootstrapReplicateFeatures = Features.DefaultFeatures.ComputeAllBootstrapReplicates(alignment, bootstrapReplicatesCount);

            // Create the alignment mask using the computed bootstrap replicates and the "accurate" parameter preset.
            Mask mask = model.GetMask(features, bootstrapReplicates: bootstrapReplicateFeatures, defaultParameters: DefaultParameters.Accurate);

            // Note that the following produces the same result without having to manually compute the features and bootstrap replicates:
            Mask mask2 = model.GetMask(alignment, defaultParameters: DefaultParameters.Accurate);

            // Verify that the two masks are identical.
            Debug.Assert(mask.ToString() == mask2.ToString());

            // Create an alignment that has been filtered with the specified mask. 
            Alignment filteredAlignment = alignment.Filter(mask);

            // Save the alignment in PHYLIP format.
            filteredAlignment.Save("accurate.phy", AlignmentFileFormat.PHYLIP);
        }
    }
}
