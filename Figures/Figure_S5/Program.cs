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

using System.Data;
using System.Diagnostics;
using VectSharp;
using VectSharp.PDF;
using VectSharp.Plots;
using VectSharp.Raster;
using VectSharp.SVG;

namespace Figure_S5
{
    internal class Program
    {
        // Names of alignments used for training, validation, and testing.
        private static readonly HashSet<string> TrainingAlignments = File.ReadAllLines("../../../Data/training.txt").ToHashSet();
        private static readonly HashSet<string> ValidationAlignments = File.ReadAllLines("../../../Data/validation.txt").ToHashSet();
        private static readonly HashSet<string> TestAlignments = File.ReadAllLines("../../../Data/test.txt").ToHashSet();

        static Program()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        static void Main(string[] args)
        {
            // Create the figure parts.
            Page figureS5a = CreateFigurePart("windows", "full", true, false, false);
            Page figureS5b = CreateFigurePart("linux", "full", false, false, false);
            Page figureS5c = CreateFigurePart("mac", "full", false, true, false);

            Page figureS5d = CreateFigurePart("windows", "dataset4", true, false, true);
            Page figureS5e = CreateFigurePart("linux", "dataset4", false, false, true);
            Page figureS5f = CreateFigurePart("mac", "dataset4", false, true, true);

            // Assemble the final figure.
            Page figureS5 = new Page(2250, 1060);

            figureS5.Graphics.Translate(50, 80);

            figureS5.Graphics.DrawGraphics(0, 30, figureS5a.Graphics);
            figureS5.Graphics.DrawGraphics(700, 30, figureS5b.Graphics);
            figureS5.Graphics.DrawGraphics(1400, 30, figureS5c.Graphics);

            figureS5.Graphics.DrawGraphics(0, 580, figureS5d.Graphics);
            figureS5.Graphics.DrawGraphics(700, 580, figureS5e.Graphics);
            figureS5.Graphics.DrawGraphics(1400, 580, figureS5f.Graphics);

            figureS5.Graphics.StrokePath(new GraphicsPath().MoveTo(750, -80).LineTo(750, 980), Colour.FromRgb(128, 128, 128), 4);
            figureS5.Graphics.StrokePath(new GraphicsPath().MoveTo(1450, -80).LineTo(1450, 980), Colour.FromRgb(128, 128, 128), 4);

            Font figurePartFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 36);

            figureS5.Graphics.FillText(70, 110, "a)", figurePartFont, Colours.Black, TextBaselines.Baseline);
            figureS5.Graphics.FillText(770, 110, "b)", figurePartFont, Colours.Black, TextBaselines.Baseline);
            figureS5.Graphics.FillText(1470, 110, "c)", figurePartFont, Colours.Black, TextBaselines.Baseline);

            figureS5.Graphics.FillText(70, 610, "d)", figurePartFont, Colours.Black, TextBaselines.Baseline);
            figureS5.Graphics.FillText(770, 610, "e)", figurePartFont, Colours.Black, TextBaselines.Baseline);
            figureS5.Graphics.FillText(1470, 610, "f)", figurePartFont, Colours.Black, TextBaselines.Baseline);

            string label1 = "Intel Core i9-13900KF (2022, 8P+16E/32T)";
            string label2 = "Intel Core i9-7980XE (2017, 18P/36T)";
            string label3 = "Apple M1 (2020, 4P+4E/8T)";
            figureS5.Graphics.FillText(380 - figurePartFont.MeasureText(label1).Width * 0.5, 30, label1, figurePartFont, Colours.Black, TextBaselines.Baseline);
            figureS5.Graphics.FillText(1100 - figurePartFont.MeasureText(label2).Width * 0.5, 30, label2, figurePartFont, Colours.Black, TextBaselines.Baseline);
            figureS5.Graphics.FillText(1790 - figurePartFont.MeasureText(label3).Width * 0.5, 30, label3, figurePartFont, Colours.Black, TextBaselines.Baseline);

            string os1 = "Windows 11";
            string os2 = "Ubuntu 24.04";
            string os3 = "macOS Sonoma 14.6.1";

