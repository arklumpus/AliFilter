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
using VectSharp.SVG;
using Markdig.Syntax;
using VectSharp.PDF.PDFObjects;

namespace AliFilter
{
    internal static partial class Utilities
    {
        // Create the model validation report.
        internal static int CreateModelValidationReport(double[][] features, Mask mask, FullModel model, OptimisationTarget target, double beta, double effortPenalty, double[][][] fScores, (double maxTargetScore, double threshold, double bootstrapThreshold, double finalScore)[] maximumScoresByBootstrapReplicates, int bestScoreIndex, int bestFScoreIndex, Arguments arguments, TextWriter outputLog)
        {
            bool outputMd = Path.GetExtension(arguments.ReportFile) == ".md";

            outputLog?.WriteLine();
            outputLog?.WriteLine("Creating model validation report...");

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

            // Number of preserved columns in the test dataset.
            int trues = mask.MaskedStates.Count(x => x);

            #region Compute the distribution of the alignment features.
            double[] featureMins = new double[arguments.Features.Count];
            double[] featureMaxs = new double[arguments.Features.Count];
            double[] featureMeans = new double[arguments.Features.Count];
            double[] featureStdDevs = new double[arguments.Features.Count];

            for (int i = 0; i < arguments.Features.Count; i++)
            {
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

            markdownDocument.AppendLine("# Model validation report");

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

            markdownDocument.AppendLine("This report is machine-readable. If the report file is called `report.pdf`, you can export the validated model to a file called `model.json` by running:");
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

            markdownDocument.AppendLine("<br type=\"page\" />");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("## Validation data analysis");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The model was validated using data from " + features.Length.ToString() + " alignment columns, of which " + trues.ToString() + " (" + ((double)trues / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture) + ") were preserved and " + (mask.Length - trues).ToString() + " (" + (1 - (double)trues / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture) + ") were deleted (**Fig. 1**). For each alignment column, " + arguments.Features.Count.ToString() + " features were computed (**Table 1**), which were analysed in a Principal Component Analysis (PCA) and in a Linear Discriminant Analysis (LDA).");
            markdownDocument.AppendLine();
            #endregion

            #region Figure 1. Proportion of preserved columns.
            {
                double figureWidth = 200;

                Page proportionFigure = new Page(1, 1);
                Graphics gpr = proportionFigure.Graphics;

                gpr.FillRectangle(0, 0, figureWidth * ((double)trues / mask.Length), 16, Colour.FromRgb(213, 94, 0));
                gpr.FillRectangle(figureWidth * ((double)trues / mask.Length) + 3, 0, figureWidth * (1 - (double)trues / mask.Length), 16, Colour.FromRgb(0, 114, 178));

                gpr.StrokePath(new GraphicsPath().MoveTo(0, -3).LineTo(0, -8).LineTo(figureWidth + 3, -8).LineTo(figureWidth + 3, -3), Colours.Black);

                gpr.StrokePath(new GraphicsPath().MoveTo(0, 19).LineTo(0, 24).LineTo(figureWidth * ((double)trues / mask.Length), 24).LineTo(figureWidth * ((double)trues / mask.Length), 19), Colours.Black);
                gpr.StrokePath(new GraphicsPath().MoveTo(figureWidth * ((double)trues / mask.Length) + 3, 19).LineTo(figureWidth * ((double)trues / mask.Length) + 3, 24).LineTo(figureWidth + 3, 24).LineTo(figureWidth + 3, 19), Colours.Black);

                Font figureFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), renderer.BaseFontSize);
                gpr.FillText(figureWidth * 0.5 + 1.5 - figureFont.MeasureText(features.Length.ToString()).Width * 0.5, -10, features.Length.ToString(), figureFont, Colours.Black, TextBaselines.Bottom);

                gpr.FillText(figureWidth * ((double)trues / mask.Length) * 0.5 - figureFont.MeasureText(trues.ToString()).Width * 0.5, 26, trues.ToString(), figureFont, Colours.Black, TextBaselines.Top);
                gpr.FillText(figureWidth * ((double)trues / mask.Length) + 3 + figureWidth * (1 - (double)trues / mask.Length) * 0.5 - figureFont.MeasureText((features.Length - trues).ToString()).Width * 0.5, 26, (features.Length - trues).ToString(), figureFont, Colours.Black, TextBaselines.Top);

                gpr.FillText(-5 - figureFont.MeasureText(((double)trues / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture)).Width, 8, ((double)trues / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture), figureFont, Colours.Black, TextBaselines.Middle);

                gpr.FillText(figureWidth + 8, 8, (1 - (double)trues / mask.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture), figureFont, Colours.Black, TextBaselines.Middle);

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
            markdownDocument.AppendLine("**Table 1. Alignment features.** The table lists the features that have been computed for each alignment column, including a brief description and the observed range, mean and standard deviation (SD) for each of them.");
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

            markdownDocument.AppendLine("When this criterion is used to analyse the input data, " + incorrectAssignments + " columns (" + ((double)incorrectAssignments / features.Length).ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + " of the total) are incorrectly preserved or deleted (**Fig. 4**). Generally, these points should be located around the discriminant hyperplane; if many of them are located far from the discriminant plane, it might be a sign that the validation dataset is internally inconsistent. Alternatively, the " + arguments.Features.Count.ToString() + " features analysed by AliFilter may not be sufficient to capture the distinction between columns that have been preserved and those that have been deleted.");
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

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("<br type=\"page\" />");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("## Model validation");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The model being validated is a logistic model, which uses a linear combination of the feature values for each column to determine the log-odds that the column be preserved. The log-odds are then converted to a preservation score (ranging from 0 to 1), and columns with a preservation score lower than a specified threshold (e.g., 0.5) are deleted.");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The confidence of the model in the classification of each alignment column can be assessed by performing row-wise bootstrap replicates (note that this is different from most phylogenetic analyses, where bootstrap is performed column-wise). Each replicate column can be assessed by the logistic model, and a final assessment can be performed by counting how many replicates pass the preservation threshold and comparing this with a bootstrap replicate threshold.");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("Three parameters were thus tuned by cross-validation:");
            markdownDocument.AppendLine("* The logistic model threshold (range: 0 - 1).");
            markdownDocument.AppendLine("* The number of bootstrap replicates (range: 0 - 1000).");
            markdownDocument.AppendLine("* The bootstrap threshold (range: 0 - 1).");
            markdownDocument.AppendLine();

