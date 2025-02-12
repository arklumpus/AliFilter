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

using VectSharp.Plots;
using VectSharp;
using MathNet.Numerics.Statistics;

namespace Figures_5_S4_Table_S2
{
    internal static partial class Program
    {
        static Page CreateFigure5a()
        {
            // Read the runtime stats.
            Dictionary<string, (int alignmentLength, int distinctPatterns, int ramRequired, List<double> runtimeHours, List<double> mlTreeLength)> results = ReadRuntimeStats();

            // Bottom-left and top-right corners.
            double[][] rangePoints = new double[][] { new double[] { 40000, 24 }, new double[] { 82564, 54 } };

            // Colours to use for each tool.
            Dictionary<string, Colour> toolColours = new Dictionary<string, Colour>()
            {
                { "raw", Colour.FromRgb(128, 128, 128) },
                { "alifilter", Colour.FromRgb(119, 170, 221) },
                { "bmge",    Colour.FromRgb(238, 136, 102) },
                { "trimal",    Colour.FromRgb(238, 221, 136) },
                { "gblocks",    Colour.FromRgb(255, 170, 187) },
                { "noisy",   Colour.FromRgb(153, 221, 255) },
                { "clipkit",   Colour.FromRgb(187, 204, 51) },
            };

            // Symbols to use for each tool.
            GraphicsPath circle = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close();
            GraphicsPath square = new GraphicsPath().MoveTo(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1).Close();
            GraphicsPath diamond = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close();
            GraphicsPath triangle = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close();

            Dictionary<string, (GraphicsPath, bool, double)> toolShapes = new Dictionary<string, (GraphicsPath, bool, double)>()
            {
                { "raw", (circle, true, 4.5) },
                { "alifilter", (triangle, true, 5) },
                { "bmge", (diamond, true, 5) },
                { "trimal", (triangle, false, 4) },
                { "gblocks", (circle, false, 4.5) },
                { "noisy", (diamond, false, 5) },
                { "clipkit", (square, false, 3.5) },
            };

            Dictionary<string, (string, bool)> toolNames = new Dictionary<string, (string, bool)>()
            {
                { "raw", ("Unfiltered", true) },
                { "alifilter", ("AliFilter", false) },
                { "bmge", ("BMGE", false) },
                { "trimal", ("trimAl", true) },
                { "gblocks", ("Gblocks", true) },
                { "noisy", ("Noisy", true) },
                { "clipkit", ("ClipKIT", true) }
            };

            // Create the scatter plot.
            Plot scatterPlot = Plot.Create.ScatterPlot(rangePoints, xAxisTitle: "Alignment length", yAxisTitle: "Runtime (h)", width: 250);
            scatterPlot.RemovePlotElement(scatterPlot.GetFirst<ScatterPoints<IReadOnlyList<double>>>());

            // Fine-tune the plot appearance.
            scatterPlot.GetAll<ContinuousAxisLabels>().ElementAt(1).TextFormat = (x, i) => FormattedText.Format(x[1].ToString("0"), FontFamily.StandardFontFamilies.Helvetica, 12);
            scatterPlot.GetAll<ContinuousAxisTitle>().ElementAt(1).Position *= 0.75;

            ((double[])scatterPlot.GetFirst<ContinuousAxisTicks>().EndPoint)[0] = 80000;
            scatterPlot.GetFirst<ContinuousAxisTicks>().IntervalCount = 4;
            ((double[])scatterPlot.GetFirst<ContinuousAxisLabels>().EndPoint)[0] = 80000;
            scatterPlot.GetFirst<ContinuousAxisLabels>().IntervalCount = 4;
            ((double[])scatterPlot.GetFirst<Grid>().Side1End)[0] = 80000;
            scatterPlot.GetFirst<Grid>().IntervalCount = 4;
            scatterPlot.GetFirst<ContinuousAxisTitle>().Position *= 1.15;

            scatterPlot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(scatterPlot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 10);

                foreach (KeyValuePair<string, (int alignmentLength, int distinctPatterns, int ramRequired, List<double> runtimeHours, List<double> mlTreeLength)> kvp in results.OrderBy(x => x.Value.alignmentLength))
                {
                    Point ptMin = coord.ToPlotCoordinates(new double[] { kvp.Value.alignmentLength, kvp.Value.runtimeHours.Min() });
                    Point ptMax = coord.ToPlotCoordinates(new double[] { kvp.Value.alignmentLength, kvp.Value.runtimeHours.Max() });
                    Point ptMedian = coord.ToPlotCoordinates(new double[] { kvp.Value.alignmentLength, kvp.Value.runtimeHours.Median() });

                    GraphicsPath pth = new GraphicsPath().MoveTo(ptMin.X - 4, ptMin.Y).LineTo(ptMin.X + 4, ptMin.Y)
                                                         .MoveTo(ptMin).LineTo(ptMax)
                                                         .MoveTo(ptMax.X - 4, ptMax.Y).LineTo(ptMax.X + 4, ptMax.Y);

                    Colour col = toolColours[kvp.Key];
                    gpr.StrokePath(pth, col, 2);
                }

                foreach (KeyValuePair<string, (int alignmentLength, int distinctPatterns, int ramRequired, List<double> runtimeHours, List<double> mlTreeLength)> kvp in results.OrderBy(x => x.Value.alignmentLength))
                {
                    Point ptMedian = coord.ToPlotCoordinates(new double[] { kvp.Value.alignmentLength, kvp.Value.runtimeHours.Median() });

                    Colour col = toolColours[kvp.Key];
                    GraphicsPath shape = toolShapes[kvp.Key].Item1;
                    bool filled = toolShapes[kvp.Key].Item2;
                    double shapeSize = toolShapes[kvp.Key].Item3;

                    gpr.Save();
                    gpr.Translate(ptMedian);
                    gpr.Scale(shapeSize, shapeSize);

                    if (filled)
                    {
                        gpr.FillPath(shape, col);
                    }
                    else
                    {
                        gpr.FillPath(shape, Colours.White);
                        gpr.StrokePath(shape, col, 2.0 / shapeSize);
                    }

                    gpr.Restore();
                }
            }));

            return scatterPlot.Render();
        }
    }
}
