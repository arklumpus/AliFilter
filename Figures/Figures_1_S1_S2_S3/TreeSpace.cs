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

using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra;
using PhyloTree.Formats;
using PhyloTree.TreeBuilding;
using PhyloTree;
using VectSharp;
using VectSharp.Plots;

namespace Figures_1_S1_S2_S3
{
    internal static class TreeSpace
    {
        /// <summary>
        /// Read the maximum-likelihood trees for each tool.
        /// </summary>
        /// <param name="geneName">The name of the gene.</param>
        /// <param name="tools">The list of tools.</param>
        /// <returns>The maximum-likelihood trees for each tool.</returns>
        static TreeNode[][] ReadMLTrees(string geneName, string[] tools)
        {
            TreeNode[][] mlTrees = new TreeNode[tools.Length][];

            Console.WriteLine("Reading ML tree files...");

            object progressLock = new object();
            double[] progresses = new double[tools.Length];
            int total = tools.Length;

            Parallel.For(0, tools.Length, i =>
            {
                // Read the tree.
                mlTrees[i] = NWKA.ParseTrees("../../../Data/Trees/" + geneName + "." + tools[i] + ".treefile").ToArray();

                // Normalise the tree so that it has a total length of 1.
                for (int k = 0; k < mlTrees[i].Length; k++)
                {
                    double treeLength = mlTrees[i][k].GetChildrenRecursiveLazy().Select(x => x.Length).Where(x => !double.IsNaN(x)).Sum();
                    foreach (TreeNode node in mlTrees[i][k].GetChildrenRecursiveLazy())
                    {
                        node.Length /= treeLength;
                    }
                }
            });

            Console.CursorLeft = 0;
            Console.WriteLine("Done.");

            return mlTrees;
        }

        /// <summary>
        /// Read the UFBoot replicates for each tool.
        /// </summary>
        /// <param name="geneName">The name of the gene.</param>
        /// <param name="tools">The list of tools.</param>
        /// <returns>The UFBoot replicates for each tool.</returns>
        static TreeNode[][] ReadUFBootTrees(string geneName, string[] tools)
        {
            TreeNode[][] ufbootTrees = new TreeNode[tools.Length][];

            Console.WriteLine("Reading UFBoot tree files...");

            object progressLock = new object();
            double[] progresses = new double[tools.Length];
            int total = tools.Length;
            Console.Write("0%");

            Parallel.For(0, tools.Length, i =>
            {
                int itemProgress = 0;

                ufbootTrees[i] = NWKA.ParseTrees("../../../Data/Trees/" + geneName + "." + tools[i] + ".ufboot", x =>
                {
                    if ((int)(x * 10) > itemProgress)
                    {
                        itemProgress = (int)(x * 10);
                        progresses[i] = itemProgress * 0.1;

                        lock (progressLock)
                        {
                            double progress = progresses.Sum() / total;

                            Console.CursorLeft = 0;
                            Console.Write(progress.ToString("0%"));
                        }
                    }
                }).ToArray();

                for (int k = 0; k < ufbootTrees[i].Length; k++)
                {
                    double treeLength = ufbootTrees[i][k].GetChildrenRecursiveLazy().Select(x => x.Length).Where(x => !double.IsNaN(x)).Sum();

                    foreach (TreeNode node in ufbootTrees[i][k].GetChildrenRecursiveLazy())
                    {
                        node.Length /= treeLength;
                    }
                }
            });

            Console.CursorLeft = 0;
            Console.WriteLine("Done.");

            return ufbootTrees;
        }

