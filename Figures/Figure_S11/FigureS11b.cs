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

using PhyloTree.Formats;
using PhyloTree;
using VectSharp.Plots;
using VectSharp;
using VectSharp.Filters;
using MathNet.Numerics.Statistics;
using MathNet.Numerics;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Math.Optimization.Losses;

namespace Figure_S11
{
    internal partial class Program
    {
        static Page CreateFigureS11b()
        {
            // Colour to use for each tool.
            Dictionary<string, Colour> toolColours = new Dictionary<string, Colour>()
            {
                { "reference", Colour.FromRgb(0, 0, 0) },
                { "simulated", Colour.FromRgb(128, 128, 128) },
                { "raw", Colour.FromRgb(80, 80, 80) },
                { "alifilter", Colour.FromRgb(119, 170, 221) },
                { "bmge",    Colour.FromRgb(238, 136, 102) },
                { "trimal",    Colour.FromRgb(238, 221, 136) },
                { "gblocks",    Colour.FromRgb(255, 170, 187) },
                { "noisy",   Colour.FromRgb(153, 221, 255) },
                { "clipkit",   Colour.FromRgb(187, 204, 51) },
            };

            // Symbols for each tool.
            GraphicsPath circle = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close();
            GraphicsPath square = new GraphicsPath().MoveTo(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1).Close();
            GraphicsPath diamond = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close();
            GraphicsPath triangle = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close();
            GraphicsPath star = new GraphicsPath();
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    star.LineTo(Math.Cos(i * 0.1 * Math.PI * 2), Math.Sin(i * 0.1 * Math.PI * 2));
                }
                else
                {
                    star.LineTo(Math.Cos(i * 0.1 * Math.PI * 2) * 0.5, Math.Sin(i * 0.1 * Math.PI * 2) * 0.5);
                }
            }
            star.Close();

            Dictionary<string, (GraphicsPath, bool, double)> toolShapes = new Dictionary<string, (GraphicsPath, bool, double)>()
            {
                { "reference", (star, true, 4.5) },
                { "simulated", (square, true, 3.5) },
                { "raw", (circle, true, 4.5) },
                { "alifilter", (triangle, true, 5) },
                { "bmge", (diamond, true, 5) },
                { "trimal", (triangle, false, 4) },
                { "gblocks", (circle, false, 4.5) },
                { "noisy", (diamond, false, 5) },
                { "clipkit", (square, false, 3.5) },
            };

            Dictionary<string, string> toolNames = new Dictionary<string, string>()
            {
                { "reference", "Reference" },
                { "simulated", "Simulated" },
                { "raw", "Unfiltered" },
                { "alifilter", "AliFilter" },
                { "bmge", "BMGE" },
                { "trimal", "trimAl" },
                { "gblocks", "Gblocks" },
                { "noisy", "Noisy" },
                { "clipkit", "ClipKIT" }
            };

            string[] tools = new string[] { "simulated", "raw", "alifilter", "bmge", "trimal", "gblocks", "noisy", "clipkit", "reference" };

            // Read the ML trees.
            TreeNode[][] mlTrees = ReadMLTrees(tools, false);

            // Get a leaf name for arbitrary rerooting.
            string outgroupName = mlTrees[0][0].GetLeafNames()[0];

            // Length estimates for each branch.
            double[][][] branchLenghts = new double[mlTrees[0][0].GetChildrenRecursiveLazy().Count() - 1][][];
            for (int i = 0; i < branchLenghts.Length; i++)
            {
                branchLenghts[i] = new double[tools.Length][];
                for (int j = 0; j < mlTrees.Length; j++)
                {
                    branchLenghts[i][j] = new double[mlTrees[j].Length];
                }
            }

            double minLengthX = double.MaxValue;
            double maxLengthX = double.MinValue;
            double minLengthY = double.MaxValue;
            double maxLengthY = double.MinValue;

