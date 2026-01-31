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

using VectSharp;
using VectSharp.PDF;
using VectSharp.Raster;
using VectSharp.SVG;

namespace Figures_5_S7_S8_Table_S2
{
    internal static partial class Program
    {
        /// <summary>
        /// Create Figure S8.
        /// </summary>
        /// <param name="useCache">If this is true, the results of each step are cached and reused, in order to make it easier to make small changes to the code without having to recompute everything.</param>
        /// <returns>The <see cref="Page"/> on which Figure S8 has been rendered.</returns>
        static void CreateFigureS8(bool useCache = true)
        {
            // Names of the tree files.
            List<string> allTrees = new List<string> { "reference", "raw", "alifilter", "bmge", "trimal", "gblocks", "noisy", "clipkit", "gb" };
            
            // Get the trees rendered by TreeViewer.
            Page[] allTreePages = new Page[allTrees.Count];

            for (int i = 0; i < allTrees.Count; i++)
            {
                if (!useCache || !File.Exists("Cache/FigureS8_" + allTrees[i] + ".svg"))
                {
                    Console.WriteLine("Creating tree plot for " + allTrees[i] + "...");
                    CreateTreeViewerPlot("../../../Data/Trees/Nostocales/" + allTrees[i] + ".tbi", "Cache/FigureS8_" + allTrees[i] + ".svg");
                }
                allTreePages[i] = Parser.FromFile("Cache/FigureS8_" + allTrees[i] + ".svg");
            }

            // Rows for the figure.
            string[][] rows = new string[][]
            {
                new string[] { "reference", "noisy" },
                new string[] { "raw", "gb", "clipkit" },
                new string[] { "bmge", "gblocks", "trimal", "alifilter" },
            };

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
                { "gb",   Colour.FromRgb(128, 128, 128) },
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
                { "gb", (square, true, 3.5) },
            };

            // Manually fixed positions for the tool names.
            Dictionary<string, (double[], double[])> toolNamePositions = new Dictionary<string, (double[], double[])>()
            {
                { "raw", (new double[] { 0, -0.2 }, new double[] { -0.05, -0.11 }) },
                { "alifilter", (new double[] { 0.27, -0.11 }, new double[] { 0.1, -0.10 }) },
                { "bmge", (new double[] { 0.09, 0.11 }, new double[]{ 0.08, 0.05 }) },
                { "trimal", (new double[] { 0.32, 0.01 }, new double[] { 0.19, 0 }) },
                { "gblocks", (new double[] { 0.10, 0.18 }, new double[] { 0.21, 0.20 }) },
                { "noisy", (new double[] { -0.35, 0 }, new double[] { -0.44, 0.08 }) },
                { "clipkit", (new double[] { -0.05, 0.05 }, new double[] { -0.03, -0.02 }) },
                { "gb", (new double[] { 0.17, -0.21 }, new double[] { 0.09, -0.15 }) }
            };

            Dictionary<string, string> toolNames = new Dictionary<string, string>()
            {
                { "raw", "Unfiltered" },
                { "alifilter", "AliFilter" },
                { "bmge", "BMGE" },
                { "trimal", "trimAl" },
                { "gblocks", "Gblocks" },
                { "noisy", "Noisy" },
                { "clipkit", "ClipKIT" },
                { "gb", "Manual" },
                { "reference", "Reference (Strunecký et al., 2023)" }
            };

            Font toolFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 12);

            double maxWidth = rows.Select(x => (x.Length - 1) * 10 + x.Select(y => allTreePages[allTrees.IndexOf(y)].Width).Sum()).Max();

            Page compositePage = new Page(1, 1);

            double[] columnMargins = new double[rows.Length];
            double rowMargin = 20;

            compositePage.Graphics.Save();

            // Draw all the trees on the same page.
            for (int i = 0; i < rows.Length; i++)
            {
                double maxHeight = 0;
                double columnMargin = (maxWidth - rows[i].Select(x => allTreePages[allTrees.IndexOf(x)].Width).Sum()) / (rows[i].Length - 1);
                double currX = 0;
                columnMargins[i] = columnMargin;

                for (int j = 0; j < rows[i].Length; j++)
                {
                    int index = allTrees.IndexOf(rows[i][j]);

                    maxHeight = Math.Max(maxHeight, allTreePages[index].Height);
                    compositePage.Graphics.DrawGraphics(currX, 0, allTreePages[index].Graphics);
                    string toolName = toolNames[rows[i][j]];

                    if (toolColours.TryGetValue(rows[i][j], out Colour col))
                    {
                        Point pt = new Point(currX + allTreePages[index].Width * 0.5, 0);

                        GraphicsPath shape = toolShapes[rows[i][j]].Item1;
                        bool filled = toolShapes[rows[i][j]].Item2;
                        double shapeSize = toolShapes[rows[i][j]].Item3;

                        compositePage.Graphics.Save();
                        compositePage.Graphics.Translate(pt.X - (toolFont.MeasureText(toolName).Width + 13) * 0.5 - 13 + shapeSize, pt.Y);
                        compositePage.Graphics.Scale(shapeSize, shapeSize);

                        if (filled)
                        {
                            compositePage.Graphics.FillPath(shape, col);
                        }
                        else
                        {
                            compositePage.Graphics.FillPath(shape, Colours.White);
                            compositePage.Graphics.StrokePath(shape, col, 2.0 / shapeSize);
                        }

                        compositePage.Graphics.Restore();
                        compositePage.Graphics.FillText(pt.X - (toolFont.MeasureText(toolName).Width + 13) * 0.5 + shapeSize, 4.5, toolNames[rows[i][j]], toolFont, Colours.Black, TextBaselines.Baseline);
                    }
                    else
                    {
                        compositePage.Graphics.FillPath(new GraphicsPath().AddText(currX + allTreePages[index].Width * 0.5 - toolFont.MeasureText(toolName).Width * 0.5, 4.5, toolNames[rows[i][j]], toolFont, TextBaselines.Baseline), Colours.Black);
                    }

                    currX += allTreePages[index].Width + columnMargin;
                }

                compositePage.Graphics.Translate(0, maxHeight + rowMargin);
            }

