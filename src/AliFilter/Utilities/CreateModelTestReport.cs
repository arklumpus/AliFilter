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

using System.Text;
using VectSharp;
using Accord.Statistics.Analysis;
using Accord.Statistics.Models.Regression.Linear;
using AliFilter.Models;
using VectSharp.Markdown;
using System.Text.Json;
using VectSharp.Plots;
using VectSharp.PDF;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using Accord.Math;
using VectSharp.SVG;
using Markdig.Syntax;
using VectSharp.PDF.PDFObjects;

namespace AliFilter
{
    internal static partial class Utilities
    {
        // Create the model test report.
        internal static int CreateModelTestReport(double[][] features, Mask mask, ValidatedModel model, Mask predictedMask, double threshold, double bootstrapThreshold, int bootstrapReplicateCount, List<(double fpr, double tpr)> rocCurve, double thresholdFpr, double thresholdTpr, double thresholdPrecision, double auc, Arguments arguments, TextWriter outputLog)
        {
            bool outputMd = Path.GetExtension(arguments.ReportFile) == ".md";

            outputLog?.WriteLine();
            outputLog?.WriteLine("Creating model test report...");

            string modelChecksum = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(model, ModelSerializerContext.Default.ValidatedModel))));

            // Run a PCA to display the data.
            PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis()
            {
                Method = PrincipalComponentMethod.Standardize,
                Whiten = true,
            };
            MultivariateLinearRegression pcaTransform = pca.Learn(features);

            // Use the LDA to transform the data for display.
            double[][] transformedLDA = model.LdaModel.Transform(features);

            // Use the LDA to classify the alignment columns.
            bool[] ldaAssignments = model.LdaModel.Classify(features).MaskedStates;
            int incorrectAssignments = ldaAssignments.Where((x, i) => x != mask.MaskedStates[i]).Count();

            // Number of true and false positives and negatives.
            int positives = 0;
            int truePositives = 0;
            int trueNegatives = 0;
            int falsePositives = 0;
            int falseNegatives = 0;

            for (int i = 0; i < mask.Length; i++)
            {
                if (mask.MaskedStates[i])
                {
                    positives++;
                    if (predictedMask.MaskedStates[i])
                    {
                        truePositives++;
                    }
                    else
                    {
                        falseNegatives++;
                    }
                }
                else
                {
                    if (predictedMask.MaskedStates[i])
                    {
                        falsePositives++;
                    }
                    else
                    {
                        trueNegatives++;
                    }
                }
            }

            #region Compute the distribution of the alignment features.
            List<double>[] featDistributionTestDeleted = new List<double>[arguments.Features.Count];
            List<double>[] featDistributionTestPreserved = new List<double>[arguments.Features.Count];
            List<double>[] featDistributionAssignedDeleted = new List<double>[arguments.Features.Count];
            List<double>[] featDistributionAssignedPreserved = new List<double>[arguments.Features.Count];

            double[] featureMins = new double[arguments.Features.Count];
            double[] featureMaxs = new double[arguments.Features.Count];
            double[] featureMeans = new double[arguments.Features.Count];
            double[] featureStdDevs = new double[arguments.Features.Count];

            for (int i = 0; i < arguments.Features.Count; i++)
            {
                featDistributionTestDeleted[i] = new List<double>();
                featDistributionTestPreserved[i] = new List<double>();
                featDistributionAssignedDeleted[i] = new List<double>();
                featDistributionAssignedPreserved[i] = new List<double>();

                featureMins[i] = double.MaxValue;
                featureMaxs[i] = double.MinValue;
            }

            for (int i = 0; i < features.Length; i++)
            {
                for (int j = 0; j < arguments.Features.Count; j++)
                {
                    featureMins[j] = Math.Min(featureMins[j], features[i][j]);
                    featureMaxs[j] = Math.Max(featureMaxs[j], features[i][j]);
                    featureMeans[j] += features[i][j];
                    featureStdDevs[j] += features[i][j] * features[i][j];
                }

                if (mask.MaskedStates[i])
                {
                    for (int j = 0; j < arguments.Features.Count; j++)
                    {
                        featDistributionTestPreserved[j].Add(features[i][j]);
                    }
                }
                else
                {
                    for (int j = 0; j < arguments.Features.Count; j++)
                    {
                        featDistributionTestDeleted[j].Add(features[i][j]);
                    }
                }

                if (predictedMask.MaskedStates[i])
                {
                    for (int j = 0; j < arguments.Features.Count; j++)
                    {
                        featDistributionAssignedPreserved[j].Add(features[i][j]);
                    }
                }
                else
                {
                    for (int j = 0; j < arguments.Features.Count; j++)
                    {
                        featDistributionAssignedDeleted[j].Add(features[i][j]);
                    }
                }
            }

            for (int i = 0; i < arguments.Features.Count; i++)
            {
                featureMeans[i] /= features.Length;
                featureStdDevs[i] = Math.Sqrt(featureStdDevs[i] / features.Length - featureMeans[i] * featureMeans[i]);
            }
            #endregion

            // Create the report as a Markdown document.
            MarkdownRenderer renderer = new MarkdownRenderer()
            {
                PageSize = new Size(595, 842),
                Margins = new Margins(55, 40, 55, 40)
            };

            StringBuilder markdownDocument = new StringBuilder();

            #region Introduction.
            using (StreamReader sr = new StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AliFilter.AliFilter_banner.svg")))
            {
                string svg = sr.ReadToEnd();
                Page pag = Parser.FromString(svg);
                PlotUtilities.InsertFigure(markdownDocument, pag, "width=\"485\" align=\"center\"");
            }

            markdownDocument.AppendLine();
            markdownDocument.AppendLine();

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("&nbsp;");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("# Model test report");

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("&nbsp;");
            markdownDocument.AppendLine();

            DateTime creationTime = DateTime.Now;

            if (!PlotUtilities.DaysOfMonth.TryGetValue(creationTime.Day, out string day))
            {
                day = creationTime.Day.ToString() + "^th^";
            }

            markdownDocument.AppendLine("Created by AliFilter version " + Program.Version + " on " + day + " " + creationTime.ToString("MMM, yyyy", System.Globalization.CultureInfo.InvariantCulture) + " at " + creationTime.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture) + ".");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("MD5 checksum of the model this report refers to: `" + modelChecksum + "` (feature signature: `" + model.FeatureSignature + "`)");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("This report may be included in any analysis using this model.");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("This report is machine-readable. If the report file is called `report.pdf`, you can export the tested model to a file called `model.json` by running:");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("* On a Unix machine:");
            markdownDocument.AppendLine("    ```");
            markdownDocument.AppendLine("    grep " + (outputMd ? "" : "-a ") + "\"@model\" report." + (outputMd ? "md" : "pdf") + " | sed \"s/@model//g\" > model.json");
            markdownDocument.AppendLine("    ```");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("* On a Windows machine (within a PowerShell environment):");
            markdownDocument.AppendLine("    ```");
            markdownDocument.AppendLine("    findstr \"@model\" report." + (outputMd ? "md" : "pdf") + " | %{$_ -replace \"@model\",\"\"} > model.json");
            markdownDocument.AppendLine("    ```");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("You can also export the performance metric summary table by running:");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("* On a Unix machine:");
            markdownDocument.AppendLine("    ```");
            markdownDocument.AppendLine("    grep " + (outputMd ? "" : "-a ") + "\"@metric\" report." + (outputMd ? "md" : "pdf") + " | sed \"s/@metric//g\"");
            markdownDocument.AppendLine("    ```");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("* On a Windows machine (within a PowerShell environment):");
            markdownDocument.AppendLine("    ```");
            markdownDocument.AppendLine("    findstr \"@metric\" report." + (outputMd ? "md" : "pdf") + " | %{$_ -replace \"@metric\",\"\"}");
            markdownDocument.AppendLine("    ```");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("<br type=\"page\" />");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("## Test data analysis");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The model was tested using data from " + features.Length.ToString() + " alignment columns, of which " + positives.ToString() + " (" + ((double)positives / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture) + ") were preserved and " + (mask.Length - positives).ToString() + " (" + (1 - (double)positives / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture) + ") were deleted (**Fig. 1**). For each alignment column, " + arguments.Features.Count.ToString() + " features were computed (**Table 1**), which were analysed in a Principal Component Analysis (PCA) and in a Linear Discriminant Analysis (LDA).");
            markdownDocument.AppendLine();
            #endregion

            #region Figure 1. Proportion of preserved columns.
            {
                double figureWidth = 200;

                Page proportionFigure = new Page(1, 1);
                Graphics gpr = proportionFigure.Graphics;

                gpr.FillRectangle(0, 0, figureWidth * ((double)positives / mask.Length), 16, Colour.FromRgb(213, 94, 0));
                gpr.FillRectangle(figureWidth * ((double)positives / mask.Length) + 3, 0, figureWidth * (1 - (double)positives / mask.Length), 16, Colour.FromRgb(0, 114, 178));

                gpr.StrokePath(new GraphicsPath().MoveTo(0, -3).LineTo(0, -8).LineTo(figureWidth + 3, -8).LineTo(figureWidth + 3, -3), Colours.Black);

                gpr.StrokePath(new GraphicsPath().MoveTo(0, 19).LineTo(0, 24).LineTo(figureWidth * ((double)positives / mask.Length), 24).LineTo(figureWidth * ((double)positives / mask.Length), 19), Colours.Black);
                gpr.StrokePath(new GraphicsPath().MoveTo(figureWidth * ((double)positives / mask.Length) + 3, 19).LineTo(figureWidth * ((double)positives / mask.Length) + 3, 24).LineTo(figureWidth + 3, 24).LineTo(figureWidth + 3, 19), Colours.Black);

                Font figureFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), renderer.BaseFontSize);
                gpr.FillText(figureWidth * 0.5 + 1.5 - figureFont.MeasureText(features.Length.ToString()).Width * 0.5, -10, features.Length.ToString(), figureFont, Colours.Black, TextBaselines.Bottom);

                gpr.FillText(figureWidth * ((double)positives / mask.Length) * 0.5 - figureFont.MeasureText(positives.ToString()).Width * 0.5, 26, positives.ToString(), figureFont, Colours.Black, TextBaselines.Top);
                gpr.FillText(figureWidth * ((double)positives / mask.Length) + 3 + figureWidth * (1 - (double)positives / mask.Length) * 0.5 - figureFont.MeasureText((features.Length - positives).ToString()).Width * 0.5, 26, (features.Length - positives).ToString(), figureFont, Colours.Black, TextBaselines.Top);

                gpr.FillText(-5 - figureFont.MeasureText(((double)positives / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture)).Width, 8, ((double)positives / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture), figureFont, Colours.Black, TextBaselines.Middle);

                gpr.FillText(figureWidth + 8, 8, (1 - (double)positives / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture), figureFont, Colours.Black, TextBaselines.Middle);

                proportionFigure.Crop();
                proportionFigure.Crop(new Point(0, -10), new Size(proportionFigure.Width, proportionFigure.Height + 10));

                Page orangeBox = new Page(6, 6) { Background = Colour.FromRgb(213, 94, 0) };
                Page blueBox = new Page(6, 6) { Background = Colour.FromRgb(0, 114, 178) };

                PlotUtilities.InsertFigure(markdownDocument, proportionFigure, "align=\"center\"");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.Append("**Figure 1. Proportion of preserved columns.** The figure shows the proportion of data columns that were preserved (in ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.Append(" orange, on the left) or deleted (in ");
                PlotUtilities.InsertFigure(markdownDocument, blueBox, "");
                markdownDocument.AppendLine(" blue, on the right).");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("&nbsp;");
                markdownDocument.AppendLine();
            }
            #endregion

            #region Table 1. Feature values.
            markdownDocument.AppendLine("**Table 1. Alignment features.** Features that have been computed for each alignment column, including a brief description and the observed range, mean and standard deviation (SD) for each of them.");
            markdownDocument.AppendLine();

            string[] ranges = new string[arguments.Features.Count];
            string[] meansTxts = new string[arguments.Features.Count];
            string[] stdDevsTxts = new string[arguments.Features.Count];

            for (int i = 0; i < arguments.Features.Count; i++)
            {
                ranges[i] = "_Range_: " + featureMins[i].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + " - " + featureMaxs[i].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "\\";
                meansTxts[i] = "_Mean_: " + featureMeans[i].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "\\";
                stdDevsTxts[i] = "_SD_: " + featureStdDevs[i].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
            }

            int maxNameLength = arguments.Features.Select(x => x.Name.Length).Max() + 2;
            int maxDescriptionLength = arguments.Features.Select(x => x.Description.Length).Max() + 2;
            int maxRangeLength = ranges.Select(x => x.Length).Max() + 2;
            maxRangeLength = Math.Max(maxRangeLength, meansTxts.Select(x => x.Length).Max() + 2);
            maxRangeLength = Math.Max(maxRangeLength, stdDevsTxts.Select(x => x.Length).Max() + 2);

            maxNameLength = Math.Max(maxNameLength, maxRangeLength);

            double ratio = 3;

            if (maxDescriptionLength > ratio * maxNameLength)
            {
                maxNameLength = (int)Math.Round(maxDescriptionLength / ratio);
            }
            else
            {
                maxDescriptionLength = (int)Math.Round(maxNameLength * ratio);
            }

            maxRangeLength = maxNameLength;

            markdownDocument.AppendLine("+" + new string('-', maxNameLength + 2) + "+" + new string('-', maxDescriptionLength + 2) + "+" + new string('-', maxRangeLength + 2) + "+");
            markdownDocument.AppendLine("| " + "Name".PadRight(maxNameLength) + " | " + "Description".PadRight(maxDescriptionLength) + " | " + "Values".PadRight(maxRangeLength) + " |");
            markdownDocument.AppendLine("+" + new string('=', maxNameLength + 2) + "+" + new string('=', maxDescriptionLength + 2) + "+" + new string('=', maxRangeLength + 2) + "+");
            for (int i = 0; i < arguments.Features.Count; i++)
            {
                markdownDocument.AppendLine("| " + arguments.Features[i].Name.PadRight(maxNameLength) + " | " + arguments.Features[i].Description.PadRight(maxDescriptionLength) + " | " + ranges[i].PadRight(maxRangeLength) + " | ");
                markdownDocument.AppendLine("| " + new string(' ', maxNameLength) + " | " + new string(' ', maxDescriptionLength) + " | " + meansTxts[i].PadRight(maxRangeLength) + " | ");
                markdownDocument.AppendLine("| " + new string(' ', maxNameLength) + " | " + new string(' ', maxDescriptionLength) + " | " + stdDevsTxts[i].PadRight(maxRangeLength) + " | ");
                markdownDocument.AppendLine("+" + new string('-', maxNameLength + 2) + "+" + new string('-', maxDescriptionLength + 2) + "+" + new string('-', maxRangeLength + 2) + "+");
            }
            markdownDocument.AppendLine();
            #endregion

            markdownDocument.AppendLine("&nbsp;");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("A PCA (**Fig. 2**) uses a linear transformation to transform the data to a coordinate system where each coordinate (component) explains as much of the variance of the data as possible, while being orthogonal to the previous components. This is useful to show the distribution of the input data, but a PCA, on its own, cannot be used to decide whether an alignment column should be preserved or not.");
            markdownDocument.AppendLine();

            #region Figure 2. PCA of the alignment features.
            {
                // Transform the alignment features using the PCA.
                double[][] transformedDataPreserved = pca.Transform(features.Where((x, i) => mask.MaskedStates[i]).ToArray());
                double[][] transformedDataDeleted = pca.Transform(features.Where((x, i) => !mask.MaskedStates[i]).ToArray());

                // Determine the range of the first two principal components.
                double pcaMinX = double.MaxValue;
                double pcaMaxX = double.MinValue;
                double pcaMinY = double.MaxValue;
                double pcaMaxY = double.MinValue;

                for (int i = 0; i < transformedDataPreserved.Length; i++)
                {
                    pcaMinX = Math.Min(pcaMinX, transformedDataPreserved[i][0]);
                    pcaMaxX = Math.Max(pcaMaxX, transformedDataPreserved[i][0]);
                    pcaMinY = Math.Min(pcaMinY, transformedDataPreserved[i][1]);
                    pcaMaxY = Math.Max(pcaMaxY, transformedDataPreserved[i][1]);
                }

                for (int i = 0; i < transformedDataDeleted.Length; i++)
                {
                    pcaMinX = Math.Min(pcaMinX, transformedDataDeleted[i][0]);
                    pcaMaxX = Math.Max(pcaMaxX, transformedDataDeleted[i][0]);
                    pcaMinY = Math.Min(pcaMinY, transformedDataDeleted[i][1]);
                    pcaMaxY = Math.Max(pcaMaxY, transformedDataDeleted[i][1]);
                }

                // There are likely too many points to plot them one by one; thus, instead of plotting each point, we plot how many points would fall in each region of the plot.
                int binsX = 100;
                int binsY = 100;

                int[,] countsPreserved = new int[binsX, binsY];
                int[,] countsDeleted = new int[binsX, binsY];

                for (int i = 0; i < transformedDataPreserved.Length; i++)
                {
                    int x = (int)Math.Min(Math.Floor((transformedDataPreserved[i][0] - pcaMinX) / (pcaMaxX - pcaMinX) * binsX), binsX - 1);
                    int y = (int)Math.Min(Math.Floor((transformedDataPreserved[i][1] - pcaMinY) / (pcaMaxY - pcaMinY) * binsY), binsY - 1);
                    countsPreserved[x, y]++;
                }

                for (int i = 0; i < transformedDataDeleted.Length; i++)
                {
                    int x = (int)Math.Min(Math.Floor((transformedDataDeleted[i][0] - pcaMinX) / (pcaMaxX - pcaMinX) * binsX), binsX - 1);
                    int y = (int)Math.Min(Math.Floor((transformedDataDeleted[i][1] - pcaMinY) / (pcaMaxY - pcaMinY) * binsY), binsY - 1);
                    countsDeleted[x, y]++;
                }

                // Start by plotting the density of deleted columns.
                Plot biplot = Plot.Create.Function((x, y) =>
                {
                    int xInt = (int)Math.Min(Math.Floor((x - pcaMinX) / (pcaMaxX - pcaMinX) * binsX), binsX - 1);
                    int yInt = (int)Math.Min(Math.Floor((y - pcaMinY) / (pcaMaxY - pcaMinY) * binsY), binsY - 1);

                    if (xInt < 0 || yInt < 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return Math.Min(countsDeleted[xInt, yInt], 10);
                    }
                }, pcaMinX, pcaMinY, pcaMaxX, pcaMaxY, colouring: new GradientStops(new GradientStop(Colour.FromRgba(0, 114, 178, 0), 0), new GradientStop(Colour.FromRgb(0, 114, 178), 1)),
                xAxisTitle: "PC1 (" + pca.Components[0].Proportion.ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + ")", yAxisTitle: "PC2 (" + pca.Components[1].Proportion.ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + ")");

                // Remove the axis labels and ticks.
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisLabels>());
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisLabels>());
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisTicks>());
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisTicks>());

                // Move the titles.
                biplot.GetFirst<ContinuousAxisTitle>().Position = 15;
                biplot.GetAll<ContinuousAxisTitle>().ElementAt(1).Position = -10;

                // Add the density of preserved columns.
                biplot.AddPlotElement(new Function2D(new Function2DGrid(p =>
                {
                    int xInt = (int)Math.Min(Math.Floor((p[0] - pcaMinX) / (pcaMaxX - pcaMinX) * binsX), binsX - 1);
                    int yInt = (int)Math.Min(Math.Floor((p[1] - pcaMinY) / (pcaMaxY - pcaMinY) * binsY), binsY - 1);

                    if (xInt < 0 || yInt < 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return Math.Min(countsPreserved[xInt, yInt], 10);
                    }
                }, pcaMinX, pcaMinY, pcaMaxX, pcaMaxY, 75, 54, Function2DGrid.GridType.HexagonHorizontal), biplot.GetFirst<IContinuousInvertibleCoordinateSystem>())
                {
                    Type = Function2D.PlotType.Tessellation,
                    Colouring = new GradientStops(new GradientStop(Colour.FromRgba(213, 94, 0, 0), 0), new GradientStop(Colour.FromRgb(213, 94, 0), 1)),
                });

                // Create the loadings plot.
                double maxLoadingX = double.MinValue;
                double maxLoadingY = double.MinValue;

                double minLoadingX = double.MaxValue;
                double minLoadingY = double.MaxValue;

                double[][] loadings = new double[pca.Components.Count][];
                for (int i = 0; i < pca.Components.Count; i++)
                {
                    loadings[i] = new double[arguments.Features.Count];

                    double sum = 0;

                    for (int j = 0; j < arguments.Features.Count; j++)
                    {
                        loadings[i][j] = pca.Components[i].Eigenvector[j];

                        sum += loadings[i][j] * loadings[i][j];
                    }

                    sum = Math.Sqrt(sum);

                    for (int j = 0; j < arguments.Features.Count; j++)
                    {
                        loadings[i][j] = loadings[i][j] / sum / Math.Sqrt(Math.Abs(pca.Components[i].Eigenvalue));
                    }
                }

                for (int i = 0; i < arguments.Features.Count; i++)
                {
                    maxLoadingX = Math.Max(loadings[0][i], maxLoadingX);
                    maxLoadingY = Math.Max(loadings[1][i], maxLoadingY);
                    minLoadingX = Math.Min(loadings[0][i], minLoadingX);
                    minLoadingY = Math.Min(loadings[1][i], minLoadingY);
                }

                double scalingFactor = Math.Min((pcaMaxX - pcaMinX) * 0.4 / Math.Max(Math.Abs(maxLoadingX), Math.Abs(minLoadingX)), (pcaMaxY - pcaMinY) * 0.4 / Math.Max(Math.Abs(maxLoadingY), Math.Abs(minLoadingY)));

                double[] center = new double[] { (pcaMaxX + pcaMinX) * 0.5, (pcaMaxY + pcaMinY) * 0.5 };

                for (int i = 0; i < arguments.Features.Count; i++)
                {
                    // Each loading is represented by a ContinuousAxis.
                    ContinuousAxis pcAxis = new ContinuousAxis(center, new double[] { center[0] + loadings[0][i] * scalingFactor, center[1] + loadings[1][i] * scalingFactor }, biplot.GetFirst<IContinuousCoordinateSystem>())
                    {
                        ArrowSize = 1,
                        PresentationAttributes = new PlotElementPresentationAttributes() { Fill = Colours.White, Stroke = Colours.White, LineWidth = 1.5 }
                    };

                    biplot.AddPlotElement(pcAxis);
                }

                // Determine the position of the loading labels.
                LinearCoordinateSystem2D biplotCoordinateSystem = biplot.GetFirst<LinearCoordinateSystem2D>();

                Point origin = biplotCoordinateSystem.ToPlotCoordinates(new double[2]);

                List<Point> attractors = new List<Point>();
                List<Point> axisLabelPoints = new List<Point>();

                for (int i = 0; i < arguments.Features.Count; i++)
                {
                    ContinuousAxis pcAxis = new ContinuousAxis(center, new double[] { center[0] + loadings[0][i] * scalingFactor, center[1] + loadings[1][i] * scalingFactor }, biplot.GetFirst<IContinuousCoordinateSystem>())
                    {
                        PresentationAttributes = new PlotElementPresentationAttributes() { LineWidth = 0.5 }
                    };

                    Point endPoint = biplotCoordinateSystem.ToPlotCoordinates(pcAxis.EndPoint);
                    Point direction = new Point(endPoint.X - origin.X, endPoint.Y - origin.Y).Normalize();

                    attractors.Add(endPoint);

                    axisLabelPoints.Add(new Point(endPoint.X + direction.X * 10, endPoint.Y + direction.Y * 10));

                    biplot.AddPlotElement(pcAxis);
                }

                Point[] optimisedPoints = PlotUtilities.RefinePositions(axisLabelPoints, attractors, origin, 10, 100, 10000);
                biplot.AddPlotElement(new DataLabels<IReadOnlyList<double>>(optimisedPoints.Select(x => biplotCoordinateSystem.ToDataCoordinates(x)).ToArray(), biplot.GetFirst<IContinuousCoordinateSystem>()) { Label = (i, x) => arguments.Features[i].ShortNameForPlot, PresentationAttributes = new PlotElementPresentationAttributes() { Fill = null, Stroke = Colours.White, LineWidth = 1.5 } });
                biplot.AddPlotElement(new DataLabels<IReadOnlyList<double>>(optimisedPoints.Select(x => biplotCoordinateSystem.ToDataCoordinates(x)).ToArray(), biplot.GetFirst<IContinuousCoordinateSystem>()) { Label = (i, x) => arguments.Features[i].ShortNameForPlot });

                // Create the scree plot.
                Plot screePlot = Plot.Create.LineChart(pca.Components.Select((x, i) => new double[] { i + 1, x.Eigenvalue }).ToArray(), xAxisTitle: "Principal component", yAxisTitle: "Eigenvalue", pointSize: 4,
                    linePresentationAttributes: new PlotElementPresentationAttributes() { Fill = null, Stroke = Colour.FromRgb(160, 160, 160), LineWidth = 1.5 },
                    pointPresentationAttributes: new PlotElementPresentationAttributes() { Fill = Colours.Black, Stroke = Colours.White, LineWidth = 0.5 });
                ContinuousAxisLabels xLabels = screePlot.GetFirst<ContinuousAxisLabels>();

                xLabels.TextFormat = (p, i) => FormattedText.Format("PC" + p[0].ToString("0", System.Globalization.CultureInfo.InvariantCulture), xLabels.PresentationAttributes.Font, xLabels.PresentationAttributes.Font, xLabels.PresentationAttributes.Font, xLabels.PresentationAttributes.Font);

                // Merge the two plots.
                Page fullPlot = new Page(1, 1);

                fullPlot.Graphics.DrawGraphics(0, 0, biplot.Render().Graphics);
                fullPlot.Graphics.DrawGraphics(450, 0, screePlot.Render().Graphics);

                fullPlot.Graphics.FillText(0, 0, "a)", new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16), Colours.Black);
                fullPlot.Graphics.FillText(450, 0, "b)", new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16), Colours.Black);

                fullPlot.Crop();

                // Colour scale for the biplot.
                GraphicsPath hexagon = new GraphicsPath();
                for (int i = 0; i < 6; i++)
                {
                    hexagon.LineTo(Math.Cos(Math.PI / 3 * i), Math.Sin(Math.PI / 3 * i));
                }
                hexagon.Close();

                Font legendFnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 0.7);
                Font legendFnt2 = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 0.9);
                Font legendTitleFnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 10);

                for (int i = 1; i <= 10; i++)
                {
                    string numberText = i.ToString();
                    if (i == 10)
                    {
                        numberText = "≥10";
                    }

                    fullPlot.Graphics.Save();
                    fullPlot.Graphics.Translate(390, fullPlot.Height * 0.5 + 50 * 1.732 - 10 * i * 1.732 - 10);
                    fullPlot.Graphics.Scale(10, 10);

                    fullPlot.Graphics.FillPath(hexagon, Colour.FromRgba(213, 94, 0, 0.1 * i));
                    fullPlot.Graphics.FillText(-legendFnt.MeasureText(numberText).Width * 0.5, 0, numberText, legendFnt, Colours.Black, TextBaselines.Middle);

                    if (i == 1)
                    {
                        fullPlot.Graphics.Save();
                        fullPlot.Graphics.Translate(0, 1.25);
                        fullPlot.Graphics.Rotate(-Math.PI / 2);
                        fullPlot.Graphics.FillText(-legendFnt2.MeasureText("Preserved").Width, 0, "Preserved", legendFnt2, Colours.Black, TextBaselines.Middle);
                        fullPlot.Graphics.Restore();
                    }

                    fullPlot.Graphics.Translate(2, 0);
                    fullPlot.Graphics.FillPath(hexagon, Colour.FromRgba(0, 114, 178, 0.1 * i));
                    fullPlot.Graphics.FillText(-legendFnt.MeasureText(numberText).Width * 0.5, 0, numberText, legendFnt, Colours.Black, TextBaselines.Middle);

                    if (i == 1)
                    {
                        fullPlot.Graphics.Save();
                        fullPlot.Graphics.Translate(0, 1.25);
                        fullPlot.Graphics.Rotate(-Math.PI / 2);
                        fullPlot.Graphics.FillText(-legendFnt2.MeasureText("Deleted").Width, 0, "Deleted", legendFnt2, Colours.Black, TextBaselines.Middle);
                        fullPlot.Graphics.Restore();
                    }

                    fullPlot.Graphics.Restore();
                }

                fullPlot.Graphics.FillText(400 - legendTitleFnt.MeasureText("# points").Width * 0.5, fullPlot.Height * 0.5 - 50 * 1.732 - 15 - 10, "# points", legendTitleFnt, Colours.Black, TextBaselines.Bottom);

                PlotUtilities.InsertFigure(markdownDocument, fullPlot, "align=\"center\"");

                Page orangeBox = new Page(6, 6);
                orangeBox.Graphics.Translate(3, 3);
                orangeBox.Graphics.Scale(3, 3);
                orangeBox.Graphics.FillPath(hexagon, Colour.FromRgb(213, 94, 0));

                Page blueBox = new Page(6, 6);
                blueBox.Graphics.Translate(3, 3);
                blueBox.Graphics.Scale(3, 3);
                blueBox.Graphics.FillPath(hexagon, Colour.FromRgb(0, 114, 178));

                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.Append("**Figure 2. Results of the PCA. a)** Biplot showing the density of test data columns in function of the principal component values and the component loadings. Preserved columns are shown in ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.Append(" orange, while deleted columns are shown in ");
                PlotUtilities.InsertFigure(markdownDocument, blueBox, "");
                markdownDocument.Append(" blue. _PC_: Principal component.");

                for (int i = 0; i < arguments.Features.Count; i++)
                {
                    markdownDocument.Append("; _");
                    markdownDocument.Append(arguments.Features[i].ShortName);
                    markdownDocument.Append("_: ");
                    markdownDocument.Append(arguments.Features[i].Name);
                }

                markdownDocument.AppendLine(". **b)** Scree plot showing the eigenvalue (amount of explained variance) corresponding to each principal component.");

                markdownDocument.AppendLine();
                markdownDocument.AppendLine("&nbsp;");
                markdownDocument.AppendLine();
            }
            #endregion

            markdownDocument.AppendLine("An LDA (**Fig. 3**) also uses a linear transformation to project the data to a different coordinate space, but in this case each component attempts to explain as much of the difference between the two classes of data (“preserved” or “deleted”) as possible.");
            markdownDocument.AppendLine();

            #region Figure 3. LDA of the alignment features.
            {
                double[][] transformedDataPreserved = transformedLDA.Where((x, i) => mask.MaskedStates[i]).ToArray();
                double[][] transformedDataDeleted = transformedLDA.Where((x, i) => !mask.MaskedStates[i]).ToArray();

                double ldaMinX = double.MaxValue;
                double ldaMaxX = double.MinValue;
                double ldaMinY = double.MaxValue;
                double ldaMaxY = double.MinValue;

                for (int i = 0; i < transformedDataPreserved.Length; i++)
                {
                    ldaMinX = Math.Min(ldaMinX, transformedDataPreserved[i][0]);
                    ldaMaxX = Math.Max(ldaMaxX, transformedDataPreserved[i][0]);
                    ldaMinY = Math.Min(ldaMinY, transformedDataPreserved[i][1]);
                    ldaMaxY = Math.Max(ldaMaxY, transformedDataPreserved[i][1]);
                }

                for (int i = 0; i < transformedDataDeleted.Length; i++)
                {
                    ldaMinX = Math.Min(ldaMinX, transformedDataDeleted[i][0]);
                    ldaMaxX = Math.Max(ldaMaxX, transformedDataDeleted[i][0]);
                    ldaMinY = Math.Min(ldaMinY, transformedDataDeleted[i][1]);
                    ldaMaxY = Math.Max(ldaMaxY, transformedDataDeleted[i][1]);
                }

                // There are likely too many points to plot them one by one; thus, instead of plotting each point, we plot how many points would fall in each region of the plot.
                int binsX = 100;
                int binsY = 100;

                int[,] countsPreserved = new int[binsX, binsY];
                int[,] countsDeleted = new int[binsX, binsY];

                for (int i = 0; i < transformedDataPreserved.Length; i++)
                {
                    int x = (int)Math.Min(Math.Floor((transformedDataPreserved[i][0] - ldaMinX) / (ldaMaxX - ldaMinX) * binsX), binsX - 1);
                    int y = (int)Math.Min(Math.Floor((transformedDataPreserved[i][1] - ldaMinY) / (ldaMaxY - ldaMinY) * binsY), binsY - 1);
                    countsPreserved[x, y]++;
                }

                for (int i = 0; i < transformedDataDeleted.Length; i++)
                {
                    int x = (int)Math.Min(Math.Floor((transformedDataDeleted[i][0] - ldaMinX) / (ldaMaxX - ldaMinX) * binsX), binsX - 1);
                    int y = (int)Math.Min(Math.Floor((transformedDataDeleted[i][1] - ldaMinY) / (ldaMaxY - ldaMinY) * binsY), binsY - 1);
                    countsDeleted[x, y]++;
                }

                // Start by plotting the density of deleted columns.
                Plot biplot = Plot.Create.Function((x, y) =>
                {
                    int xInt = (int)Math.Min(Math.Floor((x - ldaMinX) / (ldaMaxX - ldaMinX) * binsX), binsX - 1);
                    int yInt = (int)Math.Min(Math.Floor((y - ldaMinY) / (ldaMaxY - ldaMinY) * binsY), binsY - 1);

                    if (xInt < 0 || yInt < 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return Math.Min(countsDeleted[xInt, yInt], 10);
                    }
                }, ldaMinX, ldaMinY, ldaMaxX, ldaMaxY, colouring: new GradientStops(new GradientStop(Colour.FromRgba(0, 114, 178, 0), 0), new GradientStop(Colour.FromRgb(0, 114, 178), 1)),
                xAxisTitle: "DC1 (" + model.LdaModel.DiscriminantProportions[0].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + ")", yAxisTitle: "DC2 (" + model.LdaModel.DiscriminantProportions[1].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + ")");

                // Remove the axis labels and ticks.
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisLabels>());
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisLabels>());
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisTicks>());

                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisTicks>());

                // Move the titles.
                biplot.GetFirst<ContinuousAxisTitle>().Position = 15;
                biplot.GetAll<ContinuousAxisTitle>().ElementAt(1).Position = -10;

                // Add the density of preserved columns.
                biplot.AddPlotElement(new Function2D(new Function2DGrid(p =>
                {
                    int xInt = (int)Math.Min(Math.Floor((p[0] - ldaMinX) / (ldaMaxX - ldaMinX) * binsX), binsX - 1);
                    int yInt = (int)Math.Min(Math.Floor((p[1] - ldaMinY) / (ldaMaxY - ldaMinY) * binsY), binsY - 1);

                    if (xInt < 0 || yInt < 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return Math.Min(countsPreserved[xInt, yInt], 10);
                    }
                }, ldaMinX, ldaMinY, ldaMaxX, ldaMaxY, 75, 54, Function2DGrid.GridType.HexagonHorizontal), biplot.GetFirst<IContinuousInvertibleCoordinateSystem>())
                {
                    Type = Function2D.PlotType.Tessellation,
                    Colouring = new GradientStops(new GradientStop(Colour.FromRgba(213, 94, 0, 0), 0), new GradientStop(Colour.FromRgb(213, 94, 0), 1)),
                });

                LinearCoordinateSystem2D biplotCoordinateSystem = biplot.GetFirst<LinearCoordinateSystem2D>();

                // Draw the discriminant axis.
                {
                    double[] centroid1 = model.LdaModel.Means[0].Take(2).ToArray();
                    double[] centroid2 = model.LdaModel.Means[1].Take(2).ToArray();

                    double[] centroidCenter = centroid1.Select((x, i) => (x + centroid2[i]) * 0.5).ToArray();

                    double[] normal = centroid1.Select((x, i) => x - centroid2[i]).ToArray();
                    double mod = normal.Aggregate(0.0, (a, b) => a + b * b);
                    normal = normal.Select(x => x / mod).ToArray();

                    double[] perp = new double[] { normal[1], -normal[0] };

                    double[] p1 = new double[] { centroidCenter[0] + perp[0] * (ldaMaxY - centroidCenter[1]) / perp[1], ldaMaxY };
                    double[] p2 = new double[] { centroidCenter[0] + perp[0] * (ldaMinY - centroidCenter[1]) / perp[1], ldaMinY };

                    biplot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(biplotCoordinateSystem, (gpr, coordinateSystem) =>
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(coordinateSystem.ToPlotCoordinates(p1)).LineTo(coordinateSystem.ToPlotCoordinates(centroidCenter)).LineTo(coordinateSystem.ToPlotCoordinates(p2)), Colour.FromRgb(180, 180, 180), lineDash: new LineDash(5, 5, 0));
                    }));
                }

                // Create the loadings plot.
                double maxAxisX = double.MinValue;
                double maxAxisY = double.MinValue;

                double minAxisX = double.MaxValue;
                double minAxisY = double.MaxValue;

                double[][] axes = new double[arguments.Features.Count][];

                for (int i = 0; i < arguments.Features.Count; i++)
                {
                    double[] axis = new double[6];
                    axis[i] = model.LdaModel.StandardDeviations[i];
                    double[] transformedOrigin = model.LdaModel.Transform(new double[6]);
                    double[] transformedAxis = model.LdaModel.Transform(axis);
                    transformedAxis = transformedAxis.Select((x, i) => x - transformedOrigin[i]).ToArray();

                    maxAxisX = Math.Max(transformedAxis[0], maxAxisX);
                    maxAxisY = Math.Max(transformedAxis[1], maxAxisY);
                    minAxisX = Math.Min(transformedAxis[0], minAxisX);
                    minAxisY = Math.Min(transformedAxis[1], minAxisY);

                    axes[i] = transformedAxis;
                }

                double[] center = new double[] { (ldaMinX + ldaMaxX) * 0.5, (ldaMinY + ldaMaxY) * 0.5 };

                double scalingFactor = Math.Min((ldaMaxX - ldaMinX) * 0.4 / Math.Max(Math.Abs(maxAxisX), Math.Abs(minAxisX)), (ldaMaxY - ldaMinY) * 0.4 / Math.Max(Math.Abs(maxAxisY), Math.Abs(minAxisY)));

                for (int i = 0; i < arguments.Features.Count; i++)
                {
                    ContinuousAxis dcAxis = new ContinuousAxis(center, new double[] { center[0] + axes[i][0] * scalingFactor, center[1] + axes[i][1] * scalingFactor }, biplot.GetFirst<IContinuousCoordinateSystem>())
                    {
                        ArrowSize = 1,
                        PresentationAttributes = new PlotElementPresentationAttributes() { Fill = Colours.White, Stroke = Colours.White, LineWidth = 1.5 }
                    };

                    biplot.AddPlotElement(dcAxis);
                }

                Point origin = biplotCoordinateSystem.ToPlotCoordinates(new double[2]);

                List<Point> attractors = new List<Point>();
                List<Point> axisLabelPoints = new List<Point>();

                for (int i = 0; i < arguments.Features.Count; i++)
                {
                    // Each loading is represented by a ContinuousAxis.
                    ContinuousAxis dcAxis = new ContinuousAxis(center, new double[] { center[0] + axes[i][0] * scalingFactor, center[1] + axes[i][1] * scalingFactor }, biplot.GetFirst<IContinuousCoordinateSystem>())
                    {
                        PresentationAttributes = new PlotElementPresentationAttributes() { LineWidth = 0.5 }
                    };

                    Point endPoint = biplotCoordinateSystem.ToPlotCoordinates(dcAxis.EndPoint);
                    Point direction = new Point(endPoint.X - origin.X, endPoint.Y - origin.Y).Normalize();

                    attractors.Add(endPoint);

                    axisLabelPoints.Add(new Point(endPoint.X + direction.X * 10, endPoint.Y + direction.Y * 10));

                    biplot.AddPlotElement(dcAxis);
                }

                // Determine the position of the loading labels.
                Point[] optimisedPoints = PlotUtilities.RefinePositions(axisLabelPoints, attractors, origin, 10, 100, 10000);
                biplot.AddPlotElement(new DataLabels<IReadOnlyList<double>>(optimisedPoints.Select(x => biplotCoordinateSystem.ToDataCoordinates(x)).ToArray(), biplot.GetFirst<IContinuousCoordinateSystem>()) { Label = (i, x) => arguments.Features[i].ShortNameForPlot, PresentationAttributes = new PlotElementPresentationAttributes() { Fill = null, Stroke = Colours.White, LineWidth = 1.5 } });
                biplot.AddPlotElement(new DataLabels<IReadOnlyList<double>>(optimisedPoints.Select(x => biplotCoordinateSystem.ToDataCoordinates(x)).ToArray(), biplot.GetFirst<IContinuousCoordinateSystem>()) { Label = (i, x) => arguments.Features[i].ShortNameForPlot });

                // Create the scree plot.
                Plot screePlot = Plot.Create.LineChart(model.LdaModel.Eigenvalues.Select((x, i) => new double[] { i + 1, x }).ToArray(), xAxisTitle: "Discriminant component", yAxisTitle: "Eigenvalue", pointSize: 4,
                    linePresentationAttributes: new PlotElementPresentationAttributes() { Fill = null, Stroke = Colour.FromRgb(160, 160, 160), LineWidth = 1.5 },
                    pointPresentationAttributes: new PlotElementPresentationAttributes() { Fill = Colours.Black, Stroke = Colours.White, LineWidth = 0.5 });
                ContinuousAxisLabels xLabels = screePlot.GetFirst<ContinuousAxisLabels>();

                xLabels.TextFormat = (p, i) => FormattedText.Format("DC" + p[0].ToString("0", System.Globalization.CultureInfo.InvariantCulture), xLabels.PresentationAttributes.Font, xLabels.PresentationAttributes.Font, xLabels.PresentationAttributes.Font, xLabels.PresentationAttributes.Font);

                // Merge the two plots.
                Page fullPlot = new Page(1, 1);

                fullPlot.Graphics.DrawGraphics(0, 0, biplot.Render().Graphics);
                fullPlot.Graphics.DrawGraphics(450, 0, screePlot.Render().Graphics);

                fullPlot.Graphics.FillText(0, 0, "a)", new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16), Colours.Black);
                fullPlot.Graphics.FillText(450, 0, "b)", new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16), Colours.Black);

                fullPlot.Crop();

                // Colour scale for the biplot.
                GraphicsPath hexagon = new GraphicsPath();
                for (int i = 0; i < 6; i++)
                {
                    hexagon.LineTo(Math.Cos(Math.PI / 3 * i), Math.Sin(Math.PI / 3 * i));
                }
                hexagon.Close();

                Font legendFnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 0.7);
                Font legendFnt2 = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 0.9);
                Font legendTitleFnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 10);

                for (int i = 1; i <= 10; i++)
                {
                    string numberText = i.ToString();
                    if (i == 10)
                    {
                        numberText = "≥10";
                    }

                    fullPlot.Graphics.Save();
                    fullPlot.Graphics.Translate(390, fullPlot.Height * 0.5 + 50 * 1.732 - 10 * i * 1.732 - 10);
                    fullPlot.Graphics.Scale(10, 10);

                    fullPlot.Graphics.FillPath(hexagon, Colour.FromRgba(213, 94, 0, 0.1 * i));
                    fullPlot.Graphics.FillText(-legendFnt.MeasureText(numberText).Width * 0.5, 0, numberText, legendFnt, Colours.Black, TextBaselines.Middle);

                    if (i == 1)
                    {
                        fullPlot.Graphics.Save();
                        fullPlot.Graphics.Translate(0, 1.25);
                        fullPlot.Graphics.Rotate(-Math.PI / 2);
                        fullPlot.Graphics.FillText(-legendFnt2.MeasureText("Preserved").Width, 0, "Preserved", legendFnt2, Colours.Black, TextBaselines.Middle);
                        fullPlot.Graphics.Restore();
                    }

                    fullPlot.Graphics.Translate(2, 0);
                    fullPlot.Graphics.FillPath(hexagon, Colour.FromRgba(0, 114, 178, 0.1 * i));
                    fullPlot.Graphics.FillText(-legendFnt.MeasureText(numberText).Width * 0.5, 0, numberText, legendFnt, Colours.Black, TextBaselines.Middle);

                    if (i == 1)
                    {
                        fullPlot.Graphics.Save();
                        fullPlot.Graphics.Translate(0, 1.25);
                        fullPlot.Graphics.Rotate(-Math.PI / 2);
                        fullPlot.Graphics.FillText(-legendFnt2.MeasureText("Deleted").Width, 0, "Deleted", legendFnt2, Colours.Black, TextBaselines.Middle);
                        fullPlot.Graphics.Restore();
                    }

                    fullPlot.Graphics.Restore();
                }

                fullPlot.Graphics.FillText(400 - legendTitleFnt.MeasureText("# points").Width * 0.5, fullPlot.Height * 0.5 - 50 * 1.732 - 15 - 10, "# points", legendTitleFnt, Colours.Black, TextBaselines.Bottom);

                PlotUtilities.InsertFigure(markdownDocument, fullPlot, "align=\"center\"");

                Page orangeBox = new Page(6, 6);
                orangeBox.Graphics.Translate(3, 3);
                orangeBox.Graphics.Scale(3, 3);
                orangeBox.Graphics.FillPath(hexagon, Colour.FromRgb(213, 94, 0));

                Page blueBox = new Page(6, 6);
                blueBox.Graphics.Translate(3, 3);
                blueBox.Graphics.Scale(3, 3);
                blueBox.Graphics.FillPath(hexagon, Colour.FromRgb(0, 114, 178));

                Page dashedBox = new Page(15, 6);
                dashedBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(180, 180, 180), lineDash: new LineDash(3, 3, 0));

                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.Append("**Figure 3. Results of the LDA. a)** Biplot showing the density of training data columns in function of the discriminant component values and the projections of the original features in the LDA space. Preserved columns are shown in ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.Append(" orange, while deleted columns are shown in ");
                PlotUtilities.InsertFigure(markdownDocument, blueBox, "");
                markdownDocument.Append(" blue. The ");
                PlotUtilities.InsertFigure(markdownDocument, dashedBox, "");
                markdownDocument.AppendLine(" dashed line represents the intersection of the discriminant hyperplane with the 2D plane shown in the plot. Component abbreviations are _DC1_: Discriminant component 1; _DC2_: Discriminant component 2; the rest as in **Figure 2a**. **b)** Scree plot showing the eigenvalue (amount of explained variance) corresponding to each discriminant component.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("&nbsp;");
                markdownDocument.AppendLine();
            }
            #endregion

            markdownDocument.AppendLine("The LDA can be used to determine whether an alignment column should be preserved or deleted, by checking whether its representation in LDA space lies closer to centroid of the preserved columns or the centroid of the deleted columns. This defines a hyperplane in the LDA coordinate space (dashed line in **Fig. 3a**), such that all columns that are located on one side of this hyperplane are preserved, and all columns that are located on the other side are deleted.");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("When this criterion is used to analyse the input data, " + incorrectAssignments + " columns (" + ((double)incorrectAssignments / features.Length).ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + " of the total) are incorrectly preserved or deleted (**Fig. 4**). Generally, these points should be located around the discriminant hyperplane; if many of them are located far from the discriminant plane, it might be a sign that the test dataset is internally inconsistent. Alternatively, the " + arguments.Features.Count.ToString() + " features analysed by AliFilter may not be sufficient to capture the distinction between columns that have been preserved and those that have been deleted.");
            markdownDocument.AppendLine();

            #region Figure 4. Alignment columns incorrectly assigned by the LDA analysis.
            {
                // Gather the correctly and incorrectly assigned columns.
                double[][] transformedDataPreserved = transformedLDA.Where((x, i) => mask.MaskedStates[i]).ToArray();
                double[][] transformedDataDeleted = transformedLDA.Where((x, i) => !mask.MaskedStates[i]).ToArray();

                double[][] transformedDataPreservedIncorrect = transformedLDA.Where((x, i) => mask.MaskedStates[i] && ldaAssignments[i] != mask.MaskedStates[i]).ToArray();
                double[][] transformedDataDeletedIncorrect = transformedLDA.Where((x, i) => !mask.MaskedStates[i] && ldaAssignments[i] != mask.MaskedStates[i]).ToArray();

                // Determine the range of the first two discriminant components.
                double ldaMinX = double.MaxValue;
                double ldaMaxX = double.MinValue;
                double ldaMinY = double.MaxValue;
                double ldaMaxY = double.MinValue;

                for (int i = 0; i < transformedDataPreserved.Length; i++)
                {
                    ldaMinX = Math.Min(ldaMinX, transformedDataPreserved[i][0]);
                    ldaMaxX = Math.Max(ldaMaxX, transformedDataPreserved[i][0]);
                    ldaMinY = Math.Min(ldaMinY, transformedDataPreserved[i][1]);
                    ldaMaxY = Math.Max(ldaMaxY, transformedDataPreserved[i][1]);
                }

                for (int i = 0; i < transformedDataDeleted.Length; i++)
                {
                    ldaMinX = Math.Min(ldaMinX, transformedDataDeleted[i][0]);
                    ldaMaxX = Math.Max(ldaMaxX, transformedDataDeleted[i][0]);
                    ldaMinY = Math.Min(ldaMinY, transformedDataDeleted[i][1]);
                    ldaMaxY = Math.Max(ldaMaxY, transformedDataDeleted[i][1]);
                }

                // Create the biplot (in this case, we can plot the points one by one).
                Plot biplot = Plot.Create.ScatterPlot(new double[][][] { transformedDataDeletedIncorrect, transformedDataPreservedIncorrect, new double[][] { new double[] { ldaMinX, ldaMinY }, new double[] { ldaMaxX, ldaMaxY } } }, xAxisTitle: "DC1 (" + model.LdaModel.DiscriminantProportions[0].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + ")", yAxisTitle: "DC2 (" + model.LdaModel.DiscriminantProportions[1].ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + ")");

                // Remove the axis labels, ticks, and grids.
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisLabels>());
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisLabels>());
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisTicks>());
                biplot.RemovePlotElement(biplot.GetFirst<ContinuousAxisTicks>());
                biplot.RemovePlotElement(biplot.GetFirst<Grid>());
                biplot.RemovePlotElement(biplot.GetFirst<Grid>());
                biplot.RemovePlotElement(biplot.GetAll<ScatterPoints<IReadOnlyList<double>>>().ElementAt(2));

                // Move the titles.
                biplot.GetFirst<ContinuousAxisTitle>().Position = 15;
                biplot.GetAll<ContinuousAxisTitle>().ElementAt(1).Position = -10;

                LinearCoordinateSystem2D biplotCoordinateSystem = biplot.GetFirst<LinearCoordinateSystem2D>();

                // Draw the discriminant axis.
                {
                    double[] centroid1 = model.LdaModel.Means[0].Take(2).ToArray();
                    double[] centroid2 = model.LdaModel.Means[1].Take(2).ToArray();

                    double[] centroidCenter = centroid1.Select((x, i) => (x + centroid2[i]) * 0.5).ToArray();

                    double[] normal = centroid1.Select((x, i) => x - centroid2[i]).ToArray();
                    double mod = normal.Aggregate(0.0, (a, b) => a + b * b);
                    normal = normal.Select(x => x / mod).ToArray();

                    double[] perp = new double[] { normal[1], -normal[0] };

                    double[] p1 = new double[] { centroidCenter[0] + perp[0] * (ldaMaxY - centroidCenter[1]) / perp[1], ldaMaxY };
                    double[] p2 = new double[] { centroidCenter[0] + perp[0] * (ldaMinY - centroidCenter[1]) / perp[1], ldaMinY };

                    biplot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(biplotCoordinateSystem, (gpr, coordinateSystem) =>
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(coordinateSystem.ToPlotCoordinates(p1)).LineTo(coordinateSystem.ToPlotCoordinates(centroidCenter)).LineTo(coordinateSystem.ToPlotCoordinates(p2)), Colour.FromRgb(180, 180, 180), lineDash: new LineDash(5, 5, 0));
                    }));
                }

                PlotUtilities.InsertFigure(markdownDocument, biplot.Render(), "align=\"center\"");

                Page orangeBox = new Page(6, 6);
                orangeBox.Graphics.Translate(3, 3);
                orangeBox.Graphics.Scale(3, 3);
                orangeBox.Graphics.FillPath(new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI), Colour.FromRgb(213, 94, 0));

                Page blueBox = new Page(6, 6);
                blueBox.Graphics.Translate(3, 3);
                blueBox.Graphics.Scale(3, 3);
                blueBox.Graphics.FillPath(new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI), Colour.FromRgb(0, 114, 178));

                Page dashedBox = new Page(15, 6);
                dashedBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(180, 180, 180), lineDash: new LineDash(3, 3, 0));

                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.Append("**Figure 4. Incorrectly assigned input columns.** This scatter plot shows the position in the LDA space of the input columns that were assigned incorrectly by the LDA analysis. Columns that were incorrectly deleted are shown in ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.Append(" orange, while columns that were incorrectly preserved are shown in ");
                PlotUtilities.InsertFigure(markdownDocument, blueBox, "");
                markdownDocument.Append(" blue. The ");
                PlotUtilities.InsertFigure(markdownDocument, dashedBox, "");
                markdownDocument.AppendLine(" dashed line represents the intersection of the discriminant hyperplane with the 2D plane shown in the plot. _DC1_: Discriminant component 1; _DC2_: Discriminant component 2.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("&nbsp;");
                markdownDocument.AppendLine();
            }
            #endregion

            int misAssigned = predictedMask.MaskedStates.Where((x, i) => x != mask.MaskedStates[i]).Count();

            markdownDocument.AppendLine("<br type=\"page\" />");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("## Model analysis");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The model being tested is a logistic model, which uses a linear combination of the feature values for each column to determine the log-odds that the column be preserved. The log-odds are then converted to a preservation score (ranging from 0 to 1), and columns with a preservation score lower than a specified threshold (in this case, " + threshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ") are deleted.");

            if (bootstrapReplicateCount > 0)
            {
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("For each alignment column, " + bootstrapReplicateCount.ToString() + " bootstrap replicates were created, and each of them was assessed using the logistic model described above. Only columns where at least " + bootstrapThreshold.ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + " of the bootstrap replicates were above the threshold were actually preserved.");
            }

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The distribution of the preservation score for each column computed using this model is shown in **Figure 5a**.");
            markdownDocument.AppendLine("When this criterion is used to analyse the test data, " + misAssigned + " columns (" + ((double)misAssigned / features.Length).ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + " of the total) are incorrectly preserved or deleted. The distribution of the preservation scores for these incorrectly-assigned columns is shown in **Figure 5b**.");
            markdownDocument.AppendLine();

            #region Figure 5. Logistic model results.
            {
                Page fullPlot = new Page(1, 1);

                // Use a sigmoid coordinate system to highlight values close to 0 and 1.
                SigmoidCoordinateSystem coordinateSystem = new SigmoidCoordinateSystem(0, 25, 4, 300, 200);

                double plotTreshold = bootstrapReplicateCount == 0 ? threshold : bootstrapThreshold;

                {
                    (string, IReadOnlyList<double>)[] transformedLog = new (string, IReadOnlyList<double>)[]
                    {
                        ("Deleted", predictedMask.Confidence.Select((x, i) => predictedMask.MaskedStates[i] ? x : (1 - x)).Where((x, i) => !mask.MaskedStates[i]).ToArray()),
                        ("Preserved", predictedMask.Confidence.Select((x, i) => predictedMask.MaskedStates[i] ? x : (1 - x)).Where((x, i) => mask.MaskedStates[i]).ToArray())
                    };

                    transformedLog[0] = (transformedLog[0].Item1 + " (" + transformedLog[0].Item2.Count.ToString() + ")", transformedLog[0].Item2);
                    transformedLog[1] = (transformedLog[1].Item1 + " (" + transformedLog[1].Item2.Count.ToString() + ")", transformedLog[1].Item2);

                    // Display the scores of the deleted and preserved columns using a violin plot.
                    Plot violinPlotLog = Plot.Create.ViolinPlot(transformedLog, spacing: 0.5, coordinateSystem: coordinateSystem, showBoxPlots: false, yAxisTitle: "Preservation score", dataRangeMin: 1e-8, dataRangeMax: 1 - 1e-8);
                    violinPlotLog.AddPlotElement(new LinearTrendLine(0, plotTreshold, 0, plotTreshold - 0.01, 25, plotTreshold + 0.01, coordinateSystem));
                    violinPlotLog.AddPlotElement(new TextLabel<IReadOnlyList<double>>(plotTreshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), new double[] { 12.5, coordinateSystem.ToDataCoordinates(new Point(12.5, coordinateSystem.ToPlotCoordinates(new double[] { 12.5, plotTreshold }).Y - 5))[1] }, coordinateSystem) { Alignment = TextAnchors.Center, Baseline = TextBaselines.Bottom });
                    violinPlotLog.GetFirst<ContinuousAxisLabels>().TextFormat = (x, i) => FormattedText.Format(x[1].ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), FontFamily.StandardFontFamilies.Helvetica, 12);
                    violinPlotLog.GetAll<ContinuousAxisTitle>().ElementAt(1).Position -= 10;

                    Violin v1 = violinPlotLog.GetFirst<Violin>();
                    Violin v2 = violinPlotLog.GetAll<Violin>().ElementAt(1);

                    foreach (Violin violin in violinPlotLog.GetAll<Violin>())
                    {
                        violin.MaxBins = 1000;
                    }

                    if (transformedLog[0].Item2.Count == 0)
                    {
                        violinPlotLog.RemovePlotElement(v1);
                    }

                    if (transformedLog[1].Item2.Count == 0)
                    {
                        violinPlotLog.RemovePlotElement(v2);
                    }

                    foreach (Violin violin in violinPlotLog.GetAll<Violin>())
                    {
                        violin.MaxBins = 1000;
                    }

                    Page violinPlotLogPage = violinPlotLog.Render();
                    fullPlot.Graphics.DrawGraphics(0, 0, violinPlotLogPage.Graphics);
                }

                // Display the scores of the incorrectly assigned columns using another violin plot.
                {
                    (string, IReadOnlyList<double>)[] transformedLog = new (string, IReadOnlyList<double>)[]
                    {
                        ("Preserved", predictedMask.Confidence.Select((x, i) => predictedMask.MaskedStates[i] ? x : (1 - x)).Where((x, i) => (mask.MaskedStates[i] != predictedMask.MaskedStates[i]) && !mask.MaskedStates[i]).ToArray()),
                        ("Deleted", predictedMask.Confidence.Select((x, i) => predictedMask.MaskedStates[i] ? x : (1 - x)).Where((x, i) => (mask.MaskedStates[i] != predictedMask.MaskedStates[i]) && mask.MaskedStates[i]).ToArray())
                    };

                    transformedLog[0] = (transformedLog[0].Item1 + " (" + transformedLog[0].Item2.Count.ToString() + ")", transformedLog[0].Item2);
                    transformedLog[1] = (transformedLog[1].Item1 + " (" + transformedLog[1].Item2.Count.ToString() + ")", transformedLog[1].Item2);

                    Plot violinPlotLog = Plot.Create.ViolinPlot(transformedLog, spacing: 0.5, coordinateSystem: coordinateSystem, showBoxPlots: false, yAxisTitle: "Preservation score", dataRangeMin: 1e-8, dataRangeMax: 1 - 1e-8);
                    violinPlotLog.AddPlotElement(new LinearTrendLine(0, plotTreshold, 0, plotTreshold - 0.01, 25, plotTreshold + 0.01, coordinateSystem));
                    violinPlotLog.AddPlotElement(new TextLabel<IReadOnlyList<double>>(plotTreshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), new double[] { 12.5, coordinateSystem.ToDataCoordinates(new Point(12.5, coordinateSystem.ToPlotCoordinates(new double[] { 12.5, plotTreshold }).Y - 5))[1] }, coordinateSystem) { Alignment = TextAnchors.Center, Baseline = TextBaselines.Bottom });
                    violinPlotLog.GetFirst<ContinuousAxisLabels>().TextFormat = (x, i) => FormattedText.Format(x[1].ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), FontFamily.StandardFontFamilies.Helvetica, 12);
                    violinPlotLog.GetAll<ContinuousAxisTitle>().ElementAt(1).Position -= 10;

                    Violin v1 = violinPlotLog.GetFirst<Violin>();
                    Violin v2 = violinPlotLog.GetAll<Violin>().ElementAt(1);

                    foreach (Violin violin in violinPlotLog.GetAll<Violin>())
                    {
                        violin.MaxBins = 1000;
                    }

                    if (transformedLog[0].Item2.Count == 0)
                    {
                        violinPlotLog.RemovePlotElement(v1);
                    }

                    if (transformedLog[1].Item2.Count == 0)
                    {
                        violinPlotLog.RemovePlotElement(v2);
                    }

                    Page violinPlotLogPage = violinPlotLog.Render();
                    fullPlot.Graphics.DrawGraphics(450, 0, violinPlotLogPage.Graphics);
                }

                fullPlot.Graphics.FillText(0, 0, "a)", new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16), Colours.Black);
                fullPlot.Graphics.FillText(450, 0, "b)", new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16), Colours.Black);

                fullPlot.Crop();

                PlotUtilities.InsertFigure(markdownDocument, fullPlot, "align=\"center\"");

                Page orangeBox = new Page(6, 6);
                orangeBox.Graphics.Translate(3, 3);
                orangeBox.Graphics.Scale(3, 3);
                orangeBox.Graphics.FillPath(new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI), Colour.FromRgb(213, 94, 0));

                Page blueBox = new Page(6, 6);
                blueBox.Graphics.Translate(3, 3);
                blueBox.Graphics.Scale(3, 3);
                blueBox.Graphics.FillPath(new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI), Colour.FromRgb(0, 114, 178));

                Page dashedBox = new Page(15, 6);
                dashedBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(180, 180, 180), lineDash: new LineDash(3, 3, 0));

                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.Append("**Figure 5. Logistic model results. a)** Distribution of the preservation score according to the logistic model for columns that were marked as “deleted” in the input data (in ");
                PlotUtilities.InsertFigure(markdownDocument, blueBox, "");
                markdownDocument.Append(" blue, on the left) and for columns that were marked as “preserved” in the input data (in ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.Append(" orange, on the right). The ");
                PlotUtilities.InsertFigure(markdownDocument, dashedBox, "");
                markdownDocument.AppendLine(" dashed line represents the " + plotTreshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " threshold that determines whether a column is preserved or deleted according to the model. **b)** Distribution of the preservation score for columns that were incorrectly preserved (in ");
                PlotUtilities.InsertFigure(markdownDocument, blueBox, "");
                markdownDocument.Append(" blue, on the left) and for columns that were incorrectly deleted (in ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.Append(" orange, on the right). The ");
                PlotUtilities.InsertFigure(markdownDocument, dashedBox, "");
                markdownDocument.AppendLine(" dashed line represents the " + plotTreshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " threshold that determines whether a column is preserved or deleted according to the model.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("&nbsp;");
                markdownDocument.AppendLine();
            }
            #endregion

            #region Table 2. Confusion matrix.
            {
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                Page confusionMatrix = new Page(200, 70);

                Graphics gpr = confusionMatrix.Graphics;
                Font baseFont = new Font(renderer.RegularFontFamily, renderer.BaseFontSize);
                Font boldFont = new Font(renderer.BoldFontFamily, renderer.BaseFontSize);

                double preservedWidth = baseFont.MeasureText("Preserved").Width;
                double testDataWidth = boldFont.MeasureText("Test data").Width;

                gpr.FillText(renderer.BaseFontSize * 1.4 + preservedWidth + 5 + (confusionMatrix.Width - renderer.BaseFontSize * 1.4 - testDataWidth - preservedWidth - 5) * 0.5, 0, "Test data", boldFont, Colours.Black);

                gpr.Save();
                gpr.Translate(0, renderer.BaseFontSize * 1.4 * 2 + (confusionMatrix.Height - renderer.BaseFontSize * 1.4 * 2) * 0.5);
                gpr.Rotate(-Math.PI / 2);
                gpr.FillText(-boldFont.MeasureText("Model").Width * 0.5, 0, "Model", boldFont, Colours.Black);
                gpr.Restore();

                gpr.FillText(renderer.BaseFontSize * 1.4 + preservedWidth + 5 + (confusionMatrix.Width - renderer.BaseFontSize * 1.4 - preservedWidth - 5) * 0.25 - baseFont.MeasureText("Preserved").Width * 0.5, renderer.BaseFontSize * 1.4, "Preserved", baseFont, Colours.Black);
                gpr.FillText(renderer.BaseFontSize * 1.4 + preservedWidth + 5 + (confusionMatrix.Width - renderer.BaseFontSize * 1.4 - preservedWidth - 5) * 0.75 - baseFont.MeasureText("Deleted").Width * 0.5, renderer.BaseFontSize * 1.4, "Deleted", baseFont, Colours.Black);

                gpr.FillText(renderer.BaseFontSize * 1.4, renderer.BaseFontSize * 1.4 * 2 + (confusionMatrix.Height - renderer.BaseFontSize * 1.4 * 2) * 0.25, "Preserved", baseFont, Colours.Black, TextBaselines.Middle);
                gpr.FillText(renderer.BaseFontSize * 1.4, renderer.BaseFontSize * 1.4 * 2 + (confusionMatrix.Height - renderer.BaseFontSize * 1.4 * 2) * 0.75, "Deleted", baseFont, Colours.Black, TextBaselines.Middle);


                double cellWidth = (confusionMatrix.Width - renderer.BaseFontSize * 1.4 - preservedWidth - 5) * 0.5;
                double cellHeight = (confusionMatrix.Height - renderer.BaseFontSize * 1.4 * 2) * 0.5;

                gpr.Save();

                gpr.Translate(renderer.BaseFontSize * 1.4 + preservedWidth + 5, renderer.BaseFontSize * 1.4 * 2);

                gpr.StrokePath(new GraphicsPath().MoveTo(-preservedWidth - 5, 0).LineTo(cellWidth * 2, 0), renderer.TableRowSeparatorColour);
                gpr.StrokePath(new GraphicsPath().MoveTo(-preservedWidth - 5, cellHeight).LineTo(cellWidth * 2, cellHeight), renderer.TableRowSeparatorColour);

                gpr.StrokePath(new GraphicsPath().MoveTo(0, -renderer.BaseFontSize * 1.4).LineTo(0, cellHeight * 2), renderer.TableRowSeparatorColour);
                gpr.StrokePath(new GraphicsPath().MoveTo(cellWidth, -renderer.BaseFontSize * 1.4).LineTo(cellWidth, cellHeight * 2), renderer.TableRowSeparatorColour);


                MarkdownRenderer cellRenderer = new MarkdownRenderer() { Margins = new Margins(0, 0, 0, 0) };

                Page tp = cellRenderer.RenderSinglePage("$\\mathrm{TP}$ = " + truePositives.ToString(), cellWidth, out _, out _);
                tp.Crop();
                gpr.DrawGraphics(cellWidth * 0.5 - tp.Width * 0.5, cellHeight * 0.5 - tp.Height * 0.5, tp.Graphics);

                Page fp = cellRenderer.RenderSinglePage("$\\mathrm{FP}$ = " + falsePositives.ToString(), cellWidth, out _, out _);
                fp.Crop();
                gpr.DrawGraphics(cellWidth + cellWidth * 0.5 - fp.Width * 0.5, cellHeight * 0.5 - fp.Height * 0.5, fp.Graphics);

                Page fn = cellRenderer.RenderSinglePage("$\\mathrm{FN}$ = " + falseNegatives.ToString(), cellWidth, out _, out _);
                fn.Crop();
                gpr.DrawGraphics(cellWidth * 0.5 - fn.Width * 0.5, cellHeight + cellHeight * 0.5 - fn.Height * 0.5, fn.Graphics);

                Page tn = cellRenderer.RenderSinglePage("$\\mathrm{TN}$ = " + trueNegatives.ToString(), cellWidth, out _, out _);
                tn.Crop();
                gpr.DrawGraphics(cellWidth + cellWidth * 0.5 - tn.Width * 0.5, cellHeight + cellHeight * 0.5 - tn.Height * 0.5, tn.Graphics);

                gpr.Restore();

                Page legend = cellRenderer.RenderSinglePage("**Table 2. Confusion matrix.** The table shows the number of true positives ($\\mathrm{TP}$), false positives ($\\mathrm{FP}$), true negatives ($\\mathrm{TN}$), and false negatives ($\\mathrm{FN}$), when comparing the model assignments with the test data.", 200, out _, out _);

                Page fullPage = new Page(200, legend.Height + confusionMatrix.Height);

                fullPage.Graphics.DrawGraphics(0, 0, legend.Graphics);
                fullPage.Graphics.DrawGraphics(0, legend.Height, confusionMatrix.Graphics);

                PlotUtilities.InsertFigure(markdownDocument, fullPage, "align=\"right\"");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
            }
            #endregion

            markdownDocument.AppendLine("The " + (bootstrapReplicateCount > 0 ? "bootstrap " : "") + "threshold value (in this case, " + (bootstrapReplicateCount > 0 ? bootstrapThreshold : threshold).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ") determines which columns are deleted or preserved. Alignment columns with a score lower than the threshold are deleted, while those with a score higher than or equal to the threshold are preserved. This determines the number of true and false positives and negatives (summarised in the confusion matrix, **Table 2**).");

            markdownDocument.AppendLine("### Performance metrics");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("Starting from the values in the confusion matrix, a number of metrics describing the performance of the model can be computed.");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("#### Accuracy");
            markdownDocument.AppendLine("The **accuracy** ($A$) represents the proportion of correct model assignments over the total number of assignments:");
            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine(@"A = \frac{\mathrm{TP} + \mathrm{TN}}{\mathrm{TP} + \mathrm{TN} + \mathrm{FP} + \mathrm{FN}} = " + Utilities.ComputeAccuracy(truePositives, trueNegatives, falsePositives, falseNegatives).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine("The accuracy score ranges from 0 to 1, with good models scoring close to 1. Note that in the case of a very imbalanced test dataset (i.e., where most columns are preserved or discarded), a trivial classifier that either preserves all columns or discards al columns might have a surprisingly high accuracy.");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("#### Matthews correlation coefficient");
            markdownDocument.AppendLine("The **Matthews correlation coefficient** ($MCC$) measures the correlation of the predicted assignments with the test assignments:");
            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine(@"MCC = \frac{\mathrm{TP} \cdot \mathrm{TN} - \mathrm{FP} \cdot \mathrm{FN}}{\sqrt { \left ( \mathrm{TP} + \mathrm{FP} \right ) \cdot \left ( \mathrm{TP} + \mathrm{FN} \right ) \cdot \left ( \mathrm{TN} + \mathrm{FP} \right ) \cdot \left ( \mathrm{TN} + \mathrm{FN} \right ) }} = " + Utilities.ComputeMCC(truePositives, trueNegatives, falsePositives, falseNegatives).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine("The $MCC$ ranges from -1 to 1, with good models scoring close to 1. Values close to 0 indicate performance close to a random classifier. A high $MCC$ usually indicates good model performance, even when the test dataset is biased towards preserved or deleted columns; however, on a small and very biased test dataset, it is possible to obtain a low $MCC$ even when the model performance appears relatively good (i.e., a small number of incorrectly assigned columns).");
            markdownDocument.AppendLine();

            double tpr = truePositives == 0 ? 0 : ((double)truePositives / (truePositives + falseNegatives));
            double ppv = truePositives == 0 ? 0 : ((double)truePositives / (truePositives + falsePositives));
            double fpr = falsePositives == 0 ? 0 : ((double)falsePositives / (falsePositives + trueNegatives));

            markdownDocument.AppendLine("#### Rate metrics");
            markdownDocument.AppendLine("The **true positive rate** ($TPR$, also known as _recall_ or _sensitivity_) and the **positive predictive value** ($PPV$, also known as _precision_) are defined as:");
            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine(@"TPR = \frac{\mathrm{TP}}{\mathrm{TP} + \mathrm{FN}} = " + tpr.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + @" \qquad \qquad PPV = \frac{\mathrm{TP}}{\mathrm{TP} + \mathrm{FP}} = " + ppv.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine("The $TPR$ represents the proportion of columns marked as preserved in the test set, which are also preserved by the model. The $PPV$ represents the proportion of columns that were preserved by the model, which were actually marked as preserved within the test set.");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("Both of these values range from 0 to 1. A high $TPR$ indicates that the model is able to correctly identify all the columns that should be preserved, while a high $PPV$ indicates that all the columns preserved by the model should indeed be preserved. An ideal model should have both $TPR$ and $PPV$ close to 1.");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The **false positive rate** ($FPR$) is defined as:");
            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine(@"FPR = \frac{\mathrm{FP}}{\mathrm{FP} + \mathrm{TN}} = " + fpr.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine("This also ranges from 0 to 1. A high $FPR$ indicates that the model tends to preserve columns that should instead be deleted, thus an ideal model's $FPR$ should be close to 0.");
            markdownDocument.AppendLine();

            double f05 = Utilities.ComputeFBeta(0.5, truePositives, falsePositives, falseNegatives);
            double f1 = Utilities.ComputeFBeta(1, truePositives, falsePositives, falseNegatives);
            double f2 = Utilities.ComputeFBeta(2, truePositives, falsePositives, falseNegatives);

            markdownDocument.AppendLine("#### $F_\\beta$ score");
            markdownDocument.AppendLine("The **$\\mathbfit{F_{\\beta}}$ score** is a weighted harmonic mean of the $TPR$ and $PPV$, defined as:");
            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine(@"F_{\beta} = \frac{\left (1 + \beta^2 \right )\mathrm{TP}}{\left (1 + \beta^2 \right ) \mathrm{TP} + \beta^2 \mathrm{FN} + \mathrm{FP}} \qquad \qquad \begin{array}{r} F_{0.5} = " + f05.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + @" \\ F_1 = " + f1.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + @" \\ F_2 = " + f2.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + @" \end{array}");
            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine("The $F_{\\beta}$ score ranges from 0 to 1, with good models scoring close to 1. Note that this score does not account for the number of true negatives and therefore might be misleading in some situations.");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The value of $\\beta$ determines the relative weight given to false positives and false negatives. Values &gt; 1 penalise false negatives more than false positives. Values &lt; 1 penalise false positives more than false negatives.");
            markdownDocument.AppendLine();

            bool plotFBeta = false;

            if (truePositives == 0 && falsePositives == 0)
            {
                markdownDocument.AppendLine("In this case, $F_{\\beta} = 0$ for all values of $\\beta$, because $TP = 0$ and $FP = 0$. This means that the model rejects all the test data columns.");
            }
            else if (falseNegatives == 0 && falsePositives == 0)
            {
                markdownDocument.AppendLine("In this case, $F_{\\beta} = 1$ for all values of $\\beta$, because $FN = 0$ and $FP = 0$. This means that the model results perfectly align with the test data.");
            }
            else
            {
                plotFBeta = true;
                markdownDocument.AppendLine("The $F_{\\beta}$ score curve (**Fig. 6**) has a sigmoid shape, ascending if the number of false positives is greater than the number of false negatives, and descending if the number of false negatives is greater than the number of false positives.");
                markdownDocument.AppendLine("The asymptotes of this curve are " + ((double)truePositives / (truePositives + falseNegatives)).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + " (for $\\beta \\rightarrow + \\infty$) and " + ((double)truePositives / (truePositives + falsePositives)).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + " (for $\\beta \\rightarrow 0$).");
            }

            markdownDocument.AppendLine();

            #region Figure 6. F_beta curve.
            if (plotFBeta)
            {
                double[][] data = new double[101][];
                double minY = double.MaxValue;
                double maxY = double.MinValue;

                for (int i = 0; i < data.Length; i++)
                {
                    double beta = Math.Pow(10, (i - 50) * 0.02);
                    double fBeta = (1 + beta * beta) * thresholdPrecision * thresholdTpr / (beta * beta * thresholdPrecision + thresholdTpr);

                    minY = Math.Min(fBeta, minY);
                    maxY = Math.Max(fBeta, maxY);

                    data[i] = new double[] { beta, fBeta };
                }

                Plot fBetaCurvePlot = Plot.Create.LineChart(data, width: 500, linePresentationAttributes: new PlotElementPresentationAttributes() { LineWidth = 2, LineJoin = LineJoins.Round }, xAxisTitle: "β", yAxisTitle: "F<sub>β</sub>", coordinateSystem: new LinLogCoordinateSystem2D(0.1, 10, minY, maxY, 500, 225));

                double ticksY = fBetaCurvePlot.GetFirst<ContinuousAxisTicks>().StartPoint[1];

                fBetaCurvePlot.RemovePlotElement(fBetaCurvePlot.GetFirst<ContinuousAxisTicks>());
                fBetaCurvePlot.RemovePlotElement(fBetaCurvePlot.GetFirst<ContinuousAxisLabels>());
                fBetaCurvePlot.AddPlotElement(new DataLabels<IReadOnlyList<double>>(new double[] { 1, 2, 3, 5, 7 }.Select(x => new double[] { x * 0.1, ticksY }).Concat(new double[] { 1, 2, 3, 5, 7, 10 }.Select(x => new double[] { x, ticksY })), fBetaCurvePlot.GetFirst<IContinuousCoordinateSystem>()) { Margin = (_, _) => new Point(0, 5), Label = (_, x) => x[0].ToString("0.#", System.Globalization.CultureInfo.InvariantCulture), Baseline = TextBaselines.Top });

                fBetaCurvePlot.AddPlotElement(new ScatterPoints<IReadOnlyList<double>>(Enumerable.Range(1, 9).Select(x => new double[] { x * 0.1, ticksY }).Concat(Enumerable.Range(1, 10).Select(x => new double[] { x, ticksY })), fBetaCurvePlot.GetFirst<IContinuousCoordinateSystem>()) { DataPointElement = new PathDataPointElement(new GraphicsPath().MoveTo(0, -1.5).LineTo(0, 1.5)), PresentationAttributes = new PlotElementPresentationAttributes() { Fill = null, Stroke = Colours.Black, LineWidth = 0.5 } });

                PlotUtilities.InsertFigure(markdownDocument, fBetaCurvePlot.Render(), "align=\"center\"");

                Page blackBox = new Page(15, 6);
                blackBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(0, 0, 0));

                Page orangeBox = new Page(15, 6);
                orangeBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(213, 94, 0));

                Page dashedBox = new Page(15, 6);
                dashedBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(180, 180, 180), lineDash: new LineDash(3, 3, 0));

                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.Append("**Figure 6. $F_{\\beta}$ score curve.** The curve shows the value of $F_{\\beta}$ as a function of $\\beta$.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("&nbsp;");
                markdownDocument.AppendLine();
            }
            #endregion

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("&nbsp;");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("#### Receiver operating characteristic");
            markdownDocument.AppendLine("All the metrics described above are computed based on the $TP$, $TN$, $FP$, and $FN$ values, and therefore they depend on the chosen threshold value. However, it might be interesting to analyse the model's performance independently of the threshold.");
            markdownDocument.AppendLine("For example, if the model performs poorly on the test dataset, a possible explanation would be that an inappropriate threshold is being used (which may indicate that the validation set is too small); however, if there is no threshold value that would improve the model's performance, this would indicate that the training set is too small (or that the features used by AliFilter are inadequate for the task).");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("This can be achieved by analysing the **receiver operarating characteristic** (ROC) curve (**Fig. " + (plotFBeta ? "7" : "6") + "**), which represents the $TPR$ and the corresponding $FPR$ for all threshold values.");
            markdownDocument.AppendLine("For a good model, the ROC curve should be close to the upper left corner of the plot. This is summarised by the **area under the curve** ($AUC$), which in this case is " + auc.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + " (for a good model, this should be close to 1, while a random classifier would have $AUC$ close to 0.5).");
            markdownDocument.AppendLine();

            #region Figure 7. ROC curve.
            {
                Plot rocCurvePlot = Plot.Create.LineCharts(new double[][] { new double[] { 0, 0 }, new double[] { 1, 1 } }, rocCurve.OrderBy(x => x.fpr).Select(x => new double[] { x.fpr, x.tpr }).ToArray(), width: 500, height: 225, line1PresentationAttributes: new PlotElementPresentationAttributes() { LineDash = new LineDash(5, 5, 0), Stroke = Colour.FromRgb(180, 180, 180) }, line2PresentationAttributes: new PlotElementPresentationAttributes() { LineWidth = 2, LineJoin = LineJoins.Round }, xAxisTitle: "False positive rate", yAxisTitle: "True positive rate");

                rocCurvePlot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(rocCurvePlot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coords) =>
                {
                    DataLine<IReadOnlyList<double>> dataLine = rocCurvePlot.GetAll<VectSharp.Plots.DataLine<IReadOnlyList<double>>>().ElementAt(1);
                    dataLine.PresentationAttributes.Stroke = Colour.FromRgb(213, 94, 0);
                    dataLine.PresentationAttributes.LineWidth += 0.1;

                    Point pt = coords.ToPlotCoordinates(new double[] { thresholdFpr, thresholdTpr });

                    gpr.Save();
                    gpr.SetClippingPath(new GraphicsPath().Arc(pt, 4, 0, 2 * Math.PI));
                    dataLine.Plot(gpr);
                    gpr.Restore();
                }));

                PlotUtilities.InsertFigure(markdownDocument, rocCurvePlot.Render(), "align=\"center\"");

                Page blackBox = new Page(15, 6);
                blackBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(0, 0, 0));

                Page orangeBox = new Page(15, 6);
                orangeBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(213, 94, 0));

                Page dashedBox = new Page(15, 6);
                dashedBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(180, 180, 180), lineDash: new LineDash(3, 3, 0));

                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.Append("**Figure " + (plotFBeta ? "7" : "6") + ". ROC (receiver operating characteristic) curve.** The ");
                PlotUtilities.InsertFigure(markdownDocument, blackBox, "");
                markdownDocument.Append(" black line shows the ROC curve of the final model. The ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.AppendLine(" orange part of the line represents the $\\mathrm{TPR}$ and $\\mathrm{FPR}$ corresponding to the " + threshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " threshold used for the model. The");
                PlotUtilities.InsertFigure(markdownDocument, dashedBox, "");
                markdownDocument.AppendLine(" dashed line represents the performance of a random classifier.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("&nbsp;");
                markdownDocument.AppendLine();
            }
            #endregion

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("#### Model confidence");
            markdownDocument.AppendLine("When classifying the alignment columns, the model assigns a score ranging between 0 and 1 to each column. For columns confidently deleted by the model, this score is close to 0; instead, for columns confidently preserved by the model, the score is close to 1. The overall confidence of the model in its assignments can be summarised using the model confidence score $C$, which is defined as:");

            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine(@"C = 1 - \frac{4}{n} \sum_{i = 1}^{n} s_i \cdot (1 - s_i)");
            markdownDocument.AppendLine("$$");

            double modelConfidence = (1 - predictedMask.Confidence.Select(x => x * (1 - x) * 4).Sum() / predictedMask.Length);

            markdownDocument.AppendLine("Where $n$ is the number of columns in the alignment and $s_i$ is the confidence score for column $i$. For a model performing confident assignments, this score should be close to 1. In this case, it is " + modelConfidence.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + ". Note that this score does not necessarily assess whether the model is “good” or “bad”, but just how confident it is.");

            markdownDocument.AppendLine("#### Summary");

            markdownDocument.AppendLine("The performance metric values for the final model are summarised in the table below.");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("| Performance metric | Symbol | Value |");
            markdownDocument.AppendLine("| --- | --- | --- |");
            markdownDocument.AppendLine("| Accuracy | $A$ | " + Utilities.ComputeAccuracy(truePositives, trueNegatives, falsePositives, falseNegatives).ToString(System.Globalization.CultureInfo.InvariantCulture) + "|");
            markdownDocument.AppendLine("| Matthews correlation coefficient | $MCC$ | " + Utilities.ComputeMCC(truePositives, trueNegatives, falsePositives, falseNegatives).ToString(System.Globalization.CultureInfo.InvariantCulture) + "|");
            markdownDocument.AppendLine("| True positive rate | $TPR$ | " + tpr.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|");
            markdownDocument.AppendLine("| Positive predictive value | $PPV$ | " + ppv.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|");
            markdownDocument.AppendLine("| False positive rate | $FPR$ | " + fpr.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|");
            markdownDocument.AppendLine("| $F_\\beta$ score | $F_{0.5}$ | " + f05.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|");
            markdownDocument.AppendLine("| $F_\\beta$ score | $F_{1}$ | " + f1.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|");
            markdownDocument.AppendLine("| $F_\\beta$ score | $F_{2}$ | " + f2.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|");
            markdownDocument.AppendLine("| Area under the ROC curve | $AUC$ | " + auc.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|");
            markdownDocument.AppendLine("| Model confidence | $C$ | " + modelConfidence.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|");
            markdownDocument.AppendLine();

            StringBuilder annotatedSummaryTable = new StringBuilder();

            if (outputMd)
            {
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("A plaintext tab-separated version of this table is included below. Each line is prefixed with `@metric`, so that the table can be easily extracted from the report.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("@metricMetric\tValue");
                markdownDocument.AppendLine("@metricTP\t" + truePositives.ToString());
                markdownDocument.AppendLine("@metricTN\t" + trueNegatives.ToString());
                markdownDocument.AppendLine("@metricFP\t" + falsePositives.ToString());
                markdownDocument.AppendLine("@metricFN\t" + falseNegatives.ToString());
                markdownDocument.AppendLine("@metricA\t" + Utilities.ComputeAccuracy(truePositives, trueNegatives, falsePositives, falseNegatives).ToString(System.Globalization.CultureInfo.InvariantCulture));
                markdownDocument.AppendLine("@metricMCC\t" + Utilities.ComputeMCC(truePositives, trueNegatives, falsePositives, falseNegatives).ToString(System.Globalization.CultureInfo.InvariantCulture));
                markdownDocument.AppendLine("@metricTPR\t" + tpr.ToString(System.Globalization.CultureInfo.InvariantCulture));
                markdownDocument.AppendLine("@metricPPV\t" + ppv.ToString(System.Globalization.CultureInfo.InvariantCulture));
                markdownDocument.AppendLine("@metricFPR\t" + fpr.ToString(System.Globalization.CultureInfo.InvariantCulture));
                markdownDocument.AppendLine("@metricF_0.5\t" + f05.ToString(System.Globalization.CultureInfo.InvariantCulture));
                markdownDocument.AppendLine("@metricF_1\t" + f1.ToString(System.Globalization.CultureInfo.InvariantCulture));
                markdownDocument.AppendLine("@metricF_2\t" + f2.ToString(System.Globalization.CultureInfo.InvariantCulture));
                markdownDocument.AppendLine("@metricAUC\t" + auc.ToString(System.Globalization.CultureInfo.InvariantCulture));
                markdownDocument.AppendLine("@metricC\t" + modelConfidence.ToString(System.Globalization.CultureInfo.InvariantCulture));
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
            }
            else
            {
                annotatedSummaryTable.AppendLine("@metricMetric\tValue");
                annotatedSummaryTable.AppendLine("@metricTP\t" + truePositives.ToString());
                annotatedSummaryTable.AppendLine("@metricTN\t" + trueNegatives.ToString());
                annotatedSummaryTable.AppendLine("@metricFP\t" + falsePositives.ToString());
                annotatedSummaryTable.AppendLine("@metricFN\t" + falseNegatives.ToString());
                annotatedSummaryTable.AppendLine("@metricA\t" + Utilities.ComputeAccuracy(truePositives, trueNegatives, falsePositives, falseNegatives).ToString(System.Globalization.CultureInfo.InvariantCulture));
                annotatedSummaryTable.AppendLine("@metricMCC\t" + Utilities.ComputeMCC(truePositives, trueNegatives, falsePositives, falseNegatives).ToString(System.Globalization.CultureInfo.InvariantCulture));
                annotatedSummaryTable.AppendLine("@metricTPR\t" + tpr.ToString(System.Globalization.CultureInfo.InvariantCulture));
                annotatedSummaryTable.AppendLine("@metricPPV\t" + ppv.ToString(System.Globalization.CultureInfo.InvariantCulture));
                annotatedSummaryTable.AppendLine("@metricFPR\t" + fpr.ToString(System.Globalization.CultureInfo.InvariantCulture));
                annotatedSummaryTable.AppendLine("@metricF_0.5\t" + f05.ToString(System.Globalization.CultureInfo.InvariantCulture));
                annotatedSummaryTable.AppendLine("@metricF_1\t" + f1.ToString(System.Globalization.CultureInfo.InvariantCulture));
                annotatedSummaryTable.AppendLine("@metricF_2\t" + f2.ToString(System.Globalization.CultureInfo.InvariantCulture));
                annotatedSummaryTable.AppendLine("@metricAUC\t" + auc.ToString(System.Globalization.CultureInfo.InvariantCulture));
                annotatedSummaryTable.AppendLine("@metricC\t" + modelConfidence.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("<br type=\"page\"/>");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("### Feature effects");

            double[] scores = predictedMask.Confidence.Select((x, i) => predictedMask.MaskedStates[i] ? x : (1 - x)).ToArray();
            
            markdownDocument.AppendLine("To assess the effect of each alignment feature in determining whether a column should be deleted or preserved, the observed values for each feature were plotted against the preservation score of columns presenting that value (**Fig. " + ((thresholdTpr < 1 || thresholdPrecision < 1) && !double.IsNaN(thresholdPrecision) ? "8" : "7") + "**). To further assess the effect of individual features, additional datapoints were simulated, by considering 100 different values for each feature (ranging from the observed minimum to the observed maximum), and then randomly sampling 100 sets of values for the other features (**Fig " + ((thresholdTpr < 1 || thresholdPrecision < 1) && !double.IsNaN(thresholdPrecision) ? "9" : "8") + "**).");
            markdownDocument.AppendLine();

            if (!((thresholdTpr < 1 || thresholdPrecision < 1) && !double.IsNaN(thresholdPrecision)))
            {
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("<br type=\"page\">");
                markdownDocument.AppendLine();
            }

            #region Figure 8. Observed feature effects.
            Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 30);

            List<Plot> descriptivePlots = new List<Plot>();
            List<Plot> empiricalPlots = new List<Plot>();

            double[][] featDistributions = new double[arguments.Features.Count][];

            // Create the plots describing the preservation score of alignment columns in function the observed values.
            for (int i = 0; i < arguments.Features.Count; i++)
            {
                int steps = 100;

                List<double>[] valuesByBin = new List<double>[steps + 1];

                for (int j = 0; j < valuesByBin.Length; j++)
                {
                    valuesByBin[j] = new List<double>();
                }

                for (int j = 0; j < features.Length; j++)
                {
                    int bin = (int)Math.Floor((features[j][i] - featureMins[i]) / (featureMaxs[i] - featureMins[i]) * steps);
                    bin = Math.Min(steps, Math.Max(bin, 0));
                    valuesByBin[bin].Add(scores[j]);
                }

                featDistributions[i] = (from el in valuesByBin select (double)el.Count).ToArray();

                double[] lowers = new double[valuesByBin.Length];
                double[] means = new double[valuesByBin.Length];
                double[] uppers = new double[valuesByBin.Length];

                for (int j = 0; j < valuesByBin.Length; j++)
                {
                    if (valuesByBin[j].Count > 0)
                    {
                        (means[j], double stdDev) = valuesByBin[j].MeanStandardDeviation();

                        if (valuesByBin[j].Count == 1)
                        {
                            stdDev = 0;
                        }

                        lowers[j] = Math.Max(means[j] - stdDev, 0);
                        uppers[j] = Math.Min(means[j] + stdDev, 1);
                    }
                    else
                    {
                        means[j] = means[j - 1];
                        lowers[j] = lowers[j - 1];
                        uppers[j] = uppers[j - 1];
                    }
                }

                double[][] stackedAreaChart = new double[valuesByBin.Length][];
                double[][] mediansWithX = new double[valuesByBin.Length][];

                for (int j = 0; j < valuesByBin.Length; j++)
                {
                    stackedAreaChart[j] = new double[] { featureMins[i] + (featureMaxs[i] - featureMins[i]) / steps * j, lowers[j], uppers[j] };
                    mediansWithX[j] = new double[] { featureMins[i] + (featureMaxs[i] - featureMins[i]) / steps * j, means[j] };
                }

                // Create the plot.
                Plot lineChart = Plot.Create.StackedAreaChart(stackedAreaChart, title: arguments.Features[i].Name, xAxisTitle: "Value", yAxisTitle: "Preservation score", dataPresentationAttributes: new PlotElementPresentationAttributes[] {
                                new PlotElementPresentationAttributes(){ Fill = null, Stroke = Colour.FromRgba(0, 0, 0,0) },
                                new PlotElementPresentationAttributes() { Fill =Colour.FromRgb(220, 220, 220), Stroke = Colour.FromRgba(0, 0,0,0)}
                            });

                lineChart.AddPlotElement(new DataLine<IReadOnlyList<double>>(mediansWithX, lineChart.GetFirst<ICoordinateSystem<IReadOnlyList<double>>>()));

                if (lineChart.GetFirst<LinearCoordinateSystem2D>().MinY <= threshold && lineChart.GetFirst<LinearCoordinateSystem2D>().MaxY >= threshold)
                {
                    lineChart.AddPlotElement(new LinearTrendLine(0, threshold, featureMins[i], 0, featureMaxs[i], 1, lineChart.GetFirst<IContinuousCoordinateSystem>()));
                }

                descriptivePlots.Add(lineChart);
            }

            for (int i = 0; i < arguments.Features.Count; i += 2)
            {
                Page fullPlot = new Page(1, 1);
                fullPlot.Graphics.DrawGraphics(0, 0, descriptivePlots[i].Render().Graphics);
                fullPlot.Graphics.FillText(0, 0, (char)(97 + i) + ")", new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16), Colours.Black);

                if (i < arguments.Features.Count - 1)
                {
                    fullPlot.Graphics.DrawGraphics(450, 0, descriptivePlots[i + 1].Render().Graphics);
                    fullPlot.Graphics.FillText(450, 0, (char)(97 + i + 1) + ")", new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16), Colours.Black);
                }

                fullPlot.Crop();

                PlotUtilities.InsertFigure(markdownDocument, fullPlot, "align=\"center\"");

                markdownDocument.AppendLine();
            }

            #endregion

            markdownDocument.AppendLine();
            markdownDocument.AppendLine();
            markdownDocument.Append("**Figure " + ((thresholdTpr < 1 || thresholdPrecision < 1) && !double.IsNaN(thresholdPrecision) ? "8" : "7") + ". Effect of observed alignment feature values.** Each plot shows the preservation score of the input data columns in function of the observed value for each statistic. The black lines represent the mean values, while the grey backgrounds represent the standard deviation. The ");

            {
                Page dashedBox = new Page(15, 6);
                dashedBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(180, 180, 180), lineDash: new LineDash(3, 3, 0));
                PlotUtilities.InsertFigure(markdownDocument, dashedBox, "");
            }

            markdownDocument.AppendLine(" dashed line represents the " + threshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " threshold that determines whether a column is preserved or deleted according to the model.");

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("&nbsp;");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("<br type=\"page\">");

            #region Figure 9. Simulated feature effects.

            for (int i = 0; i < arguments.Features.Count; i++)
            {
                int steps = 100;
                int trials = 100;

                double[][] valuesByBin = new double[steps + 1][];

                for (int j = 0; j < steps + 1; j++)
                {
                    double minVal = featureMins[i] + (featureMaxs[i] - featureMins[i]) / steps * j;
                    double maxVal = featureMaxs[i] + (featureMaxs[i] - featureMins[i]) / steps * (j + 1);

                    double[][] data = new double[trials][];

                    for (int k = 0; k < trials; k++)
                    {
                        data[k] = new double[arguments.Features.Count];

                        for (int l = 0; l < arguments.Features.Count; l++)
                        {
                            if (l == i)
                            {
                                data[k][l] = ContinuousUniform.Sample(minVal, maxVal);
                            }
                            else
                            {
                                int binIndex = Categorical.Sample(featDistributions[l]);

                                data[k][l] = Math.Min(ContinuousUniform.Sample(featureMins[l] + (featureMaxs[l] - featureMins[l]) / steps * binIndex, featureMins[l] + (featureMaxs[l] - featureMins[l]) / steps * (binIndex + 1)), featureMaxs[l]);
                            }
                        }
                    }

                    Mask currMask = model.Classify(data, arguments.MaxParallelism);

                    valuesByBin[j] = currMask.Confidence.Select((x, i) => currMask.MaskedStates[i] ? x : (1 - x)).ToArray();
                }

                double[] lowers = new double[valuesByBin.Length];
                double[] means = new double[valuesByBin.Length];
                double[] uppers = new double[valuesByBin.Length];

                for (int j = 0; j < valuesByBin.Length; j++)
                {
                    if (valuesByBin[j].Length > 0)
                    {
                        (means[j], double stdDev) = valuesByBin[j].MeanStandardDeviation();

                        if (valuesByBin[j].Length == 1)
                        {
                            stdDev = 0;
                        }

                        lowers[j] = Math.Max(means[j] - stdDev, 0);
                        uppers[j] = Math.Min(means[j] + stdDev, 1);
                    }
                    else
                    {
                        means[j] = means[j - 1];
                        lowers[j] = lowers[j - 1];
                        uppers[j] = uppers[j - 1];
                    }
                }

                double[][] stackedAreaChart = new double[valuesByBin.Length][];
                double[][] mediansWithX = new double[valuesByBin.Length][];
                double[][] topWithX = new double[valuesByBin.Length][];
                double[][] bottomWithX = new double[valuesByBin.Length][];

                for (int j = 0; j < valuesByBin.Length; j++)
                {
                    stackedAreaChart[j] = new double[] { featureMins[i] + (featureMaxs[i] - featureMins[i]) / steps * j, lowers[j], uppers[j] };
                    topWithX[j] = new double[] { stackedAreaChart[j][0], stackedAreaChart[j][2] };
                    bottomWithX[j] = new double[] { stackedAreaChart[j][0], stackedAreaChart[j][1] };
                    mediansWithX[j] = new double[] { featureMins[i] + (featureMaxs[i] - featureMins[i]) / steps * j, means[j] };
                }

                // Create the plot.
                {
                    Plot lineChart = Plot.Create.StackedAreaChart(stackedAreaChart, title: arguments.Features[i].Name, xAxisTitle: "Value", yAxisTitle: "Preservation score", dataPresentationAttributes: new PlotElementPresentationAttributes[] {
                                new PlotElementPresentationAttributes(){ Fill = null, Stroke = Colour.FromRgba(0, 0, 0,0) },
                                new PlotElementPresentationAttributes() { Fill = Colour.FromRgb(220, 220, 220), Stroke = Colour.FromRgba(0, 0,0,0)}
                            });

                    lineChart.RemovePlotElement(lineChart.GetFirst<Area<IReadOnlyList<double>>>());
                    lineChart.RemovePlotElement(lineChart.GetFirst<Area<IReadOnlyList<double>>>());

                    double[][] transformedMedian = (from el in mediansWithX where el[1] > 0 && el[1] < 1 select new double[] { el[0], Math.Log(1 / el[1] - 1) }).ToArray();

                    Func<double, double> medianFunc;

                    if (transformedMedian.Length > 10)
                    {
                        LinearTrendLine linearRegression = new LinearTrendLine(transformedMedian, lineChart.GetFirst<IContinuousCoordinateSystem>());

                        double k = -linearRegression.Slope;
                        double x0 = linearRegression.Intercept / k;

                        medianFunc = x => 1 / (1 + Math.Exp(-k * (x - x0)));
                    }
                    else
                    {
                        LinearTrendLine linearRegression = new LinearTrendLine(mediansWithX, lineChart.GetFirst<IContinuousCoordinateSystem>());
                        medianFunc = x => linearRegression.Slope * x + linearRegression.Intercept;
                    }

                    double[][] transformedBottom = (from el in stackedAreaChart where el[1] > 0 && el[1] < 1 select new double[] { el[0], Math.Log(1 / el[1] - 1) }).ToArray();

                    Func<double, double> bottomFunc;

                    if (transformedBottom.Length > 10)
                    {
                        LinearTrendLine linearRegression = new LinearTrendLine(transformedBottom, lineChart.GetFirst<IContinuousCoordinateSystem>());

                        double k = -linearRegression.Slope;
                        double x0 = linearRegression.Intercept / k;

                        bottomFunc = x => 1 / (1 + Math.Exp(-k * (x - x0)));
                    }
                    else
                    {
                        LinearTrendLine linearRegression = new LinearTrendLine(stackedAreaChart, lineChart.GetFirst<IContinuousCoordinateSystem>());
                        bottomFunc = x => linearRegression.Slope * x + linearRegression.Intercept;
                    }


                    double[][] transformedTop = (from el in stackedAreaChart where el[2] > 0 && el[2] < 1 select new double[] { el[0], Math.Log(1 / el[2] - 1) }).ToArray();

                    Func<double, double> topFunc;

                    if (transformedTop.Length > 10)
                    {
                        LinearTrendLine linearRegression = new LinearTrendLine(transformedTop, lineChart.GetFirst<IContinuousCoordinateSystem>());

                        double k = -linearRegression.Slope;
                        double x0 = linearRegression.Intercept / k;

                        topFunc = x => 1 / (1 + Math.Exp(-k * (x - x0)));
                    }
                    else
                    {
                        LinearTrendLine linearRegression = new LinearTrendLine((from el in stackedAreaChart select new double[] { el[0], el[2] }).ToArray(), lineChart.GetFirst<IContinuousCoordinateSystem>());
                        topFunc = x => linearRegression.Slope * x + linearRegression.Intercept;
                    }

                    lineChart.AddPlotElement(new PlotElement<IReadOnlyList<double>>(lineChart.GetFirst<IContinuousCoordinateSystem>(), (gpr, coordinateSystem) =>
                    {
                        LinearCoordinateSystem2D coords = (LinearCoordinateSystem2D)coordinateSystem;

                        Point minPoint = coords.ToPlotCoordinates(new double[] { coords.MinX, coords.MinY });
                        Point maxPoint = coords.ToPlotCoordinates(new double[] { coords.MaxX, coords.MaxY });

                        gpr.Save();
                        gpr.SetClippingPath(minPoint.X, minPoint.Y, maxPoint.X - minPoint.X, maxPoint.Y - minPoint.Y);

                        GraphicsPath top = new GraphicsPath();

                        for (int i = 0; i < 1001; i++)
                        {
                            double x = mediansWithX[0][0] + (mediansWithX[^1][0] - mediansWithX[0][0]) * 0.001 * i;
                            double yTop = topFunc(x);
                            top.LineTo(coordinateSystem.ToPlotCoordinates(new double[] { x, yTop }));
                        }

                        for (int i = 0; i < 1001; i++)
                        {
                            double xBottom = mediansWithX[0][0] + (mediansWithX[^1][0] - mediansWithX[0][0]) * (1 - 0.001 * i);
                            double yBottom = bottomFunc(xBottom);

                            top.LineTo(coordinateSystem.ToPlotCoordinates(new double[] { xBottom, yBottom }));
                        }

                        gpr.FillPath(top, Colour.FromRgb(220, 220, 220));
                        gpr.Restore();
                    }));

                    lineChart.AddPlotElement(new DataLine<IReadOnlyList<double>>(topWithX, lineChart.GetFirst<ICoordinateSystem<IReadOnlyList<double>>>()) { PresentationAttributes = new PlotElementPresentationAttributes() { Stroke = Colour.FromRgb(200, 200, 200) } });
                    lineChart.AddPlotElement(new DataLine<IReadOnlyList<double>>(bottomWithX, lineChart.GetFirst<ICoordinateSystem<IReadOnlyList<double>>>()) { PresentationAttributes = new PlotElementPresentationAttributes() { Stroke = Colour.FromRgb(200, 200, 200) } });

                    lineChart.AddPlotElement(new DataLine<IReadOnlyList<double>>(mediansWithX, lineChart.GetFirst<ICoordinateSystem<IReadOnlyList<double>>>()) { PresentationAttributes = new PlotElementPresentationAttributes() { Stroke = Colour.FromRgb(160, 160, 160) } });

                    lineChart.AddPlotElement(new PlotElement<IReadOnlyList<double>>(lineChart.GetFirst<IContinuousCoordinateSystem>(), (gpr, coordinateSystem) =>
                    {
                        LinearCoordinateSystem2D coords = (LinearCoordinateSystem2D)coordinateSystem;

                        Point minPoint = coords.ToPlotCoordinates(new double[] { coords.MinX, coords.MinY });
                        Point maxPoint = coords.ToPlotCoordinates(new double[] { coords.MaxX, coords.MaxY });

                        gpr.Save();
                        gpr.SetClippingPath(minPoint.X, minPoint.Y, maxPoint.X - minPoint.X, maxPoint.Y - minPoint.Y);

                        GraphicsPath median = new GraphicsPath();

                        for (int i = 0; i < 1001; i++)
                        {
                            double x = mediansWithX[0][0] + (mediansWithX[^1][0] - mediansWithX[0][0]) * 0.001 * i;
                            double yMedian = medianFunc(x);
                            median.LineTo(coordinateSystem.ToPlotCoordinates(new double[] { x, yMedian }));
                        }

                        gpr.StrokePath(median, Colours.Black);
                        gpr.Restore();
                    }));

                    int ind = i;

                    empiricalPlots.Add(lineChart);
                }
            }


            for (int i = 0; i < arguments.Features.Count; i += 2)
            {
                Page fullPlot = new Page(1, 1);
                fullPlot.Graphics.DrawGraphics(0, 0, empiricalPlots[i].Render().Graphics);
                fullPlot.Graphics.FillText(0, 0, (char)(97 + i) + ")", new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16), Colours.Black);

                if (i < arguments.Features.Count - 1)
                {
                    fullPlot.Graphics.DrawGraphics(450, 0, empiricalPlots[i + 1].Render().Graphics);
                    fullPlot.Graphics.FillText(450, 0, (char)(97 + i + 1) + ")", new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16), Colours.Black);
                }

                fullPlot.Crop();

                PlotUtilities.InsertFigure(markdownDocument, fullPlot, "align=\"center\"");

                markdownDocument.AppendLine();
            }

            #endregion

            markdownDocument.AppendLine();
            markdownDocument.AppendLine();
            markdownDocument.Append("**Figure " + ((thresholdTpr < 1 || thresholdPrecision < 1) && !double.IsNaN(thresholdPrecision) ? "9" : "8") + ". Effect of simulated alignment feature values.** Each plot shows the preservation score of a simulated data column as a function of the sampled values for each statistic. In each plot, the black line represents the mean values, while the grey background represents the standard deviation. To produce these plots, the value for the “focal” feature was fixed along a the range of observed values. Then, random values were sampled for each feature, according to its observed distribution. The logistic model was then used to determine the preservation score for each combination of random feature values. Finally, logistic curves were used to interpolate the relationship between the preservation score and the feature values. The grey lines show the computed values, while the black line and the grey background show the interpolated values.");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("&nbsp;");
            markdownDocument.AppendLine();

            StringBuilder annotatedModel = new StringBuilder();
            using (StringReader sr = new StringReader(JsonSerializer.Serialize(model, ModelSerializerContext.Default.ValidatedModel)))
            {
                string line = sr.ReadLine();
                while (line != null)
                {
                    annotatedModel.AppendLine("@model" + line);
                    line = sr.ReadLine();
                }
            }

            if (outputMd)
            {
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("The raw JSON model is included below. Each line is prefixed with `@model`, so that the model can be easily extracted from the report.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("```");
                markdownDocument.Append(annotatedModel.ToString());
                markdownDocument.AppendLine("```");
                markdownDocument.AppendLine();
            }

            string text = markdownDocument.ToString();

            renderer.HeaderLineThicknesses[0] = 0;
            renderer.HeaderFontSizeMultipliers[0] *= 2;

            bool renderingTitle = false;

            void blockRendering(object sender, BlockRenderingEventArgs e)
            {
                if (e.Block is HeadingBlock heading && heading.Level == 1)
                {
                    renderingTitle = true;
                }
            }

            void lineRendering(object sender, LineEventArgs e)
            {
                if (renderingTitle)
                {
                    e.Graphics.Translate((e.PageMaxX - e.ContentMaxX) * 0.5, 0);
                }
            }

            void lineRendered(object sender, LineEventArgs e)
            {
                if (renderingTitle)
                {
                    e.Graphics.Translate(-(e.PageMaxX - e.ContentMaxX) * 0.5, 0);
                }
            }

            void blockRendered(object sender, BlockRenderedEventArgs e)
            {
                if (renderingTitle)
                {
                    renderingTitle = false;
                    renderer.BlockRendering -= blockRendering;
                    renderer.LineRendering -= lineRendering;
                    renderer.LineRendered -= lineRendered;
                    renderer.BlockRendered -= blockRendered;
                }
            }

            renderer.BlockRendering += blockRendering;
            renderer.LineRendering += lineRendering;
            renderer.LineRendered += lineRendered;
            renderer.BlockRendered += blockRendered;

            if (!outputMd)
            {
                outputLog?.WriteLine("Rendering Markdown report to PDF...");
                Document doc = renderer.Render(markdownDocument.ToString(), out Dictionary<string, string> linkDestinations, out List<(int, string, string)> headingTree);

                PDFMetadata metadata = new PDFMetadata()
                {
                    CreationDate = creationTime,
                    ModificationDate = creationTime,
                    Creator = "AliFilter v" + Program.Version,
                    Title = "Model test report",
                    Subject = "Test report for an AliFilter model",
                    Keywords = "AliFilter, Model, Test, Report",
                    CreationDateTimeZone = TimeZoneInfo.Local,
                    ModificationDateTimeZone = TimeZoneInfo.Local
                };

                PDFDocument pdfDoc = doc.CreatePDFDocument(linkDestinations: linkDestinations, outline: OutlineTree.CreateFromHeadings(headingTree), metadata: metadata);

                using (MemoryStream modelStream = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(modelStream, leaveOpen: true))
                    {
                        sw.Write(annotatedModel.ToString());
                        sw.Write(annotatedSummaryTable.ToString());
                    }

                    modelStream.Seek(0, SeekOrigin.Begin);

                    pdfDoc.Contents.Add(new PDFStream(modelStream, false));
                    outputLog?.WriteLine("Saving PDF to " + Path.GetFullPath(arguments.ReportFile) + "...");
                    pdfDoc.Write(arguments.ReportFile);
                }
            }
            else
            {
                outputLog?.WriteLine("Saving Markdown report to " + Path.GetFullPath(arguments.ReportFile) + "...");
                File.WriteAllText(arguments.ReportFile, text);
            }

            return 0;
        }
    }
}