            if (target == OptimisationTarget.MCC)
            {
                markdownDocument.AppendLine("For each combination of parameters, the Matthews correlation coefficient $MCC$ was computed. This is defined as:");
                markdownDocument.AppendLine("$$");
                markdownDocument.AppendLine(@"MCC = \frac{\mathrm{TP} \cdot \mathrm{TN} - \mathrm{FP} \cdot \mathrm{FN}}{\sqrt { \left ( \mathrm{TP} + \mathrm{FP} \right ) \cdot \left ( \mathrm{TP} + \mathrm{FN} \right ) \cdot \left ( \mathrm{TN} + \mathrm{FP} \right ) \cdot \left ( \mathrm{TN} + \mathrm{FN} \right ) }}");
                markdownDocument.AppendLine("$$");
                markdownDocument.AppendLine("Where $\\mathrm{TP}$ is the number of true positives, $\\mathrm{TN}$ is the number of true negatives, $\\mathrm{FP}$ is the number of false positives, and $\\mathrm{FN}$ is the number of false negatives.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("The $MCC$ ranges from -1 to 1, with good models scoring close to 1. Values close to 0 indicate performance similar to a random classifier.");
            }
            else if (target == OptimisationTarget.Accuracy)
            {
                markdownDocument.AppendLine("For each combination of parameters, the accuracy score $A$ was computed. This is defined as:");
                markdownDocument.AppendLine("$$");
                markdownDocument.AppendLine(@"A = \frac{\mathrm{TP} + \mathrm{TN}}{\mathrm{TP} + \mathrm{TN} + \mathrm{FP} + \mathrm{FN}}");
                markdownDocument.AppendLine("$$");
                markdownDocument.AppendLine("Where $\\mathrm{TP}$ is the number of true positives, $\\mathrm{TN}$ is the number of true negatives, $\\mathrm{FP}$ is the number of false positives, and $\\mathrm{FN}$ is the number of false negatives.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("The accuracy score ranges from 0 to 1, with good models scoring close to 1. Note that in the case of a very imbalanced validation dataset (i.e., where most columns are preserved or discarded), a trivial classifier that either preserves all columns or discards al columns might have a surprisingly high accuracy.");
            }
            else if (target == OptimisationTarget.FBeta)
            {
                markdownDocument.AppendLine("For each combination of parameters, the $F_{" + beta.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}$ score was computed. This is defined as:");
                markdownDocument.AppendLine("$$");
                markdownDocument.AppendLine(@"F_{\beta} = \frac{\left (1 + \beta^2 \right )\mathrm{TP}}{\left (1 + \beta^2 \right ) \mathrm{TP} + \beta^2 \mathrm{FN} + \mathrm{FP}}");
                markdownDocument.AppendLine("$$");
                markdownDocument.AppendLine("Where $\\mathrm{TP}$ is the number of true positives, $\\mathrm{FN}$ is the number of false negatives, and $\\mathrm{FP}$ is the number of false positives.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("The value of $\\beta$ (in this case, `" + beta.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`) determines the relative weight given to false positives and false negatives. Values &gt; 1 result in models that produce less false positives, but more false negatives (e.g., by using low threshold values). Values &lt; 1 result in models that produce less false negatives, but more false positives (e.g., by using high threshold values).");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("The $F_{\\beta}$ score ranges from 0 to 1, with good models scoring close to 1. Note that this score does not account for the number of true negatives, and therefore it might be misleading in some situations.");
            }

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("An additional score $S$ was also computed, which takes into account the fact that while using bootstrap replicates can improve the prediction score, computing them requires time. This is defined as:");

            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine("S = " + target switch { OptimisationTarget.Accuracy => "A", OptimisationTarget.FBeta => "F_\\beta", OptimisationTarget.MCC => "MCC", _ => "" } + @" - \frac{b}{100} \cdot " + arguments.EffortPenalty.ToString(System.Globalization.CultureInfo.InvariantCulture));
            markdownDocument.AppendLine("$$");

            markdownDocument.AppendLine("Where $b$ is the number of bootstrap replicates. This ensures that an additional 100 bootstrap replicates are only performed if they improve the prediction score by at least " + arguments.EffortPenalty.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");

            markdownDocument.AppendLine();

            markdownDocument.AppendLine("The following plots show the value of the prediction score as a function of the two threshold parameters, for each number of bootstrap replicates. **Table 2** summarises the best scores and the corresponding threshold values for each number of bootstrap replicates.");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("<br type=\"page\">");
            markdownDocument.AppendLine();

            #region Target score plots
            {
                Plot[] plots = new Plot[fScores[0].Length];

                for (int j = 0; j < plots.Length; j++)
                {
                    int bootstrapReplicateCount = (j - 1) * 100 + 100;

                    double[][][] plotData = new double[fScores[0][j].Length][][];
                    Colour[] colours = new Colour[plotData.Length];

                    for (int k = 0; k < plotData.Length; k++)
                    {
                        double bootstrapThreshold = (double)k / (plotData.Length - 1);
                        plotData[k] = new double[fScores.Length][];

                        for (int i = 0; i < fScores.Length; i++)
                        {
                            plotData[k][i] = new double[] { (double)i / (fScores.Length - 1), fScores[i][j][k] };
                        }

                        colours[k] = Gradients.ViridisColouring(double.IsNaN(bootstrapThreshold) ? 0.5 : bootstrapThreshold);
                    }

                    plots[j] = Plot.Create.LineCharts(plotData, width: 300, height: 200, xAxisTitle: "Threshold", yAxisTitle: target switch { OptimisationTarget.Accuracy => "A", OptimisationTarget.FBeta => "F<sub>" + beta.ToString(System.Globalization.CultureInfo.InvariantCulture) + "</sub>", OptimisationTarget.MCC => "MCC", _ => "" }, title: bootstrapReplicateCount.ToString() + " bootstrap replicates", linePresentationAttributes: colours.Select(x => new PlotElementPresentationAttributes() { Stroke = x, LineWidth = (j == 0 ? 1 : 0.5) }).ToArray());

                    foreach (ContinuousAxisLabels labels in plots[j].GetAll<ContinuousAxisLabels>())
                    {
                        Func<IReadOnlyList<double>, int, IEnumerable<FormattedText>> previousFormat = labels.TextFormat;

                        labels.TextFormat = (x, i) =>
                        {
                            double[] newVal = new double[x.Count];

                            for (int j = 0; j < x.Count; j++)
                            {
                                if (x[j].ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) == "-0.00")
                                {
                                    newVal[j] = 0;
                                }
                                else
                                {
                                    newVal[j] = x[j];
                                }
                            }
                            return previousFormat(newVal, i);
                        };
                    }
                }

                Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16);

                for (int i = 0; i < plots.Length; i += 2)
                {
                    Page fullPlot = new Page(1, 1);
                    fullPlot.Graphics.DrawGraphics(0, 0, plots[i].Render().Graphics);

                    if (i < plots.Length - 1)
                    {
                        fullPlot.Graphics.DrawGraphics(400, 0, plots[i + 1].Render().Graphics);
                        fullPlot.Crop();
                    }
                    else
                    {
                        fullPlot.Crop();
                        fullPlot.Width += 400;
                    }

                    fullPlot.Graphics.Save();
                    fullPlot.Graphics.Translate(375, fullPlot.Height * 0.5);
                    fullPlot.Graphics.Rotate(-Math.PI / 2);
                    fullPlot.Graphics.Translate(-fullPlot.Height * 0.5, 0);
                    fullPlot.Graphics.FillText(0, 0, "BS threshold:", fnt, Colours.Black, TextBaselines.Middle);
                    fullPlot.Graphics.Restore();

                    double height = fullPlot.Height - fnt.MeasureText("BS threshold:").Width - 10;
                    Point p1 = new Point(375, height);
                    Point p2 = new Point(375, 0);

                    for (int b = 0; b <= 10; b++)
                    {
                        fullPlot.Graphics.FillRectangle(365, height / 11 * b, 20, height / 11, Gradients.ViridisColouring(1 - b * 0.1));
                    }

                    PlotUtilities.InsertFigure(markdownDocument, fullPlot, "align=\"center\" width=\"800\"");

                    markdownDocument.AppendLine();
                    markdownDocument.AppendLine("&nbsp;");
                    markdownDocument.AppendLine();
                }
            }
            #endregion

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("&nbsp;");
            markdownDocument.AppendLine();

            #region Table 2. Maximum scores by bootstrap replicate.
            markdownDocument.AppendLine("**Table 2. Scores and threshold values.** The table shows for each number of bootstrap replicates, the maximum value for the $" + target switch { OptimisationTarget.Accuracy => "A", OptimisationTarget.FBeta => "F_\\beta", OptimisationTarget.MCC => "MCC", _ => "" } + "$ score and the corresponding $S$ score, logistic model threshold, and bootstrap threshold.");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("+-----------------+-----------+-----------+-----------+--------------+");
            markdownDocument.AppendLine("| BS replicates   | Score     | " + target switch { OptimisationTarget.Accuracy => "Accuracy ", OptimisationTarget.FBeta => "$F_\\beta$", OptimisationTarget.MCC => "MCC      ", _ => "         " } + " | Threshold | BS threshold |");
            markdownDocument.AppendLine("+=================+===========+===========+===========+==============+");

            for (int j = 0; j < maximumScoresByBootstrapReplicates.Length; j++)
            {
                int bootstrapReplicateCount = (j - 1) * 100 + 100;

                markdownDocument.Append("| ");
                markdownDocument.Append(bootstrapReplicateCount.ToString().PadRight(16));
                markdownDocument.Append("| ");
                markdownDocument.Append(maximumScoresByBootstrapReplicates[j].finalScore.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture).PadRight(10));
                markdownDocument.Append("| ");
                markdownDocument.Append(maximumScoresByBootstrapReplicates[j].maxTargetScore.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture).PadRight(10));
                markdownDocument.Append("| ");
                markdownDocument.Append(maximumScoresByBootstrapReplicates[j].threshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).PadRight(10));
                markdownDocument.Append("| ");
                markdownDocument.Append(maximumScoresByBootstrapReplicates[j].bootstrapThreshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).PadRight(13));
                markdownDocument.AppendLine("|");
                markdownDocument.AppendLine("+-----------------+-----------+-----------+-----------+--------------+");
            }
            #endregion

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The overall best $" + target switch { OptimisationTarget.Accuracy => "A", OptimisationTarget.FBeta => "F_\\beta", OptimisationTarget.MCC => "MCC", _ => "" } + "$ score (" + maximumScoresByBootstrapReplicates[bestFScoreIndex].maxTargetScore.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", corresponding $S$ score " + maximumScoresByBootstrapReplicates[bestFScoreIndex].finalScore.ToString(System.Globalization.CultureInfo.InvariantCulture) + ") was obtained for " + ((bestFScoreIndex - 1) * 100 + 100).ToString() + " bootstrap replicates, with logistic model threshold " + maximumScoresByBootstrapReplicates[bestFScoreIndex].threshold.ToString(System.Globalization.CultureInfo.InvariantCulture) + " and bootstrap threshold " + maximumScoresByBootstrapReplicates[bestFScoreIndex].bootstrapThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The overall best $S$ score (" + maximumScoresByBootstrapReplicates[bestScoreIndex].finalScore.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", corresponding $" + target switch { OptimisationTarget.Accuracy => "A", OptimisationTarget.FBeta => "F_\\beta", OptimisationTarget.MCC => "MCC", _ => "" } + "$ score " + maximumScoresByBootstrapReplicates[bestScoreIndex].maxTargetScore.ToString(System.Globalization.CultureInfo.InvariantCulture) + ") was obtained for " + ((bestScoreIndex - 1) * 100 + 100).ToString() + " bootstrap replicates, with logistic model threshold " + maximumScoresByBootstrapReplicates[bestScoreIndex].threshold.ToString(System.Globalization.CultureInfo.InvariantCulture) + " and bootstrap threshold " + maximumScoresByBootstrapReplicates[bestScoreIndex].bootstrapThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("These parameter values have been stored with the validated model.");
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
                    Title = "Model validation report",
                    Subject = "Validation report for an AliFilter model",
                    Keywords = "AliFilter, Model, Validation, Report",
                    CreationDateTimeZone = TimeZoneInfo.Local,
                    ModificationDateTimeZone = TimeZoneInfo.Local
                };

                PDFDocument pdfDoc = doc.CreatePDFDocument(linkDestinations: linkDestinations, outline: OutlineTree.CreateFromHeadings(headingTree), metadata: metadata);

                using (MemoryStream modelStream = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(modelStream, leaveOpen: true))
                    {
                        sw.Write(annotatedModel.ToString());
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
