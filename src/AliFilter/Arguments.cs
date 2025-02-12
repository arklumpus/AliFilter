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

using AliFilter.AlignmentFeatures;
using AliFilter.AlignmentFormatting;

namespace AliFilter
{
    /// <summary>
    /// Which parameters to use from the model.
    /// </summary>
    public enum DefaultParameters
    {
        /// <summary>
        /// Fast parameters (no bootstrap replicates).
        /// </summary>
        Fast,

        /// <summary>
        /// Parameters providing the best balancing between number of bootstrap replicates and the validation score.
        /// </summary>
        Best,

        /// <summary>
        /// Parameters resulting in the best validation score, without accounting for the computational effort involved.
        /// </summary>
        Accurate,
    }

    /// <summary>
    /// The kind of output file produced.
    /// </summary>
    internal enum OutputKind
    {
        /// <summary>
        /// A filtered alignment.
        /// </summary>
        FilteredAlignment,

        /// <summary>
        /// A mask sequence containing 0s and 1s.
        /// </summary>
        Mask,

        /// <summary>
        /// A mask sequence where each character is determined using the Sanger convention for storing the log-transformed preservation score.
        /// For a preservation score of p (ranging from 0 - the column should be deleted - to 1 - the column should be preserved), an ASCII character with value 126 + 10 * log10(p) is used. 
        /// </summary>
        FuzzyMask,

        /// <summary>
        /// A list of floating point numbers, separated by spaces, where each number represents the preservation score of an alignment column.
        /// </summary>
        FloatMask
    }

    /// <summary>
    /// Target measure to be optimised during model validation.
    /// </summary>
    internal enum OptimisationTarget
    {
        /// <summary>
        /// Accuracy
        /// </summary>
        Accuracy,

        /// <summary>
        /// F_beta score
        /// </summary>
        FBeta,

        /// <summary>
        /// Matthews correlation coefficient
        /// </summary>
        MCC
    }

    /// <summary>
    /// Command line arguments used to invoke the program.
    /// </summary>
    internal class Arguments
    {
        /// <summary>
        /// Input alignment file.
        /// </summary>
        public string InputAlignment { get; set; }

        /// <summary>
        /// First alignment/mask being compared.
        /// </summary>
        public string InputFirstCompare { get; set; }

        /// <summary>
        /// Second alignment/mask being compared.
        /// </summary>
        public string InputSecondCompare { get; set; }

        /// <summary>
        /// Input alignment type.
        /// </summary>
        public AlignmentType AlignmentType { get; set; } = AlignmentType.Autodetect;

        /// <summary>
        /// Input alignment format.
        /// </summary>
        public AlignmentFileFormat? InputFormat { get; set; } = null;

        /// <summary>
        /// Input mask file or sequence name.
        /// </summary>
        public string InputMask { get; set; }

        /// <summary>
        /// Input feature file.
        /// </summary>
        public string InputFeatures { get; set; }

        /// <summary>
        /// Input model file.
        /// </summary>
        public string InputModel { get; set; }

        /// <summary>
        /// Output alignment or model file.
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// Output kind.
        /// </summary>
        public OutputKind OutputKind { get; set; }

        /// <summary>
        /// Output alignment format.
        /// </summary>
        public AlignmentFileFormat? OutputFormat { get; set; } = null;

        /// <summary>
        /// After the filtering has been performed, sequences that contain more than this proportion of gaps are removed.
        /// </summary>
        public double? Clean { get; set; } = null;

        /// <summary>
        /// Before processing any alignment, remove the specified sequence(s).
        /// </summary>
        public string Remove { get; set; } = null;

        /// <summary>
        /// Before processing any alignment, remove all sequences except the ones specified in this file.
        /// </summary>
        public string Keep { get; set; } = null;

        /// <summary>
        /// Output report file in PDF or Markdown format.
        /// </summary>
        public string ReportFile { get; set; }

        /// <summary>
        /// When computing features, append them to an existing file rather than overwriting it.
        /// </summary>
        public bool Append { get; set; } = false;

        /// <summary>
        /// Indicates that the features being computed will be used for model validation or testing, hence bootstrap replicates should be computed as well.
        /// </summary>
        public bool FeaturesForValidation { get; set; } = false;

        /// <summary>
        /// Feature set to use.
        /// </summary>
        public FeatureCollection Features { get; set; } = AlignmentFeatures.Features.DefaultFeatures;

