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

using Accord.Statistics.Analysis;
using Accord.Statistics.Models.Regression.Linear;
using AliFilter.Models;
using System.Text;
using System.Text.Json;
using VectSharp.Markdown;
using VectSharp;
using VectSharp.PDF;
using VectSharp.Plots;
using VectSharp.SVG;
using Markdig.Syntax;
using VectSharp.PDF.PDFObjects;

namespace AliFilter
{
    internal static partial class Utilities
    {
        // Create the alignment report.
        internal static int CreateAlignmentReport(double[][] features, bool protein, ValidatedModel model, Mask predictedMask, double threshold, double bootstrapThreshold, int bootstrapReplicateCount, Arguments arguments, TextWriter outputLog)
        {
            bool outputMd = Path.GetExtension(arguments.ReportFile) == ".md";

            outputLog?.WriteLine();
            outputLog?.WriteLine("Creating alignment report...");

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

            // Create the report as a markdown document.
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

            markdownDocument.AppendLine("# Alignment report");

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("&nbsp;");
            markdownDocument.AppendLine();

            DateTime creationTime = DateTime.Now;

            if (!PlotUtilities.DaysOfMonth.TryGetValue(creationTime.Day, out string day))
            {
                day = creationTime.Day.ToString() + "^th^";
            }

            int trues = predictedMask.MaskedStates.Count(x => x);

            markdownDocument.AppendLine("Created by AliFilter version " + Program.Version + " on " + day + " " + creationTime.ToString("MMM, yyyy", System.Globalization.CultureInfo.InvariantCulture) + " at " + creationTime.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture) + ".");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("MD5 checksum of the model file used to filter this alignment: `" + modelChecksum + "`");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("This report may be included in any analysis using this alignment.");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("This report is machine-readable. If the report file is called `report.pdf`, you can export the model to a file called `model.json` by running:");
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
            markdownDocument.AppendLine("## Input data analysis");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The input alignment contained " + features.Length.ToString() + " columns of " + (protein ? "amino acids" : "nucleotides") + ". For each alignment column, " + arguments.Features.Count.ToString() + " features were computed (**Table 1**), which were analysed in a Principal Component Analysis (PCA) and in a Linear Discriminant Analysis (LDA).");
            markdownDocument.AppendLine();
            #endregion

            #region Table 1. Alignment features.

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

            markdownDocument.AppendLine("A PCA (**Fig. 1**) uses a linear transformation to transform the data to a coordinate system where each coordinate (component) explains as much of the variance of the data as possible, while being orthogonal to the previous components. This is useful to show the distribution of the input data, but a PCA, on its own, cannot be used to decide whether an alignment column should be preserved or not.");
            markdownDocument.AppendLine();

            markdownDocument.AppendLine("An LDA (**Fig. 2**) also uses a linear transformation to project the data to a different coordinate space, but in this case each component attempts to explain as much of the difference between the two classes of data (“preserved” or “deleted”) as possible. While the transform used to create the PCA plot (**Fig. 1**) was computed using only the input data, the LDA transform used to create **Figure 2** was computed during the model training step.");
            markdownDocument.AppendLine();
            
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("<br type=\"page\" />");
            markdownDocument.AppendLine();

