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

using PhyloTree.Formats;
using PhyloTree;
using VectSharp;
using VectSharp.Plots;
using VectSharp.Filters;
using VectSharp.SVG;
using VectSharp.PDF;
using VectSharp.Raster;

namespace Figures_5_S4_Table_S2
{
    internal static partial class Program
    {
        static void CreateFigureS4(bool useCache = true)
        {

            // Number of replicate ML analyses for each tool.
            int replicates = 3;

            // Number of bootstrap replicates to plot for each tool.
            int sampleSize = 300;

            // Colour to use for each tool.
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
                { "raw", new double[][]{ new double[] { -9.5, 13 }, new double[] { -8.5, 0.5 } } },
                { "alifilter", new double[][]{ new double[] { 6, 13 }, new double[] { -7.5, 0.2 } } },
                { "bmge", new double[][]{ new double[] { -5, -12.5 }, new double[]{ -2, -9 } } },
                { "trimal", new double[][]{ new double[] { 15, -12.5 }, new double[]{ 7.7, -6.3 } } },
                { "gblocks", new double[][]{ new double[] { 29, -4 }, new double[]{ 22.8, -1.2 }, new double[] { 25.5, 5 }, new double[] { 23.7, 4.9 } } },
                { "noisy", new double[][] { new double[] { -11.5, -5.5 }, new double[] { -11.7, 0.6 }, new double[] { -9.5, -1.5 } } },
                { "clipkit", new double[][]{ new double[] { -8, -9 }, new double[] { -8.7, -2 } } }
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
            Plot plot = Plot.Create.ScatterPlot(treeCoordinates, width: 450, height: 350, xAxisTitle: "Coordinate 1", yAxisTitle: "Coordinate 2");

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

                (double[], double[], float)[] lines = new (double[], double[], float)[]
                {
                    (treeCoordinates[toolIndices["noisy"] * replicates + 2], treeCoordinates[toolIndices["raw"] * replicates], mlTreeDistances[toolIndices["noisy"] * replicates + 2][toolIndices["raw"] * replicates]),
                    (treeCoordinates[toolIndices["raw"] * replicates], treeCoordinates[toolIndices["bmge"] * replicates], mlTreeDistances[toolIndices["bmge"] * replicates][toolIndices["raw"] * replicates]),
                    (treeCoordinates[toolIndices["raw"] * replicates], treeCoordinates[toolIndices["trimal"] * replicates], mlTreeDistances[toolIndices["trimal"] * replicates][toolIndices["raw"] * replicates]),
                    (treeCoordinates[toolIndices["bmge"] * replicates], treeCoordinates[toolIndices["trimal"] * replicates], mlTreeDistances[toolIndices["trimal"] * replicates][toolIndices["bmge"] * replicates]),
                    (treeCoordinates[toolIndices["raw"] * replicates], treeCoordinates[toolIndices["gblocks"] * replicates + 2], mlTreeDistances[toolIndices["gblocks"] * replicates + 2][toolIndices["raw"] * replicates]),
                    (treeCoordinates[toolIndices["gblocks"] * replicates], treeCoordinates[toolIndices["gblocks"] * replicates + 2], mlTreeDistances[toolIndices["gblocks"] * replicates + 2][toolIndices["gblocks"] * replicates]),
                    (treeCoordinates[toolIndices["gblocks"] * replicates + 1], treeCoordinates[toolIndices["gblocks"] * replicates], mlTreeDistances[toolIndices["gblocks"] * replicates + 1][toolIndices["gblocks"] * replicates]),
                    (treeCoordinates[toolIndices["gblocks"] * replicates + 2], treeCoordinates[toolIndices["gblocks"] * replicates + 1], mlTreeDistances[toolIndices["gblocks"] * replicates + 2][toolIndices["gblocks"] * replicates + 1])
                };

                Font fntSmall = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 8);

