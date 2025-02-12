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

namespace Figures_5_S4_Table_S2
{
    internal static partial class Program
    {
        /// <summary>
        /// Create Figure 5b.
        /// </summary>
        /// <param name="useCache">If this is true, the results of each step are cached and reused, in order to make it easier to make small changes to the code without having to recompute everything.</param>
        /// <returns>The <see cref="Page"/> on which Figure 5b has been rendered.</returns>
        static Page CreateFigure5b(bool useCache = true)
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
            Dictionary<string, (double[], double[])> toolNamePositions = new Dictionary<string, (double[], double[])>()
            {
                { "raw", (new double[] { 0, 0.2 }, new double[] { -0.04, 0.125 }) },
                { "alifilter", (new double[] { 0.27, 0.12 }, new double[] { 0.1, 0.11 }) },
                { "bmge", (new double[] { 0.08, -0.1 }, new double[]{ 0.07, -0.04 }) },
                { "trimal", (new double[] { 0.32, 0 }, new double[] { 0.19, 0.01 }) },
                { "gblocks", (new double[] { 0.11, -0.17 }, new double[] { 0.22, -0.19 }) },
                { "noisy", (new double[] { -0.35, 0 }, new double[] { -0.44, -0.08 }) },
                { "clipkit", (new double[] { -0.05, -0.04 }, new double[] { -0.03, 0.03 }) }
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

            // Compute the 2D tree coordinates induced by the Frobenius distance metric.
            double[][] treeCoordinates = GetTreeCoordinatesFrobenius(useCache, tools, sampleSize, replicates);

            // Create the scatter plot.
            Plot plot = Plot.Create.ScatterPlot(treeCoordinates, width: 450, xAxisTitle: "Coordinate 1", yAxisTitle: "Coordinate 2");

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
                        gpr.StrokePath(shape, Colours.White, 3.0 / shapeSize);
                        gpr.FillPath(shape, col);
                        gpr.Restore();

                    }

                    if (toolNamePositions.TryGetValue(tools[i], out (double[], double[]) toolPos))
                    {
                        Point pt = coord.ToPlotCoordinates(toolPos.Item1);
                        Point pt2 = coord.ToPlotCoordinates(toolPos.Item2);
                        string toolName = toolNames[tools[i]];

                        double arrowSize = 4;

                        gpr.StrokePath(new GraphicsPath().MoveTo(pt.X - (fnt.MeasureText(toolName).Width + 13) * 0.5 - 13 - 4 + (fnt.MeasureText(toolName).Width + 13 + 10) * 0.5, pt.Y).LineTo(pt2), Colours.Black);
                        gpr.Save();
                        gpr.Translate(pt2);
                        gpr.Rotate(Math.Atan2(pt2.Y - pt.Y, pt2.X - (pt.X - (fnt.MeasureText(toolName).Width + 13) * 0.5 - 13 - 4 + (fnt.MeasureText(toolName).Width + 13 + 10) * 0.5)));
                        gpr.FillPath(new GraphicsPath().MoveTo(-arrowSize, -arrowSize).LineTo(-arrowSize, arrowSize).LineTo(arrowSize, 0).Close(), Colours.Black);
                        gpr.Restore();

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

            return plot.Render();
        }

        /// <summary>
        /// Compute the 2D tree coordinates induced by the Frobenius distance metric.
        /// </summary>
        /// <param name="useCache">If this is true, the results of each step are cached and reused, in order to make it easier to make small changes to the code without having to recompute everything.</param>
        /// <param name="tools">The names of the tools.</param>
        /// <param name="sampleSize">The number of UFBoot replicates to preserve for each tool.</param>
        /// <param name="replicates">The number of replicate ML analyses for each tool.</param>
        /// <returns>The 2D tree coordinates for each tree.</returns>
        static double[][] GetTreeCoordinatesFrobenius(bool useCache, string[] tools, int sampleSize, int replicates)
        {
            double[][] treeCoordinates;

            if (!useCache || !File.Exists("Cache/Figure5b_coordinates.txt"))
            {
                float[][] distanceMatrixOfTrees;

                if (!useCache || !File.Exists("Cache/Figure5b_distMat.bin"))
                {
                    TreeNode[][] subsampledTrees;

                    if (!useCache || !File.Exists("Cache/Figure5b_raw.tbi"))
                    {
                        // Step 1: subsample the UFBoot replicates, only preserving the requested number of trees.

                        // Read all the UFBoot replicates.
                        TreeNode[][] allTrees = ReadUFBootTrees(tools);

                        // Subsample the replicates.
                        subsampledTrees = SubsampleTrees(allTrees, sampleSize, CreateFrobeniusDistanceMatrixOfTrees);

                        if (useCache)
                        {
                            // Save the subsampled tree list in the cache.
                            Directory.CreateDirectory("Cache");
                            for (int i = 0; i < tools.Length; i++)
                            {
                                BinaryTree.WriteAllTrees(subsampledTrees[i], "Cache/Figure5b_" + tools[i] + ".tbi");
                            }
                        }
                    }
                    else
                    {
                        // Reuse the cached subsampled trees.
                        subsampledTrees = new TreeNode[tools.Length][];

                        for (int i = 0; i < tools.Length; i++)
                        {
                            subsampledTrees[i] = BinaryTree.ParseAllTrees("Cache/Figure5b_" + tools[i] + ".tbi").ToArray();
                        }
                    }

                    // Step 2: create a distance matrix of trees according to the Frobenius distance metric.

                    // Read the ML trees.
                    TreeNode[][] mlTrees = ReadMLTrees(tools);

                    // Concatenate all the trees.
                    TreeNode[] joinedTrees = mlTrees.Aggregate(Enumerable.Empty<TreeNode>(), (a, b) => a.Concat(b)).Concat(subsampledTrees.Aggregate(Enumerable.Empty<TreeNode>(), (a, b) => a.Concat(b))).ToArray();

                    // Create the distance matrix of trees.
                    distanceMatrixOfTrees = CreateDistanceMatrixOfTrees(joinedTrees, CreateFrobeniusDistanceMatrixOfTrees);

                    if (useCache)
                    {
                        // Save the computed distance matrix in the cache.
                        SaveDistanceMatrix("Cache/Figure5b_distMat.bin", distanceMatrixOfTrees);
                    }
                }
                else
                {
                    // Reuse the cached distance matrix of trees.
                    distanceMatrixOfTrees = ReadDistanceMatrix("Cache/Figure5b_distMat.bin");
                }

                // Step 3: use the distance matrix of trees to perform a classical MDS extracting the first two coordinates.
                treeCoordinates = PerformMDS(distanceMatrixOfTrees, 2);

                if (useCache)
                {
                    // Save the tree coordinates.
                    SaveTreeCoordinates("Cache/Figure5b_coordinates.txt", treeCoordinates);
                }
            }
            else
            {
                // Reuse the cached coordinates.
                treeCoordinates = ReadTreeCoordinates("Cache/Figure5b_coordinates.txt");
            }

            return treeCoordinates;
        }