            #region Figure 1. PCA of the alignment features.
            {
                // Transform the alignment features using the PCA.
                double[][] transformedDataPreserved = pca.Transform(features.Where((x, i) => predictedMask.MaskedStates[i]).ToArray());
                double[][] transformedDataDeleted = pca.Transform(features.Where((x, i) => !predictedMask.MaskedStates[i]).ToArray());


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
                markdownDocument.Append("**Figure 1. Results of the PCA. a)** Biplot showing the density of input data columns in function of the principal component values and the component loadings. Columns that were preserved in the filtered alignment are shown in ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.Append(" orange, while deleted columns are shown in ");
                PlotUtilities.InsertFigure(markdownDocument, blueBox, "");
                markdownDocument.Append(" blue. Component abbreviations are _PC1_: Principal component 1; _PC2_: Principal component 2");

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

            #region Figure 3. LDA of the alignment features.
            {
                double[][] transformedDataPreserved = transformedLDA.Where((x, i) => predictedMask.MaskedStates[i]).ToArray();
                double[][] transformedDataDeleted = transformedLDA.Where((x, i) => !predictedMask.MaskedStates[i]).ToArray();

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
                    // Each loading is represented by a ContinuousAxis.
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

                // Create the plot with the legend.
                Page fullPlot = biplot.Render();

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

                fullPlot.Crop();

                PlotUtilities.InsertFigure(markdownDocument, fullPlot, "width=\"410\" align=\"left\"");

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
                markdownDocument.Append("**Figure 2. Results of the LDA.** The biplot shows the density of input data columns in function of the discriminant component values and the projections of the original features in the LDA space. Columns that were preserved in the filtered alignment are shown in ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.Append(" orange, while deleted columns are shown in ");
                PlotUtilities.InsertFigure(markdownDocument, blueBox, "");
                markdownDocument.Append(" blue. The ");
                PlotUtilities.InsertFigure(markdownDocument, dashedBox, "");
                markdownDocument.AppendLine(" dashed line represents the intersection of the discriminant hyperplane with the 2D plane shown in the plot. Component abbreviations are _DC1_: Discriminant component 1; _DC2_: Discriminant component 2; the rest as in **Figure 1a**.");
                markdownDocument.AppendLine();
            }
            #endregion

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("<br type=\"page\" />");
            markdownDocument.AppendLine();

            #region Results.

            markdownDocument.AppendLine("## Results");
            markdownDocument.AppendLine();


            markdownDocument.AppendLine("The data described above were analysed with a logistic model, which uses a linear combination of the feature values for each column to determine the log-odds that the column be preserved. The log-odds were converted to a preservation score (ranging from 0 to 1), and columns with a preservation score lower than a specified threshold (for this model, " + threshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ") were deleted.");

            if (bootstrapReplicateCount > 0)
            {
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("For each alignment column, " + bootstrapReplicateCount.ToString() + " bootstrap replicates were created, and each of them was assessed using the logistic model described above. Only columns where at least " + bootstrapThreshold.ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + " of the bootstrap replicates were above the threshold were actually preserved.");
            }

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("Using this approach, " + trues.ToString() + " columns (" + ((double)trues / features.Length).ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + ") were preserved, while " + (features.Length - trues).ToString() + " columns (" + (1 - (double)trues / features.Length).ToString("0.##%", System.Globalization.CultureInfo.InvariantCulture) + ") were deleted (**Fig. 3**). The distribution of the preservation scores for each column computed using this model is shown in **Figure 4**.");
            markdownDocument.AppendLine();

            #endregion

            #region Figure 3. Preserved and deleted columns in the alignment.
            {
                double figureWidth = 400;

                Page proportionFigure = new Page(1, 1);
                Graphics gpr = proportionFigure.Graphics;

                // Bottom part of the figure: proportion of preserved and deleted columns.
                gpr.FillRectangle(0, 0, figureWidth * ((double)trues / features.Length), 16, Colour.FromRgb(213, 94, 0));
                gpr.FillRectangle(figureWidth * ((double)trues / features.Length) + 3, 0, figureWidth * (1 - (double)trues / features.Length), 16, Colour.FromRgb(0, 114, 178));

                // The alignment could be arbitrarily long, which would make plotting it impractical. Hence, we divide it in 400 sections and compute the (relative) proportion of columns in each section that are preserved.
                int[] counts = new int[(int)figureWidth];

                for (int i = 0; i < figureWidth; i++)
                {
                    counts[i] = (from el in Enumerable.Range((int)(i * features.Length / figureWidth), Math.Max(1, (int)(features.Length / figureWidth))) select predictedMask.MaskedStates[el]).Count(x => x);
                }

                double maxCount = counts.Max();

                // Colour for deleted columns, in Lab coordinates.
                (double L, double a, double b) deletedColour = Colour.FromRgb(0, 114, 178).ToLab();

                // Colour for preserved columns, in Lab coordinates.
                (double L, double a, double b) preservedColour = Colour.FromRgb(213, 94, 0).ToLab();

                for (int i = 0; i < figureWidth; i++)
                {
                    // Interpolate the colour in Lab space for more accurate results.
                    gpr.FillRectangle(i * (figureWidth + 3) / figureWidth, -20, (figureWidth + 3) / figureWidth, 16,
                        Colour.FromLab(
                            deletedColour.L * (1 - counts[i] / maxCount) + preservedColour.L * counts[i] / maxCount,
                            deletedColour.a * (1 - counts[i] / maxCount) + preservedColour.a * counts[i] / maxCount,
                            deletedColour.b * (1 - counts[i] / maxCount) + preservedColour.b * counts[i] / maxCount)
                        );
                }

                // Brackets.
                gpr.StrokePath(new GraphicsPath().MoveTo(0, -23).LineTo(0, -28).LineTo(figureWidth + 3, -28).LineTo(figureWidth + 3, -23), Colours.Black);
                gpr.StrokePath(new GraphicsPath().MoveTo(0, 19).LineTo(0, 24).LineTo(figureWidth * ((double)trues / features.Length), 24).LineTo(figureWidth * ((double)trues / features.Length), 19), Colours.Black);
                gpr.StrokePath(new GraphicsPath().MoveTo(figureWidth * ((double)trues / features.Length) + 3, 19).LineTo(figureWidth * ((double)trues / features.Length) + 3, 24).LineTo(figureWidth + 3, 24).LineTo(figureWidth + 3, 19), Colours.Black);

                // Numbers.
                Font figureFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), renderer.BaseFontSize);
                gpr.FillText(figureWidth * 0.5 + 1.5 - figureFont.MeasureText(features.Length.ToString()).Width * 0.5, -30, features.Length.ToString(), figureFont, Colours.Black, TextBaselines.Bottom);
                gpr.FillText(figureWidth * ((double)trues / features.Length) * 0.5 - figureFont.MeasureText(trues.ToString()).Width * 0.5, 26, trues.ToString(), figureFont, Colours.Black, TextBaselines.Top);
                gpr.FillText(figureWidth * ((double)trues / features.Length) + 3 + figureWidth * (1 - (double)trues / features.Length) * 0.5 - figureFont.MeasureText((features.Length - trues).ToString()).Width * 0.5, 26, (features.Length - trues).ToString(), figureFont, Colours.Black, TextBaselines.Top);
                gpr.FillText(-5 - figureFont.MeasureText(((double)trues / features.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture)).Width, 8, ((double)trues / features.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture), figureFont, Colours.Black, TextBaselines.Middle);
                gpr.FillText(figureWidth + 8, 8, (1 - (double)trues / features.Length).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture), figureFont, Colours.Black, TextBaselines.Middle);

                // Crop the figure and add margin.
                proportionFigure.Crop();
                proportionFigure.Crop(new Point(0, -10), new Size(proportionFigure.Width, proportionFigure.Height + 10));

                Page orangeBox = new Page(6, 6) { Background = Colour.FromRgb(213, 94, 0) };
                Page blueBox = new Page(6, 6) { Background = Colour.FromRgb(0, 114, 178) };

                PlotUtilities.InsertFigure(markdownDocument, proportionFigure, "align=\"center\"");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.Append("**Figure 3. Proportion of preserved columns.** The upper part of the figure shows the alignment columns that were preserved (in ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.Append(" orange) or deleted (in ");
                PlotUtilities.InsertFigure(markdownDocument, blueBox, "");
                markdownDocument.AppendLine(" blue) in the filtered alignment. The bottom part of the figure shows the proportion of preserved and deleted columns in the filtered alignment.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("&nbsp;");
                markdownDocument.AppendLine();
            }
            #endregion

            markdownDocument.AppendLine();

            #region Figure 4. Preservation score distribution for the alignment columns.
            {
                // Use a sigmoid coordinate system to highlight values close to 0 and 1.
                SigmoidCoordinateSystem coordinateSystem = new SigmoidCoordinateSystem(0, 25, 4, 300, 200);

                (string, IReadOnlyList<double>)[] transformedLog = new (string, IReadOnlyList<double>)[]
                {
                    ("Deleted", predictedMask.Confidence.Where((x, i) => !predictedMask.MaskedStates[i]).Select(x => 1 - x).ToArray()),
                    ("Preserved", predictedMask.Confidence.Where((x, i) => predictedMask.MaskedStates[i]).ToArray())
                };


                // Display the scores of the deleted and preserved columns using a violin plot.
                Plot violinPlotLog = Plot.Create.ViolinPlot(transformedLog, spacing: 0.5, coordinateSystem: coordinateSystem, showBoxPlots: false, yAxisTitle: "Preservation score", dataRangeMin: 1e-8, dataRangeMax: 1 - 1e-8);

                violinPlotLog.AddPlotElement(new LinearTrendLine(0, threshold, 0, threshold - 0.1, 25, threshold + 0.1, coordinateSystem));
                violinPlotLog.AddPlotElement(new TextLabel<IReadOnlyList<double>>(threshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), new double[] { 12.5, coordinateSystem.ToDataCoordinates(new Point(12.5, coordinateSystem.ToPlotCoordinates(new double[] { 12.5, threshold }).Y - 5))[1] }, coordinateSystem) { Alignment = TextAnchors.Center, Baseline = TextBaselines.Bottom });
                violinPlotLog.GetFirst<ContinuousAxisLabels>().TextFormat = (x, i) => FormattedText.Format(x[1].ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), FontFamily.StandardFontFamilies.Helvetica, 12);
                violinPlotLog.GetAll<ContinuousAxisTitle>().ElementAt(1).Position -= 10;

                foreach (Violin violin in violinPlotLog.GetAll<Violin>())
                {
                    violin.MaxBins = 1000;
                }

                Page violinPlotPage = violinPlotLog.Render();

                violinPlotPage.Crop();

                PlotUtilities.InsertFigure(markdownDocument, violinPlotPage, "width=\"500\" align=\"left\"");

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
                markdownDocument.Append("**Figure 4. Logistic model results.** This violin plot shows the distribution of the preservation score according to the logistic model for columns that were deleted (in ");
                PlotUtilities.InsertFigure(markdownDocument, blueBox, "");
                markdownDocument.Append(" blue, on the left) or preserved (in ");
                PlotUtilities.InsertFigure(markdownDocument, orangeBox, "");
                markdownDocument.Append(" orange, on the right) in the filtered alignment. The ");
                PlotUtilities.InsertFigure(markdownDocument, dashedBox, "");
                markdownDocument.AppendLine(" dashed line represents the " + threshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " threshold that determines whether a column is preserved or deleted according to the model.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("&nbsp;");
                markdownDocument.AppendLine();
            }
            #endregion

            markdownDocument.AppendLine();
            markdownDocument.AppendLine("&nbsp;");
            markdownDocument.AppendLine();
            markdownDocument.AppendLine("The " + (bootstrapReplicateCount > 0 ? "bootstrap " : "")  + "threshold value (here, " + (bootstrapReplicateCount > 0 ? bootstrapThreshold : threshold).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ") determines which columns are deleted or preserved. Alignment columns with a score lower than the threshold are deleted, while those with a score higher than or equal to the threshold are preserved. **Figure 5** shows the proportion of columns that are deleted as a function of the threshold value. If the model is able confidently classify the columns in the alignment, this line should be almost flat for a wide range of values between 0 and 1.");

            markdownDocument.AppendLine("This can be summarised using the model confidence score $C$, which is defined as:");

            markdownDocument.AppendLine("$$");
            markdownDocument.AppendLine(@"C = 1 - \frac{4}{n} \sum_{i = 1}^{n} s_i \cdot (1 - s_i)");
            markdownDocument.AppendLine("$$");

            markdownDocument.AppendLine("Where $n$ is the number of columns in the alignment and $s_i$ is the confidence score for column $i$ (ranging between 0 and 1). For a model performing confident assignments, this score should be close to 1. In this case, it is " + (1 - predictedMask.Confidence.Select(x => x * (1 - x) * 4).Sum() / predictedMask.Length).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + ".");

            markdownDocument.AppendLine();

            #region Figure 5. Proportion of preserved and deleted columns as a function of the preservation threshold.
            {
                // Proportion of columns with a preservation score greather than a certain threshold.
                double[][] preservationPerc = (from el in Enumerable.Range(0, 1001) select new double[] { el * 0.001, (double)predictedMask.Confidence.Select((x, i) => (predictedMask.MaskedStates[i] ? x : (1 - x)) < el * 0.001).Count(x => x) / predictedMask.Length }).ToArray();

                Plot plot = Plot.Create.LineChart(preservationPerc, width: 500, linePresentationAttributes: new PlotElementPresentationAttributes() { LineWidth = 2 }, xAxisTitle: "Threshold", yAxisTitle: "Proportion of deleted columns");

                plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coordinateSystem) =>
                {
                    gpr.StrokePath(new GraphicsPath().MoveTo(coordinateSystem.ToPlotCoordinates(new double[] { threshold, 0 })).LineTo(coordinateSystem.ToPlotCoordinates(new double[] { threshold, 1 })), Colour.FromRgb(180, 180, 180), lineDash: new LineDash(5, 5, 0));
                }));

                PlotUtilities.InsertFigure(markdownDocument, plot.Render(), "align=\"center\"");

                Page dashedBox = new Page(15, 6);
                dashedBox.Graphics.StrokePath(new GraphicsPath().MoveTo(0, 3).LineTo(15, 3), Colour.FromRgb(180, 180, 180), lineDash: new LineDash(3, 3, 0));

                markdownDocument.AppendLine();
                markdownDocument.AppendLine();
                markdownDocument.Append("**Figure 5. Proportion of deleted columns as a function of the threshold value.** The line chart shows the proportion of columns in the input alignment that are deleted when a certain threshold value is used. The ");
                PlotUtilities.InsertFigure(markdownDocument, dashedBox, "");
                markdownDocument.AppendLine(" dashed line represents the " + (bootstrapReplicateCount > 0 ? bootstrapThreshold : threshold).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " preservation threshold.");
                markdownDocument.AppendLine();
                markdownDocument.AppendLine("&nbsp;");
                markdownDocument.AppendLine();
            }
            #endregion

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
                    Title = "Alignment filtering report",
                    Subject = "Alignment filtering report for a " + (protein ? "protein" : "DNA") + " sequence.",
                    Keywords = "AliFilter, Alignment, Filtering, Report",
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