        /// <summary>
        /// Given a phylogenetic tree, subsample it until only the specified number of representatives are preserved.
        /// </summary>
        /// <param name="tree">The tree to subsample.</param>
        /// <param name="targetTaxa">The target number of taxa to preserve.</param>
        /// <remarks>This method is used to subsample a "tree of trees", thus selecting a number of representative trees to preserve.</remarks>
        private static void SubsampleTree(ref TreeNode tree, int targetTaxa)
        {
            List<TreeNode> leaves = tree.GetLeaves();
            targetTaxa = Math.Max(targetTaxa, 3);

            HashSet<int> removedIndices = new HashSet<int>(leaves.Count - targetTaxa);

            double[][] distanceMatrix = tree.CreateDistanceMatrixDouble();

            while (leaves.Count - removedIndices.Count > targetTaxa)
            {
                double minDist = double.MaxValue;
                int minI = -1;
                int minJ = -1;

                for (int i = 0; i < leaves.Count; i++)
                {
                    if (!removedIndices.Contains(i))
                    {
                        for (int j = 0; j < i; j++)
                        {
                            if (!removedIndices.Contains(j))
                            {
                                if (distanceMatrix[i][j] < minDist)
                                {
                                    minDist = distanceMatrix[i][j];
                                    minI = i;
                                    minJ = j;
                                }
                            }
                        }
                    }
                }

                int indexToRemove;

                if (leaves[minI].Length < leaves[minJ].Length)
                {
                    indexToRemove = minI;
                }
                else
                {
                    indexToRemove = minJ;
                }

                removedIndices.Add(indexToRemove);
            }

            foreach (int index in removedIndices)
            {
                tree = tree.Prune(leaves[index], false);
            }
        }

        /// <summary>
        /// Subsample the UFBoot replicates for each tool, only keeping the specified number of representatives.
        /// </summary>
        /// <param name="allTrees">The full list of UFBoot replicates.</param>
        /// <param name="sampleSize">The number of trees to preserve for each tool.</param>
        /// <param name="distanceMatrixOfTreesFunction">A function that converts a list of trees into a distance matrix of trees.</param>
        /// <returns>The subsampled tree lists.</returns>
        static TreeNode[][] SubsampleTrees(TreeNode[][] allTrees, int sampleSize, Func<TreeNode[], Action<double>, float[][]> distanceMatrixOfTreesFunction)
        {
            Console.WriteLine("Creating distance matrix of UFBoot trees and selecting {0} representatives per tool...", sampleSize);
            int lastProgress = 0;

            TreeNode[][] subsampledTrees = new TreeNode[allTrees.Length][];

            for (int i = 0; i < allTrees.Length; i++)
            {
                // Create a distance matrix of all the bootstrap replicates for the current tool.
                float[][] distanceMatrixOfTrees = distanceMatrixOfTreesFunction(allTrees[i], x =>
                {
                    int prog = (int)(x / allTrees.Length * 100);

                    if (prog > lastProgress)
                    {
                        lastProgress = prog;
                        Console.CursorLeft = 0;
                        Console.Write("{0}%", prog);
                    }
                });

                // Create a neighbour-joining tree of trees from the distance matrix.
                TreeNode treeOfTrees = NeighborJoining.BuildTree(distanceMatrixOfTrees, Enumerable.Range(0, distanceMatrixOfTrees.Length).Select(x => "Tree" + x.ToString()).ToList(), allowNegativeBranches: false, copyMatrix: false);

                // Subsample the tree of trees.
                SubsampleTree(ref treeOfTrees, sampleSize);

                // Select the trees to preserve.
                int[] treesToKeep = treeOfTrees.GetLeafNames().Select(x => int.Parse(x.Replace("Tree", ""))).ToArray();
                subsampledTrees[i] = treesToKeep.Select(x => allTrees[i][x]).ToArray();

                Console.CursorLeft = 0;
                Console.Write(((double)(i + 1) / allTrees.Length).ToString("0%"));
            }

            Console.CursorLeft = 0;
            Console.WriteLine("Done.");

            return subsampledTrees;
        }

        /// <summary>
        /// Convert between a linear index and a triangular index.
        /// </summary>
        /// <param name="k">The linear index.</param>
        /// <param name="n">The total number of elements in the matrix.</param>
        /// <returns>The row and column corresponding to the k-th element.</returns>
        private static (int i, int j) GetIndices(int k, int n)
        {
            int i = n - 2 - (int)Math.Floor(Math.Sqrt(-8 * k + 4 * n * (n - 1) - 7) / 2.0 - 0.5);
            int j = k + i + 1 - n * (n - 1) / 2 + (n - i) * ((n - i) - 1) / 2;
            return (j, i);
        }

