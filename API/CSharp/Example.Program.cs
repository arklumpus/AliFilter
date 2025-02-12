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

namespace AliFilterExample
{
    internal partial class Example
    {
        static void Main(string[] args)
        {
            /**
             * Examples for computing features and creating alignment masks.
             * These example methods are defined in Example.MaskAlignments.cs 
             */

            // Create an alignment mask and filter an alignment using the "best" preset.
            CreateAlignmentMaskAndFilter();

            // Compute alignment features, then create multiple masks with different thresholds.
            ComputeFeaturesAndCreateMasks();

            // Compute bootstrap replicates and create a mask using the "accurate" preset.
            ComputeBootstrapReplicatesAndGetAccurateMask();

            /**
             * Examples for training, validating, and testing models.
             * These example methods are defined in Example.TrainModel.cs
             */

            // Train a model using manually filtered alignments.
            TrainModel();

            // Validate the model with a different alignment.
            ValidateModel();

            // Test the validated model yet another alignment.
            TestModel();
        }
    }
}