        /// <summary>
        /// Proportion of column assignments that should be randomly altered to simulate classification errors.
        /// </summary>
        public double Mistakes { get; set; } = 0;

        /// <summary>
        /// Maximum degree of parallelism (number of threads).
        /// </summary>
        public int MaxParallelism { get; set; } = -1;

        /// <summary>
        /// The logistic model threshold used to filter the alignment columns.
        /// </summary>
        public double? Threshold { get; set; } = null;

        /// <summary>
        /// The bootstrap replicate threshold used to filter the alignment columns.
        /// </summary>
        public double? BootstrapThreshold { get; set; } = null;

        /// <summary>
        /// The number of bootstrap replicates to compute.
        /// </summary>
        public int? BootstrapReplicates { get; set; } = null;

        /// <summary>
        /// Target measure to optimise during model validation.
        /// </summary>
        public OptimisationTarget OptimisationTarget { get; set; } = OptimisationTarget.MCC;

        /// <summary>
        /// Beta parameter used to determine the balance between false positives and false negatives when validating the model to optimize the F_beta score.
        /// </summary>
        public double Beta { get; set; } = 1;

        /// <summary>
        /// Parameter used to penalise results obtained with higher numbers of bootstrap replicates.
        /// </summary>
        public double EffortPenalty { get; set; } = 0.005;

        /// <summary>
        /// Folder containing unfiltered alignments that should be filtered to produce a training, validation and test set.
        /// </summary>
        public string SuggestFolder { get; set; } = null;

        /// <summary>
        /// Number of alignments that will be manually filtered.
        /// </summary>
        public int SuggestCount { get; set; } = 40;

        /// <summary>
        /// Split between training, validation, and test set.
        /// </summary>
        public double[] SuggestSplit { get; set; } = new double[] { 2, 1, 1 };

        /// <summary>
        /// Output file for the training set.
        /// </summary>
        public string SuggestTrainingOut { get; set; } = null;

        /// <summary>
        /// Output file for the validation set.
        /// </summary>
        public string SuggestValidationOut { get; set; } = null;

        /// <summary>
        /// Output file for the test set.
        /// </summary>
        public string SuggestTestOut { get; set; } = null;

        /// <summary>
        /// Which set of default parameters to use when applying or testing the model.
        /// </summary>
        public DefaultParameters DefaultParameters { get; set; } = DefaultParameters.Best;

        /// <summary>
        /// Bitwise operation to apply to the supplied alignment mask(s).
        /// </summary>
        public string BitwiseOperation { get; set; } = null;

        public Arguments() { }
    }

    /// <summary>
    /// Represents an <see cref="Action"/> with one or two parameters of type <typeparamref name="T"/>.
    /// </summary>
    internal class Action1or2<T>
    {
        /// <summary>
        /// Number of method parameters.
        /// </summary>
        public int ParameterCount { get; }

        /// <summary>
        /// Delegate with one parameter, or null if this object represents a delegate with two parameters.
        /// </summary>
        public Action<T> Action1 { get; }

        /// <summary>
        /// Delegate with two parameters, or null if this object represents a delegate with one parameter.
        /// </summary>
        public Action<T, T> Action2 { get; }

        /// <summary>
        /// Create a new <see cref="Action1or2{T}"/> from the specified delegate with one parameter.
        /// </summary>
        /// <param name="action">The delegate represented by this instance.</param>
        public Action1or2(Action<T> action)
        {
            this.ParameterCount = 1;
            this.Action1 = action;
        }

        /// <summary>
        /// Create a new <see cref="Action1or2{T}"/> from the specified delegate with two parameters.
        /// </summary>
        /// <param name="action">The delegate represented by this instance.</param>
        public Action1or2(Action<T, T> action)
        {
            this.ParameterCount = 2;
            this.Action2 = action;
        }

        /// <summary>
        /// Convert an <see cref="Action"/> delegate into a <see cref="Action1or2{T}"/>.
        /// </summary>
        /// <param name="action">The delegate to convert.</param>
        public static implicit operator Action1or2<T>(Action<T> action)
        {
            return new Action1or2<T>(action);
        }

        /// <summary>
        /// Convert an <see cref="Action"/> delegate into a <see cref="Action1or2{T}"/>.
        /// </summary>
        /// <param name="action">The delegate to convert.</param>
        public static implicit operator Action1or2<T>(Action<T, T> action)
        {
            return new Action1or2<T>(action);
        }
    }
}
