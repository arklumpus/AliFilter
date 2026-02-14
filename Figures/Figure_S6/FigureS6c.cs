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
using VectSharp.Plots;

namespace Figure_S6
{
    partial class Program
    {
        static Page CreateFigureS6c()
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

            string[] tools = new string[] { "alifilter", "bmge", "clipkit", "gblocks" };

            Dictionary<string, (double[] runtime, long[] ram, int filteredLength)>[] benchmarkResults = tools.Select(x => ReadResults(x)).ToArray();

            double minUnfilteredLength = 50;
            double maxUnfilteredLength = 3000;

            double minFilteredLength = 1;
            double maxFilteredLength = 40000;

            // Use a logarithmic coordinate system.
            LogarithmicCoordinateSystem2D coordinateSystem = new LogarithmicCoordinateSystem2D(minUnfilteredLength, maxUnfilteredLength, minFilteredLength, maxFilteredLength, 450, 250);

            // Create the initial plot.
            Plot plot = Plot.Create.ScatterPlot(new double[][] { new double[] { minUnfilteredLength, minFilteredLength }, new double[] { maxUnfilteredLength, maxFilteredLength } }, coordinateSystem: coordinateSystem, xAxisTitle: "Median sequence length", yAxisTitle: "Filtered alignment length");
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

                double[] xTicks = new double[] { 50, 100, 200, 500, 1000, 2000 };
                string[] xLabels = new string[] { "50", "100", "200", "500", "1000", "2000" };

                double[] xIntervals = Enumerable.Range(5, 5).Select(x => x * 10.0).Concat(Enumerable.Range(1, 9).Select(x => x * 100.0)).Concat(Enumerable.Range(1, 3).Select(x => x * 1000.0)).ToArray();

                for (int i = 0; i < xTicks.Length; i++)
                {
                    Point p1;
                    Point p2;

                    p1 = coord.ToPlotCoordinates(new double[] { xTicks[i] + 1, bottomRight[1] });
                    p2 = coord.ToPlotCoordinates(new double[] { xTicks[i] + 1, topLeft[1] });
                    gpr.FillText(p1.X - labelFont.MeasureText(xLabels[i]).Width * 0.5, p1.Y + 18, xLabels[i], labelFont, Colours.Black, TextBaselines.Baseline);
                }

                for (int i = 0; i < xIntervals.Length; i++)
                {
                    Point p1;
                    Point p2;

                    p1 = coord.ToPlotCoordinates(new double[] { xIntervals[i] + 1, bottomRight[1] });
                    p2 = coord.ToPlotCoordinates(new double[] { xIntervals[i] + 1, topLeft[1] });

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

                double[] yTicks = new double[] { 0, 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 40000 };
                string[] yLabels = new string[] { "0", "1", "2", "5", "10", "20", "50", "100", "200", "500", "1000", "2000", "5000", "10000", "20000", "40000" };

                double[] yIntervals = Enumerable.Range(0, 10).Select(x => (double)x).Concat(Enumerable.Range(1, 9).Select(x => x * 10.0)).Concat(Enumerable.Range(1, 9).Select(x => x * 100.0)).Concat(Enumerable.Range(1, 9).Select(x => x * 1000.0)).Concat(Enumerable.Range(1, 4).Select(x => x * 10000.0)).ToArray();

                for (int i = 0; i < yTicks.Length; i++)
                {
                    Point p1;
                    Point p2;

                    p1 = coord.ToPlotCoordinates(new double[] { topLeft[0], yTicks[i] + 1 });
                    p2 = coord.ToPlotCoordinates(new double[] { bottomRight[0], yTicks[i] + 1 });
                    gpr.FillText(p1.X - labelFont.MeasureText(yLabels[i]).Width - 10, p1.Y, yLabels[i], labelFont, Colours.Black, TextBaselines.Middle);
                }

                for (int i = 0; i < yIntervals.Length; i++)
                {
                    Point p1;
                    Point p2;

                    p1 = coord.ToPlotCoordinates(new double[] { topLeft[0], yIntervals[i] + 1 });
                    p2 = coord.ToPlotCoordinates(new double[] { bottomRight[0], yIntervals[i] + 1 });

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
                        Point pt = coord.ToPlotCoordinates(new double[] { AlignmentData[kvp.Key].medianLength, kvp.Value.filteredLength + 1 });

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
                    double minX = AlignmentData.Where(x => x.Key != "LSU" && x.Key != "SSU").Select(x => x.Value.medianLength + 1).Min();
                    double maxX = AlignmentData.Where(x => x.Key != "LSU" && x.Key != "SSU").Select(x => x.Value.medianLength + 1).Max();

                    double maxY = benchmarkResults.SelectMany(x => x.Where(y => y.Key != "LSU" && y.Key != "SSU").Select(x => x.Value.filteredLength + 1)).Max();

                    Point p1 = coord.ToPlotCoordinates(new double[] { minX, maxY });
                    Point p2 = coord.ToPlotCoordinates(new double[] { maxX, maxY });



                    gpr.StrokePath(new GraphicsPath().MoveTo(p1 + new Point(-5, -5)).LineTo(p1 + new Point(-5, -15)).LineTo(p2 + new Point(5, -15)).LineTo(p2 + new Point(5, -5)), Colours.Black);
                    gpr.FillText((p1 + p2) * 0.5 + new Point(-labelFont.MeasureText("GTDB bac120").Width * 0.5, -21), "GTDB bac120", labelFont, Colours.Black, TextBaselines.Baseline);
                }

                {
                    double xLSU = AlignmentData["LSU"].medianLength + 1;
                    double xSSU = AlignmentData["SSU"].medianLength + 1;

                    double minYLSU = benchmarkResults.SelectMany(x => x.Where(y => y.Key == "LSU").Select(x => x.Value.filteredLength + 1)).Min();
                    double minYSSU = benchmarkResults.SelectMany(x => x.Where(y => y.Key == "SSU").Select(x => x.Value.filteredLength + 1)).Min();

                    Point p1 = coord.ToPlotCoordinates(new double[] { xSSU, minYLSU });
                    Point p2 = coord.ToPlotCoordinates(new double[] { xLSU, minYLSU });
                    Point p3 = coord.ToPlotCoordinates(new double[] { xSSU, minYSSU });

                    gpr.FillText(p2 + new Point(-labelFont.MeasureText("LSU").Width * 0.5, 11), "LSU", labelFont, Colours.Black);
                    gpr.FillText(p3 + new Point(-labelFont.MeasureText("SSU").Width * 0.5, -11), "SSU", labelFont, Colours.Black, TextBaselines.Bottom);

                    gpr.StrokePath(new GraphicsPath().MoveTo(p1 + new Point(-5, 40)).LineTo(p1 + new Point(-5, 50)).LineTo(p2 + new Point(5, 50)).LineTo(p2 + new Point(5, 40)), Colours.Black);

                    gpr.FillText((p1 + p2) * 0.5 + new Point(-labelFont.MeasureText("SILVA Ref NR99").Width * 0.5, 55), "SILVA Ref NR99", labelFont, Colours.Black);
                }

            }));

            // Add the y = x line
            plot.AddPlotElement(new LinearTrendLine(1, 0, minUnfilteredLength, minFilteredLength, maxUnfilteredLength, maxFilteredLength, plot.GetFirst<IContinuousCoordinateSystem>()) { PresentationAttributes = new PlotElementPresentationAttributes() { Stroke = Colour.FromRgb(128, 128, 128), LineDash = new LineDash(5, 7, 0), LineWidth = 2 } });

            return plot.Render();
        }
    }
}