                for (int i = 0; i < lines.Length; i++)
                {
                    Point pt1 = coord.ToPlotCoordinates(lines[i].Item1);
                    Point pt2 = coord.ToPlotCoordinates(lines[i].Item2);

                    gpr.StrokePath(new GraphicsPath().MoveTo(pt1).LineTo(pt2), Colour.FromRgb(160, 160, 160));
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

                for (int i = 0; i < lines.Length; i++)
                {
                    Point pt1 = coord.ToPlotCoordinates(lines[i].Item1);
                    Point pt2 = coord.ToPlotCoordinates(lines[i].Item2);

                    blurGpr.Save();
                    blurGpr.Translate((pt1 + pt2) * 0.5);
                    blurGpr.Rotate(Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X));
                    string textDist = lines[i].Item3.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    blurGpr.FillText(-fntSmall.MeasureText(textDist).Width * 0.5, -2, textDist, fntSmall, Colours.White, TextBaselines.Bottom);
                    blurGpr.StrokeText(-fntSmall.MeasureText(textDist).Width * 0.5, -2, textDist, fntSmall, Colours.White, TextBaselines.Bottom, 2);
                    blurGpr.Restore();
                }

                gpr.DrawGraphics(0, 0, blurGpr, new GaussianBlurFilter(0.5));

                for (int i = 0; i < lines.Length; i++)
                {
                    Point pt1 = coord.ToPlotCoordinates(lines[i].Item1);
                    Point pt2 = coord.ToPlotCoordinates(lines[i].Item2);

                    gpr.Save();
                    gpr.Translate((pt1 + pt2) * 0.5);
                    gpr.Rotate(Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X));
                    string textDist = lines[i].Item3.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    gpr.FillText(-fntSmall.MeasureText(textDist).Width * 0.5, -2, textDist, fntSmall, Colour.FromRgb(160, 160, 160), TextBaselines.Bottom);
                    gpr.Restore();
                }

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

                        if (tools[i] == "clipkit" || tools[i] == "alifilter" || tools[i] == "raw" || (tools[i] == "noisy" && j != 2))
                        {
                            int index = tools[i] switch
                            {
                                "clipkit" => 0,
                                "alifilter" => 1,
                                "raw" => 3,
                                "noisy" => 2,
                                _ => 0
                            };
                            GraphicsPath clippingPath = new GraphicsPath().MoveTo(0, 0).Arc(0, 0, 1.6, Math.PI / 2 * index, Math.PI / 2 * (index + 1)).Close();
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
            Page figureS4 = plot.Render();

            // Resize to a width of 17cm.
            Page finalFigureS4 = new Page(482, figureS4.Height * 482 / figureS4.Width);
            finalFigureS4.Background = Colours.White;
            finalFigureS4.Graphics.Scale(482 / figureS4.Width, 482 / figureS4.Width);
            finalFigureS4.Graphics.DrawGraphics(0, 0, figureS4.Graphics);
            
            Document doc = new Document();
            doc.Pages.Add(finalFigureS4);

            finalFigureS4.SaveAsSVG("Figure_S4.svg");
            finalFigureS4.SaveAsSVG("Figure_S4.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_S4.pdf");
            finalFigureS4.SaveAsPNG("Figure_S4.png", 600.0 / 72);
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

            if (!useCache || !File.Exists("Cache/FigureS4_coordinates.txt"))
            {
                float[][] distanceMatrixOfTrees;

                if (!useCache || !File.Exists("Cache/FigureS4_distMat.bin"))
                {
                    TreeNode[][] subsampledTrees;

                    if (!useCache || !File.Exists("Cache/FigureS4_raw.tbi"))
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
                                BinaryTree.WriteAllTrees(subsampledTrees[i], "Cache/FigureS4_" + tools[i] + ".tbi");
                            }
                        }
                    }
                    else
                    {
                        // Reuse the cached subsampled trees.
                        subsampledTrees = new TreeNode[tools.Length][];

                        for (int i = 0; i < tools.Length; i++)
                        {
                            subsampledTrees[i] = BinaryTree.ParseAllTrees("Cache/FigureS4_" + tools[i] + ".tbi").ToArray();
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
                        SaveDistanceMatrix("Cache/FigureS4_distMat.bin", distanceMatrixOfTrees);
                    }
                }
                else
                {
                    // Reuse the cached distance matrix of trees.
                    distanceMatrixOfTrees = ReadDistanceMatrix("Cache/FigureS4_distMat.bin");
                }

                // Step 3: use the distance matrix of trees to perform a classical MDS extracting the first two coordinates.
                treeCoordinates = PerformMDS(distanceMatrixOfTrees, 2);

                if (useCache)
                {
                    // Save the tree coordinates.
                    SaveTreeCoordinates("Cache/FigureS4_coordinates.txt", treeCoordinates);
                }
            }
            else
            {
                // Reuse the cached coordinates.
                treeCoordinates = ReadTreeCoordinates("Cache/FigureS4_coordinates.txt");
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
