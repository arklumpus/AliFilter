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

using AliFilter;
using AliFilter.AlignmentFeatures;
using AliFilter.Models;

namespace AliFilterExample
{
    internal partial class Example
    {
        /// <summary>
        /// Train a model using data from two alignments, then save the trained model to a file in JSON format.
        /// </summary>
        static void TrainModel()
        {
            // Read the raw unfiltered training alignments.
            Alignment training1 = Alignment.FromFile("../../../Data/training1.fas");
            Alignment training2 = Alignment.FromFile("../../../Data/training2.fas");

            // Read the "true" filtered alignments.
            Alignment training1True = Alignment.FromFile("../../../Data/training1.true.fas");
            Alignment training2True = Alignment.FromFile("../../../Data/training2.true.fas");

            // Create the training masks from the filtered alignments.
            Mask training1Mask = new Mask(training1, training1True);
            Mask training2Mask = new Mask(training2, training2True);

            // Compute the features for the two raw alignments.
            double[][] training1Features = Features.DefaultFeatures.ComputeAll(training1);
            double[][] training2Features = Features.DefaultFeatures.ComputeAll(training2);

            // Concatenate the training features.
            double[][] allTrainingFeatures = [.. training1Features, .. training2Features];

            // Concatenate the masked states.
            bool[] allTrainingMask = [.. training1Mask.MaskedStates, .. training2Mask.MaskedStates];

            // NOTE: Always concatenate the features and masks, and NOT the alignments. If you concatenate
            //       the alignments before computing the features, the values for the "Distance from extremity"
            //       feature will be incorrect (because the information about where each alignment starts
            //       is lost).
            //
            // In the real world you may want to use more than two alignments for training.

            // Train the model.
            FullModel model = FullModel.Train(allTrainingFeatures, Features.DefaultFeatures.Signature, allTrainingMask);

            // Save the model as a JSON file.
            model.Save("model.trained.json");
        }

        /// <summary>
        /// Validate a trained model using an additional alignment.
        /// </summary>
        static void ValidateModel()
        {
            // Deserialise the trained (unvalidated) model.
            // This assumes that Example.TrainModel() has been executed first.
            FullModel model = FullModel.FromFile("model.trained.json");

            // Read the unfiltered validation alignment.
            Alignment validation = Alignment.FromFile("../../../Data/validation.fas");

            // Read the "true" filtered validation alignment.
            Alignment validationTrue = Alignment.FromFile("../../../Data/validation.true.fas");

            // Create the validation mask by comparing the two alignments.
            Mask validationMask = new Mask(validation, validationTrue);

            // Compute the validation features from the unfiltered alignment.
            double[][] validationFeatures = Features.DefaultFeatures.ComputeAll(validation);

            // If you wish to use multiple alignments for validation, you should just
            // concatenate the features and masks as shown in the TrainModel example.
            // In the real world you should definitely use more than one alignment.

            // Compute bootstrap replicates for the validation features. We are going to
            // test up to 1000 bootstrap replicates, therefore we need to generate at
            // least this many.
            double[][,] validationBootstrapFeatures = Features.DefaultFeatures.ComputeAllBootstrapReplicates(validation, 1000);

            // Validate the model using the default metric (MCC).
            ValidatedModel validatedModel = model.Validate(validationMask, validationFeatures, validationBootstrapFeatures, maxBootstrapReplicates: 1000);
            // To use an alternative target metric, provide a value for the optional targetMetric argument, e.g.:
            // ValidatedModel validatedModel = model.Validate(validationMask, validationFeatures, validationBootstrapFeatures, maxBootstrapReplicates: 1000, targetMetric: confusionMatrix => confusionMatrix.Accuracy);

            // We can export the validated model to a JSON file.
            validatedModel.Save("model.validated.json");
        }

        /// <summary>
        /// Test a validated model using a test alignment.
        /// </summary>
        static void TestModel()
        {
            // Deserialise the trained (unvalidated) model.
            // This assumes that Example.TrainModel() and Example.ValidateModel() have
            // been executed first.
            ValidatedModel model = ValidatedModel.FromFile("model.validated.json");

            // Read the unfiltered test alignment.
            Alignment test = Alignment.FromFile("../../../Data/test.fas");

            // Read the "true" test validation alignment.
            Alignment testTrue = Alignment.FromFile("../../../Data/test.true.fas");

            // Create the test mask by comparing the two alignments.
            Mask testMask = new Mask(test, testTrue);

            // If you wish to use multiple alignments for validation, you should just
            // concatenate the features and masks as shown in the TrainModel example.
            // In the real world you should definitely use more than one alignment.

            // Create a mask using the "Fast" preset (no bootstrap replicates and the
            // correspondingly appropriate logistic model threshold).
            Mask fastMask = model.GetMask(test, defaultParameters: DefaultParameters.Fast);

            // Create a mask using the "Best" preset (providing the best balance between
            // performance and accuracy).
            Mask bestMask = model.GetMask(test, defaultParameters: DefaultParameters.Best);

            // Create a mask using the "Accurate" preset (providing the best accuracy at the
            // cost of lower performance).
            Mask accurateMask = model.GetMask(test, defaultParameters: DefaultParameters.Accurate);

            // Create a confusion matrix comparing the fast mask and the test mask.
            ConfusionMatrix fastConfusionMatrix = new ConfusionMatrix(fastMask, testMask);

            // Create a confusion matrix comparing the best mask and the test mask.
            ConfusionMatrix bestConfusionMatrix = new ConfusionMatrix(bestMask, testMask);

            // Create a confusion matrix comparing the accurate mask and the test mask.
            ConfusionMatrix accurateConfusionMatrix = new ConfusionMatrix(accurateMask, testMask);

            // The results can be assessed, e.g., by inspecting the MCC values for the
            // confusion matrices.
        }
    }
}
