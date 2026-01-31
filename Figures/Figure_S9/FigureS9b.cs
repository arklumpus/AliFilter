/*
    AliFilter: A Machine Learning Approach to Alignment Filtering

    by Giorgio Bianchini, Rui Zhu, Francesco Cicconardi, Edmund RR Moody

    Source code for manuscript figures.

    Copyright (C) 2025  Giorgio Bianchini
 
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

namespace Figure_S9
{
    internal partial class Program
    {
        static Page CreateFigureS9b(bool useCache = true)
        {
            // Number of replicate ML analyses for each tool.
            int replicates = 3;

            // Number of bootstrap replicates to plot for each tool.
            int sampleSize = 300;

            // Colour to use for each tool.
            Dictionary<string, Colour> toolColours = new Dictionary<string, Colour>()
            {
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
                { "raw", (circle, true, 4.5) },
                { "alifilter", (triangle, true, 5) },
                { "bmge", (diamond, true, 5) },
                { "trimal", (triangle, false, 4) },
                { "gblocks", (circle, false, 4.5) },
                { "noisy", (diamond, false, 5) },
                { "clipkit", (square, false, 3.5) },
            };

            // Manually fixed positions for the tool names.
            Dictionary<string, double[][]> toolNamePositions = new Dictionary<string, double[][]>()
            {
                { "raw", new double[][]{ new double[] { -2, 7 }, new double[] { 1.8, 1.3 } } },
                { "alifilter", new double[][]{ new double[] { 2.7, 6 }, new double[] { 2.2, 1.8 } } },
                { "bmge", new double[][]{ new double[] { -3, 4 }, new double[]{ 1.5, 0.9 } } },
                { "trimal", new double[][]{ new double[] { 3.2, -5.2 }, new double[]{ 2.3, -1 } } },
                { "gblocks", new double[][]{ new double[] { -10, -3 }, new double[]{ -14.8, 0.6 }, new double[] { -7.3, -4.5 } } },
                { "noisy", new double[][] { new double[] { -4, 1 }, new double[] { -0.9, -0.2 } } },
                { "clipkit", new double[][]{ new double[] { -1, -7.7 }, new double[] { 1.8, -0.8 } } }
            };

            Dictionary<string, string> toolNames = new Dictionary<string, string>()
            {
                { "raw", "Unfiltered" },
                { "alifilter", "AliFilter" },
                { "bmge", "BMGE" },
                { "trimal", "trimAl" },
                { "gblocks", "Gblocks" },
                { "noisy", "Noisy" },
                { "clipkit", "ClipKIT" }
            };

            string[] tools = new string[] { "raw", "alifilter", "bmge", "trimal", "gblocks", "noisy", "clipkit" };

            Dictionary<string, int> toolIndices = tools.Select((x, i) => new KeyValuePair<string, int>(x, i)).ToDictionary();

            // Compute the 2D tree coordinates induced by the Robinson-Foulds distance.
            (double[][] treeCoordinates, float[][] mlTreeDistances) = GetTreeCoordinatesRobinsonFoulds(useCache, tools, sampleSize, replicates);

            // Create the scatter plot.
            Plot plot = Plot.Create.ScatterPlot(treeCoordinates, width: 450, height: 250, xAxisTitle: "Coordinate 1", yAxisTitle: "Coordinate 2");

            plot.GetAll<ContinuousAxisTitle>().ElementAt(1).Position += 10;

            // Fine-tune the plot appearance.
            plot.RemovePlotElement(plot.GetFirst<ScatterPoints<IReadOnlyList<double>>>());
            plot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(plot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Random rnd = new Random();

                for (int i = 0; i < tools.Length; i++)
                {
                    for (int j = 0; j < sampleSize; j++)
                    {
                        Point pt = coord.ToPlotCoordinates(treeCoordinates[tools.Length * replicates + i * sampleSize + j]);

                        Colour col = toolColours[tools[i]];
                        GraphicsPath shape = toolShapes[tools[i]].Item1;
                        bool filled = toolShapes[tools[i]].Item2;
                        double shapeSize = toolShapes[tools[i]].Item3;

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

                Graphics blurGpr = new Graphics();

                for (int i = 0; i < tools.Length; i++)
                {
                    for (int j = 0; j < replicates; j++)
                    {
                        Point pt = coord.ToPlotCoordinates(treeCoordinates[i * replicates + j]);
                        GraphicsPath shape = star;
                        double shapeSize = 6;

                        blurGpr.Save();
                        blurGpr.Translate(pt);
                        blurGpr.Scale(shapeSize, shapeSize);

                        blurGpr.FillPath(shape, Colours.White);
                        blurGpr.StrokePath(shape, Colours.White, 3.0 / shapeSize);

                        blurGpr.Restore();
                    }
                }

                gpr.DrawGraphics(0, 0, blurGpr, new GaussianBlurFilter(0.5));

                Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);

                for (int i = 0; i < tools.Length; i++)
                {
                    for (int j = 0; j < replicates; j++)
                    {
                        Point pt = coord.ToPlotCoordinates(treeCoordinates[i * replicates + j]);

                        Colour col = toolColours[tools[i]];
                        GraphicsPath shape = star;
                        double shapeSize = 6;

                        gpr.Save();
                        gpr.Translate(pt);
                        gpr.Scale(shapeSize, shapeSize);

                        if (tools[i] == "clipkit" || tools[i] == "alifilter" || tools[i] == "raw" || tools[i] == "bmge" || tools[i] == "trimal")
                        {
                            int index = tools[i] switch
                            {
                                "clipkit" => 1,
                                "alifilter" => 4,
                                "raw" => 3,
                                "bmge" => 2,
                                "trimal" => 0,
                                _ => 0
                            };
                            GraphicsPath clippingPath = new GraphicsPath().MoveTo(0, 0).Arc(0, 0, 1.6, 2 * Math.PI / 5 * index, 2 * Math.PI / 5 * (index + 1)).Close();
                            gpr.SetClippingPath(clippingPath);
                        }

                        gpr.FillPath(shape, col);

                        gpr.Restore();
                    }

                    if (toolNamePositions.TryGetValue(tools[i], out double[][] toolPos))
                    {
                        string toolName = toolNames[tools[i]];
                        double arrowSize = 3;

                        Point pt = coord.ToPlotCoordinates(toolPos[0]);

                        for (int j = 1; j < toolPos.Length; j++)
                        {
                            Point pt2 = coord.ToPlotCoordinates(toolPos[j]);

                            gpr.StrokePath(new GraphicsPath().MoveTo(pt.X - (fnt.MeasureText(toolName).Width + 13) * 0.5 - 13 - 4 + (fnt.MeasureText(toolName).Width + 13 + 10) * 0.5, pt.Y).LineTo(pt2), Colours.Black);
                            gpr.Save();
                            gpr.Translate(pt2);
                            gpr.Rotate(Math.Atan2(pt2.Y - pt.Y, pt2.X - (pt.X - (fnt.MeasureText(toolName).Width + 13) * 0.5 - 13 - 4 + (fnt.MeasureText(toolName).Width + 13 + 10) * 0.5)));
                            gpr.FillPath(new GraphicsPath().MoveTo(-arrowSize, -arrowSize).LineTo(-arrowSize, arrowSize).LineTo(arrowSize, 0).Close(), Colours.Black);
                            gpr.Restore();
                        }

                        gpr.FillRectangle(pt.X - (fnt.MeasureText(toolName).Width + 13) * 0.5 - 13 - 4, pt.Y - 9, fnt.MeasureText(toolName).Width + 13 + 10, 18, Colours.White);
                        gpr.StrokeRectangle(pt.X - (fnt.MeasureText(toolName).Width + 13) * 0.5 - 13 - 4, pt.Y - 9, fnt.MeasureText(toolName).Width + 13 + 10, 18, Colours.Black);

                        Colour col = toolColours[tools[i]];
                        GraphicsPath shape = toolShapes[tools[i]].Item1;
                        bool filled = toolShapes[tools[i]].Item2;
                        double shapeSize = toolShapes[tools[i]].Item3;

                        gpr.Save();
                        gpr.Translate(pt.X - (fnt.MeasureText(toolName).Width + 13) * 0.5 - 13 + shapeSize, pt.Y);
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

                        gpr.FillText(pt.X - (fnt.MeasureText(toolName).Width + 11) * 0.5, pt.Y, toolName, fnt, Colours.Black, TextBaselines.Middle);
                    }
                }
            }));

            // Render the plot.
            Page figureS9b = plot.Render();

            return figureS9b;
        }

        /// <summary>
        /// Compute the 2D tree coordinates induced by the Robinson-Foulds distance.
        /// </summary>
        /// <param name="useCache">If this is true, the results of each step are cached and reused, in order to make it easier to make small changes to the code without having to recompute everything.</param>
        /// <param name="tools">The names of the tools.</param>
        /// <param name="sampleSize">The number of UFBoot replicates to preserve for each tool.</param>
        /// <param name="replicates">The number of replicate ML analyses for each tool.</param>
        /// <returns>The 2D tree coordinates for each tree.</returns>
        static (double[][], float[][]) GetTreeCoordinatesRobinsonFoulds(bool useCache, string[] tools, int sampleSize, int replicates)
        {
            double[][] treeCoordinates;

            if (!useCache || !File.Exists("Cache/FigureS9b_coordinates.txt"))
            {
                float[][] distanceMatrixOfTrees;

                if (!useCache || !File.Exists("Cache/FigureS9b_distMat.bin"))
                {
                    TreeNode[][] subsampledTrees;

                    if (!useCache || !File.Exists("Cache/FigureS9b_raw.tbi"))
                    {
                        // Step 1: subsample the UFBoot replicates, only preserving the requested number of trees.

                        // Read all the UFBoot replicates.
                        TreeNode[][] allTrees = ReadUFBootTrees(tools);

                        // Subsample the replicates.
                        subsampledTrees = SubsampleTrees(allTrees, sampleSize, CreateRobinsonFouldsDistanceMatrixOfTrees);

                        if (useCache)
                        {
                            // Save the subsampled tree list in the cache.
                            Directory.CreateDirectory("Cache");
                            for (int i = 0; i < tools.Length; i++)
                            {
                                BinaryTree.WriteAllTrees(subsampledTrees[i], "Cache/FigureS9b_" + tools[i] + ".tbi");
                            }
                        }
                    }
                    else
                    {
                        // Reuse the cached subsampled trees.
                        subsampledTrees = new TreeNode[tools.Length][];

                        for (int i = 0; i < tools.Length; i++)
                        {
                            subsampledTrees[i] = BinaryTree.ParseAllTrees("Cache/FigureS9b_" + tools[i] + ".tbi").ToArray();
                        }
                    }

                    // Step 2: create a distance matrix of trees according to the Frobenius distance metric.

                    // Read the ML trees.
                    TreeNode[][] mlTrees = ReadMLTrees(tools);

                    // Concatenate all the trees.
                    TreeNode[] joinedTrees = mlTrees.Aggregate(Enumerable.Empty<TreeNode>(), (a, b) => a.Concat(b)).Concat(subsampledTrees.Aggregate(Enumerable.Empty<TreeNode>(), (a, b) => a.Concat(b))).ToArray();

                    // Create the distance matrix of trees.
                    distanceMatrixOfTrees = CreateDistanceMatrixOfTrees(joinedTrees, CreateRobinsonFouldsDistanceMatrixOfTrees);

                    if (useCache)
                    {
                        // Save the computed distance matrix in the cache.
                        SaveDistanceMatrix("Cache/FigureS9b_distMat.bin", distanceMatrixOfTrees);
                    }
                }
                else
                {
                    // Reuse the cached distance matrix of trees.
                    distanceMatrixOfTrees = ReadDistanceMatrix("Cache/FigureS9b_distMat.bin");
                }

                // Step 3: use the distance matrix of trees to perform a classical MDS extracting the first two coordinates.
                treeCoordinates = PerformMDS(distanceMatrixOfTrees, 2);

                if (useCache)
                {
                    // Save the tree coordinates.
                    SaveTreeCoordinates("Cache/FigureS9b_coordinates.txt", treeCoordinates);
                }
            }
            else
            {
                // Reuse the cached coordinates.
                treeCoordinates = ReadTreeCoordinates("Cache/FigureS9b_coordinates.txt");
            }

            // Compute distances between the ML trees (used to add the distances on the scatter plot).
            TreeNode[] allMlTrees = ReadMLTrees(tools).Aggregate(Enumerable.Empty<TreeNode>(), (a, b) => a.Concat(b)).ToArray();

            return (treeCoordinates, CreateRobinsonFouldsDistanceMatrixOfTrees(allMlTrees, _ => { }));
        }

        /// <summary>
        /// Creates a distance matrix from a set of trees, according to the Robinson-Foulds metric.
        /// </summary>
        /// <param name="allTrees">The trees that will be used to compute the distance matrix.</param>
        /// <param name="progressAction">A progress callback.</param>
        /// <returns>A distance matrix of trees.</returns>
        static float[][] CreateRobinsonFouldsDistanceMatrixOfTrees(TreeNode[] allTrees, Action<double> progressAction)
        {
            double[,] rfDistances = TreeNode.RobinsonFouldsDistances(allTrees, false, progress: new Progress<double>(progressAction));

            float[][] distanceMatrixOfTrees = new float[allTrees.Length][];

            for (int i = 0; i < allTrees.Length; i++)
            {
                distanceMatrixOfTrees[i] = new float[i];
                for (int j = 0; j < i; j++)
                {
                    distanceMatrixOfTrees[i][j] = (float)rfDistances[i, j];
                }
            }

            return distanceMatrixOfTrees;
        }
    }
}