        /// <summary>
        /// Creates a distance matrix from a set of trees, according to the Frobenius distance metric.
        /// </summary>
        /// <param name="allTrees">The trees that will be used to compute the distance matrix.</param>
        /// <param name="progressAction">A progress callback.</param>
        /// <returns>A distance matrix of trees.</returns>
        static float[][] CreateFrobeniusDistanceMatrixOfTrees(TreeNode[] allTrees, Action<double> progressAction)
        {
            List<string> leafNames = allTrees[0].GetLeafNames();

            float[][] distanceMatrixOfTrees;

            float[][][] treesAsDistanceMatrices = new float[allTrees.Length][][];
            Dictionary<string, int>[] leafIndices = new Dictionary<string, int>[treesAsDistanceMatrices.Length];

            // Convert each tree into a patristic distance matrix (note: the trees will have already been normalised).
            Parallel.For(0, allTrees.Length, k =>
            {
                treesAsDistanceMatrices[k] = allTrees[k].CreateDistanceMatrixFloat(maxDegreeOfParallelism: 1);
                leafIndices[k] = new Dictionary<string, int>(allTrees[k].GetLeafNames().Select((x, i) => new KeyValuePair<string, int>(x, i)));
            });

            distanceMatrixOfTrees = new float[treesAsDistanceMatrices.Length][];

            for (int j = 0; j < treesAsDistanceMatrices.Length; j++)
            {
                distanceMatrixOfTrees[j] = new float[j];
            }

            int totalMatrices = treesAsDistanceMatrices.Length * (treesAsDistanceMatrices.Length - 1) / 2;

            object progressLock = new object();
            int countDone = 0;

            // Compute the Frobenius distance between each pair of patristic distance matrices.
            Parallel.For(0, totalMatrices, j =>
            {
                (int i2, int j2) = GetIndices(j, treesAsDistanceMatrices.Length);

                distanceMatrixOfTrees[i2][j2] = (float)ComputeFrobeniusTreeDistance(leafNames, treesAsDistanceMatrices[i2], leafIndices[i2], treesAsDistanceMatrices[j2], leafIndices[j2]);

                lock (progressLock)
                {
                    countDone++;
                    double progress = (double)countDone / totalMatrices;
                    progressAction(progress);
                }
            });

            return distanceMatrixOfTrees;
        }

        /// <summary>
        /// Compute the Frobenius distance between two patristic distance matrices.
        /// </summary>
        /// <param name="leafNames">The names of all the leaves in the trees.</param>
        /// <param name="tree1AsDistMat">The first patristric distance matrix.</param>
        /// <param name="tree1LeafIndices">The indices of each leaf in the patristic distance matrix.</param>
        /// <param name="tree2AsDistMat">The second patristic distance matrix.</param>
        /// <param name="tree2LeafIndices">The indices of each leaf in the patristic distance matrix.</param>
        /// <returns></returns>
        private static double ComputeFrobeniusTreeDistance(List<string> leafNames, float[][] tree1AsDistMat, Dictionary<string, int> tree1LeafIndices, float[][] tree2AsDistMat, Dictionary<string, int> tree2LeafIndices)
        {
            double distance = 0;

            for (int i = 0; i < leafNames.Count; i++)
            {
                int tree1I = tree1LeafIndices[leafNames[i]];
                int tree2I = tree2LeafIndices[leafNames[i]];

                for (int j = 0; j < i; j++)
                {
                    int tree1J = tree1LeafIndices[leafNames[j]];
                    int tree2J = tree2LeafIndices[leafNames[j]];

                    double val = tree1AsDistMat[Math.Max(tree1I, tree1J)][Math.Min(tree1I, tree1J)] - tree2AsDistMat[Math.Max(tree2I, tree2J)][Math.Min(tree2I, tree2J)];

                    // Multiply by 2 because the matrices are symmetric.
                    distance += val * val * 2;
                }
            }

            return Math.Sqrt(distance);
        }
    }
}
