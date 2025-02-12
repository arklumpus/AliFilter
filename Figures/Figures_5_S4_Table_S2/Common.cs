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

using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra;
using PhyloTree;
using PhyloTree.Formats;
using PhyloTree.TreeBuilding;

namespace Figures_5_S4_Table_S2
{
    internal static partial class Program
    {
        /// <summary>
        /// Read runtime stats for the phylogenomic analysis.
        /// </summary>
        /// <returns>The runtime stats for the phylogenomic analysis for each tool.</returns>
        static Dictionary<string, (int alignmentLength, int distinctPatterns, int ramRequired, List<double> runtimeHours, List<double> mlTreeLength)> ReadRuntimeStats()
        {
            Dictionary<string, (int alignmentLength, int distinctPatterns, int ramRequired, List<double> runtimeHours, List<double> mlTreeLength)> results = new Dictionary<string, (int alignmentLength, int distinctPatterns, int ramRequired, List<double> runtimeHours, List<double> mlTreeLength)>();

            foreach (string line in File.ReadLines("../../../Data/runtime_stats.txt").Skip(1))
            {
                string[] splitLine = line.Split('\t');

                string tool = splitLine[0];

                List<double> runtime;
                List<double> mlTreeLength;

                if (!results.TryGetValue(tool, out (int, int, int, List<double> runtime, List<double> mlTreeLength) item))
                {
                    runtime = new List<double>();
                    mlTreeLength = new List<double>();
                    results[tool] = (int.Parse(splitLine[1]), int.Parse(splitLine[2]), int.Parse(splitLine[3]), runtime, mlTreeLength);
                }
                else
                {
                    runtime = item.runtime;
                }

                runtime.Add(double.Parse(splitLine[4]));
            }

            // Read the tree lengths.
            foreach (KeyValuePair<string, (int alignmentLength, int distinctPatterns, int ramRequired, List<double> runtimeHours, List<double> mlTreeLength)> kvp in results)
            {
                List<TreeNode> trees = NWKA.ParseAllTrees("../../../Data/Trees/" + kvp.Key + ".treefile");
                kvp.Value.mlTreeLength.AddRange(trees.Select(x => x.GetChildrenRecursiveLazy().Select(x => x.Length).Where(x => !double.IsNaN(x)).Sum()));
            }

            return results;
        }

        /// <summary>
        /// Read the maximum-likelihood trees for each tool.
        /// </summary>
        /// <param name="tools">The list of tools.</param>
        /// <returns>The maximum-likelihood trees for each tool.</returns>
        static TreeNode[][] ReadMLTrees(string[] tools)
        {
            TreeNode[][] mlTrees = new TreeNode[tools.Length][];

            Console.WriteLine("Reading ML tree files...");

            object progressLock = new object();
            double[] progresses = new double[tools.Length];
            int total = tools.Length;

            Parallel.For(0, tools.Length, i =>
            {
                // Read the tree.
                mlTrees[i] = NWKA.ParseTrees("../../../Data/Trees/" + tools[i] + ".treefile").ToArray();

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
        /// <param name="tools">The list of tools.</param>
        /// <returns>The UFBoot replicates for each tool.</returns>
        static TreeNode[][] ReadUFBootTrees(string[] tools)
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

                ufbootTrees[i] = NWKA.ParseTrees("../../../Data/Trees/" + tools[i] + ".ufboot", x =>
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
    }
}