            Page os1Icon = GetOSIcon("windows");
            Page os2Icon = GetOSIcon("linux");
            Page os3Icon = GetOSIcon("mac");

            figureS5.Graphics.FillText(380 - (figurePartFont.MeasureText(os1).Width + os1Icon.Width + 10) * 0.5 + os1Icon.Width + 10, -25, os1, figurePartFont, Colours.Black, TextBaselines.Baseline);
            figureS5.Graphics.FillText(1100 - (figurePartFont.MeasureText(os2).Width + os2Icon.Width + 10) * 0.5 + os2Icon.Width + 10, -25, os2, figurePartFont, Colours.Black, TextBaselines.Baseline);
            figureS5.Graphics.FillText(1790 - (figurePartFont.MeasureText(os3).Width + os3Icon.Width + 10) * 0.5 + os3Icon.Width + 10, -25, os3, figurePartFont, Colours.Black, TextBaselines.Baseline);

            figureS5.Graphics.DrawGraphics(380 - (figurePartFont.MeasureText(os1).Width + os1Icon.Width + 10) * 0.5, -40 - os1Icon.Height * 0.5, os1Icon.Graphics);
            figureS5.Graphics.DrawGraphics(1100 - (figurePartFont.MeasureText(os2).Width + os2Icon.Width + 10) * 0.5, -40 - os1Icon.Height * 0.5, os2Icon.Graphics);
            figureS5.Graphics.DrawGraphics(1790 - (figurePartFont.MeasureText(os3).Width + os3Icon.Width + 10) * 0.5, -40 - os1Icon.Height * 0.5, os3Icon.Graphics);

            figureS5.Graphics.Save();
            figureS5.Graphics.Translate(-10, 335);
            figureS5.Graphics.Rotate(-Math.PI / 2);
            figureS5.Graphics.FillText(-figurePartFont.MeasureText("Dataset 9").Width * 0.5, 0, "Dataset 9", figurePartFont, Colours.Black, TextBaselines.Baseline);
            figureS5.Graphics.Restore();

            figureS5.Graphics.Save();
            figureS5.Graphics.Translate(-10, 785);
            figureS5.Graphics.Rotate(-Math.PI / 2);
            figureS5.Graphics.FillText(-figurePartFont.MeasureText("Dataset 4").Width * 0.5, 0, "Dataset 4", figurePartFont, Colours.Black, TextBaselines.Baseline);
            figureS5.Graphics.Restore();

            // Resize to a width of 25.7cm.
            Page finalFigureS5 = new Page(729, figureS5.Height * 729 / figureS5.Width);
            finalFigureS5.Background = Colours.White;
            finalFigureS5.Graphics.Scale(729 / figureS5.Width, 729 / figureS5.Width);
            finalFigureS5.Graphics.DrawGraphics(0, 0, figureS5.Graphics);

            Document doc = new Document();
            doc.Pages.Add(finalFigureS5);

            finalFigureS5.SaveAsSVG("Figure_S5.svg");
            finalFigureS5.SaveAsSVG("Figure_S5.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_S5.pdf");
            finalFigureS5.SaveAsPNG("Figure_S5.png", 600.0 / 72);
        }

        static Page CreateFigurePart(string os, string dataset, bool labelsLeft, bool labelsRight, bool lowerMax)
        {
            // Read the runtimes of the feature computation.
            (Dictionary<string, double> overallRuntimes, Dictionary<string, List<double>[]> individualRuntimes) = ReadFeatureRuntimeFile(os, dataset);

            // Get the parallelisation strategies sorted by number of threads per job.
            string[] parallelisationStrategies = overallRuntimes.Keys.OrderBy(x => int.Parse(x.Substring(0, x.IndexOf("x")))).ToArray();

            Func<double, double> dataTransform = x => x;

            // Extract the overall runtimes for the box plots.
            double[][] runtimeBoxPlots = parallelisationStrategies.SelectMany(x => individualRuntimes[x].Select(y => y.Select(dataTransform).ToArray()).ToArray()).ToArray();

            Colour trainingColour = Colour.FromRgb(100, 143, 255);
            Colour validationColour = Colour.FromRgb(255, 176, 0);
            Colour testColour = Colour.FromRgb(220, 38, 127);
            Font labelFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 24);

