/*
    AliFilter: A Machine Learning Approach to Alignment Filtering

    by Giorgio Bianchini, Rui Zhu, Francesco Cicconardi, Edmund RR Moody

    Source code for manuscript figures.

    Copyright (C) 2026  Giorgio Bianchini
 
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

using VectSharp;
using VectSharp.PDF;
using VectSharp.Plots;
using VectSharp.Raster;
using VectSharp.SVG;

namespace Figure2_S3_Table_S1
{
    internal class FigureS3
    {
        public static void CreateFigureS3()
        {
            // Create the plots.
            Page figS3a = CreateFigureS3a();
            Page figS3b = CreateFigureS3b();
            Page legend = CreateLegend();

            // Resize to a width of 17cm.
            double scalingFactor = Math.Min(235 / figS3a.Width, 235 / figS3b.Width);
            Page resizedPage = new Page(482, scalingFactor * Math.Max(figS3a.Height, figS3b.Height) + legend.Height + 20);
            resizedPage.Background = Colours.White;

            Font partLetterFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 12);

            // Draw the legend.
            resizedPage.Graphics.DrawGraphics(resizedPage.Width * 0.5 - legend.Width * 0.5, 0, legend.Graphics);

            resizedPage.Graphics.Translate(0, legend.Height + 20);

            // Draw part a)
            resizedPage.Graphics.FillText(0, 0, "a)", partLetterFont, Colours.Black);
            resizedPage.Graphics.Save();
            resizedPage.Graphics.Scale(scalingFactor, scalingFactor);
            resizedPage.Graphics.DrawGraphics(235 * 0.5 - figS3a.Width * scalingFactor * 0.5, scalingFactor * Math.Max(figS3a.Height, figS3b.Height) * 0.5 - figS3a.Height * scalingFactor * 0.5, figS3a.Graphics);
            resizedPage.Graphics.Restore();

            resizedPage.Graphics.Translate(247, 0);

            // Draw part b)
            resizedPage.Graphics.FillText(0, 0, "b)", partLetterFont, Colours.Black);
            resizedPage.Graphics.Save();
            resizedPage.Graphics.Scale(scalingFactor, scalingFactor);
            resizedPage.Graphics.DrawGraphics(235 * 0.5 - figS3b.Width * scalingFactor * 0.5, scalingFactor * Math.Max(figS3a.Height, figS3b.Height) * 0.5 - figS3b.Height * scalingFactor * 0.5, figS3b.Graphics);
            resizedPage.Graphics.Restore();

            Document doc = new Document();
            doc.Pages.Add(resizedPage);

            resizedPage.SaveAsSVG("Figure_S3.svg");
            resizedPage.SaveAsSVG("Figure_S3.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_S3.pdf");
            resizedPage.SaveAsPNG("Figure_S3.png", 600.0 / 72);
        }

        /// <summary>
        /// Creates the plot for Figure S3a.
        /// </summary>
        /// <returns>A <see cref="Page"/> on which the plot has been rendered.</returns>
        static Page CreateFigureS3a()
        {
            // Datasets.
            string[] datasets = new string[2] { "Dataset12", "Dataset13" };
            Colour[] datasetColours = new Colour[2]
            {
                Colour.FromRgb(238, 136, 102), // Mitochondrial
                Colour.FromRgb(187, 204, 51), // Viral
            };

            // Shapes for the points in the scatter plot.
            GraphicsPath triangle = new GraphicsPath().MoveTo(0, 1).LineTo(-1, -1).LineTo(1, -1).Close(); // Area: 2

            GraphicsPath triangleTarget = new GraphicsPath().MoveTo(0, 1).LineTo(-1, -1).LineTo(1, -1).Close().MoveTo(0, 1).LineTo(0, 2).MoveTo(-1, -1).LineTo(-1.866, -1.5).MoveTo(1, -1).LineTo(1.866, -1.5); // Area: 2

            IDataPointElement[] datapointElements = new IDataPointElement[6]
            {
                new PathDataPointElement(triangle),
                new PathDataPointElement(triangle),

                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(triangleTarget),

                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(triangleTarget),
            };

            // Size for each shape.
            double[] pointSizes = new double[] { 5, 5, 5, 5 };

            PlotElementPresentationAttributes[] presentationAttributes = datasetColours.Select(x => Program.BlendWithWhite(x, 0.5)).Select((x, i) => new PlotElementPresentationAttributes() { Fill = i %2 == 0 ? x : null, Stroke = x, LineWidth = 2.5 / pointSizes[i + 1] })
                .Concat(datasetColours.Select((x, i) => new PlotElementPresentationAttributes() { Fill = null, Stroke = Colours.White, LineWidth = 5 / pointSizes[i + 2], LineCap = LineCaps.Round }))
                .Concat(datasetColours.Select((x, i) => new PlotElementPresentationAttributes() { Fill = i % 2 == 0 ? x : Colours.White, Stroke = x, LineWidth = 2.5 / pointSizes[i + 2], LineCap = LineCaps.Round })).ToArray();

            // Overall score for each dataset.
            double[][] overallScores = new double[datasets.Length][];

            // Score for each alignment in each dataset.
            double[][][] alignmentScores = new double[datasets.Length][][];

            // Name of each alignment in each dataest.
            string[][] alignmentNames = new string[datasets.Length][];

            for (int i = 0; i < datasets.Length; i++)
            {
                // Read the scores for each dataset.
                ((double a, double mcc, double c, double auc) overall, Dictionary<string, (double a, double mcc, double c, double auc)> scores) = Program.ReadScores(datasets[i], true);

                // Store the overall MCC and accuracy.
                overallScores[i] = new double[] { overall.mcc, overall.a };

                // Store the MCC and A scores and the name of each alignment.
                alignmentScores[i] = new double[scores.Count][];
                alignmentNames[i] = new string[scores.Count];
                int j = 0;
                foreach (KeyValuePair<string, (double a, double mcc, double c, double auc)> kvp in scores)
                {
                    alignmentScores[i][j] = new double[] { kvp.Value.mcc, kvp.Value.a };
                    alignmentNames[i][j] = kvp.Key;
                    j++;
                }
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

            // Highlight the area in Figure S3b.
            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Point bottomLeft = coord.ToPlotCoordinates(new double[] { 0.75, 0.9 });
                Point topRight = coord.ToPlotCoordinates(new double[] { 1, 1 });

                gpr.StrokeRectangle(bottomLeft.X, topRight.Y - 12, topRight.X - bottomLeft.X + 9, bottomLeft.Y - topRight.Y + 18, Colour.FromRgb(128, 128, 128), 2);
            }));

            return plot.Render();
        }

        /// <summary>
        /// Creates the plot for Figure S3a.
        /// </summary>
        /// <returns>A <see cref="Page"/> on which the plot has been rendered.</returns>
        static Page CreateFigureS3b()
        {
            // Datasets.
            string[] datasets = new string[2] { "Dataset12", "Dataset13" };
            Colour[] datasetColours = new Colour[2]
            {
                Colour.FromRgb(238, 136, 102), // Mitochondrial
                Colour.FromRgb(187, 204, 51), // Viral
            };

            // Shapes for the points in the scatter plot.
            GraphicsPath triangle = new GraphicsPath().MoveTo(0, 1).LineTo(-1, -1).LineTo(1, -1).Close(); // Area: 2

            GraphicsPath triangleTarget = new GraphicsPath().MoveTo(0, 1).LineTo(-1, -1).LineTo(1, -1).Close().MoveTo(0, 1).LineTo(0, 2).MoveTo(-1, -1).LineTo(-1.866, -1.5).MoveTo(1, -1).LineTo(1.866, -1.5); // Area: 2

            IDataPointElement[] datapointElements = new IDataPointElement[6]
            {
                new PathDataPointElement(triangle),
                new PathDataPointElement(triangle),

                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(triangleTarget),

                new PathDataPointElement(triangleTarget),
                new PathDataPointElement(triangleTarget),
            };

            // Size for each shape.
            double[] pointSizes = new double[] { 5, 5, 5, 5 };

            PlotElementPresentationAttributes[] presentationAttributes = datasetColours.Select(x => Program.BlendWithWhite(x, 0.5)).Select((x, i) => new PlotElementPresentationAttributes() { Fill = i % 2 == 0 ? x : null, Stroke = x, LineWidth = 2.5 / pointSizes[i + 1] })
                .Concat(datasetColours.Select((x, i) => new PlotElementPresentationAttributes() { Fill = null, Stroke = Colours.White, LineWidth = 5 / pointSizes[i + 2], LineCap = LineCaps.Round }))
                .Concat(datasetColours.Select((x, i) => new PlotElementPresentationAttributes() { Fill = i % 2 == 0 ? x : Colours.White, Stroke = x, LineWidth = 2.5 / pointSizes[i + 2], LineCap = LineCaps.Round })).ToArray();

            // Overall score for each dataset.
            double[][] overallScores = new double[datasets.Length][];

            // Score for each alignment in each dataset.
            double[][][] alignmentScores = new double[datasets.Length][][];

            // Name of each alignment in each dataest.
            string[][] alignmentNames = new string[datasets.Length][];

            for (int i = 0; i < datasets.Length; i++)
            {
                // Read the scores for each dataset.
                ((double a, double mcc, double c, double auc) overall, Dictionary<string, (double a, double mcc, double c, double auc)> scores) = Program.ReadScores(datasets[i], false);

                // Store the overall MCC and accuracy.
                overallScores[i] = new double[] { overall.mcc, overall.a };

                // Store the MCC and A scores and the name of each alignment.
                alignmentScores[i] = new double[scores.Count][];
                alignmentNames[i] = new string[scores.Count];
                int j = 0;
                foreach (KeyValuePair<string, (double a, double mcc, double c, double auc)> kvp in scores)
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
        /// Creates the figure legend.
        /// </summary>
        /// <returns>A <see cref="Page"/> on which the legend has been rendered.</returns>
        static Page CreateLegend()
        {
            // Datasets.
            string[] datasetNames = new string[3] { "Dataset 12", "Dataset 13", "Individual datasets (12 - 13)" };
            Colour[] datasetColours = new Colour[3]
            {
                Colour.FromRgb(238, 136, 102), // Mitochondrial
                Colour.FromRgb(187, 204, 51), // Viral
                Colours.Black
            };

            // Shapes for the points in the scatter plot.
            GraphicsPath triangle = new GraphicsPath().MoveTo(0, 1).LineTo(-1, -1).LineTo(1, -1).Close(); // Area: 2

            GraphicsPath triangleTarget = new GraphicsPath().MoveTo(0, 1).LineTo(-1, -1).LineTo(1, -1).Close().MoveTo(0, 1).LineTo(0, 2).MoveTo(-1, -1).LineTo(-1.866, -1.5).MoveTo(1, -1).LineTo(1.866, -1.5); // Area: 2

            IDataPointElement[] datapointElements = new IDataPointElement[3]
            {
                new PathDataPointElement(triangle),
                new PathDataPointElement(triangle),
                new PathDataPointElement(triangleTarget),
            };

            // Size for each shape.
            double[] pointSizes = new double[] { 5, 5, 5, 5 };

            PlotElementPresentationAttributes[] presentationAttributes = datasetColours.Select((x, i) => i == 2 ? x : Program.BlendWithWhite(x, 0.5)).Select((x, i) => new PlotElementPresentationAttributes() { Fill = i %2 == 0 ? x : null, Stroke = x, LineWidth = 2.5 / pointSizes[i + 1] }).ToArray();

            Page legendPage = new Page(1, 1);

            Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);
            Font fntBold = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 12);

            Graphics graphics = legendPage.Graphics;

            double column1Width = fnt.MeasureText(datasetNames[0]).Width + 10;
            double column2Width = fnt.MeasureText(datasetNames[1]).Width + 10;
            double column3Width = fnt.MeasureText(datasetNames[2]).Width + 12;

            graphics.Save();

            for (int i = 0; i < 1; i++)
            {
                double effectiveSize = pointSizes[i + 1] * 0.65;

                graphics.Save();
                graphics.Translate(2.75, 18);
                graphics.Scale(effectiveSize, effectiveSize);
                datapointElements[i].Plot(graphics, presentationAttributes[i], null);
                graphics.Restore();

                graphics.FillText(10, 22, datasetNames[i], fnt, Colours.Black, TextBaselines.Baseline);
            }

            graphics.Translate(column1Width + 20, 0);

            for (int i = 1; i < 2; i++)
            {
                double effectiveSize = pointSizes[i + 1] * 0.65;

                graphics.Save();
                graphics.Translate(2.75, 18);
                graphics.Scale(effectiveSize, effectiveSize);
                datapointElements[i].Plot(graphics, presentationAttributes[i], null);
                graphics.Restore();

                graphics.FillText(10, 22, datasetNames[i], fnt, Colours.Black, TextBaselines.Baseline);
            }

            graphics.Translate(column2Width + 30, 0);

            for (int i = 2; i < 3; i++)
            {
                double effectiveSize = pointSizes[i + 1] * 0.65;

                graphics.Save();
                graphics.Translate(2.75, 18);
                graphics.Scale(effectiveSize, effectiveSize);
                datapointElements[i].Plot(graphics, presentationAttributes[i], null);
                graphics.Restore();

                graphics.FillText(12, 22, "Individual datasets (12 - 13)", fnt, Colours.Black, TextBaselines.Baseline);
            }

            
            graphics.Restore();

            legendPage.Crop();
            return legendPage;
        }
    }


}
