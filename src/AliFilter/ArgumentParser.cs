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

using Mono.Options;
using AliFilter.AlignmentFormatting;
using static AliFilter.Program;

namespace AliFilter
{
    internal class ArgumentParser
    {
        public bool ShowHelp { get; private set; }
        public bool ShowUsage { get; private set; }
        public Arguments Arguments { get; } = new Arguments();
        public TextWriter OutputLog { get; private set; } = Console.Error;
        public HashSet<string> SpecifiedArguments { get; } = new HashSet<string>();
        public List<string> UnrecognisedArguments { get; private set; }

        private OptionSet ArgParser { get; }

        public ArgumentParser()
        {
            // Command-line arguments for the program.
            (string, string, Action1or2<string>)[] programArguments = new (string, string, Action1or2<string>)[]
            {
                // Help
                ( "h|help", "Print this message and exit.", (Action1or2<string>)(v => { this.ShowHelp = v != null; } )),

                // Input alignment.
                ( "i|input=", "[FC*] Input alignment file, or \"stdin\" to read to the standard input).", (Action1or2<string>)(v => { Arguments.InputAlignment = v; })),
                ( "t|type=", "[FC=*S] Input alignment type. Possible values are \"dna\", \"protein\", or \"auto\" (the default).", (Action1or2<string>)(v => {
                    if(string.Equals(v, "dna", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.AlignmentType = AlignmentType.DNA;
                    }
                    else if(string.Equals(v, "protein", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.AlignmentType = AlignmentType.Protein;
                    }
                    else if(string.Equals(v, "auto", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.AlignmentType = AlignmentType.Autodetect;
                    }
                    else
                    {
                        OutputLog?.WriteLine();
                        OutputLog?.WriteLine("Unknown alignment type: " + v);
                        ShowUsage = true;
                    }
                } )),
                ( "input-format=", "[FC=*S] Input alignment format. Possible values are " + Enum.GetNames<AlignmentFormatting.AlignmentFileFormat>().Select(x => "\"" + x + "\"").Aggregate((a, b) => a + ", " + b).ToLower() + ", or \"auto\" (the default).", (Action1or2<string>)(v => {
                    try
                    {
                        Arguments.InputFormat = FormatUtilities.ParseFormat(v);
                    }
                    catch (ArgumentException)
                    {
                        OutputLog?.WriteLine();
                        OutputLog?.WriteLine("Unknown alignment format: " + v);
                        ShowUsage = true;
                    }
                } )),
                ( "remove=", "[FC=*] After reading the alignment, remove the specified sequence(s) before any further processing. This can either be the name of a single sequence to remove, or the path to a file containing the names of multiple sequences. If multiple sequence names are provided, no error occurs if some of them are not present in the alignment.", (Action1or2<string>)(v => Arguments.Remove = v)),
                ( "keep=", "[FC=*] After reading the alignment and before any further processing, keep only the sequences whose name is present in the specified file. No error occurs if the file contains sequence names that are not present in the alignment.", (Action1or2<string>)(v => Arguments.Keep = v)),

                // Input mask.
                ( "mask=", "[C] Input alignment mask. This can either be the name of a sequence in the input alignment, which should contain only 0s and 1s, or the path to a filtered version of the input alignment.", (Action1or2<string>)(v => { Arguments.InputMask = v; } )),

                // Alignment or mask comparison.
                ( "compare={,}", "[=*] If -i|--input is specified, this argument instructs the program to compare two alignment masks, determining for how many columns they agree or disagree. If -i|--input is not specified, this argument instructs the program to compare two alignments, determining how many columns are present in both of them or only in one.", (Action1or2<string>)((v1, v2) => { Arguments.InputFirstCompare = v1; Arguments.InputSecondCompare = v2; } )),

                // Mask bitwise operations.
                ( "b|bitwise=", "[B] Apply a bitwise operation to the supplied alignment mask(s). The value of this argument should be either the name of a bitwise operation or a truth table. This argument should be followed by a one or more mask files, each containing the same number of 0s or 1s representing columns to preserve or discard. These masks(s) are combined using the specified operation. Supported bitwise operations are \"and\", \"or\", \"nor\", \"xor\", \"nand\", \"xnor\" (any number of masks), and \"not\" (only 1 mask). Other operations can be applied by supplying a binary truth table string of length 2^n, where n is the number of masks and entries are sorted by their bitwise representation. For example, the \"xor\" operation on two masks corresponds to truth table \"0110\".", (Action1or2<string>)(v => { Arguments.BitwiseOperation = v; } )),

                // Input features.
                ( "f|features=", "[MVT] Input file containing alignment features, used to train or validate models.", (Action1or2<string>)(v => { Arguments.InputFeatures = v; } )),
                ( "mistakes=", "[MVT] Proportion of column assignments that should be randomly changed. This is useful e.g., to simulate human error in the training dataset. Default: 0.", (Action1or2<string>)(v => { Arguments.Mistakes = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } )),
                
                // Input model.
                ( "m|model=", "[FVT] Input file containing a trained (and possibly validated) model. When filtering an alignment, this can also be a path to a file containing a sequence of 0 and 1 representing a pre-computed filtering mask. When filtering an alignment, the default value is \"alifilter\", which specifies the default model included with the program.", (Action1or2<string>)(v => { Arguments.InputModel = v; } )),

                // Output.
                ( "o|output=", "[FCMV] Output file for the alignment, features, or model.", (Action1or2<string>)(v => { Arguments.OutputFile = v; } )),
                ( "k|output-kind=", "[F=] Kind of output file produced when filtering an alignment. Possible values are \"alignment\" (the default, a filtered alignment), \"mask\" (a mask string of 0s and 1s), \"fuzzy\" (a mask sequence with preservation scores encoded following the Sanger convention for quality values), or \"float\" (a sequence of floating point numbers representing the preservation scores, separated by spaces). If the value \"mask\" is provided when comparing two alignments, a mask string will be produced instead of counting the number of columns in common to the two alignments.", (Action1or2<string>)(v => {
                    if(string.Equals(v, "alignment", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.OutputKind = OutputKind.FilteredAlignment;
                    }
                    else if(string.Equals(v, "mask", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.OutputKind = OutputKind.Mask;
                    }
                    else if(string.Equals(v, "fuzzy", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.OutputKind = OutputKind.FuzzyMask;
                    }
                    else if(string.Equals(v, "float", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.OutputKind = OutputKind.FloatMask;
                    }
                    else
                    {
                        OutputLog?.WriteLine();
                        OutputLog?.WriteLine("Unknown output kind: " + v);
                        ShowUsage = true;
                    }
                } )),

                // Output alignment.
                ( "output-format=", "[F] Output alignment format. Possible values are " + Enum.GetNames<AlignmentFormatting.AlignmentFileFormat>().Select(x => "\"" + x + "\"").Aggregate((a, b) => a + ", " + b).ToLower() + ", or \"auto\" (the default - i.e., same as input).", (Action1or2<string>)(v => {
                    try
                    {
                        Arguments.InputFormat = FormatUtilities.ParseFormat(v);
                    }
                    catch (ArgumentException)
                    {
                        OutputLog?.WriteLine();
                        OutputLog?.WriteLine("Unknown alignment format: " + v);
                        ShowUsage = true;
                    }
                } )),
                ( "clean:", "[F] If this argument is specified, after the filtering has been performed, sequences that contain more than the specified proportion of gaps are removed. If no value is specified, sequences that contain 100% gaps are removed. Default: no sequences are removed.", (Action1or2<string>)(v =>
                {
                    if (string.IsNullOrEmpty(v))
                    {
                        Arguments.Clean = 1;
                    }
                    else
                    {
                        if (double.TryParse(v, out double d) && d >= 0 && d <= 1)
                        {
                            Arguments.Clean = d;
                        }
                        else
                        {
                            OutputLog?.WriteLine();
                            OutputLog?.WriteLine("Invalid cleaning threshold: " + v);
                            ShowUsage = true;
                        }
                    }
                })),

                // Output report.
                ( "report=", "[FVT] Output path for a report file which will contain information about the model or the alignment. If this has \".md\" extension, the report will be saved in Markdown format; otherwise, it will be saved in PDF format (which is probably what you want).", (Action1or2<string>)(v => { Arguments.ReportFile = v; } )),

                // Output features.
                ( "a|append", "[C] When exporting alignment features, append them to the output file rather than overwriting it. This can be useful to include multiple alignments in the same model. Default: no.", (Action1or2<string>)(v => { Arguments.Append = v != null; } )),
                ( "v|validation", "[C] When computing features, signals that the features will be used for model validation or testing, hence bootstrap replicates should be computed as well. Default: no.", (Action1or2<string>)(v => { Arguments.FeaturesForValidation = v != null; } )),

                // Model settings.
                ( "threshold=", "[FT] Logistic model score threshold for keeping alignment columns. Default: 0.5, or as specified by model validation. Range: 0 - 1.", (Action1or2<string>)(v => { Arguments.Threshold = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } )),
                ( "bootstrap-threshold=", "[FT] Bootstrap replicate threshold for keeping alignment columns. Default: 0.5, or as specified by model validation. Range: 0 - 1.", (Action1or2<string>)(v => { Arguments.BootstrapThreshold = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } )),
                ( "bootstrap-replicates=", "[FT] Number of bootstrap replicates to compute. Default: 0, or as specified by model validation. Range: >= 0.", (Action1or2<string>)(v => { Arguments.BootstrapReplicates = int.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } )),
                ( "target=", "[V] Target measure to optimise during model validation. Possible values are \"MCC\" (Matthews correlation coefficient, the default), \"accuracy\", or \"F_<beta>\" (F score, where \"<beta>\" > 0 is a parameter that determines the balance between false positives and false negatives; e.g. \"F_1\" or \"F_0.5\").", (Action1or2<string>)(v =>
                {
                    if (string.Equals(v, "MCC", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.OptimisationTarget = OptimisationTarget.MCC;
                    }
                    else if (string.Equals(v, "accuracy", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.OptimisationTarget = OptimisationTarget.Accuracy;
                    }
                    else if (v != null && (v.StartsWith("F_") || v.StartsWith("f_")))
                    {
                        Arguments.OptimisationTarget = OptimisationTarget.FBeta;
                        Arguments.Beta = double.Parse(v.AsSpan(2), System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        OutputLog?.WriteLine();
                        OutputLog?.WriteLine("Unknown optimisation target: " + v);
                        ShowUsage = true;
                    }
                })),
                ( "d|default=", "[FT] Which default settings to apply when using or testing a validated model. Possible values are \"fast\" (no bootstrap replicates), \"accurate\" (parameters resulting in the best validation score, regardless of computational effort), or \"best\" (the default, which provides the best balance between computational effort and accuracy).", (Action1or2<string>)(v =>
                {
                    if(string.Equals(v, "fast", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.DefaultParameters = DefaultParameters.Fast;
                    }
                    else if(string.Equals(v, "best", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.DefaultParameters = DefaultParameters.Best;
                    }
                    else if(string.Equals(v, "accurate", StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.DefaultParameters = DefaultParameters.Accurate;
                    }
                    else
                    {
                        OutputLog?.WriteLine();
                        OutputLog?.WriteLine("Unknown default settings: " + v);
                        ShowUsage = true;
                    }
                } )),

                // Suggest training set.
                ( "suggest=", "[S] Folder containing unfiltered alignments that may be used to train a model. When this argument is specified, the program suggests which alignments should be manually filtered and used as training, validation, and test sets.", (Action1or2<string>)(v => { Arguments.SuggestFolder = v; } )),
                ( "count=", "[S] Number of alignments that may be used to train a model. This should be set to the maximum number of alignments that you are willing to manually (or otherwise) filter in order to build training, validation, and test sets. Default: 40.", (Action1or2<string>)(v => { Arguments.SuggestCount = int.Parse(v); } )),
                ( "split=", "[S] Ratio between the sizes of the training, validation, and test sets that should be suggested. Default: 2:1:1.", (Action1or2<string>)(v => { Arguments.SuggestSplit = v.Split(":").Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray(); } )),
                ( "training-out=", "[S] Output file for the suggested training set. If this is not specified, the training set is printed to the standard output.", (Action1or2<string>)(v => Arguments.SuggestTrainingOut = v) ),
                ( "validation-out=", "[S] Output file for the suggested validation set. If this is not specified, the validation set is printed to the standard output.", (Action1or2<string>)(v => Arguments.SuggestValidationOut = v) ),
                ( "test-out=", "[S] Output file for the suggested test set. If this is not specified, the test set is printed to the standard output.", (Action1or2<string>)(v => Arguments.SuggestTestOut = v) ),


                // Other settings.
                ( "p|max-threads=", "Maximum degree of parallelism for operations that support parallelisation. Default: autodetect based on the system.", (Action1or2<string>)(v => { Arguments.MaxParallelism = int.Parse(v); } )),
                ( "q|quiet", "Quiet mode (do not write anything to the standard error, except for unrecoverable errors).", (Action1or2<string>)(v => { OutputLog = (v == null ? Console.Error : null); } ))
            };

            OptionSet argParser = new OptionSet();

            foreach ((string prototype, string description, Action1or2<string> action) in programArguments)
            {
                if (action.ParameterCount == 1)
                {
                    argParser.Add(prototype, description, v => { SpecifiedArguments.Add(prototype); action.Action1(v); });
                }
                else if (action.ParameterCount == 2)
                {
                    argParser.Add(prototype, description, (v1, v2) => { SpecifiedArguments.Add(prototype); action.Action2(v1, v2); });
                }
            }

            this.ArgParser = argParser;
        }

        public ProgramTask? ParseArguments(string[] args)
        {
            if (args.Length == 0)
            {
                OutputLog?.WriteLine("AliFilter was invoked without command-line arguments and will thus read from");
                OutputLog?.WriteLine("the standard input and write to the standard output.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("If this was not intended, please invoke \"AliFilter -h\" for the help message.");
                OutputLog?.WriteLine();
            }

            ProgramTask? task = null;

            UnrecognisedArguments = ArgParser.Parse(args);

            if (Arguments.MaxParallelism < 1)
            {
                Arguments.MaxParallelism = -1;
            }

            if (UnrecognisedArguments.Count > 0 && Arguments.BitwiseOperation == null)
            {
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("Unrecognised argument" + (UnrecognisedArguments.Count > 1 ? "s" : "") + ": " + UnrecognisedArguments.Aggregate((a, b) => a + " " + b));
                ShowUsage = true;
            }

            if (Arguments.Threshold != null && (Arguments.Threshold.Value < 0 || Arguments.Threshold.Value > 1))
            {
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("Treshold value out of range: " + Arguments.Threshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ShowUsage = true;
            }

            if (Arguments.BootstrapThreshold != null && (Arguments.BootstrapThreshold.Value < 0 || Arguments.BootstrapThreshold.Value > 1))
            {
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("Bootstrap threshold value out of range: " + Arguments.BootstrapThreshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ShowUsage = true;
            }

            if (Arguments.BootstrapReplicates != null && Arguments.BootstrapReplicates.Value < 0)
            {
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("Bootstrap replicate count out of range: " + Arguments.BootstrapReplicates.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ShowUsage = true;
            }

            if (Arguments.Beta <= 0)
            {
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("Beta parameter out of range: " + Arguments.Beta.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ShowUsage = true;
            }

            if (!string.IsNullOrEmpty(Arguments.SuggestFolder) && Arguments.SuggestCount < 3)
            {
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("At least three alignments must be used for training, validation, and test!");
                ShowUsage = true;
            }

            if (!string.IsNullOrEmpty(Arguments.SuggestFolder) && (Arguments.SuggestSplit.Length != 3 || Arguments.SuggestSplit.Sum() <= 0))
            {
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("Invalid set split specified!");
                ShowUsage = true;
            }

            if (!ShowUsage && !ShowHelp)
            {
                if (!string.IsNullOrEmpty(Arguments.BitwiseOperation))
                {
                    task = ProgramTask.BitwiseOperation;

                    SpecifiedArguments.Remove("b|bitwise=");
                }
                else if (!string.IsNullOrEmpty(Arguments.InputMask) && !string.IsNullOrEmpty(Arguments.InputModel))
                {
                    // Test model.
                    task = ProgramTask.TestModelOnAlignment;

                    SpecifiedArguments.Remove("mistakes=");
                    SpecifiedArguments.Remove("i|input=");
                    SpecifiedArguments.Remove("t|type=");
                    SpecifiedArguments.Remove("input-format=");
                    SpecifiedArguments.Remove("keep=");
                    SpecifiedArguments.Remove("remove=");
                    SpecifiedArguments.Remove("mask=");
                    SpecifiedArguments.Remove("m|model=");
                    SpecifiedArguments.Remove("report=");
                    SpecifiedArguments.Remove("threshold=");
                    SpecifiedArguments.Remove("bootstrap-threshold=");
                    SpecifiedArguments.Remove("bootstrap-replicates=");
                    SpecifiedArguments.Remove("d|default=");
                    SpecifiedArguments.Remove("p|max-threads=");
                    SpecifiedArguments.Remove("q|quiet");
                }
                else if (!string.IsNullOrEmpty(Arguments.InputMask))
                {
                    // Compute alignment features.
                    if (string.IsNullOrEmpty(Arguments.OutputFile))
                    {
                        OutputLog?.WriteLine();
                        OutputLog?.WriteLine("Missing required argument: -o|--output!");
                        ShowUsage = true;
                    }
                    else
                    {
                        task = ProgramTask.ComputeFeatures;
                    }

                    SpecifiedArguments.Remove("i|input=");
                    SpecifiedArguments.Remove("t|type=");
                    SpecifiedArguments.Remove("input-format=");
                    SpecifiedArguments.Remove("keep=");
                    SpecifiedArguments.Remove("remove=");
                    SpecifiedArguments.Remove("mask=");
                    SpecifiedArguments.Remove("o|output=");
                    SpecifiedArguments.Remove("a|append");
                    SpecifiedArguments.Remove("v|validation");
                    SpecifiedArguments.Remove("p|max-threads=");
                    SpecifiedArguments.Remove("q|quiet");
                }
                else if (!string.IsNullOrEmpty(Arguments.InputModel) && !string.IsNullOrEmpty(Arguments.InputFeatures) && !string.IsNullOrEmpty(Arguments.OutputFile))
                {
                    // Validate model.
                    task = ProgramTask.ValidateModel;

                    SpecifiedArguments.Remove("f|features=");
                    SpecifiedArguments.Remove("mistakes=");
                    SpecifiedArguments.Remove("m|model=");
                    SpecifiedArguments.Remove("o|output=");
                    SpecifiedArguments.Remove("report=");
                    SpecifiedArguments.Remove("target=");
                    SpecifiedArguments.Remove("p|max-threads=");
                    SpecifiedArguments.Remove("q|quiet");

                }
                else if (!string.IsNullOrEmpty(Arguments.InputModel) && !string.IsNullOrEmpty(Arguments.InputFeatures))
                {
                    // Test model.
                    task = ProgramTask.TestModel;

                    SpecifiedArguments.Remove("f|features=");
                    SpecifiedArguments.Remove("mistakes=");
                    SpecifiedArguments.Remove("m|model=");
                    SpecifiedArguments.Remove("report=");
                    SpecifiedArguments.Remove("threshold=");
                    SpecifiedArguments.Remove("bootstrap-threshold=");
                    SpecifiedArguments.Remove("bootstrap-replicates=");
                    SpecifiedArguments.Remove("d|default=");
                    SpecifiedArguments.Remove("p|max-threads=");
                    SpecifiedArguments.Remove("q|quiet");

                }
                else if (!string.IsNullOrEmpty(Arguments.OutputFile) && !string.IsNullOrEmpty(Arguments.InputFeatures))
                {
                    // Train model.
                    task = ProgramTask.TrainModel;

                    SpecifiedArguments.Remove("f|features=");
                    SpecifiedArguments.Remove("mistakes=");
                    SpecifiedArguments.Remove("o|output=");
                    SpecifiedArguments.Remove("p|max-threads=");
                    SpecifiedArguments.Remove("q|quiet");
                }
                else if (!string.IsNullOrEmpty(Arguments.InputFirstCompare) && string.IsNullOrEmpty(Arguments.InputAlignment))
                {
                    // Compare alignments.
                    task = ProgramTask.CompareAlignments;

                    SpecifiedArguments.Remove("compare={,}");
                    SpecifiedArguments.Remove("keep=");
                    SpecifiedArguments.Remove("remove=");
                    if (Arguments.OutputKind == OutputKind.Mask)
                    {
                        SpecifiedArguments.Remove("k|output-kind=");
                    }
                    SpecifiedArguments.Remove("t|type=");
                    SpecifiedArguments.Remove("input-format=");
                    SpecifiedArguments.Remove("p|max-threads=");
                    SpecifiedArguments.Remove("q|quiet");
                }
                else if (!string.IsNullOrEmpty(Arguments.InputFirstCompare) && !string.IsNullOrEmpty(Arguments.InputAlignment))
                {
                    // Compare masks.
                    task = ProgramTask.CompareMasks;

                    SpecifiedArguments.Remove("i|input=");
                    SpecifiedArguments.Remove("compare={,}");
                    SpecifiedArguments.Remove("keep=");
                    SpecifiedArguments.Remove("remove=");
                    SpecifiedArguments.Remove("t|type=");
                    SpecifiedArguments.Remove("input-format=");
                    SpecifiedArguments.Remove("p|max-threads=");
                    SpecifiedArguments.Remove("q|quiet");
                }
                else if (!string.IsNullOrEmpty(Arguments.SuggestFolder))
                {
                    // Suggest training set.
                    task = ProgramTask.SuggestSets;

                    SpecifiedArguments.Remove("suggest=");
                    SpecifiedArguments.Remove("t|type=");
                    SpecifiedArguments.Remove("count=");
                    SpecifiedArguments.Remove("split=");
                    SpecifiedArguments.Remove("training-out=");
                    SpecifiedArguments.Remove("validation-out=");
                    SpecifiedArguments.Remove("test-out=");
                    SpecifiedArguments.Remove("input-format=");
                    SpecifiedArguments.Remove("q|quiet");
                }
                else
                {
                    // Filter alignment.
                    task = ProgramTask.FilterAlignment;

                    SpecifiedArguments.Remove("i|input=");
                    SpecifiedArguments.Remove("t|type=");
                    SpecifiedArguments.Remove("input-format=");
                    SpecifiedArguments.Remove("keep=");
                    SpecifiedArguments.Remove("remove=");
                    SpecifiedArguments.Remove("clean:");
                    SpecifiedArguments.Remove("m|model=");
                    SpecifiedArguments.Remove("o|output=");
                    SpecifiedArguments.Remove("k|output-kind=");
                    SpecifiedArguments.Remove("output-format=");
                    SpecifiedArguments.Remove("report=");
                    SpecifiedArguments.Remove("threshold=");
                    SpecifiedArguments.Remove("bootstrap-threshold=");
                    SpecifiedArguments.Remove("bootstrap-replicates=");
                    SpecifiedArguments.Remove("d|default=");
                    SpecifiedArguments.Remove("p|max-threads=");
                    SpecifiedArguments.Remove("q|quiet");
                }
            }

            if (ShowUsage || ShowHelp)
            {
                OutputLog?.WriteLine();
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("AliFilter version {0}", Program.Version);
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("Usage:");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter {-h|--help}");
                OutputLog?.WriteLine("    Show the help message.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter [-i <input alignment>] [-o <output file>] [-m <input model file>]");
                OutputLog?.WriteLine("            [-t <alignment type>] [--input-format <format>] [-k <kind>]");
                OutputLog?.WriteLine("            [--output-format <format>] [--report <report file>] [-d <settings>]");
                OutputLog?.WriteLine("            [--clean[=gap threshold]] [--remove {sequence name|list file}]");
                OutputLog?.WriteLine("            [--keep <list file>] [--threshold <value>]");
                OutputLog?.WriteLine("            [--bootstrap-threshold <value>] [--bootstrap-replicates <value>]");
                OutputLog?.WriteLine("            [-p <max threads>] [-q]");
                OutputLog?.WriteLine("    Use a trained model to filter an alignment.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter --compare <alignment 1> <alignment 2> [-p <max threads>] [-k mask]");
                OutputLog?.WriteLine("            [-q]");
                OutputLog?.WriteLine("    Compare two alignments.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter -i <input alignment> --compare <mask 1> <mask 2> [-p <max threads>]");
                OutputLog?.WriteLine("            [-q]");
                OutputLog?.WriteLine("    Compare two alignment masks.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter -b <operation> <mask 1> [<mask 2> [<mask 3> ...]]");
                OutputLog?.WriteLine("            [-q]");
                OutputLog?.WriteLine("    Apply a bitwise operation to the alignment mask(s).");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter --suggest <alignment folder> [--count <number>]");
                OutputLog?.WriteLine("            [--split <training:validation:test>] [-t <alignment type>]");
                OutputLog?.WriteLine("            [--training-out <training list file>]");
                OutputLog?.WriteLine("            [--validation-out <validation list file>]");
                OutputLog?.WriteLine("            [--test-out <test list file>] [--input format <format>] [-q]");
                OutputLog?.WriteLine("    Suggest alignments that should be used for training, validation, and test.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter --evaluate <training feature file> <test feature file>");
                OutputLog?.WriteLine("            [-p <max threads>] [-q]");
                OutputLog?.WriteLine("    Evaluate a dataset, estimating its proportion of inconsistencies.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter [-i <input alignment>] --mask <input mask> -o <output feature file>");
                OutputLog?.WriteLine("            [-v] [-t <alignment type>] [--input format <format>] [-a]");
                OutputLog?.WriteLine("            [--remove {sequence name|list file}] [--keep <list file>]");
                OutputLog?.WriteLine("            [-p <max threads>] [-q]");
                OutputLog?.WriteLine("    Compute alignment features.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter -f <input feature file> -o <output model file>");
                OutputLog?.WriteLine("            [--mistakes <proportion>] [-p <max threads>] [-q]");
                OutputLog?.WriteLine("    Train a model using pre-computed alignment features.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter -m <input model file> -f <input feature file>");
                OutputLog?.WriteLine("            -o <output validated model file> [--report <report file>]");
                OutputLog?.WriteLine("            [--target <target metric>] [--mistakes <proportion>]");
                OutputLog?.WriteLine("            [-p <max threads>] [-q]");
                OutputLog?.WriteLine("    Validate a pre-trained model using pre-computed alignment features.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter -m <input model file> -f <input feature file>");
                OutputLog?.WriteLine("            [--report <report file>] [-d <settings>] [--threshold <value>]");
                OutputLog?.WriteLine("            [--bootstrap-threshold <value>] [--bootstrap-replicates <value>]");
                OutputLog?.WriteLine("            [--mistakes <proportion>] [-p <max threads>] [-q]");
                OutputLog?.WriteLine("    Test a pre-trained model using pre-computed alignment features.");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("  AliFilter -m <input model file> [-i <input alignment>] --mask <input mask>");
                OutputLog?.WriteLine("            [--report <report file>] [-d <settings>] [--threshold <value>]");
                OutputLog?.WriteLine("            [--bootstrap-threshold <value>] [--bootstrap-replicates <value>]");
                OutputLog?.WriteLine("            [-p <max threads>] [-q]");
                OutputLog?.WriteLine("    Test a pre-trained model using alignment features computed from the.");
                OutputLog?.WriteLine("    specified alignment and alignment mask.");
            }

            if (ShowHelp)
            {
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("Options:");
                OutputLog?.WriteLine();
                OutputLog?.WriteLine("In the list below, options that apply to specific tasks are tagged as follows:");
                OutputLog?.WriteLine("  * [F]: Alignment filtering.");
                OutputLog?.WriteLine("  * [=]: Alignment comparison.");
                OutputLog?.WriteLine("  * [*]: Mask comparison.");
                OutputLog?.WriteLine("  * [B]: Bitwise operations.");
                OutputLog?.WriteLine("  * [C]: Computing features.");
                OutputLog?.WriteLine("  * [M]: Model training.");
                OutputLog?.WriteLine("  * [V]: Model validation.");
                OutputLog?.WriteLine("  * [T]: Model testing.");
                OutputLog?.WriteLine("  * [S]: Training set suggestion.");
                OutputLog?.WriteLine();
                if (OutputLog != null)
                {
                    ArgParser.WriteOptionDescriptions(OutputLog);
                }
            }

            return task;
        }
    }
}