            PlotElementPresentationAttributes[] presentationAttributes = new PlotElementPresentationAttributes[]
            {
                new PlotElementPresentationAttributes() { Fill = trainingColour, Stroke = trainingColour, LineWidth = 2 },
                new PlotElementPresentationAttributes() { Fill = validationColour, Stroke = validationColour, LineWidth = 2 },
                new PlotElementPresentationAttributes() { Fill = testColour, Stroke = testColour, LineWidth = 2 }
            };

            // Coordinate system.
            TimeCoordinateSystem coords = new TimeCoordinateSystem(0, 1, 120, 2.5 * 60 * 60 * 1000, 350, 200);

            // Start by creating the box plots.
            Plot plot = Plot.Create.BoxPlot(runtimeBoxPlots, useNotches: false, outlierPointElement: new PathDataPointElement(new VectSharp.GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close()), boxPresentationAttributes: presentationAttributes, dataRangeMin: dataTransform(1), dataRangeMax: dataTransform((lowerMax ? 1 : 3) * 60 * 60 * 1000), coordinateSystem: coords);

            // Horizontal coordinates of the central box plots.
            double[] xTicks = new double[parallelisationStrategies.Length];

            // Reposition the box plots closer together.
            {
                int i = 0;
                foreach (BoxPlot box in plot.GetAll<BoxPlot>())
                {
                    box.Width *= 0.4;

                    ((double[])box.Position)[0] -= 5 + (i / 3) * 10;
                    box.BoxPresentationAttributes.LineWidth = 2;

                    switch (i % 3)
                    {
                        case 0:
                            ((double[])box.Position)[0] += 5;
                            break;
                        case 1:
                            xTicks[i / 3] = ((double[])box.Position)[0];
                            break;
                        case 2:
                            ((double[])box.Position)[0] -= 5;
                            break;
                    }

                    i++;
                }
            }

            // Replace the outlier display with swarm plots.
            {
                foreach (ScatterPoints<IReadOnlyList<double>> outliers in plot.GetAll<ScatterPoints<IReadOnlyList<double>>>().Skip(1).ToList())
                {
                    plot.RemovePlotElement(outliers);

                    if (outliers.Data.Any())
                    {
                        int i = (int)(Math.Round((outliers.Data.First()[0] - 5) / 11));

                        Console.WriteLine(outliers.Data.First()[0]);

                        double x = outliers.Data.First()[0] - (5 + (i / 3) * 10);

                        switch (i % 3)
                        {
                            case 0:
                                x += 5;
                                break;
                            case 1:
                                break;
                            case 2:
                                x -= 5;
                                break;
                        }

                        Swarm outlierSwarm = new Swarm(new double[] { x, 0 }, new double[] { 0, 1 }, outliers.Data.Select(x => x[1]), plot.GetFirst<IContinuousInvertibleCoordinateSystem>());
                        outlierSwarm.PresentationAttributes = outliers.PresentationAttributes;
                        outlierSwarm.PointMargin = -1;

                        plot.AddPlotElement(outlierSwarm);
                    }
                }
            }

