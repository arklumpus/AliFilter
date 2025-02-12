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

using VectSharp;
using VectSharp.SVG;
using VectSharp.PDF;
using VectSharp.Plots;
using MathNet.Numerics.Statistics;
using VectSharp.Raster;

namespace Figure_3
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Create the figure parts.
            Page mistakes = CreateFigure3a();
            Page reducedTraining = CreateFigure3b();

            // Draw both figure parts on the same page.
            Page pag = new Page(1, 1);
            pag.Graphics.DrawGraphics(0, 0, mistakes.Graphics);
            pag.Graphics.DrawGraphics(mistakes.Width - reducedTraining.Width, mistakes.Height + 5, reducedTraining.Graphics);

            // Figure part labels.
            Font fntBold = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 16);
            pag.Graphics.FillText(0, 0, "a)", fntBold, Colours.Black);
            pag.Graphics.FillText(0, mistakes.Height + 5, "b)", fntBold, Colours.Black);

            pag.Crop();

            // Resize to a width of 8.5cm.
            Page resizedPage = new Page(241, pag.Height / pag.Width * 241);
            resizedPage.Background = Colours.White;
            resizedPage.Graphics.Scale(241 / pag.Width, 241 / pag.Width);
            resizedPage.Graphics.DrawGraphics(0, 0, pag.Graphics);

            Document doc = new Document();
            doc.Pages.Add(resizedPage);

            resizedPage.SaveAsSVG("Figure_3.svg");
            resizedPage.SaveAsSVG("Figure_3.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_3.pdf");
            resizedPage.SaveAsPNG("Figure_3.png", 600.0 / 72);
        }

        static Page CreateFigure3a()
        {
            // Read the mistake data.
            List<double[]> data = ReadMistakes();

            double[] mistakes = new double[] { 0, 0.05, 0.1, 0.15, 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5 };
            string[] scores = new string[] { "MCC", "Accuracy", "Confidence" };
            Colour[] scoreColours = new Colour[] { Colour.FromRgb(100, 143, 255), Colour.FromRgb(255, 176, 0), Colour.FromRgb(220, 38, 127) };

            // Extract the medians and ranges for each score.
            double[][][] medians = Enumerable.Range(0, 3).Select(i => mistakes.Select(x => new double[] { x, data.Where(y => y[0] == x).Select(z => z[i + 1]).Median() }).ToArray()).ToArray();
            double[][][] ranges = Enumerable.Range(0, 3).Select(i => mistakes.Select(x => new double[] { data.Where(y => y[0] == x).Select(z => z[i + 1]).Min(), data.Where(y => y[0] == x).Select(z => z[i + 1]).Max() }).ToArray()).ToArray();

            //Create the line chart.
            Plot plot = Plot.Create.LineCharts(new double[][][] { new double[][] { new double[] { 0, -0.3 }, new double[] { 0.5, 1 } } }.Concat(medians).ToArray(), xAxisTitle: "% Mistakes", yAxisTitle: "Score",
                linePresentationAttributes: new PlotElementPresentationAttributes[] { new PlotElementPresentationAttributes() }.Concat(scoreColours.Select(x => new PlotElementPresentationAttributes() { Fill = null, Stroke = BlendWithWhite(x, 0.35) })).ToArray());

            // Fine-tune the plot appearance.
            plot.RemovePlotElement(plot.GetFirst<DataLine<IReadOnlyList<double>>>());
            plot.GetFirst<ContinuousAxisLabels>().StartPoint = new double[] { 0, plot.GetFirst<ContinuousAxisLabels>().StartPoint[1] };
            plot.GetFirst<ContinuousAxisLabels>().TextFormat = (x, i) => FormattedText.Format(x[0].ToString("0%", System.Globalization.CultureInfo.InvariantCulture), FontFamily.StandardFontFamilies.Helvetica, 12);
            plot.GetFirst<ContinuousAxisTicks>().StartPoint = new double[] { 0, plot.GetFirst<ContinuousAxisTicks>().StartPoint[1] };
            plot.GetFirst<Grid>().Side1Start = new double[] { 0, plot.GetFirst<Grid>().Side1Start[1] };
            plot.GetFirst<Grid>().Side2Start = new double[] { 0, plot.GetFirst<Grid>().Side2Start[1] };
            plot.GetAll<ContinuousAxisTicks>().ElementAt(1).IntervalCount = 13;
            plot.GetAll<ContinuousAxisLabels>().ElementAt(1).StartPoint = new double[] { plot.GetAll<ContinuousAxisLabels>().ElementAt(1).StartPoint[0], -0.2 };
            plot.GetAll<ContinuousAxisLabels>().ElementAt(1).IntervalCount = 6;
            plot.GetAll<ContinuousAxisTicks>().ElementAt(1).SizeAbove = i => i % 2 == 1 ? 3 : 2;
            plot.GetAll<ContinuousAxisTicks>().ElementAt(1).SizeBelow = i => i % 2 == 1 ? 3 : 2;
            plot.GetAll<Grid>().ElementAt(1).IntervalCount = 6;
            plot.GetAll<Grid>().ElementAt(1).Side1End = new double[] { plot.GetAll<Grid>().ElementAt(1).Side1End[0], -0.2 };
            plot.GetAll<Grid>().ElementAt(1).Side2End = new double[] { plot.GetAll<Grid>().ElementAt(1).Side2End[0], -0.2 };

            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                GraphicsPath circle = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close();
                GraphicsPath diamond = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close();
                GraphicsPath triangle = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close();

                GraphicsPath[] pointElements = new GraphicsPath[] { circle, diamond, triangle };

                double[] pointSizes = new double[] { 3.6, 4, 4 };

                for (int i = 0; i < medians.Length; i++)
                {
                    for (int j = 0; j < medians[i].Length; j++)
                    {
                        Point pt = coord.ToPlotCoordinates(medians[i][j]);

                        Point p1 = coord.ToPlotCoordinates(new double[] { medians[i][j][0], ranges[i][j][0] });
                        Point p2 = coord.ToPlotCoordinates(new double[] { medians[i][j][0], ranges[i][j][1] });

                        Point whiskerSize = new Point(8, 0);

                        gpr.StrokePath(new GraphicsPath().MoveTo(p1 - whiskerSize).LineTo(p1 + whiskerSize).MoveTo(p1).LineTo(p2).MoveTo(p2 - whiskerSize).LineTo(p2 + whiskerSize), BlendWithWhite(scoreColours[i], 0.75));

                        gpr.Save();
                        gpr.Translate(pt);
                        gpr.Scale(pointSizes[i], pointSizes[i]);
                        gpr.FillPath(pointElements[i], scoreColours[i]);

                        gpr.Restore();
                    }
                }
            }));

            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Page legendPage = new Page(1, 1);

                Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);
                Font fntBold = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 12);

                GraphicsPath circle = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close();
                GraphicsPath diamond = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close();
                GraphicsPath triangle = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close();

                GraphicsPath[] pointElements = new GraphicsPath[] { circle, diamond, triangle };

                double[] pointSizes = new double[] { 3.6, 4, 4 };

                Graphics graphics = legendPage.Graphics;

                graphics.Save();

                for (int i = 0; i < scores.Length; i++)
                {
                    double effectiveSize = pointSizes[i];
                    graphics.Save();
                    graphics.Translate(0, 5);
                    graphics.Scale(effectiveSize, effectiveSize);
                    graphics.FillPath(pointElements[i], scoreColours[i]);
                    graphics.Restore();

                    graphics.FillText(10, 9, scores[i], fnt, Colours.Black, TextBaselines.Baseline);

                    graphics.Translate(fnt.MeasureText(scores[i]).Width + 40, 0);
                }

                graphics.Restore();

                legendPage.Crop();

                Point topLeft = coord.ToPlotCoordinates(new double[] { 0, -0.65 });
                Point topRight = coord.ToPlotCoordinates(new double[] { 0.5, -0.65 });

                gpr.DrawGraphics((topLeft.X + topRight.X - legendPage.Width) * 0.5, topLeft.Y, legendPage.Graphics);
            }));
            return plot.Render();
        }


        static Page CreateFigure3b()
        {
            // Read the reduced training data.
            List<(int, int, double[])> data = ReadReducedTraining();

            int[] trainingAlignments = new int[] { 1, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 158 };
            string[] scores = new string[] { "Confidence", "MCC", "Accuracy" };
            Colour[] scoreColours = new Colour[] { Colour.FromRgb(220, 38, 127), Colour.FromRgb(100, 143, 255), Colour.FromRgb(255, 176, 0) };

            // Extrac the medians and ranges for each score.
            double[][][] medians = new int[] { 2, 0, 1 }.Select(i => trainingAlignments.Select(x => new double[] { x < 158 ? x : 24, data.Where(y => y.Item1 == x).Select(z => z.Item3[i]).Median() }).ToArray()).ToArray();
            double[][][] ranges = new int[] { 2, 0, 1 }.Select(i => trainingAlignments.Select(x => new double[] { data.Where(y => y.Item1 == x).Select(z => z.Item3[i]).Min(), data.Where(y => y.Item1 == x).Select(z => z.Item3[i]).Max() }).ToArray()).ToArray();

            // Create the plot.
            Plot plot = Plot.Create.LineCharts(new double[][][] { new double[][] { new double[] { 1, 0 }, new double[] { 24, 1 } } }.Concat(medians).ToArray(), xAxisTitle: "Number of alignments", yAxisTitle: "Score",
                linePresentationAttributes: new PlotElementPresentationAttributes[] { new PlotElementPresentationAttributes() }.Concat(scoreColours.Select(x => new PlotElementPresentationAttributes() { Fill = null, Stroke = BlendWithWhite(x, 0.35) })).ToArray());

            // Fine-tune the plot appearance.
            plot.RemovePlotElement(plot.GetFirst<DataLine<IReadOnlyList<double>>>());
            plot.GetFirst<ContinuousAxisTicks>().StartPoint = new double[] { 2, plot.GetFirst<ContinuousAxisLabels>().EndPoint[1] };
            plot.GetFirst<ContinuousAxisTicks>().EndPoint = new double[] { 20, plot.GetFirst<ContinuousAxisLabels>().EndPoint[1] };
            plot.GetFirst<ContinuousAxisTicks>().IntervalCount = 9;
            Func<int, double> sizeAbove = plot.GetFirst<ContinuousAxisTicks>().SizeAbove;
            plot.GetFirst<ContinuousAxisTicks>().SizeAbove = i => sizeAbove(i + 1);
            plot.GetFirst<ContinuousAxisTicks>().SizeBelow = i => sizeAbove(i + 1);
            plot.GetFirst<ContinuousAxisTitle>().Position += 16;

            plot.AddPlotElement(new ContinuousAxisTicks(new double[] { 1, plot.GetFirst<ContinuousAxisTicks>().EndPoint[1] }, new double[] { 24, plot.GetFirst<ContinuousAxisTicks>().EndPoint[1] }, plot.GetFirst<IContinuousCoordinateSystem>())
            {
                PresentationAttributes = plot.GetFirst<ContinuousAxisTicks>().PresentationAttributes,
                IntervalCount = 1,
                SizeAbove = i => sizeAbove(0),
                SizeBelow = i => sizeAbove(0),
            });

            Grid newGrid = new Grid(plot.GetFirst<Grid>().Side1Start, plot.GetFirst<Grid>().Side1End, plot.GetFirst<Grid>().Side2Start, plot.GetFirst<Grid>().Side2End, plot.GetFirst<IContinuousCoordinateSystem>())
            {
                PresentationAttributes = plot.GetFirst<Grid>().PresentationAttributes,
                IntervalCount = 1
            };

            plot.GetFirst<Grid>().Side1Start = new double[] { 4, plot.GetFirst<Grid>().Side1Start[1] };
            plot.GetFirst<Grid>().Side2Start = new double[] { 4, plot.GetFirst<Grid>().Side2Start[1] };

            plot.GetFirst<Grid>().Side1End = new double[] { 20, plot.GetFirst<Grid>().Side1End[1] };
            plot.GetFirst<Grid>().Side2End = new double[] { 20, plot.GetFirst<Grid>().Side2End[1] };

            plot.GetFirst<Grid>().IntervalCount = 4;

            List<IPlotElement> plotElements = plot.PlotElements.ToList();

            foreach (var plotElement in plotElements)
            {
                plot.RemovePlotElement(plotElement);
            }

            plot.AddPlotElement(newGrid);
            plot.AddPlotElements(plotElements);

            plot.GetFirst<ContinuousAxisLabels>().StartPoint = new double[] { 4, plot.GetFirst<ContinuousAxisLabels>().EndPoint[1] };
            plot.GetFirst<ContinuousAxisLabels>().EndPoint = new double[] { 20, plot.GetFirst<ContinuousAxisLabels>().EndPoint[1] };
            plot.GetFirst<ContinuousAxisLabels>().IntervalCount = 4;
            plot.GetFirst<ContinuousAxisLabels>().TextFormat = (x, i) => FormattedText.Format(x[0].ToString("0", System.Globalization.CultureInfo.InvariantCulture), FontFamily.StandardFontFamilies.Helvetica, 12);

            plot.AddPlotElement(new ContinuousAxisLabels(new double[] { 4, plot.GetFirst<ContinuousAxisLabels>().EndPoint[1] }, new double[] { 20, plot.GetFirst<ContinuousAxisLabels>().EndPoint[1] }, plot.GetFirst<IContinuousCoordinateSystem>())
            {
                PresentationAttributes = plot.GetFirst<ContinuousAxisLabels>().PresentationAttributes,
                Rotation = plot.GetFirst<ContinuousAxisLabels>().Rotation,
                Alignment = TextAnchors.Center,
                Position = i => plot.GetFirst<ContinuousAxisLabels>().Position(i) * 2.5,
                Baseline = plot.GetFirst<ContinuousAxisLabels>().Baseline,
                IntervalCount = 4,
                TextFormat = (x, i) => FormattedText.Format((x[0] / 2).ToString("0", System.Globalization.CultureInfo.InvariantCulture), FontFamily.StandardFontFamilies.Helvetica, 12)
            });

            plot.AddPlotElement(new ContinuousAxisLabels(new double[] { 1, plot.GetFirst<ContinuousAxisLabels>().EndPoint[1] }, new double[] { 24, plot.GetFirst<ContinuousAxisLabels>().EndPoint[1] }, plot.GetFirst<IContinuousCoordinateSystem>())
            {
                PresentationAttributes = plot.GetFirst<ContinuousAxisLabels>().PresentationAttributes,
                Rotation = plot.GetFirst<ContinuousAxisLabels>().Rotation,
                Alignment = TextAnchors.Center,
                Position = plot.GetFirst<ContinuousAxisLabels>().Position,
                Baseline = plot.GetFirst<ContinuousAxisLabels>().Baseline,
                IntervalCount = 1,
                TextFormat = (x, i) => FormattedText.Format((x[0] switch { 1 => 1, 24 => 158, _ => double.NaN }).ToString("0", System.Globalization.CultureInfo.InvariantCulture), FontFamily.StandardFontFamilies.Helvetica, 12)
            });

            plot.AddPlotElement(new ContinuousAxisLabels(new double[] { 1, plot.GetFirst<ContinuousAxisLabels>().EndPoint[1] }, new double[] { 24, plot.GetFirst<ContinuousAxisLabels>().EndPoint[1] }, plot.GetFirst<IContinuousCoordinateSystem>())
            {
                PresentationAttributes = plot.GetFirst<ContinuousAxisLabels>().PresentationAttributes,
                Rotation = plot.GetFirst<ContinuousAxisLabels>().Rotation,
                Alignment = TextAnchors.Center,
                Position = i => plot.GetFirst<ContinuousAxisLabels>().Position(i) * 2.5,
                Baseline = plot.GetFirst<ContinuousAxisLabels>().Baseline,
                IntervalCount = 1,
                TextFormat = (x, i) => FormattedText.Format((x[0] switch { 1 => 1, 24 => 77, _ => double.NaN }).ToString("0", System.Globalization.CultureInfo.InvariantCulture), FontFamily.StandardFontFamilies.Helvetica, 12)
            });

            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                GraphicsPath circle = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close();
                GraphicsPath diamond = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close();
                GraphicsPath triangle = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close();

                GraphicsPath[] pointElements = new GraphicsPath[] { triangle, circle, diamond };

                double[] pointSizes = new double[] { 3.6, 4, 4 };

                for (int i = 0; i < medians.Length; i++)
                {
                    for (int j = 0; j < medians[i].Length - 1; j++)
                    {
                        Point pt = coord.ToPlotCoordinates(medians[i][j]);

                        Point p1 = coord.ToPlotCoordinates(new double[] { medians[i][j][0], ranges[i][j][0] });
                        Point p2 = coord.ToPlotCoordinates(new double[] { medians[i][j][0], ranges[i][j][1] });

                        Point whiskerSize = new Point(8, 0);

                        gpr.StrokePath(new GraphicsPath().MoveTo(p1 - whiskerSize).LineTo(p1 + whiskerSize).MoveTo(p1).LineTo(p2).MoveTo(p2 - whiskerSize).LineTo(p2 + whiskerSize), BlendWithWhite(scoreColours[i], 0.75));
                    }
                }

                for (int i = 0; i < medians.Length; i++)
                {
                    for (int j = 0; j < medians[i].Length; j++)
                    {
                        Point pt = coord.ToPlotCoordinates(medians[i][j]);

                        gpr.Save();
                        gpr.Translate(pt);
                        gpr.Scale(pointSizes[i], pointSizes[i]);
                        gpr.FillPath(pointElements[i], scoreColours[i]);

                        gpr.Restore();
                    }
                }
            }));

            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                double[][] pts = new double[][]
                {
                    new double[] { 22, plot.GetFirst<ContinuousAxis>().EndPoint[1] },
                    new double[] { 22, (medians[0][^1][1] + medians[0][^2][1]) * 0.5 },
                    new double[] { 22, (medians[1][^1][1] + medians[1][^2][1]) * 0.5 },
                    new double[] { 22, (medians[2][^1][1] + medians[2][^2][1]) * 0.5 },
                };

                Colour[] cols = new Colour[] { Colours.Black }.Concat(scoreColours).ToArray();

                GraphicsPath symbolBg = new GraphicsPath().MoveTo(-4, 4).LineTo(-0, -4).LineTo(4, -4).LineTo(0, 4).Close();
                GraphicsPath symbol = new GraphicsPath().MoveTo(-4, 4).LineTo(-0, -4).MoveTo(4, -4).LineTo(0, 4);

                for (int i = 0; i < pts.Length; i++)
                {
                    Point pt = coord.ToPlotCoordinates(pts[i]);
                    gpr.Save();
                    gpr.Translate(pt);
                    gpr.FillPath(symbolBg, Colours.White);
                    gpr.Restore();
                }

                for (int i = 0; i < pts.Length; i++)
                {
                    Point pt = coord.ToPlotCoordinates(pts[i]);
                    gpr.Save();
                    gpr.Translate(pt);
                    gpr.StrokePath(symbol, cols[i]);
                    gpr.Restore();
                }

                Point pt2 = coord.ToPlotCoordinates(new double[] { 1, plot.GetFirst<ContinuousAxisLabels>().StartPoint[1] });

                Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);

                gpr.FillText(pt2.X - fnt.MeasureText("Training").Width - 15, pt2.Y + plot.GetFirst<ContinuousAxisLabels>().Position(0) - 1, "Training", fnt, Colours.Black);
                gpr.FillText(pt2.X - fnt.MeasureText("Validation").Width - 15, pt2.Y + plot.GetAll<ContinuousAxisLabels>().ElementAt(2).Position(0) - 1, "Validation", fnt, Colours.Black);
            }));

            return plot.Render();
        }

        /// <summary>
        /// Read the results obtained for models trained with a proportion of mistakes.
        /// </summary>
        /// <returns>The results obtained for models trained with a proportion of mistakes [ % mistakes, MCC, A, C ]</returns>
        static List<double[]> ReadMistakes()
        {
            List<double[]> tbr = new List<double[]>();

            using (StreamReader sr = new StreamReader("../../../Data/mistakes.txt"))
            {
                string line = sr.ReadLine();

                while (line != null)
                {
                    string[] splitLine = line.Split('\t');
                    tbr.Add(new double[] { double.Parse(splitLine[0], System.Globalization.CultureInfo.InvariantCulture), double.Parse(splitLine[1], System.Globalization.CultureInfo.InvariantCulture), double.Parse(splitLine[2], System.Globalization.CultureInfo.InvariantCulture), double.Parse(splitLine[3], System.Globalization.CultureInfo.InvariantCulture) });
                    line = sr.ReadLine();
                }
            }

            return tbr;
        }


        /// <summary>
        /// Read the results obtained for models trained using a reduced number of alignments.
        /// </summary>
        /// <returns>The results obtained for models trained using a reduced number of alignments (Training, Validation, [ MCC, A, C ])</returns>
        static List<(int, int, double[])> ReadReducedTraining()
        {
            List<(int, int, double[])> tbr = new List<(int, int, double[])>();

            using (StreamReader sr = new StreamReader("../../../Data/reducedTraining.txt"))
            {
                string line = sr.ReadLine();
                
                // Skip the header line.
                line = sr.ReadLine();

                while (line != null)
                {
                    string[] splitLine = line.Split('\t');
                    tbr.Add((int.Parse(splitLine[0]), int.Parse(splitLine[1]), new double[] { double.Parse(splitLine[2], System.Globalization.CultureInfo.InvariantCulture), double.Parse(splitLine[3], System.Globalization.CultureInfo.InvariantCulture), double.Parse(splitLine[4], System.Globalization.CultureInfo.InvariantCulture) }));
                    line = sr.ReadLine();
                }
            }
            return tbr;
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
