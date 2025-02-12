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
using System.Data;
using VectSharp;
using VectSharp.PDF;
using VectSharp.Plots;
using VectSharp.Raster;
using VectSharp.SVG;

namespace Figure2_Table_S1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Create the plots.
            Page fig2a = CreateFigure2a();
            Page fig2b = CreateFigure2b();
            Page legend = CreateLegend();

            // Resize to a width of 17cm.
            double scalingFactor = Math.Min(235 / fig2a.Width, 235 / fig2b.Width);
            Page resizedPage = new Page(482, scalingFactor * Math.Max(fig2a.Height, fig2b.Height) + legend.Height + 20);
            resizedPage.Background = Colours.White;

            Font partLetterFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 12);

            // Draw the legend.
            resizedPage.Graphics.DrawGraphics(resizedPage.Width * 0.5 - legend.Width * 0.5, 0, legend.Graphics);

            resizedPage.Graphics.Translate(0, legend.Height + 20);

            // Draw part a)
            resizedPage.Graphics.FillText(0, 0, "a)", partLetterFont, Colours.Black);
            resizedPage.Graphics.Save();
            resizedPage.Graphics.Scale(scalingFactor, scalingFactor);
            resizedPage.Graphics.DrawGraphics(235 * 0.5 - fig2a.Width * scalingFactor * 0.5, scalingFactor * Math.Max(fig2a.Height, fig2b.Height) * 0.5 - fig2a.Height * scalingFactor * 0.5, fig2a.Graphics);
            resizedPage.Graphics.Restore();

            resizedPage.Graphics.Translate(247, 0);

            // Draw part b)
            resizedPage.Graphics.FillText(0, 0, "b)", partLetterFont, Colours.Black);
            resizedPage.Graphics.Save();
            resizedPage.Graphics.Scale(scalingFactor, scalingFactor);
            resizedPage.Graphics.DrawGraphics(235 * 0.5 - fig2b.Width * scalingFactor * 0.5, scalingFactor * Math.Max(fig2a.Height, fig2b.Height) * 0.5 - fig2b.Height * scalingFactor * 0.5, fig2b.Graphics);
            resizedPage.Graphics.Restore();

            Document doc = new Document();
            doc.Pages.Add(resizedPage);

            resizedPage.SaveAsSVG("Figure_2.svg");
            resizedPage.SaveAsSVG("Figure_2.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_2.pdf");
            resizedPage.SaveAsPNG("Figure_2.png", 600.0 / 72);
        }

        /// <summary>
        /// Creates the figure legend.
        /// </summary>
        /// <returns>A <see cref="Page"/> on which the legend has been rendered.</returns>
        static Page CreateLegend()
        {
            string[] datasetNames = new string[9] { "Dataset 1", "Dataset 2", "Dataset 3", "Dataset 4", "Dataset 5", "Dataset 6", "Dataset 7", "Dataset 8", "Dataset 9" };
            Colour[] datasetColours = new Colour[9]
            {
                Colour.FromRgb(160, 160, 160), // Phylo
                Colour.FromRgb(0, 158, 115), // Cyano
                Colour.FromRgb(0, 114, 178), // Rhodo
                Colour.FromRgb(230, 159, 0), // Prok
                Colour.FromRgb(213, 94, 0), // Collembola
                Colour.FromRgb(214, 199, 32), // Formicidae
                Colour.FromRgb(86, 180, 233), // Heliconiini
                Colour.FromRgb(204, 121, 167), // Silva
                Colour.FromRgb(0, 0, 0), // FullDataset
            };

            // Shapes for the points in the scatter plot.
            GraphicsPath circle = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close(); // Area: Math.PI
            GraphicsPath square = new GraphicsPath().MoveTo(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1).Close(); // Area: 4
            GraphicsPath diamond = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close(); // Area: 2
            GraphicsPath triangle = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close(); // Area: 2

            GraphicsPath circleTarget = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close().MoveTo(0, -1).LineTo(0, -2).MoveTo(0, 1).LineTo(0, 2).MoveTo(-1, 0).LineTo(-2, 0).MoveTo(1, 0).LineTo(2, 0); // Area: Math.PI
            GraphicsPath squareTarget = new GraphicsPath().MoveTo(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1).Close().MoveTo(0, -1).LineTo(0, -2.5).MoveTo(0, 1).LineTo(0, 2.5).MoveTo(-1, 0).LineTo(-2.5, 0).MoveTo(1, 0).LineTo(2.5, 0); // Area: 4
            GraphicsPath diamondTarget = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close().MoveTo(0, -1).LineTo(0, -2).MoveTo(0, 1).LineTo(0, 2).MoveTo(-1, 0).LineTo(-2, 0).MoveTo(1, 0).LineTo(2, 0); // Area: 2
            GraphicsPath triangleTarget = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close().MoveTo(0, -1).LineTo(0, -2).MoveTo(-1, 1).LineTo(-1.866, 1.5).MoveTo(1, 1).LineTo(1.866, 1.5); // Area: 2

            GraphicsPath star = new GraphicsPath();
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    star.LineTo(Math.Cos(-Math.PI / 10 + 2 * Math.PI / 10 * i), Math.Sin(-Math.PI / 10 + 2 * Math.PI / 10 * i));
                }
                else
                {
                    star.LineTo(Math.Cos(-Math.PI / 10 + 2 * Math.PI / 10 * i) * 0.5, Math.Sin(-Math.PI / 10 + 2 * Math.PI / 10 * i) * 0.5);
                }
            }
            star.Close();

            IDataPointElement[] datapointElements = new IDataPointElement[27]
            {
                new PathDataPointElement(circle),
                new PathDataPointElement(square),
                new PathDataPointElement(diamond),
                new PathDataPointElement(triangle),
                new PathDataPointElement(circle),
                new PathDataPointElement(square),
                new PathDataPointElement(diamond),
                new PathDataPointElement(triangle),
                new PathDataPointElement(),

                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(star),

                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(star)
            };

            double[] pointSizes = new double[] { 1,
                4.5, 3.5, 5, 5, 4.5, 3.5, 5, 5, 1,
                5, 4, 5, 5, 4.5, 3.5, 5, 5, 5,
                5, 4, 5, 5, 4.5, 3.5, 5, 5, 5 };

            PlotElementPresentationAttributes[] presentationAttributes = datasetColours.Select(x => BlendWithWhite(x, 0.5)).Select((x, i) => new PlotElementPresentationAttributes() { Fill = i < 4 ? x : null, Stroke = x, LineWidth = 2.5 / pointSizes[i + 1] })
                .Concat(datasetColours.Select((x, i) => new PlotElementPresentationAttributes() { Fill = null, Stroke = Colours.White, LineWidth = 5 / pointSizes[i + 10], LineCap = LineCaps.Round }))
                .Concat(datasetColours.Select((x, i) => new PlotElementPresentationAttributes() { Fill = i < 4 || i == 8 ? x : Colours.White, Stroke = x, LineWidth = 2.5 / pointSizes[i + 10], LineCap = LineCaps.Round })).ToArray();

            Page legendPage = new Page(1, 1);

            Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);
            Font fntBold = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 12);

            Graphics graphics = legendPage.Graphics;

            double column1Width = datasetNames[0..2].Select(x => fnt.MeasureText(x).Width).Max() + 10;
            double column2Width = datasetNames[2..4].Select(x => fnt.MeasureText(x).Width).Max() + 10;
            double column3Width = datasetNames[4..6].Select(x => fnt.MeasureText(x).Width).Max() + 10;
            double column4Width = datasetNames[6..8].Select(x => fnt.MeasureText(x).Width).Max() + 10;

            graphics.FillText((column1Width + column2Width + 20) * 0.5 - fntBold.MeasureText("Amino acid alignments").Width * 0.5, 0, "Amino acid alignments", fntBold, Colours.Black);

            graphics.Save();

            for (int i = 0; i < 2; i++)
            {
                double effectiveSize = pointSizes[i + 1] * 0.65;

                graphics.Save();
                graphics.Translate(2.75, 18 + i * 14);
                graphics.Scale(effectiveSize, effectiveSize);
                datapointElements[i].Plot(graphics, presentationAttributes[i + 18], null);
                graphics.Restore();

                graphics.FillText(10, 22 + i * 14, datasetNames[i], fnt, Colours.Black, TextBaselines.Baseline);
            }

            graphics.Translate(column1Width + 20, 0);

            for (int i = 2; i < 4; i++)
            {
                double effectiveSize = pointSizes[i + 1] * 0.65;

                graphics.Save();
                graphics.Translate(2.75, 18 + (i - 2) * 14);
                graphics.Scale(effectiveSize, effectiveSize);
                datapointElements[i].Plot(graphics, presentationAttributes[i + 18], null);
                graphics.Restore();

                graphics.FillText(10, 22 + (i - 2) * 14, datasetNames[i], fnt, Colours.Black, TextBaselines.Baseline);
            }

            graphics.Translate(column2Width + 30, 0);
            graphics.FillText((column3Width + column4Width + 20) * 0.5 - fntBold.MeasureText("Nucleotide alignments").Width * 0.5, 0, "Nucleotide alignments", fntBold, Colours.Black);

            for (int i = 4; i < 6; i++)
            {
                double effectiveSize = pointSizes[i + 1] * 0.65;

                graphics.Save();
                graphics.Translate(2.75, 18 + (i - 4) * 14);
                graphics.Scale(effectiveSize, effectiveSize);
                datapointElements[i].Plot(graphics, presentationAttributes[i + 18], null);
                graphics.Restore();

                graphics.FillText(10, 22 + (i - 4) * 14, datasetNames[i], fnt, Colours.Black, TextBaselines.Baseline);
            }

            graphics.Translate(column3Width + 20, 0);

            for (int i = 6; i < 8; i++)
            {
                double effectiveSize = pointSizes[i + 1] * 0.65;

                graphics.Save();
                graphics.Translate(2.75, 18 + (i - 6) * 14);
                graphics.Scale(effectiveSize, effectiveSize);
                datapointElements[i].Plot(graphics, presentationAttributes[i + 18], null);
                graphics.Restore();

                graphics.FillText(10, 22 + (i - 6) * 14, datasetNames[i], fnt, Colours.Black, TextBaselines.Baseline);
            }

            graphics.Restore();

            double bottomLineWidth = fnt.MeasureText("Full dataset (9)").Width + fnt.MeasureText("Individual datasets (1 - 8)").Width + 30 + 13 + 20;

            Rectangle currBounds = legendPage.Graphics.GetBounds();

            graphics.Save();
            graphics.Translate(currBounds.Location.X + currBounds.Size.Width * 0.5 - bottomLineWidth * 0.5, -currBounds.Location.Y - 20);

            {
                double effectiveSize = pointSizes[10] * 0.5;
                graphics.Save();
                graphics.Translate(10, 5);
                graphics.Scale(effectiveSize, effectiveSize);
                datapointElements[9].Plot(graphics, presentationAttributes[26], null);
                graphics.Restore();
            }

            graphics.FillText(20, 8, "Individual datasets (1 - 8)", fnt, Colours.Black, TextBaselines.Baseline);

            {
                double effectiveSize = pointSizes[26] * 0.65;
                graphics.Save();
                graphics.Translate(20 + 30 + 5 + fnt.MeasureText("Individual datasets (1 - 8)").Width, 5);
                graphics.Scale(effectiveSize, effectiveSize);
                datapointElements[26].Plot(graphics, presentationAttributes[26], null);
                graphics.Restore();
            }

            graphics.FillText(20 + 30 + 13 + fnt.MeasureText("Individual datasets (1 - 8)").Width, 8, "Full dataset (9)", fnt, Colours.Black, TextBaselines.Baseline);

            graphics.Restore();
            legendPage.Crop();
            return legendPage;
        }

        /// <summary>
        /// Creates the plot for Figure 2a.
        /// </summary>
        /// <returns>A <see cref="Page"/> on which the plot has been rendered.</returns>
        static Page CreateFigure2a()
        {
            // Datasets.
            string[] datasets = new string[9] { "Dataset1", "Dataset2", "Dataset3", "Dataset4", "Dataset5", "Dataset6", "Dataset7", "Dataset8", "Dataset9" };
            Colour[] datasetColours = new Colour[9]
            {
                Colour.FromRgb(160, 160, 160), // Phylo
                Colour.FromRgb(0, 158, 115), // Cyano
                Colour.FromRgb(0, 114, 178), // Rhodo
                Colour.FromRgb(230, 159, 0), // Prok
                Colour.FromRgb(213, 94, 0), // Collembola
                Colour.FromRgb(214, 199, 32), // Formicidae
                Colour.FromRgb(86, 180, 233), // Heliconiini
                Colour.FromRgb(204, 121, 167), // Silva
                Colour.FromRgb(0, 0, 0), // FullDataset
            };

            // Shapes for the points in the scatter plot.
            GraphicsPath circle = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close(); // Area: Math.PI
            GraphicsPath square = new GraphicsPath().MoveTo(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1).Close(); // Area: 4
            GraphicsPath diamond = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close(); // Area: 2
            GraphicsPath triangle = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close(); // Area: 2

            GraphicsPath circleTarget = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close().MoveTo(0, -1).LineTo(0, -2).MoveTo(0, 1).LineTo(0, 2).MoveTo(-1, 0).LineTo(-2, 0).MoveTo(1, 0).LineTo(2, 0); // Area: Math.PI
            GraphicsPath squareTarget = new GraphicsPath().MoveTo(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1).Close().MoveTo(0, -1).LineTo(0, -2.5).MoveTo(0, 1).LineTo(0, 2.5).MoveTo(-1, 0).LineTo(-2.5, 0).MoveTo(1, 0).LineTo(2.5, 0); // Area: 4
            GraphicsPath diamondTarget = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close().MoveTo(0, -1).LineTo(0, -2).MoveTo(0, 1).LineTo(0, 2).MoveTo(-1, 0).LineTo(-2, 0).MoveTo(1, 0).LineTo(2, 0); // Area: 2
            GraphicsPath triangleTarget = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close().MoveTo(0, -1).LineTo(0, -2).MoveTo(-1, 1).LineTo(-1.866, 1.5).MoveTo(1, 1).LineTo(1.866, 1.5); // Area: 2

            GraphicsPath star = new GraphicsPath();
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    star.LineTo(Math.Cos(-Math.PI / 10 + 2 * Math.PI / 10 * i), Math.Sin(-Math.PI / 10 + 2 * Math.PI / 10 * i));
                }
                else
                {
                    star.LineTo(Math.Cos(-Math.PI / 10 + 2 * Math.PI / 10 * i) * 0.5, Math.Sin(-Math.PI / 10 + 2 * Math.PI / 10 * i) * 0.5);
                }
            }
            star.Close();

            IDataPointElement[] datapointElements = new IDataPointElement[27]
            {
                new PathDataPointElement(circle),
                new PathDataPointElement(square),
                new PathDataPointElement(diamond),
                new PathDataPointElement(triangle),
                new PathDataPointElement(circle),
                new PathDataPointElement(square),
                new PathDataPointElement(diamond),
                new PathDataPointElement(triangle),
                new PathDataPointElement(),

                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(star),

                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(star)
            };

            // Size for each shape.
            double[] pointSizes = new double[] { 1,
                4.5, 3.5, 5, 5, 4.5, 3.5, 5, 5, 1,
                5, 4, 5, 5, 4.5, 3.5, 5, 5, 5,
                5, 4, 5, 5, 4.5, 3.5, 5, 5, 5 };

            PlotElementPresentationAttributes[] presentationAttributes = datasetColours.Select(x => BlendWithWhite(x, 0.5)).Select((x, i) => new PlotElementPresentationAttributes() { Fill = i < 4 ? x : null, Stroke = x, LineWidth = 2.5 / pointSizes[i + 1] })
                .Concat(datasetColours.Select((x, i) => new PlotElementPresentationAttributes() { Fill = null, Stroke = Colours.White, LineWidth = 5 / pointSizes[i + 10], LineCap = LineCaps.Round }))
                .Concat(datasetColours.Select((x, i) => new PlotElementPresentationAttributes() { Fill = i < 4 || i == 8 ? x : Colours.White, Stroke = x, LineWidth = 2.5 / pointSizes[i + 10], LineCap = LineCaps.Round })).ToArray();

            // Overall score for each dataset.
            double[][] overallScores = new double[datasets.Length][];

            // Score for each alignment in each dataset.
            double[][][] alignmentScores = new double[datasets.Length][][];

            // Name of each alignment in each dataest.
            string[][] alignmentNames = new string[datasets.Length][];

            // Overall model confidence.
            double overallC = double.NaN;
            // Model confidence for each alignment.
            List<double> alignmentC = new List<double>();

            // Print the header for Table S1
            Console.WriteLine("# Table S1");
            Console.WriteLine();
            Console.WriteLine("            \t      Overall     \t\t      Alignment-wise (1st quartile - median - 3rd quartile)");
            Console.WriteLine("Test dataset\t A  \tMCC \t C  \t\t        A         \t        MCC       \t        C         ");

            for (int i = 0; i < datasets.Length; i++)
            {
                // Read the scores for each dataset.
                ((double a, double mcc, double c) overall, Dictionary<string, (double a, double mcc, double c)> scores) = ReadScores(datasets[i]);

                // Store the overall MCC and accuracy.
                overallScores[i] = new double[] { overall.mcc, overall.a };

                // Store the model confidence score.
                alignmentC.AddRange(scores.Select(x => x.Value.c));
                if (i == datasets.Length - 1)
                {
                    overallC = overall.c;
                }

                // Store the MCC and A scores and the name of each alignment.
                alignmentScores[i] = new double[scores.Count][];
                alignmentNames[i] = new string[scores.Count];
                int j = 0;
                foreach (KeyValuePair<string, (double a, double mcc, double c)> kvp in scores)
                {
                    alignmentScores[i][j] = new double[] { kvp.Value.mcc, kvp.Value.a };
                    alignmentNames[i][j] = kvp.Key;
                    j++;
                }
            }

            // Print the scores for the full dataset.
            {
                Console.WriteLine("  {0}\t{1}\t{2}\t{3}\t\t{4} - {5} - {6}\t{7} - {8} - {9}\t{10} - {11} - {12}", ("9 (n=" + alignmentC.Count + ")").PadRight(12), overallScores[^1][1].ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), overallScores[^1][0].ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), overallC.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                alignmentScores.SelectMany(x => x.Select(y => y[1])).LowerQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), alignmentScores.SelectMany(x => x.Select(y => y[1])).Median().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), alignmentScores.SelectMany(x => x.Select(y => y[1])).UpperQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                alignmentScores.SelectMany(x => x.Select(y => y[0])).LowerQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), alignmentScores.SelectMany(x => x.Select(y => y[0])).Median().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), alignmentScores.SelectMany(x => x.Select(y => y[0])).UpperQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                alignmentC.LowerQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), alignmentC.Median().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), alignmentC.UpperQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            }

            // Create the scatter plot.
            Plot plot = Plot.Create.ScatterPlot(new double[][][] { new double[][] { new double[] { 0, 0 }, new double[] { 1, 1 } } }.Concat(alignmentScores).Concat(overallScores.Select(x => new double[][] { x })).Concat(overallScores.Select(x => new double[][] { x })).ToArray(),
            dataPresentationAttributes: new PlotElementPresentationAttributes[] { new PlotElementPresentationAttributes() }.Concat(presentationAttributes).ToArray(),
            dataPointElements: new IDataPointElement[] { new PathDataPointElement() }.Concat(datapointElements).ToArray(), xAxisTitle: "Matthews correlation coefficient", yAxisTitle: "Accuracy",
            pointSizes: pointSizes);

            plot.RemovePlotElement(plot.GetFirst<ScatterPoints<IReadOnlyList<double>>>());

            // Points where the model has a relatively low score.
            (int, string, double[])[] interestingPoints = alignmentScores.SelectMany((x, i) => x.Select((y, j) => (i, alignmentNames[i][j], y)).Where(z => z.Item3[0] < 0.7)).ToArray();

            // Position for the text labels.
            double[][] shifts = new double[6][]
            {
                new double[] { -0.025, -0.065 },
                new double[] { 0.025, -0.025 },
                new double[] { -0.025, -0.065 },
                new double[] { -0.23, -0.065 },
                new double[] { 0.025, 0.025 },
                new double[] { -0.22, 0 },
            };


            // Draw the text labels.
            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);

                for (int i = 0; i < interestingPoints.Length; i++)
                {
                    Point pt = coord.ToPlotCoordinates(new double[] { interestingPoints[i].Item3[0] + shifts[i][0], interestingPoints[i].Item3[1] + shifts[i][1] });

                    gpr.FillText(pt, interestingPoints[i].Item2, fnt, datasetColours[interestingPoints[i].Item1], TextBaselines.Middle);
                }
            }));

            // Highlight the area in Figure 2b.
            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Point bottomLeft = coord.ToPlotCoordinates(new double[] { 0.75, 0.9 });
                Point topRight = coord.ToPlotCoordinates(new double[] { 1, 1 });

                gpr.StrokeRectangle(bottomLeft.X, topRight.Y - 12, topRight.X - bottomLeft.X + 9, bottomLeft.Y - topRight.Y + 18, Colour.FromRgb(128, 128, 128), 2);
            }));

            return plot.Render();
        }

        /// <summary>
        /// Creates the plot for Figure 2a.
        /// </summary>
        /// <returns>A <see cref="Page"/> on which the plot has been rendered.</returns>
        static Page CreateFigure2b()
        {
            // Datasets.
            string[] datasets = new string[9] { "Dataset1", "Dataset2", "Dataset3", "Dataset4", "Dataset5", "Dataset6", "Dataset7", "Dataset8", "Dataset9" };
            Colour[] datasetColours = new Colour[9]
            {
                Colour.FromRgb(160, 160, 160), // Phylo
                Colour.FromRgb(0, 158, 115), // Cyano
                Colour.FromRgb(0, 114, 178), // Rhodo
                Colour.FromRgb(230, 159, 0), // Prok
                Colour.FromRgb(213, 94, 0), // Collembola
                Colour.FromRgb(214, 199, 32), // Formicidae
                Colour.FromRgb(86, 180, 233), // Heliconiini
                Colour.FromRgb(204, 121, 167), // Silva
                Colour.FromRgb(0, 0, 0), // FullDataset
            };

            // Shapes for the points in the scatter plot.
            GraphicsPath circle = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close(); // Area: Math.PI
            GraphicsPath square = new GraphicsPath().MoveTo(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1).Close(); // Area: 4
            GraphicsPath diamond = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close(); // Area: 2
            GraphicsPath triangle = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close(); // Area: 2

            GraphicsPath circleTarget = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close().MoveTo(0, -1).LineTo(0, -2).MoveTo(0, 1).LineTo(0, 2).MoveTo(-1, 0).LineTo(-2, 0).MoveTo(1, 0).LineTo(2, 0); // Area: Math.PI
            GraphicsPath squareTarget = new GraphicsPath().MoveTo(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1).Close().MoveTo(0, -1).LineTo(0, -2.5).MoveTo(0, 1).LineTo(0, 2.5).MoveTo(-1, 0).LineTo(-2.5, 0).MoveTo(1, 0).LineTo(2.5, 0); // Area: 4
            GraphicsPath diamondTarget = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close().MoveTo(0, -1).LineTo(0, -2).MoveTo(0, 1).LineTo(0, 2).MoveTo(-1, 0).LineTo(-2, 0).MoveTo(1, 0).LineTo(2, 0); // Area: 2
            GraphicsPath triangleTarget = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close().MoveTo(0, -1).LineTo(0, -2).MoveTo(-1, 1).LineTo(-1.866, 1.5).MoveTo(1, 1).LineTo(1.866, 1.5); // Area: 2

            GraphicsPath star = new GraphicsPath();
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    star.LineTo(Math.Cos(-Math.PI / 10 + 2 * Math.PI / 10 * i), Math.Sin(-Math.PI / 10 + 2 * Math.PI / 10 * i));
                }
                else
                {
                    star.LineTo(Math.Cos(-Math.PI / 10 + 2 * Math.PI / 10 * i) * 0.5, Math.Sin(-Math.PI / 10 + 2 * Math.PI / 10 * i) * 0.5);
                }
            }
            star.Close();

            IDataPointElement[] datapointElements = new IDataPointElement[27]
            {
                new PathDataPointElement(circle),
                new PathDataPointElement(square),
                new PathDataPointElement(diamond),
                new PathDataPointElement(triangle),
                new PathDataPointElement(circle),
                new PathDataPointElement(square),
                new PathDataPointElement(diamond),
                new PathDataPointElement(triangle),
                new PathDataPointElement(),

                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(star),

                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(circleTarget),
                new PathDataPointElement(squareTarget),
                new PathDataPointElement(diamondTarget),
                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(star)
            };

            // Size for each shape.
            double[] pointSizes = new double[] { 1,
                4.5, 3.5, 5, 5, 4.5, 3.5, 5, 5, 1,
                5, 4, 5, 5, 4.5, 3.5, 5, 5, 5,
                5, 4, 5, 5, 4.5, 3.5, 5, 5, 5 };

            PlotElementPresentationAttributes[] presentationAttributes = datasetColours.Select(x => BlendWithWhite(x, 0.5)).Select((x, i) => new PlotElementPresentationAttributes() { Fill = i < 4 ? x : null, Stroke = x, LineWidth = 2.5 / pointSizes[i + 1] })
                .Concat(datasetColours.Select((x, i) => new PlotElementPresentationAttributes() { Fill = null, Stroke = Colours.White, LineWidth = 5 / pointSizes[i + 10], LineCap = LineCaps.Round }))
                .Concat(datasetColours.Select((x, i) => new PlotElementPresentationAttributes() { Fill = i < 4 || i == 8 ? x : Colours.White, Stroke = x, LineWidth = 2.5 / pointSizes[i + 10], LineCap = LineCaps.Round })).ToArray();

            // Overall score for each dataset.
            double[][] overallScores = new double[datasets.Length][];

            // Score for each alignment in each dataset.
            double[][][] alignmentScores = new double[datasets.Length][][];

            // Name of each alignment in each dataest.
            string[][] alignmentNames = new string[datasets.Length][];

            // Overall model confidence.
            double overallC = double.NaN;
            // Model confidence for each alignment.
            List<double> alignmentC = new List<double>();

            for (int i = 0; i < datasets.Length; i++)
            {
                // Read the scores for each dataset.
                ((double a, double mcc, double c) overall, Dictionary<string, (double a, double mcc, double c)> scores) = ReadScores(datasets[i]);

                // Store the overall MCC and accuracy.
                overallScores[i] = new double[] { overall.mcc, overall.a };

                // Store the model confidence score.
                alignmentC.AddRange(scores.Select(x => x.Value.c));
                if (i == datasets.Length - 1)
                {
                    overallC = overall.c;
                }

                // Store the MCC and A scores and the name of each alignment.
                alignmentScores[i] = new double[scores.Count][];
                alignmentNames[i] = new string[scores.Count];
                int j = 0;
                foreach (KeyValuePair<string, (double a, double mcc, double c)> kvp in scores)
                {
                    alignmentScores[i][j] = new double[] { kvp.Value.mcc, kvp.Value.a };
                    alignmentNames[i][j] = kvp.Key;
                    j++;
                }
            }

            // Create the scatter plot.
            Plot plot = Plot.Create.ScatterPlot(new double[][][] { new double[][] { new double[] { 0.75, 0.9 }, new double[] { 1, 1 } } }.Concat(alignmentScores.Select(x => x.Where(y => y[0] >= 0.75 && y[1] >= 0.9).ToArray())).Concat(overallScores.Select(x => new double[][] { x })).Concat(overallScores.Select(x => new double[][] { x })).ToArray(),
            dataPresentationAttributes: new PlotElementPresentationAttributes[] { new PlotElementPresentationAttributes() }.Concat(presentationAttributes).ToArray(),
            dataPointElements: new IDataPointElement[] { new PathDataPointElement() }.Concat(datapointElements).ToArray(), xAxisTitle: "Matthews correlation coefficient", yAxisTitle: "Accuracy",
            pointSizes: pointSizes);

            plot.GetFirst<ContinuousAxisLabels>().TextFormat = (x, i) => FormattedText.Format(x[0].ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), FontFamily.StandardFontFamilies.Helvetica, 12);
            plot.GetAll<ContinuousAxisLabels>().ElementAt(1).TextFormat = (x, i) => FormattedText.Format(x[1].ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), FontFamily.StandardFontFamilies.Helvetica, 12);

            plot.RemovePlotElement(plot.GetFirst<ScatterPoints<IReadOnlyList<double>>>());

            return plot.Render();
        }

        /// <summary>
        /// Read the scores of the default AliFilter model against the specified dataset.
        /// </summary>
        /// <param name="dataset">The dataset.</param>
        /// <returns>The scores of the default AliFilter model against the specified dataset.</returns>
        private static ((double a, double mcc, double c), Dictionary<string, (double a, double mcc, double c)>) ReadScores(string dataset)
        {
            using (StreamReader sr = new StreamReader("../../../Data/" + dataset + ".txt"))
            {
                string line = sr.ReadLine();

                string[] splitLine = line.Split("\t");

                (double a, double mcc, double c) overall = (double.Parse(splitLine[1], System.Globalization.CultureInfo.InvariantCulture), double.Parse(splitLine[2], System.Globalization.CultureInfo.InvariantCulture), double.Parse(splitLine[3], System.Globalization.CultureInfo.InvariantCulture));

                Dictionary<string, (double, double, double)> scores = new Dictionary<string, (double, double, double)>();

                line = sr.ReadLine();

                while (line != null)
                {
                    splitLine = line.Split("\t");

                    scores.Add(splitLine[0], (double.Parse(splitLine[1], System.Globalization.CultureInfo.InvariantCulture), double.Parse(splitLine[2], System.Globalization.CultureInfo.InvariantCulture), double.Parse(splitLine[3], System.Globalization.CultureInfo.InvariantCulture)));

                    line = sr.ReadLine();
                }

                if (dataset != "Dataset9")
                {
                    Console.WriteLine("  {0}\t{1}\t{2}\t{3}\t\t{4} - {5} - {6}\t{7} - {8} - {9}\t{10} - {11} - {12}", (dataset.Replace("Dataset", "") + " (n=" + scores.Count + ")").PadRight(12), overall.a.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), overall.mcc.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), overall.c.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        scores.Select(x => x.Value.Item1).LowerQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), scores.Select(x => x.Value.Item1).Median().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), scores.Select(x => x.Value.Item1).UpperQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        scores.Select(x => x.Value.Item2).LowerQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), scores.Select(x => x.Value.Item2).Median().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), scores.Select(x => x.Value.Item2).UpperQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        scores.Select(x => x.Value.Item3).LowerQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), scores.Select(x => x.Value.Item3).Median().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), scores.Select(x => x.Value.Item3).UpperQuartile().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                }
                return (overall, scores);
            }
        }

        /// <summary>
        /// Blend a colour with white.
        /// </summary>
        /// <param name="col">The colour to blend.</param>
        /// <param name="percentage">The colour intensity (1 is white, 0 is col).</param>
        /// <returns>The blended colour.</returns>
        private static Colour BlendWithWhite(Colour col, double percentage)
        {
            return Colour.FromRgb(col.R * percentage + 1 - percentage, col.G * percentage + 1 - percentage, col.B * percentage + 1 - percentage);
        }
    }
}