            // Remove unnecessary plot elements.
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxis>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxis>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxisLabels>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxisTicks>());
            plot.RemovePlotElement(plot.GetFirst<Grid>());
            plot.RemovePlotElement(plot.GetFirst<ScatterPoints<IReadOnlyList<double>>>());

            // Temporarily remove all plot elements.
            IPlotElement[] plotElements = plot.PlotElements.ToArray();
            foreach (IPlotElement pe in plotElements)
            {
                plot.RemovePlotElement(pe);
            }

            // Add the grid and axis labels.
            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>((ICoordinateSystem<IReadOnlyList<double>>)plotElements[0].CoordinateSystem, (gpr, coord) =>
            {
                double minX = -5;
                double maxX = 220;

                Point bottomLeft = coord.ToPlotCoordinates(new double[] { minX, 0 });
                Point topRight = coord.ToPlotCoordinates(new double[] { maxX, (lowerMax ? 1.5 : 3) * 60 * 60 * 1000 });


                gpr.FillRectangle(bottomLeft.X - 60, topRight.Y, topRight.X - bottomLeft.X + 120, bottomLeft.Y - topRight.Y + 20, Colour.FromRgba(0, 0, 0, 0));

                double[] yTicks;
                string[] yLabels;

                if (!lowerMax)
                {
                    yTicks = new double[] { 1, 500, 1000, 30000, 60000, 1800000, 3600000, 5400000, 7200000 };
                    yLabels = new string[] { "1ms", "0.5s", "1s", "30s", "1m", "30m", "1h", "1.5h", "2h" };
                }
                else
                {
                    yTicks = new double[] { 1, 500, 1000, 30000, 60000, 1800000, 3600000 };
                    yLabels = new string[] { "1ms", "0.5s", "1s", "30s", "1m", "30m", "1h" };
                }

                for (int i = 0; i < yTicks.Length; i++)
                {
                    Point p1;
                    Point p2;

                    if (labelsLeft)
                    {
                        p1 = coord.ToPlotCoordinates(new double[] { minX, yTicks[i] });
                    }
                    else
                    {
                        p1 = coord.ToPlotCoordinates(new double[] { minX - 10, yTicks[i] });
                    }

                    if (labelsRight)
                    {
                        p2 = coord.ToPlotCoordinates(new double[] { maxX, yTicks[i] });
                    }
                    else
                    {
                        p2 = coord.ToPlotCoordinates(new double[] { maxX + 10, yTicks[i] });
                    }

                    gpr.StrokePath(new GraphicsPath().MoveTo(p1).LineTo(p2), Colour.FromRgb(200, 200, 200), 2);

                    if (labelsLeft)
                    {
                        gpr.FillText(p1.X - labelFont.MeasureText(yLabels[i]).Width - 10, p1.Y, yLabels[i], labelFont, Colours.Black, TextBaselines.Middle);
                    }

                    if (labelsRight)
                    {
                        gpr.FillText(p2.X + 10, p2.Y, yLabels[i], labelFont, Colours.Black, TextBaselines.Middle);
                    }
                }
            }));

            // Add back the removed plot elements.
            plot.AddPlotElements(plotElements);

            // Add a data line representing the total time spent computing features.
            plot.AddPlotElement(new DataLine<IReadOnlyList<double>>(parallelisationStrategies.Select((x, i) => new double[] { xTicks[i], dataTransform(overallRuntimes[x]) }), plot.GetFirst<IContinuousCoordinateSystem>()) { PresentationAttributes = new PlotElementPresentationAttributes() { Stroke = Colour.FromRgb(128, 128, 128), LineWidth = 2 } });
            plot.AddPlotElement(new ScatterPoints<IReadOnlyList<double>>(parallelisationStrategies.Select((x, i) => new double[] { xTicks[i], dataTransform(overallRuntimes[x]) }), plot.GetFirst<IContinuousCoordinateSystem>()) { PresentationAttributes = new PlotElementPresentationAttributes() { Stroke = null, Fill = Colour.FromRgb(128, 128, 128) }, Size = 6 });

            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Point p1 = coord.ToPlotCoordinates(new double[] { 0, 0 });
                Point p2 = coord.ToPlotCoordinates(new double[] { 0, 0 }) + new Point(0, 10);
                Point p3 = coord.ToPlotCoordinates(new double[] { 91, 0 }) + new Point(0, 10);
                Point p4 = coord.ToPlotCoordinates(new double[] { 91, 0 });

                gpr.StrokePath(new GraphicsPath().MoveTo(p1).LineTo(p2).LineTo(p3).LineTo(p4), Colours.Black, 2);

                gpr.FillText((p2 + p3) * 0.5 + new Point(-labelFont.MeasureText("Features").Width * 0.5, 12), "Features", labelFont, Colours.Black);
            }));

            // Read the training, validation, and test runtime data.
            double[] trainingRuntimes = ReadTrainingRuntimeFile(os, dataset);

            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Point pTraining = coord.ToPlotCoordinates(new double[] { 110, trainingRuntimes[0] });
                Point pValidation = coord.ToPlotCoordinates(new double[] { 110 + 15 * 1, trainingRuntimes[1] });
                Point pTest = coord.ToPlotCoordinates(new double[] { 110 + 15 * 2, trainingRuntimes[2] });

                gpr.FillPath(new GraphicsPath().Arc(pTraining, 6, 0, 2 * Math.PI).Close(), trainingColour);
                gpr.FillText(pTraining + new Point(-labelFont.MeasureText("Tr").Width * 0.5, 12), "Tr", labelFont, trainingColour);

                gpr.FillPath(new GraphicsPath().Arc(pValidation, 6, 0, 2 * Math.PI).Close(), validationColour);
                gpr.FillText(pValidation + new Point(-labelFont.MeasureText("V").Width * 0.5, -12), "V", labelFont, validationColour, TextBaselines.Bottom);

                gpr.FillPath(new GraphicsPath().Arc(pTest, 6, 0, 2 * Math.PI).Close(), testColour);
                gpr.FillText(pTest + new Point(-labelFont.MeasureText("Te").Width * 0.5, 12), "Te", labelFont, testColour);


                Point p1 = coord.ToPlotCoordinates(new double[] { 99, trainingRuntimes.Min() }) + new Point(0, 36);
                Point p2 = coord.ToPlotCoordinates(new double[] { 99, trainingRuntimes.Min() }) + new Point(0, 46);
                Point p3 = coord.ToPlotCoordinates(new double[] { 151, trainingRuntimes.Min() }) + new Point(0, 46);
                Point p4 = coord.ToPlotCoordinates(new double[] { 151, trainingRuntimes.Min() }) + new Point(0, 36);

                gpr.StrokePath(new GraphicsPath().MoveTo(p1).LineTo(p2).LineTo(p3).LineTo(p4), Colours.Black, 2);
                gpr.FillText((p2 + p3) * 0.5 + new Point(-labelFont.MeasureText("Model").Width * 0.5, 12), "Model", labelFont, Colours.Black);
            }));

            double totalTrainingRuntime = trainingRuntimes.Sum();

            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Point[] pointsFeatures = parallelisationStrategies.Select((x, i) => coord.ToPlotCoordinates(new double[] { xTicks[i], overallRuntimes[x] })).ToArray();

                for (int i = 0; i < parallelisationStrategies.Length; i++)
                {
                    string strategy = parallelisationStrategies[i].Replace("x", "×");
                    gpr.FillPath(new GraphicsPath().AddText(pointsFeatures[i] + new Point(-labelFont.MeasureText(strategy).Width * 0.5, -18 - 32), strategy, labelFont, TextBaselines.Bottom), Colour.FromRgb(128, 128, 128));
                    Page icon = GetParallelisationIcon(parallelisationStrategies[i], Colour.FromRgb(128, 128, 128));
                    gpr.DrawGraphics(pointsFeatures[i] + new Point(-16, -12 - 32), icon.Graphics);
                }

                Point[] pointsTotal = parallelisationStrategies.Select((x, i) => coord.ToPlotCoordinates(new double[] { 160 + 15 * i, overallRuntimes[x] + totalTrainingRuntime })).ToArray();

                for (int i = 0; i < pointsTotal.Length; i++)
                {
                    gpr.FillPath(new GraphicsPath().Arc(pointsTotal[i], 6, 0, 2 * Math.PI).Close(), Colours.Black);

                    if (overallRuntimes[parallelisationStrategies[i]] == overallRuntimes.Values.Min())
                    {
                        TimeSpan runtimeSpan = TimeSpan.FromMilliseconds(overallRuntimes[parallelisationStrategies[i]] + totalTrainingRuntime);

                        string runtime = (runtimeSpan.Hours > 0 ? (runtimeSpan.Hours.ToString() + "h ") : "") + runtimeSpan.Minutes.ToString() + "m " + runtimeSpan.Seconds.ToString() + "s";

                        gpr.FillText(new Point(pointsTotal[i].X, pointsTotal.Select(x => x.Y).Min()) + new Point(-labelFont.MeasureText(runtime).Width * 0.5, -18 - 40), runtime, labelFont, Colour.FromRgb(0, 0, 0), TextBaselines.Bottom);

                        gpr.StrokePath(new GraphicsPath().MoveTo(pointsTotal[i]).LineTo(new Point(pointsTotal[i].X, pointsTotal.Select(x => x.Y).Min()) + new Point(0, -12 - 40)), Colours.Black, 2);
                    }

                    Page icon = GetParallelisationIcon(parallelisationStrategies[i], Colour.FromRgb(0, 0, 0));
                    gpr.DrawGraphics(pointsTotal[i] + new Point(-16, -12 - 32), icon.Graphics);
                }


                Point p1 = coord.ToPlotCoordinates(new double[] { 149, overallRuntimes.Values.Min() + totalTrainingRuntime }) + new Point(0, 12);
                Point p2 = coord.ToPlotCoordinates(new double[] { 149, overallRuntimes.Values.Min() + totalTrainingRuntime }) + new Point(0, 22);
                Point p3 = coord.ToPlotCoordinates(new double[] { 216, overallRuntimes.Values.Min() + totalTrainingRuntime }) + new Point(0, 22);
                Point p4 = coord.ToPlotCoordinates(new double[] { 216, overallRuntimes.Values.Min() + totalTrainingRuntime }) + new Point(0, 12);

                gpr.StrokePath(new GraphicsPath().MoveTo(p1).LineTo(p2).LineTo(p3).LineTo(p4), Colours.Black, 2);
                gpr.FillText((p2 + p3) * 0.5 + new Point(-labelFont.MeasureText("Total").Width * 0.5, 12), "Total", labelFont, Colours.Black);
            }));

            return plot.Render();
        }

        // Get the parallelisation strategy icon.
        static Page GetParallelisationIcon(string parallelisationStrategy, Colour colour)
        {
            int threadsPerJob = int.Parse(parallelisationStrategy.Substring(0, parallelisationStrategy.IndexOf("x")));
            int jobs = int.Parse(parallelisationStrategy.Substring(parallelisationStrategy.IndexOf("x") + 1));

            Page pag = new Page(32, 32);

            int radius = 5;
            GraphicsPath contour = new GraphicsPath();

            contour.MoveTo(radius, 0).LineTo(pag.Width - radius, 0).Arc(pag.Width - radius, radius, radius, -Math.PI / 2, 0).LineTo(pag.Width, pag.Height - radius);
            contour.Arc(pag.Width - radius, pag.Height - radius, radius, 0, Math.PI / 2).LineTo(radius, pag.Height).Arc(radius, pag.Height - radius, radius, Math.PI / 2, Math.PI);
            contour.LineTo(0, radius).Arc(radius, radius, radius, Math.PI, 3 * Math.PI / 2).Close();

            pag.Graphics.FillPath(contour, Colours.White);
            pag.Graphics.StrokePath(contour, colour, 2);

            if (threadsPerJob == 1)
            {
                pag.Graphics.FillRectangle(13, 3, 6, 26, colour);
            }
            else if (jobs == 1)
            {
                pag.Graphics.FillRectangle(3, 13, 26, 6, colour);
            }
            else if (threadsPerJob < jobs)
            {
                pag.Graphics.FillRectangle(11, 6, 10, 20, colour);
            }
            else if (threadsPerJob > jobs)
            {
                pag.Graphics.FillRectangle(6, 11, 20, 10, colour);
            }

            return pag;
        }

        // Get the OS icon.
        static Page GetOSIcon(string os)
        {
            Page pag = new Page(48, 48);

            Page icon = Parser.FromFile($"../../../Data/{os}.svg");

            pag.Graphics.Save();

            pag.Graphics.Scale(pag.Width / icon.Width, pag.Height / icon.Height);

            pag.Graphics.DrawGraphics(0, 0, icon.Graphics);

            pag.Graphics.Restore();

            return pag;
        }

        // Read the runtime data files for model training, validation and test.
        static double[] ReadTrainingRuntimeFile(string os, string dataset)
        {
            Dictionary<string, double> stepRuntimes = new Dictionary<string, double>();

            foreach (string line in File.ReadLines($"../../../Data/{os}.training.{dataset}.runtime.txt"))
            {
                string[] splitLine = line.Split(' ');

                stepRuntimes.Add(splitLine[0], double.Parse(splitLine[1]));
            }

            return new double[]
            {
                stepRuntimes["training"],
                stepRuntimes["validation"],
                stepRuntimes["test"],
            };
        }

        // Read the runtime file for feature computation.
        static (Dictionary<string, double> overallRuntimes, Dictionary<string, List<double>[]> individualRuntimes) ReadFeatureRuntimeFile(string os, string dataset)
        {
            Dictionary<string, double> overallRuntimes = new Dictionary<string, double>();
            Dictionary<string, List<double>[]> individualRuntimes = new Dictionary<string, List<double>[]>();

            foreach (string line in File.ReadLines($"../../../Data/{os}.features.{dataset}.runtime.txt"))
            {
                string[] splitLine = line.Split(' ');

                if (splitLine[1] == "overall")
                {
                    overallRuntimes.Add(splitLine[0], double.Parse(splitLine[2]));
                }
                else
                {
                    if (!individualRuntimes.TryGetValue(splitLine[0], out List<double>[]? runtimes))
                    {
                        runtimes = new List<double>[3] { new List<double>(), new List<double>(), new List<double>() };
                        individualRuntimes.Add(splitLine[0], runtimes);
                    }

                    if (TrainingAlignments.Contains(splitLine[1]))
                    {
                        runtimes[0].Add(double.Parse(splitLine[2]));
                    }
                    else if (ValidationAlignments.Contains(splitLine[1]))
                    {
                        runtimes[1].Add(double.Parse(splitLine[2]));
                    }
                    else
                    {
                        Debug.Assert(TestAlignments.Contains(splitLine[1]));

                        runtimes[2].Add(double.Parse(splitLine[2]));
                    }
                }
            }

            return (overallRuntimes, individualRuntimes);
        }
    }

    // A non-linear coordinate system for the runtime axis (necessary because the times we need to display range from milliseconds to hours).
    class TimeCoordinateSystem : IContinuousInvertibleCoordinateSystem
    {
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public bool IsLinear => false;

        public TimeCoordinateSystem(double minX, double minY, double maxX, double maxY, double width, double height)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            Width = width;
            Height = height;
        }

        public double[] Resolution => new double[] { (MaxX - MinX) * 0.01, (MaxY - MinY) * 0.01 };

        public double[] GetAround(IReadOnlyList<double> point, IReadOnlyList<double> direction)
        {
            double magnitude = Math.Sqrt(direction[0] * direction[0] + direction[1] * direction[1]);

            return new double[] { point[0] + direction[0] / magnitude * 0.01, point[1] + direction[1] / magnitude * 0.01 };
        }

        public bool IsDirectionStraight(IReadOnlyList<double> direction)
        {
            return direction[0] == 0 || direction[1] == 0;
        }

        public double[] ToDataCoordinates(Point plotPoint)
        {
            double y;

            if ((Height - plotPoint.Y) / Height * 2 < 1)
            {
                y = ((Height - plotPoint.Y) / Height * 2) * 1000;
            }
            else if (((Height - plotPoint.Y) / Height * 2) < 2)
            {
                y = (((Height - plotPoint.Y) / Height * 2) - 1) * 60000 + 1000;
            }
            else
            {
                y = (((Height - plotPoint.Y) / Height * 2) - 2) * 3600000 + 61000;
            }

            return new double[] { MinX + plotPoint.X / Width * (MaxX - MinX), y };
        }

        public Point ToPlotCoordinates(IReadOnlyList<double> dataPoint)
        {
            double y;

            if (dataPoint[1] < 1000)
            {
                y = dataPoint[1] / 1000.0;
            }
            else if (dataPoint[1] < 60000)
            {
                y = 1 + (dataPoint[1] - 1000) / 60000.0;
            }
            else
            {
                y = 2 + (dataPoint[1] - 61000) / 3600000.0;
            }

            return new Point((dataPoint[0] - MinX) / (MaxX - MinX) * Width, Height - y / 2 * Height);
        }
    }
}
