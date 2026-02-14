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

using MathNet.Numerics.Statistics;
using VectSharp;
using VectSharp.Plots;

namespace Figure_S6
{
    partial class Program
    {
        static Page CreateFigureS6a()
        {
            // Colour to use for each tool.
            Dictionary<string, Colour> toolColours = new Dictionary<string, Colour>()
            {
                { "alifilter", Colour.FromRgb(119, 170, 221) },
                { "bmge",    Colour.FromRgb(238, 136, 102) },
                { "clipkit",   Colour.FromRgb(187, 204, 51) },
                { "gblocks",    Colour.FromRgb(255, 170, 187) },
            };

            // Symbols for each tool.
            GraphicsPath circle = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close();
            GraphicsPath square = new GraphicsPath().MoveTo(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1).Close();
            GraphicsPath diamond = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close();
            GraphicsPath triangle = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close();

            Dictionary<string, (GraphicsPath, bool, double)> toolShapes = new Dictionary<string, (GraphicsPath, bool, double)>()
            {
                { "alifilter", (triangle, true, 5) },
                { "bmge", (diamond, true, 5) },
                { "clipkit", (square, false, 3.5) },
                { "gblocks", (circle, false, 4.5) },
            };

            Dictionary<string, string> toolNames = new Dictionary<string, string>()
            {
                { "alifilter", "AliFilter" },
                { "bmge", "BMGE" },
                { "clipkit", "ClipKIT" },
                { "gblocks", "Gblocks" }
            };

            string[] tools = new string[] { "alifilter", "bmge", "clipkit", "gblocks" };

            Dictionary<string, (double[] runtime, long[] ram, int filteredLength)>[] benchmarkResults = tools.Select(x => ReadResults(x)).ToArray();

            double minRuntime = 5000;
            double maxRuntime = 2 * 60 * 60 * 1000;

            double minSize = 200.0 * 1024 * 1024;
            double maxSize = 30.0 * 1024 * 1024 * 1024;

            // Use a logarithmic coordinate system.
            LogarithmicCoordinateSystem2D coordinateSystem = new LogarithmicCoordinateSystem2D(minSize, maxSize, minRuntime, maxRuntime, 250, 250);

            // Create the initial plot.
            Plot plot = Plot.Create.ScatterPlot(new double[][] { new double[] { minSize, minRuntime }, new double[] { maxSize, maxRuntime } }, coordinateSystem: coordinateSystem, xAxisTitle: "Alignment file size", yAxisTitle: "Runtime");
            plot.RemovePlotElement(plot.GetFirst<ScatterPoints<IReadOnlyList<double>>>());

            double[] bottomRight = (double[])plot.GetFirst<ContinuousAxis>().EndPoint;
            double[] topLeft = (double[])plot.GetAll<ContinuousAxis>().ElementAt(1).EndPoint;

            // Remove unnecessary plot elements.
            plot.RemovePlotElement(plot.GetFirst<Grid>());
            plot.RemovePlotElement(plot.GetFirst<Grid>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxisTicks>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxisTicks>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxisLabels>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxisLabels>());
            plot.GetAll<ContinuousAxisTitle>().ElementAt(1).Position *= 0.6;

            // Temporarily remove all plot elements.
            IPlotElement[] plotElements = plot.PlotElements.ToArray();
            foreach (IPlotElement pe in plotElements)
            {
                plot.RemovePlotElement(pe);
            }

            // Add the X grid and axis labels.
            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>((ICoordinateSystem<IReadOnlyList<double>>)plotElements[0].CoordinateSystem, (gpr, coord) =>
            {
                Font labelFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);

                double[] xTicks = new double[] { 200, 500, 1024, 2 * 1024, 5 * 1024, 10 * 1024, 20 * 1024 };
                string[] xLabels = new string[] { "200MiB", "500MiB", "1GiB", "2GiB", "5GiB", "10GiB", "20GiB" };

                double[] xIntervals = Enumerable.Range(2, 8).Select(x => x * 100.0).Concat(Enumerable.Range(1, 9).Select(x => x * 1024.0)).Concat(Enumerable.Range(1, 3).Select(x => x * 10240.0)).ToArray();

                for (int i = 0; i < xTicks.Length; i++)
                {
                    Point p1;
                    Point p2;

                    p1 = coord.ToPlotCoordinates(new double[] { xTicks[i] * 1024 * 1024, bottomRight[1] });
                    p2 = coord.ToPlotCoordinates(new double[] { xTicks[i] * 1024 * 1024, topLeft[1] });
                    gpr.FillText(p1.X - labelFont.MeasureText(xLabels[i]).Width * 0.5, p1.Y + 18, xLabels[i], labelFont, Colours.Black, TextBaselines.Baseline);
                }

                for (int i = 0; i < xIntervals.Length; i++)
                {
                    Point p1;
                    Point p2;

                    p1 = coord.ToPlotCoordinates(new double[] { xIntervals[i] * 1024 * 1024, bottomRight[1] });
                    p2 = coord.ToPlotCoordinates(new double[] { xIntervals[i] * 1024 * 1024, topLeft[1] });

                    gpr.StrokePath(new GraphicsPath().MoveTo(p1).LineTo(p2), Colour.FromRgb(220, 220, 220), 1);

                    if (xTicks.Contains(xIntervals[i]))
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(p1 + new Point(0, -3.5)).LineTo(p1 + new Point(0, 3.5)), Colours.Black, 1);
                    }
                    else
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(p1 + new Point(0, -2.5)).LineTo(p1 + new Point(0, 2.5)), Colours.Black, 1);
                    }
                }
            }));

            // Add the Y grid and axis labels.
            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>((ICoordinateSystem<IReadOnlyList<double>>)plotElements[0].CoordinateSystem, (gpr, coord) =>
            {
                Font labelFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);

                double[] yTicks = new double[] { 5000, 10000, 20000, 60000, 120000, 300000, 600000, 1200000, 3600000, 7200000 };
                string[] yLabels = new string[] { "5s", "10s", "20s", "1m", "2m", "5m", "10m", "20m", "1h", "2h" };

                double[] yIntervals = Enumerable.Range(5, 5).Select(x => x * 1000.0).Concat(Enumerable.Range(1, 5).Select(x => x * 10000.0)).Concat(Enumerable.Range(1, 9).Select(x => x * 60000.0)).Concat(Enumerable.Range(1, 5).Select(x => x * 600000.0)).Concat(Enumerable.Range(1, 2).Select(x => x * 3600000.0)).ToArray();

                for (int i = 0; i < yTicks.Length; i++)
                {
                    Point p1;
                    Point p2;

                    p1 = coord.ToPlotCoordinates(new double[] { topLeft[0], yTicks[i] });
                    p2 = coord.ToPlotCoordinates(new double[] { bottomRight[0], yTicks[i] });
                    gpr.FillText(p1.X - labelFont.MeasureText(yLabels[i]).Width - 10, p1.Y, yLabels[i], labelFont, Colours.Black, TextBaselines.Middle);
                }

                for (int i = 0; i < yIntervals.Length; i++)
                {
                    Point p1;
                    Point p2;

                    p1 = coord.ToPlotCoordinates(new double[] { topLeft[0], yIntervals[i] });
                    p2 = coord.ToPlotCoordinates(new double[] { bottomRight[0], yIntervals[i] });

                    gpr.StrokePath(new GraphicsPath().MoveTo(p1).LineTo(p2), Colour.FromRgb(220, 220, 220), 1);

                    if (yTicks.Contains(yIntervals[i]))
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(p1 + new Point(-3.5, 0)).LineTo(p1 + new Point(3.5, 0)), Colours.Black, 1);
                    }
                    else
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(p1 + new Point(-2.5, 0)).LineTo(p1 + new Point(2.5, 0)), Colours.Black, 1);
                    }
                }
            }));

            // Add back the removed plot elements.
            plot.AddPlotElements(plotElements);



            // Plot the data points.
            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                for (int i = 0; i < tools.Length; i++)
                {
                    foreach (KeyValuePair<string, (double[] runtime, long[] ram, int filteredLength)> kvp in benchmarkResults[i])
                    {
                        Point pt = coord.ToPlotCoordinates(new double[] { AlignmentData[kvp.Key].fileSize, kvp.Value.runtime.Median() });

                        Colour col = toolColours[tools[i]];
                        GraphicsPath shape = toolShapes[tools[i]].Item1;
                        bool filled = toolShapes[tools[i]].Item2;
                        double shapeSize = toolShapes[tools[i]].Item3;

                        gpr.Save();
                        gpr.Translate(pt);
                        gpr.Scale(shapeSize, shapeSize);

                        if (kvp.Key == "LSU" || kvp.Key == "SSU")
                        {
                            if (filled)
                            {
                                gpr.StrokePath(shape, Colour.FromRgb(128, 128, 128), 3.0 / shapeSize);
                            }
                            else
                            {
                                gpr.StrokePath(shape, Colour.FromRgb(128, 128, 128), 5.0 / shapeSize);
                            }
                        }

                        if (filled)
                        {
                            gpr.FillPath(shape, col);
                        }
                        else
                        {
                            gpr.FillPath(shape, Colours.White);
                            gpr.StrokePath(shape, col, 2.0 / shapeSize);
                        }

                        if (tools[i] == "gblocks" && (kvp.Key == "LSU" || kvp.Key == "SSU"))
                        {
                            Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 2);
                            gpr.FillText(-fnt.MeasureText("(     )").Width * 0.5, 0, "(     )", fnt, Colours.Black, TextBaselines.Middle);
                        }

                        gpr.Restore();
                    }
                }

                Font labelFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 14);

                {
                    double minX = AlignmentData.Where(x => x.Key != "LSU" && x.Key != "SSU").Select(x => x.Value.fileSize).Min();
                    double maxX = AlignmentData.Where(x => x.Key != "LSU" && x.Key != "SSU").Select(x => x.Value.fileSize).Max();

                    double maxY = benchmarkResults.SelectMany(x => x.Where(y => y.Key != "LSU" && y.Key != "SSU").Select(x => x.Value.runtime.Max())).Max();

                    Point p1 = coord.ToPlotCoordinates(new double[] { minX, maxY });
                    Point p2 = coord.ToPlotCoordinates(new double[] { maxX, maxY });

                    gpr.StrokePath(new GraphicsPath().MoveTo(p1 + new Point(-7, 5)).LineTo(p1 + new Point(-7, -5)).LineTo(p2 + new Point(7, -5)).LineTo(p2 + new Point(7, 5)), Colours.Black);
                    gpr.FillText((p1 + p2) * 0.5 + new Point(-labelFont.MeasureText("GTDB bac120").Width * 0.5, -12), "GTDB bac120", labelFont, Colours.Black, TextBaselines.Baseline);
                }

                {
                    double minX = AlignmentData.Where(x => x.Key == "LSU" || x.Key == "SSU").Select(x => x.Value.fileSize).Min();
                    double maxX = AlignmentData.Where(x => x.Key == "LSU" || x.Key == "SSU").Select(x => x.Value.fileSize).Max();

                    double minYLSU = benchmarkResults.SelectMany(x => x.Where(y => y.Key == "LSU").Select(x => x.Value.runtime.Min())).Min();
                    double minYSSU = benchmarkResults.SelectMany(x => x.Where(y => y.Key == "SSU").Select(x => x.Value.runtime.Min())).Min();

                    Point p1 = coord.ToPlotCoordinates(new double[] { minX, minYLSU });
                    Point p2 = coord.ToPlotCoordinates(new double[] { maxX, minYLSU });
                    Point p3 = coord.ToPlotCoordinates(new double[] { maxX, minYSSU });

                    gpr.FillText(p1 + new Point(-labelFont.MeasureText("LSU").Width * 0.5, 11), "LSU", labelFont, Colours.Black);
                    gpr.FillText(p3 + new Point(-labelFont.MeasureText("SSU").Width * 0.5, 11), "SSU", labelFont, Colours.Black);

                    gpr.StrokePath(new GraphicsPath().MoveTo(p1 + new Point(-20, 15)).LineTo(p1 + new Point(-20, 25)).LineTo(p2 + new Point(20, 25)).LineTo(p2 + new Point(20, 15)), Colours.Black);

                    gpr.FillText((p1 + p2) * 0.5 + new Point(-labelFont.MeasureText("SILVA Ref NR99").Width * 0.5, 30), "SILVA Ref NR99", labelFont, Colours.Black);
                }

            }));

            return plot.Render();
        }
    }
}