        /// <summary>
        /// Create a distance matrix of trees.
        /// </summary>
        /// <param name="allTrees">The trees whose distance matrix should be computed.</param>
        /// <param name="distanceMatrixOfTreesFunction">A function that converts a list of trees into a distance matrix.</param>
        /// <returns></returns>
        static float[][] CreateDistanceMatrixOfTrees(TreeNode[] allTrees, Func<TreeNode[], Action<double>, float[][]> distanceMatrixOfTreesFunction)
        {
            Console.WriteLine("Creating distance matrix of trees...");
            int lastProgress = 0;

            float[][] distanceMatrixOfTrees = distanceMatrixOfTreesFunction(allTrees, x =>
            {
                int prog = (int)(x * 100);

                if (prog > lastProgress)
                {
                    lastProgress = prog;
                    Console.CursorLeft = 0;
                    Console.Write("{0}%", prog);
                }
            });

            Console.CursorLeft = 0;
            Console.WriteLine("Done.");

            return distanceMatrixOfTrees;
        }

        /// <summary>
        /// Save a distance matrix to disk for caching purposes.
        /// </summary>
        /// <param name="outputFile">The path to the output file.</param>
        /// <param name="distanceMatrix">The distance matrix to save.</param>
        static void SaveDistanceMatrix(string outputFile, float[][] distanceMatrix)
        {
            using (FileStream fs = File.Create(outputFile))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(distanceMatrix.Length);

                    for (int i = 0; i < distanceMatrix.Length; i++)
                    {
                        for (int j = 0; j < distanceMatrix[i].Length; j++)
                        {
                            bw.Write(distanceMatrix[i][j]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read a distance matrix from disk.
        /// </summary>
        /// <param name="inputFile">The path to the distance matrix file.</param>
        /// <returns>The read distance matrix.</returns>
        static float[][] ReadDistanceMatrix(string inputFile)
        {
            using (FileStream fs = File.OpenRead(inputFile))
            {
                using (BinaryReader bw = new BinaryReader(fs))
                {
                    int length = bw.ReadInt32();

                    float[][] tbr = new float[length][];

                    for (int i = 0; i < tbr.Length; i++)
                    {
                        tbr[i] = new float[i];

                        for (int j = 0; j < i; j++)
                        {
                            tbr[i][j] = bw.ReadSingle();
                        }
                    }

                    return tbr;
                }
            }
        }

        /// <summary>
        /// Perform a classical MDS analysis.
        /// </summary>
        /// <param name="distanceMatrix">A distance matrix.</param>
        /// <param name="numComponents">The number of coordinates to return.</param>
        /// <returns>The results of the MDS analysis.</returns>
        static double[][] PerformMDS(float[][] distanceMatrix, int numComponents)
        {
            int count = distanceMatrix.Length;

            // Set up the squared proximity matrix and the centering matrix.
            Matrix<double> dSq = Matrix<double>.Build.Dense(count, count);
            Matrix<double> centeringMatrix = Matrix<double>.Build.Dense(count, count, -1.0 / count);

            for (int j = 0; j < count; j++)
            {
                for (int i = 0; i < j; i++)
                {
                    double val = distanceMatrix[j][i];

                    dSq[i, j] = val * val;
                    dSq[j, i] = val * val;
                }

                centeringMatrix[j, j] = 1 - 1.0 / count;
            }

            // Apply centering.
            Matrix<double> b = -0.5 * centeringMatrix * dSq * centeringMatrix;

            // Eigenvalue decomposition.
            Evd<double> eigen = b.Evd();

            // Select the largest eigenvalues.
            int[] sortedEigen = (from el in Enumerable.Range(0, count) orderby eigen.EigenValues[el].Real descending select el).Take(numComponents).ToArray();

            // Compute the coordinates.
            Matrix<double> lamdbaMSqrt = Matrix<double>.Build.DiagonalOfDiagonalArray(sortedEigen.Where(x => eigen.EigenValues[x].Real >= 0).Select(x => Math.Sqrt(eigen.EigenValues[x].Real)).ToArray());
            Matrix<double> eM = Matrix<double>.Build.DenseOfColumnVectors(sortedEigen.Where(x => eigen.EigenValues[x].Real >= 0).Select(eigen.EigenVectors.Column));
            Matrix<double> X = eM * lamdbaMSqrt;

            // Conver the matrix to a jagged array.
            double[][] tbr = new double[count][];
            for (int i = 0; i < count; i++)
            {
                tbr[i] = new double[X.ColumnCount];
                for (int j = 0; j < X.ColumnCount; j++)
                {
                    tbr[i][j] = X[i, j];
                }
            }

            return tbr;
        }

        /// <summary>
        /// Save the tree coordinates for caching.
        /// </summary>
        /// <param name="outputFile">The path to the output file.</param>
        /// <param name="treeCoordinates">The coordinates to save.</param>
        static void SaveTreeCoordinates(string outputFile, double[][] treeCoordinates)
        {
            using (StreamWriter sw = new StreamWriter(outputFile))
            {
                sw.NewLine = "\n";
                for (int i = 0; i < treeCoordinates.Length; i++)
                {
                    for (int j = 0; j < treeCoordinates[i].Length; j++)
                    {
                        sw.Write(treeCoordinates[i][j].ToString(System.Globalization.CultureInfo.InvariantCulture));
                        if (j < treeCoordinates[i].Length - 1)
                        {
                            sw.Write("\t");
                        }
                    }
                    sw.WriteLine();
                }
            }
        }

        /// <summary>
        /// Read the cached tree coordinates.
        /// </summary>
        /// <param name="inputFile">The path to the input file.</param>
        /// <returns>The tree coordinates that have been read.</returns>
        static double[][] ReadTreeCoordinates(string inputFile)
        {
            List<double[]> tbr = new List<double[]>();

            using (StreamReader sr = new StreamReader(inputFile))
            {
                string line = sr.ReadLine();

                while (line != null)
                {
                    tbr.Add(line.Split("\t").Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray());

                    line = sr.ReadLine();
                }
            }

            return tbr.ToArray();
        }

        /// <summary>
        /// Compute the 2D tree coordinates induced by the Frobenius distance metric.
        /// </summary>
        /// <param name="useCache">If this is true, the results of each step are cached and reused, in order to make it easier to make small changes to the code without having to recompute everything.</param>
        /// <param name="geneName">The name of the gene.</param>
        /// <param name="tools">The list of tools.</param>
        /// <param name="sampleSize">The number of UFBoot replicates to preserve for each tool.</param>
        /// <param name="replicates">The number of replicate ML analyses for each tool.</param>
        /// <returns>The 2D tree coordinates for each tree.</returns>
        static double[][] GetTreeCoordinatesFrobenius(bool useCache, string geneName, string[] tools, int sampleSize, int replicates)
        {
            double[][] treeCoordinates;

            if (!useCache || !File.Exists("Cache/" + geneName + "_coordinates.txt"))
            {
                float[][] distanceMatrixOfTrees;

                if (!useCache || !File.Exists("Cache/" + geneName + "_distMat.bin"))
                {
                    TreeNode[][] subsampledTrees;

                    if (!useCache || !File.Exists("Cache/" + geneName + "_raw.tbi"))
                    {
                        // Step 1: subsample the UFBoot replicates, only preserving the requested number of trees.

                        // Read all the UFBoot replicates.
                        TreeNode[][] allTrees = ReadUFBootTrees(geneName, tools);

                        // Subsample the replicates.
                        subsampledTrees = SubsampleTrees(allTrees, sampleSize, CreateFrobeniusDistanceMatrixOfTrees);

                        if (useCache)
                        {
                            // Save the subsampled tree list in the cache.
                            Directory.CreateDirectory("Cache");
                            for (int i = 0; i < tools.Length; i++)
                            {
                                BinaryTree.WriteAllTrees(subsampledTrees[i], "Cache/" + geneName + "_" + tools[i] + ".tbi");
                            }
                        }
                    }
                    else
                    {
                        // Reuse the cached subsampled trees.
                        subsampledTrees = new TreeNode[tools.Length][];

                        for (int i = 0; i < tools.Length; i++)
                        {
                            subsampledTrees[i] = BinaryTree.ParseAllTrees("Cache/" + geneName + "_" + tools[i] + ".tbi").ToArray();
                        }
                    }

                    // Step 2: create a distance matrix of trees according to the Frobenius distance metric.

                    // Read the ML trees.
                    TreeNode[][] mlTrees = ReadMLTrees(geneName, tools);

                    // Concatenate all the trees.
                    TreeNode[] joinedTrees = mlTrees.Aggregate(Enumerable.Empty<TreeNode>(), (a, b) => a.Concat(b)).Concat(subsampledTrees.Aggregate(Enumerable.Empty<TreeNode>(), (a, b) => a.Concat(b))).ToArray();

                    // Create the distance matrix of trees.
                    distanceMatrixOfTrees = CreateDistanceMatrixOfTrees(joinedTrees, CreateFrobeniusDistanceMatrixOfTrees);

                    if (useCache)
                    {
                        // Save the computed distance matrix in the cache.
                        SaveDistanceMatrix("Cache/" + geneName + "_distMat.bin", distanceMatrixOfTrees);
                    }
                }
                else
                {
                    // Reuse the cached distance matrix of trees.
                    distanceMatrixOfTrees = ReadDistanceMatrix("Cache/" + geneName + "_distMat.bin");
                }

                // Step 3: use the distance matrix of trees to perform a classical MDS extracting the first two coordinates.
                treeCoordinates = PerformMDS(distanceMatrixOfTrees, 2);

                if (useCache)
                {
                    // Save the tree coordinates.
                    SaveTreeCoordinates("Cache/" + geneName + "_coordinates.txt", treeCoordinates);
                }
            }
            else
            {
                // Reuse the cached coordinates.
                treeCoordinates = ReadTreeCoordinates("Cache/" + geneName + "_coordinates.txt");
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

        /// <summary>
        /// Create the tree space plot.
        /// </summary>
        /// <param name="geneName">The name of the gene to plot.</param>
        /// <param name="sampleSize">Number of bootstrap replicates to preserve.</param>
        /// <param name="replicates">Number of replicates.</param>
        /// <returns>The rendered tree space plot.</returns>
        public static Page GetTreeSpacePlot(string geneName, int sampleSize, int replicates)
        {
            // List of tool names.
            string[] tools = new string[] { "species", "raw", "alifilter", "gb" };

            // Colour to use for each tool.
            Dictionary<string, Colour> toolColours = new Dictionary<string, Colour>()
            {
                { "raw", Colour.FromRgb(80, 80, 80) },
                { "alifilter", Colour.FromRgb(119, 170, 221) },
                { "gb",   Colour.FromRgb(128, 128, 128) },
                { "species",    Colour.FromRgb(120, 94, 240) },
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
                { "raw", (circle, true, 2.25) },
                { "alifilter", (triangle, true, 2.5) },
                { "gb", (square, true, 1.75) },
                { "species", (diamond, true, 2.5) },
            };

            // Manually fixed positions for the tool names.
            Dictionary<string, (double[], double[])> toolNamePositions = null;
            
            if (geneName == "27")
            {
                toolNamePositions = new Dictionary<string, (double[], double[])>()
                {
                    { "raw", (new double[] { 0, -0.3 }, new double[] { 0.5, -0.31 }) },
                    { "alifilter", (new double[] { 0.1, 0.6 }, new double[] { 0.57, 0.55 }) },
                    { "gb", (new double[] { 0.13, -0.09 }, new double[] { 0.52, -0.15 }) },
                    { "species", (new double[] { -1.2, 0 }, new double[] { -2, -0.02 }) }
                };
            }
            else if (geneName == "78")
            {
                toolNamePositions = new Dictionary<string, (double[], double[])>()
                {
                    { "raw", (new double[] { 0.3, -0.3 }, new double[] { -0.4, -0.27 }) },
                    { "alifilter", (new double[] { 0.25, 0.25 }, new double[] { -0.48, 0.15 }) },
                    { "gb", (new double[] { 0.31, -0.01 }, new double[] { -0.4, -0.04 }) },
                    { "species", (new double[] { 1.5, 0.04 }, new double[] { 2.08, 0.02 }) }
                };
            }

            Dictionary<string, string> toolNames = new Dictionary<string, string>()
            {
                { "raw", "Unfiltered" },
                { "alifilter", "AliFilter" },
                { "gb", "Manual" },
                { "species", "Species tree" }
            };

            // Compute the 2D tree coordinates induced by the Frobenius distance metric.
            double[][] treeCoordinates = GetTreeCoordinatesFrobenius(true, geneName, tools, sampleSize, replicates);

            // Create the scatter plot.
            Plot plot = Plot.Create.ScatterPlot(treeCoordinates, width: 250, height: 100, xAxisTitle: "Coordinate 1", yAxisTitle: "Coordinate 2",
                axisLabelPresentationAttributes: new PlotElementPresentationAttributes()
                {
                    Stroke = null,
                    Fill = Colours.Black,
                    Font = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 8)
                },
                axisTitlePresentationAttributes: new PlotElementPresentationAttributes()
                {
                    Stroke = null,
                    Fill = Colours.Black,
                    Font = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 10)
                },
                axisPresentationAttributes: new PlotElementPresentationAttributes()
                {
                    Stroke = Colours.Black,
                    LineWidth = 0.5
                },
                gridPresentationAttributes: new PlotElementPresentationAttributes()
                {
                    Stroke = Colour.FromRgb(200, 200, 200),
                    LineWidth = 0.5
                }, axisArrowSize: 5);

            plot.GetAll<ContinuousAxisTitle>().ElementAt(1).Position += 5;

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

                Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 8);

                for (int i = 0; i < tools.Length; i++)
                {
                    for (int j = 0; j < replicates; j++)
                    {
                        Point pt = coord.ToPlotCoordinates(treeCoordinates[i * replicates + j]);

                        Colour col = toolColours[tools[i]];
                        GraphicsPath shape = star;
                        double shapeSize = 3;

                        gpr.Save();
                        gpr.Translate(pt);
                        gpr.Scale(shapeSize, shapeSize);
                        gpr.StrokePath(shape, Colours.White, 1.5 / shapeSize);
                        gpr.FillPath(shape, col);
                        gpr.Restore();

                    }

                    if (toolNamePositions.TryGetValue(tools[i], out (double[], double[]) toolPos))
                    {
                        Point pt = coord.ToPlotCoordinates(toolPos.Item1);
                        Point pt2 = coord.ToPlotCoordinates(toolPos.Item2);
                        string toolName = toolNames[tools[i]];

                        double arrowSize = 3;

                        gpr.StrokePath(new GraphicsPath().MoveTo(pt.X - (fnt.MeasureText(toolName).Width + 13) * 0.5 - 13 - 4 + (fnt.MeasureText(toolName).Width + 13 + 10) * 0.5, pt.Y).LineTo(pt2), Colours.Black);
                        gpr.Save();
                        gpr.Translate(pt2);
                        gpr.Rotate(Math.Atan2(pt2.Y - pt.Y, pt2.X - (pt.X - (fnt.MeasureText(toolName).Width + 13) * 0.5 - 13 - 4 + (fnt.MeasureText(toolName).Width + 13 + 10) * 0.5)));
                        gpr.FillPath(new GraphicsPath().MoveTo(-arrowSize, -arrowSize).LineTo(-arrowSize, arrowSize).LineTo(arrowSize, 0).Close(), Colours.Black);
                        gpr.Restore();

                        gpr.FillRectangle(pt.X - (fnt.MeasureText(toolName).Width + 6.5) * 0.5 - 6.5 - 3, pt.Y - 6, fnt.MeasureText(toolName).Width + 6.5 + 8, 12, Colours.White);
                        gpr.StrokeRectangle(pt.X - (fnt.MeasureText(toolName).Width + 6.5) * 0.5 - 6.5 - 3, pt.Y - 6, fnt.MeasureText(toolName).Width + 6.5 + 8, 12, Colours.Black);

                        Colour col = toolColours[tools[i]];
                        GraphicsPath shape = toolShapes[tools[i]].Item1;
                        bool filled = toolShapes[tools[i]].Item2;
                        double shapeSize = toolShapes[tools[i]].Item3;

                        gpr.Save();
                        gpr.Translate(pt.X - (fnt.MeasureText(toolName).Width + 6.5) * 0.5 - 6.5 + shapeSize, pt.Y);
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

                        gpr.FillText(pt.X - (fnt.MeasureText(toolName).Width + 5.5) * 0.5, pt.Y, toolName, fnt, Colours.Black, TextBaselines.Middle);
                    }
                }
            }));

            Page renderedPlot = plot.Render();
            renderedPlot.Crop();

            return renderedPlot;
        }
    }
}