            for (int i = 0; i < mlTrees.Length; i++)
            {
                for (int j = 0; j < mlTrees[i].Length; j++)
                {
                    // Arbitrarily root, unroot, and sort each treee so that the nodes are always in the same order.
                    if (mlTrees[i][j].IsRooted())
                    {
                        mlTrees[i][j] = mlTrees[i][j].GetUnrootedTree();
                    }
                    mlTrees[i][j] = mlTrees[i][j].GetRootedTree(mlTrees[i][j].GetNodeFromName(outgroupName)).GetUnrootedTree();
                    mlTrees[i][j].SortNodes(true);

                    // Store the branch lengths (skipping the root node).
                    int ind = 0;
                    foreach (TreeNode node in mlTrees[i][j].GetChildrenRecursiveLazy().Skip(1))
                    {
                        double len = node.Length;

                        branchLenghts[ind][i][j] = len;

                        if (i < tools.Length - 1)
                        {
                            maxLengthY = Math.Max(maxLengthY, len);
                            minLengthY = Math.Min(minLengthY, len);
                        }
                        else
                        {
                            maxLengthX = Math.Max(maxLengthX, len);
                            minLengthX = Math.Min(minLengthX, len);
                        }

                        ind++;
                    }
                }
            }

            Plot plot = Plot.Create.ScatterPlot(new double[][] { new double[] { Math.Log10(minLengthX), Math.Log10(minLengthY) }, new double[] { Math.Log10(maxLengthX), Math.Log10(maxLengthY) } }, width: 620, height: 400, xAxisTitle: "Reference branch length", yAxisTitle: "Estimated branch length");
            plot.RemovePlotElement(plot.GetFirst<ScatterPoints<IReadOnlyList<double>>>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxisLabels>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxisLabels>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxisTicks>());
            plot.RemovePlotElement(plot.GetFirst<ContinuousAxisTicks>());
            plot.RemovePlotElement(plot.GetFirst<Grid>());
            plot.RemovePlotElement(plot.GetFirst<Grid>());

            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coords) =>
            {
                double[] yTicks = Enumerable.Range(3, 7).Select(x => x * 0.0001).Concat(Enumerable.Range(1, 9).Select(x => x * 0.001)).Concat(Enumerable.Range(1, 9).Select(x => x * 0.01)).Concat(Enumerable.Range(1, 10).Select(x => x * 0.1)).ToArray();
                double[] yLabels = new double[] { 0.0005, 0.001, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1 };

                double[] xTicks = Enumerable.Range(2, 8).Select(x => x * 0.001).Concat(Enumerable.Range(1, 9).Select(x => x * 0.01)).Concat(Enumerable.Range(1, 4).Select(x => x * 0.1)).ToArray();
                double[] xLabels = new double[] { 0.002, 0.005, 0.01, 0.02, 0.05, 0.1, 0.2, 0.4 };

                double xLeft = plot.GetFirst<ContinuousAxis>().StartPoint[0];
                double xRight = plot.GetFirst<ContinuousAxis>().EndPoint[0];

                double yBottom = plot.GetAll<ContinuousAxis>().ElementAt(1).StartPoint[1];
                double yTop = plot.GetAll<ContinuousAxis>().ElementAt(1).EndPoint[1];

                Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);

                for (int i = 0; i < yTicks.Length; i++)
                {
                    if (!yLabels.Contains(yTicks[i]))
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(coords.ToPlotCoordinates(new double[] { xLeft, Math.Log10(yTicks[i]) })).LineTo(coords.ToPlotCoordinates(new double[] { xRight, Math.Log10(yTicks[i]) })), Colour.FromRgb(220, 220, 220));

                        gpr.StrokePath(new GraphicsPath().MoveTo(coords.ToPlotCoordinates(new double[] { xLeft, Math.Log10(yTicks[i]) }) + new Point(-2.5, 0)).LineTo(coords.ToPlotCoordinates(new double[] { xLeft, Math.Log10(yTicks[i]) }) + new Point(2.5, 0)), Colours.Black);
                    }
                    else
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(coords.ToPlotCoordinates(new double[] { xLeft, Math.Log10(yTicks[i]) })).LineTo(coords.ToPlotCoordinates(new double[] { xRight, Math.Log10(yTicks[i]) })), Colour.FromRgb(180, 180, 180));

                        gpr.StrokePath(new GraphicsPath().MoveTo(coords.ToPlotCoordinates(new double[] { xLeft, Math.Log10(yTicks[i]) }) + new Point(-3.5, 0)).LineTo(coords.ToPlotCoordinates(new double[] { xLeft, Math.Log10(yTicks[i]) }) + new Point(3.5, 0)), Colours.Black);
                    }
                }

                for (int i = 0; i < yLabels.Length; i++)
                {
                    gpr.FillText(coords.ToPlotCoordinates(new double[] { xLeft, Math.Log10(yLabels[i]) }) + new Point(-10 - fnt.MeasureText(yLabels[i].ToString()).Width, 0), yLabels[i].ToString(), fnt, Colours.Black, TextBaselines.Middle);
                }

                for (int i = 0; i < xTicks.Length; i++)
                {
                    if (!xLabels.Contains(xTicks[i]))
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(coords.ToPlotCoordinates(new double[] { Math.Log10(xTicks[i]), yBottom })).LineTo(coords.ToPlotCoordinates(new double[] { Math.Log10(xTicks[i]), yTop })), Colour.FromRgb(220, 220, 220));
                        gpr.StrokePath(new GraphicsPath().MoveTo(coords.ToPlotCoordinates(new double[] { Math.Log10(xTicks[i]), yBottom }) + new Point(0, -2.5)).LineTo(coords.ToPlotCoordinates(new double[] { Math.Log10(xTicks[i]), yBottom }) + new Point(0, 2.5)), Colours.Black);
                    }
                    else
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(coords.ToPlotCoordinates(new double[] { Math.Log10(xTicks[i]), yBottom })).LineTo(coords.ToPlotCoordinates(new double[] { Math.Log10(xTicks[i]), yTop })), Colour.FromRgb(180, 180, 180));
                        gpr.StrokePath(new GraphicsPath().MoveTo(coords.ToPlotCoordinates(new double[] { Math.Log10(xTicks[i]), yBottom }) + new Point(0, -3.5)).LineTo(coords.ToPlotCoordinates(new double[] { Math.Log10(xTicks[i]), yBottom }) + new Point(0, 3.5)), Colours.Black);
                    }
                }

                for (int i = 0; i < xLabels.Length; i++)
                {
                    gpr.FillText(coords.ToPlotCoordinates(new double[] { Math.Log10(xLabels[i]), yBottom }) + new Point(-fnt.MeasureText(xLabels[i].ToString()).Width * 0.5, 12), xLabels[i].ToString(), fnt, Colours.Black, TextBaselines.Middle);
                }

            }));


            double[] xValues = new double[branchLenghts.Length];
            double[][] yValues = new double[tools.Length][];
            for (int i = 0; i < tools.Length; i++)
            {
                yValues[i] = new double[branchLenghts.Length];
            }

            for (int i = 0; i < branchLenghts.Length; i++)
            {
                xValues[i] = branchLenghts[i][^1].Single();

                for (int j = 0; j < tools.Length - 1; j++)
                {
                    yValues[j][i] = branchLenghts[i][j].Median();
                }
            }

            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Random rnd = new Random(123456);

                for (int i = 0; i < branchLenghts.Length; i++)
                {
                    double x = Math.Log10(xValues[i]);

                    List<int> sortedIndices = new List<int>();

                    List<int> allIndices = Enumerable.Range(0, 8).ToList();

                    while (allIndices.Count > 0)
                    {
                        int ind = rnd.Next(allIndices.Count);
                        sortedIndices.Add(allIndices[ind]);
                        allIndices.RemoveAt(ind);
                    }

                    for (int j = 0; j < tools.Length - 1; j++)
                    {
                        double y = Math.Log10(yValues[sortedIndices[j]][i]);

                        Point pt = coord.ToPlotCoordinates(new double[] { x, y });

                        Colour col = toolColours[tools[sortedIndices[j]]];
                        GraphicsPath shape = toolShapes[tools[sortedIndices[j]]].Item1;
                        bool filled = toolShapes[tools[sortedIndices[j]]].Item2;
                        double shapeSize = toolShapes[tools[sortedIndices[j]]].Item3;

                        gpr.Save();
                        gpr.Translate(pt);
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
                }
            }));

            double[] slopes = new double[tools.Length - 1];

            for (int i = 0; i < tools.Length - 1; i++)
            {
                OrdinaryLeastSquares ols = new OrdinaryLeastSquares() { UseIntercept = false };
                SimpleLinearRegression regression = ols.Learn(xValues, yValues[i]);

                double slope = regression.Slope;
                slopes[i] = slope;

                plot.AddPlotElement(new LinearTrendLine(1, Math.Log10(slope), Math.Log10(minLengthX), Math.Log10(minLengthY), Math.Log10(maxLengthX), Math.Log10(maxLengthY), plot.GetFirst<IContinuousCoordinateSystem>())
                {
                    PresentationAttributes = new PlotElementPresentationAttributes() { Fill = null, Stroke = Colours.White, LineWidth = 4 }
                });
            }

            for (int i = 0; i < tools.Length - 1; i++)
            {
                plot.AddPlotElement(new LinearTrendLine(1, Math.Log10(slopes[i]), Math.Log10(minLengthX), Math.Log10(minLengthY), Math.Log10(maxLengthX), Math.Log10(maxLengthY), plot.GetFirst<IContinuousCoordinateSystem>())
                {
                    PresentationAttributes = new PlotElementPresentationAttributes() { Fill = null, Stroke = toolColours[tools[i]], LineWidth = 2, LineDash = new LineDash(5, 5, i * 2.5), LineCap = LineCaps.Butt }
                });
            }

            int[] slopeOrder = slopes.Select((x, i) => (x, i)).OrderByDescending(y => y.x).Select(y => y.i).ToArray();

            Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);

            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Point topLeft = coord.ToPlotCoordinates(new double[] { Math.Log10(maxLengthX), Math.Log10(maxLengthY) }) + new Point(20, -30);

                for (int i = 0; i < slopeOrder.Length; i++)
                {
                    Point pt = new Point(topLeft.X + 10, topLeft.Y + i * 20);
                    Point pt2 = coord.ToPlotCoordinates(new double[] { Math.Log10(maxLengthX), Math.Log10(maxLengthX * slopes[slopeOrder[i]]) });

                    pt2 = pt + (pt2 - pt) * (((pt2 - pt).Modulus() - 5) / (pt2 - pt).Modulus());

                    gpr.StrokePath(new GraphicsPath().MoveTo(pt).LineTo(pt2), toolColours[tools[slopeOrder[i]]]);

                    double arrowSize = 4;

                    gpr.Save();
                    gpr.Translate(pt2);
                    gpr.Rotate(Math.Atan2(pt2.Y - pt.Y, pt2.X - pt.X));
                    gpr.FillPath(new GraphicsPath().MoveTo(-arrowSize, -arrowSize).LineTo(-arrowSize, arrowSize).LineTo(arrowSize, 0).Close(), toolColours[tools[slopeOrder[i]]]);
                    gpr.Restore();

                    gpr.FillRectangle(topLeft.X + 5, topLeft.Y + i * 20 - 8, fnt.MeasureText(toolNames[tools[slopeOrder[i]]] + $" ({slopes[slopeOrder[i]]:0.00})").Width + 15 + 10, 16, Colours.White);
                    gpr.StrokeRectangle(topLeft.X + 5, topLeft.Y + i * 20 - 8, fnt.MeasureText(toolNames[tools[slopeOrder[i]]] + $" ({slopes[slopeOrder[i]]:0.00})").Width + 15 + 10, 16, Colours.Black);

                    Colour col = toolColours[tools[slopeOrder[i]]];
                    GraphicsPath shape = toolShapes[tools[slopeOrder[i]]].Item1;
                    bool filled = toolShapes[tools[slopeOrder[i]]].Item2;
                    double shapeSize = toolShapes[tools[slopeOrder[i]]].Item3;

                    gpr.Save();
                    gpr.Translate(topLeft.X + 15, topLeft.Y + i * 20);
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

                    gpr.FillText(topLeft.X + 25, topLeft.Y + i * 20 + 4, toolNames[tools[slopeOrder[i]]] + $" ({slopes[slopeOrder[i]]:0.00})", fnt, Colours.Black, TextBaselines.Baseline);
                }


            }));

            return plot.Render();
        }
    }
}