            compositePage.Graphics.Restore();

            // Draw the paths that separate trees with the same topologies
            GraphicsPath topology1Path = new GraphicsPath();
            topology1Path.MoveTo(maxWidth, -10);
            topology1Path.LineTo(maxWidth - allTreePages[allTrees.IndexOf("noisy")].Width - columnMargins[0] / 3, -10);
            topology1Path.LineTo(maxWidth - allTreePages[allTrees.IndexOf("noisy")].Width - columnMargins[0] / 3, allTreePages[allTrees.IndexOf("noisy")].Height + rowMargin * 0.5);
            topology1Path.LineTo(0, allTreePages[allTrees.IndexOf("noisy")].Height + rowMargin * 0.5);
            topology1Path.LineTo(0, allTreePages[allTrees.IndexOf("raw")].Height + allTreePages[allTrees.IndexOf("noisy")].Height + rowMargin);
            topology1Path.LineTo(maxWidth - allTreePages[allTrees.IndexOf("alifilter")].Width - columnMargins[2] / 3, allTreePages[allTrees.IndexOf("raw")].Height + allTreePages[allTrees.IndexOf("noisy")].Height + rowMargin);
            topology1Path.LineTo(maxWidth - allTreePages[allTrees.IndexOf("alifilter")].Width - columnMargins[2] / 3, allTreePages[allTrees.IndexOf("raw")].Height + allTreePages[allTrees.IndexOf("noisy")].Height + rowMargin * 2 + allTreePages[allTrees.IndexOf("alifilter")].Height);
            topology1Path.LineTo(maxWidth, allTreePages[allTrees.IndexOf("raw")].Height + allTreePages[allTrees.IndexOf("noisy")].Height + rowMargin * 2 + allTreePages[allTrees.IndexOf("alifilter")].Height);
            topology1Path.Close();

            compositePage.Graphics.StrokePath(topology1Path, Colours.Black, 2);

            GraphicsPath topology2Path = new GraphicsPath();
            topology2Path.MoveTo(allTreePages[allTrees.IndexOf("bmge")].Width + columnMargins[2] / 3, allTreePages[allTrees.IndexOf("raw")].Height + allTreePages[allTrees.IndexOf("noisy")].Height + rowMargin * 1.5);
            topology2Path.LineTo(allTreePages[allTrees.IndexOf("bmge")].Width + allTreePages[allTrees.IndexOf("gblocks")].Width + allTreePages[allTrees.IndexOf("trimal")].Width + columnMargins[2] * 2, allTreePages[allTrees.IndexOf("raw")].Height + allTreePages[allTrees.IndexOf("noisy")].Height + rowMargin * 1.5);
            topology2Path.LineTo(allTreePages[allTrees.IndexOf("bmge")].Width + allTreePages[allTrees.IndexOf("gblocks")].Width + allTreePages[allTrees.IndexOf("trimal")].Width + columnMargins[2] * 2, allTreePages[allTrees.IndexOf("raw")].Height + allTreePages[allTrees.IndexOf("noisy")].Height + rowMargin * 2 + allTreePages[allTrees.IndexOf("trimal")].Height);
            topology2Path.LineTo(allTreePages[allTrees.IndexOf("bmge")].Width + columnMargins[2] / 3, allTreePages[allTrees.IndexOf("raw")].Height + allTreePages[allTrees.IndexOf("noisy")].Height + rowMargin * 2 + allTreePages[allTrees.IndexOf("trimal")].Height);
            topology2Path.Close();

            compositePage.Graphics.StrokePath(topology2Path, Colours.Black, 2);

            compositePage.Crop();

            // Resize to a width of 17cm.
            Page finalFigureS8 = new Page(482, compositePage.Height * 482 / compositePage.Width);
            finalFigureS8.Background = Colours.White;
            finalFigureS8.Graphics.Scale(482 / compositePage.Width, 482 / compositePage.Width);
            finalFigureS8.Graphics.DrawGraphics(0, 0, compositePage.Graphics);

            Document doc = new Document();
            doc.Pages.Add(finalFigureS8);

            finalFigureS8.SaveAsSVG("Figure_S8.svg");
            finalFigureS8.SaveAsSVG("Figure_S8.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_S8.pdf");
            finalFigureS8.SaveAsPNG("Figure_S8.png", 600.0 / 72);
        }
    }
}
