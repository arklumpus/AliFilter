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
using VectSharp.SVG;
using VectSharp;
using VectSharp.PDF;
using VectSharp.Raster;

namespace Figure_4
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Create the figure parts.
            Page barChart = CreateFigure4a();
            Page accuracyPlot = CreateFigure4b();

            // Draw both figure parts on the same figure.
            Page pag = new Page(1, 1);
            pag.Graphics.DrawGraphics(20, 0, barChart.Graphics);
            pag.Graphics.DrawGraphics(barChart.Width + 30, barChart.Height - accuracyPlot.Height, accuracyPlot.Graphics);

            // Draw the figure part letters.
            Font fntBold = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 14);
            pag.Graphics.FillText(0, 0, "a)", fntBold, Colours.Black);
            pag.Graphics.FillText(barChart.Width + 30, 0, "b)", fntBold, Colours.Black);
            pag.Crop();

            // Resize to a width of 17cm.
            Page resizedPage = new Page(482, pag.Height / pag.Width * 482);
            resizedPage.Background = Colours.White;
            resizedPage.Graphics.Scale(482 / pag.Width, 482 / pag.Width);
            resizedPage.Graphics.DrawGraphics(0, 0, pag.Graphics);

            Document doc = new Document();
            doc.Pages.Add(resizedPage);

            resizedPage.SaveAsSVG("Figure_4.svg");
            resizedPage.SaveAsSVG("Figure_4.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_4.pdf");
            resizedPage.SaveAsPNG("Figure_4.png", 600.0 / 72);
        }

        private static Page CreateFigure4a()
        {
            // Read the alignment lengths
            List<int[]>[] alignmentLengths = ReadAlignmentLengths();

            string[] tools = new string[] { "Unfiltered", "Manual", "AliFilter", "BMGE", "trimAl", "Gblocks", "Noisy", "ClipKIT" };
            string[] datasets = new string[] { "Dataset1", "Dataset2", "Dataset3", "Dataset4", "Dataset5", "Dataset6", "Dataset7", "Dataset8" };
            string[] datasetNames = new string[] { "Dataset 1", "Dataset 2", "Dataset 3", "Dataset 4", "Dataset 5", "Dataset 6", "Dataset 7", "Dataset 8" };

            Colour[] datasetColours = new Colour[8]
            {
                Colour.FromRgb(160, 160, 160), // Phylo
                Colour.FromRgb(0, 158, 115), // Cyano
                Colour.FromRgb(0, 114, 178), // Rhodo
                Colour.FromRgb(230, 159, 0), // Prok
                Colour.FromRgb(213, 94, 0), // Collembola
                Colour.FromRgb(214, 199, 32), // Formicidae
                Colour.FromRgb(86, 180, 233), // Heliconiini
                Colour.FromRgb(204, 121, 167), // Silva
            };

            // Compute the overall dataset lengths.
            (string, int[])[] datasetLengthByTool = tools.Select((x, i) => (x, alignmentLengths.Select(y => y.Select(z => z[i]).Sum()).ToArray())).ToArray();

            // Create the bar chart.
            Page pag = new Page(1, 1);
            Graphics gpr = pag.Graphics;

            double width = 400;
            double height = 250;
            double margin = width * 0.1;
            double marginY = 5;

            int maxY = datasetLengthByTool.Select(x => x.Item2.Sum()).Max();
            double barWidth = (width - margin * (tools.Length - 1)) / tools.Length;

            Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);
            Font fntBold = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 12);

            for (int i = 0; i < tools.Length; i++)
            {
                double centerX = barWidth * 0.5 + i * (barWidth + margin);

                int total = 0;

                for (int j = 0; j < datasetLengthByTool[i].Item2.Length; j++)
                {
                    double bottomY = height - (double)total / maxY * height - marginY * j;
                    double topY = height - (double)(total + datasetLengthByTool[i].Item2[j]) / maxY * height - marginY * j;

                    if (j == 0)
                    {
                        gpr.Save();
                        gpr.Translate(centerX, bottomY + marginY * 2);

                        gpr.Rotate(-Math.PI / 6);

                        gpr.FillText(-fnt.MeasureText(tools[i]).Width, 0, tools[i], fnt, Colours.Black, TextBaselines.Middle);

                        gpr.Restore();
                    }

                    GraphicsPath rect = new GraphicsPath().MoveTo(centerX - barWidth * 0.5, bottomY).LineTo(centerX + barWidth * 0.5, bottomY).LineTo(centerX + barWidth * 0.5, topY).LineTo(centerX - barWidth * 0.5, topY);
                    gpr.FillPath(rect, datasetColours[j]);

                    if (i > 0)
                    {
                        double perc = (double)datasetLengthByTool[i].Item2[j] / datasetLengthByTool[0].Item2[j];

                        gpr.FillText(centerX - barWidth * 0.5 - 2 - fnt.MeasureText(perc.ToString("0%", System.Globalization.CultureInfo.InvariantCulture)).Width, (topY + bottomY) * 0.5 + (j switch { 1 => 3, 3 => -3, _ => 0 }), perc.ToString("0%", System.Globalization.CultureInfo.InvariantCulture), fnt, datasetColours[j], TextBaselines.Middle);

                        if (j == datasetLengthByTool[i].Item2.Length - 1)
                        {
                            string totalLength = datasetLengthByTool[i].Item2.Sum().ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                            string percString = ((double)datasetLengthByTool[i].Item2.Sum() / datasetLengthByTool[0].Item2.Sum()).ToString("0%", System.Globalization.CultureInfo.InvariantCulture);

                            gpr.FillText(centerX - fntBold.MeasureText(percString).Width * 0.5, topY - marginY - fntBold.FontSize * 1.2, percString, fntBold, Colours.Black, TextBaselines.Bottom);
                            gpr.FillText(centerX - fntBold.MeasureText(totalLength).Width * 0.5, topY - marginY, totalLength, fntBold, Colours.Black, TextBaselines.Bottom);
                        }

                    }
                    else
                    {
                        gpr.FillText(centerX - barWidth * 0.5 - 2 - fnt.MeasureText(datasetLengthByTool[0].Item2[j].ToString("0", System.Globalization.CultureInfo.InvariantCulture)).Width, (topY + bottomY) * 0.5 + (j switch { 1 => 3, 3 => -3, _ => 0 }), datasetLengthByTool[0].Item2[j].ToString("0", System.Globalization.CultureInfo.InvariantCulture), fnt, datasetColours[j], TextBaselines.Middle);
                        gpr.FillText(centerX - barWidth * 0.5 - fnt.MeasureText("100000").Width - fnt.MeasureText(datasetNames[j]).Width - 4, (topY + bottomY) * 0.5 + (j switch { 1 => 3, 3 => -3, _ => 0 }), datasetNames[j], fnt, datasetColours[j], TextBaselines.Middle);

                        if (j == datasetLengthByTool[i].Item2.Length - 1)
                        {
                            string totalLength = datasetLengthByTool[0].Item2.Sum().ToString("0", System.Globalization.CultureInfo.InvariantCulture);

                            gpr.FillText(centerX - fntBold.MeasureText(totalLength).Width * 0.5, topY - marginY, totalLength, fntBold, Colours.Black, TextBaselines.Baseline);

                            gpr.FillText(centerX - barWidth * 0.5 - fnt.MeasureText("100000").Width - fntBold.MeasureText("Full dataset (9)").Width - 4, topY - marginY, "Full dataset (9)", fntBold, Colours.Black, TextBaselines.Baseline);
                        }
                    }

                    total += datasetLengthByTool[i].Item2[j];
                }
            }

            pag.Crop();
            return pag;
        }

        private static Page CreateFigure4b()
        {
            // Read thhe confusion matrices.
            List<int[][]>[] confusionMatrices = ReadConfusionMatrices();
            string[] tools = new string[] { "Unfiltered", "Manual", "AliFilter", "BMGE", "trimAl", "Gblocks", "Noisy", "ClipKIT" };
            string[] datasets = new string[] { "Dataset1", "Dataset2", "Dataset3", "Dataset4", "Dataset5", "Dataset6", "Dataset7", "Dataset8" };

            // Compute the "accuracy" score for each alignment.
            double[][][] accuracyByAlignment = datasets.Select((d, j) => tools.Skip(2).Select((t, i) => confusionMatrices[j].Select(z => ComputeAccuracy(z[i][0], z[i][1], z[i][2], z[i][3])).ToArray()).ToArray()).ToArray();

            Colour[] toolColours = new Colour[8]
            {
                    Colour.FromRgb(68, 187, 153), // Unfiltered
                    Colour.FromRgb(170, 170, 170), // Manual
                    Colour.FromRgb(119, 170, 221), // AliFilter
                    Colour.FromRgb(238, 136, 102), // BMGE
                    Colour.FromRgb(238, 221, 136), // trimAl
                    Colour.FromRgb(255, 170, 187), // Gblocks
                    Colour.FromRgb(153, 221, 255), // Noisy
                    Colour.FromRgb(187, 204, 51), // ClipKIT
            };

            // Create the box plots.
            Plot accuracyPlot = Plot.Create.BoxPlot(tools.Skip(2).Select((t, i) => accuracyByAlignment.SelectMany(y => y[i]).ToArray()).ToArray(), useNotches: false, width: 150, height: 280, yAxisTitle: "Accuracy", dataRangeMin: 0, dataRangeMax: 1,
                boxPresentationAttributes: toolColours.Skip(2).Select(x => new PlotElementPresentationAttributes()
                {
                    Stroke = x,
                    Fill = BlendWithWhite(x, 0.25)
                }).ToArray());

            // Fine-tune the plot appearance.
            accuracyPlot.RemovePlotElement(accuracyPlot.GetFirst<ScatterPoints<IReadOnlyList<double>>>());
            accuracyPlot.RemovePlotElement(accuracyPlot.GetFirst<ContinuousAxis>());
            accuracyPlot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(accuracyPlot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 12);

                for (int i = 0; i < tools.Length - 2; i++)
                {
                    Point pt = coord.ToPlotCoordinates(new double[] { i * 11 + 5, -0.075 });

                    gpr.Save();
                    gpr.Translate(pt);

                    gpr.Rotate(-Math.PI / 6);

                    gpr.FillText(-fnt.MeasureText(tools[i + 2]).Width, 0, tools[i + 2], fnt, Colours.Black, TextBaselines.Middle);

                    gpr.Restore();
                }
            }));

            int plotIndex = 0;

            foreach (ScatterPoints<IReadOnlyList<double>> outliers in accuracyPlot.GetAll<ScatterPoints<IReadOnlyList<double>>>())
            {
                double x = outliers.Data.First()[0];

                Swarm swarm = new Swarm(new double[] { x, 0 }, new double[] { 0, 1 }, outliers.Data.Select(x => x[1]), (IContinuousInvertibleCoordinateSystem)outliers.CoordinateSystem);

                swarm.PresentationAttributes = new PlotElementPresentationAttributes() { Stroke = null, Fill = toolColours[plotIndex + 2] };

                accuracyPlot.AddPlotElement(swarm);
                accuracyPlot.RemovePlotElement(outliers);

                plotIndex++;
            }

            Page pag = accuracyPlot.Render();
            pag.Crop();

            return pag;
        }

        /// <summary>
        /// Read the filtered and unfiltered alignment lengths.
        /// </summary>
        /// <returns>The filtered and unfiltered alignment lengths.</returns>
        private static List<int[]>[] ReadAlignmentLengths()
        {
            string[] datasets = new string[] { "Dataset1", "Dataset2", "Dataset3", "Dataset4", "Dataset5", "Dataset6", "Dataset7", "Dataset8" };

            List<int[]>[] alignmentLengths = datasets.Select(x => new List<int[]>()).ToArray();

            using (StreamReader sr = new StreamReader("../../../Data/alignmentLengths.txt"))
            {
                string line = sr.ReadLine();
                line = sr.ReadLine();

                while (line != null)
                {
                    string[] splitLine = line.Split('\t');

                    int[] lengths = splitLine.Skip(2).Select(x => int.Parse(x)).ToArray();
                    int[] orderedLengths = new int[] { lengths[0], lengths[1], lengths[2], lengths[3], lengths[7], lengths[5], lengths[6], lengths[4] };


                    alignmentLengths[Array.IndexOf(datasets, splitLine[0])].Add(orderedLengths);

                    line = sr.ReadLine();
                }
            }

            return alignmentLengths;
        }

        /// <summary>
        /// Read the confusion matrices.
        /// </summary>
        /// <returns>The confusion matrices.</returns>
        private static List<int[][]>[] ReadConfusionMatrices()
        {
            string[] datasets = new string[] { "Dataset1", "Dataset2", "Dataset3", "Dataset4", "Dataset5", "Dataset6", "Dataset7", "Dataset8" };

            List<int[][]>[] confusionMatrices = datasets.Select(x => new List<int[][]>()).ToArray();

            using (StreamReader sr = new StreamReader("../../../Data/confusionMatrices.txt"))
            {
                string line = sr.ReadLine();
                line = sr.ReadLine();

                while (line != null)
                {
                    string[] splitLine = line.Split('\t');

                    int[][] matrices = splitLine.Skip(2).Select(x => x.Split(",").Select(y => int.Parse(y)).ToArray()).ToArray();
                    int[][] orderedmatrices = new int[][] { matrices[0], matrices[1], matrices[5], matrices[3], matrices[4], matrices[2] };


                    confusionMatrices[Array.IndexOf(datasets, splitLine[0])].Add(orderedmatrices);

                    line = sr.ReadLine();
                }
            }

            return confusionMatrices;
        }

        /// <summary>
        /// Compute the accuracy score.
        /// </summary>
        /// <param name="tp">True positives.</param>
        /// <param name="tn">True negatives.</param>
        /// <param name="fp">False positives.</param>
        /// <param name="fn">False negatives.</param>
        /// <returns>The accuracy.</returns>
        private static double ComputeAccuracy(int tp, int tn, int fp, int fn)
        {
            return ((double)tp + tn) / ((double)tp + tn + fp + fn);
        }

        /// <summary>
        /// Blend a colour with white.
        /// </summary>
        /// <param name="col">The colour to blend.</param>
        /// <param name="percentage">The colour intensity (1 is white, 0 is col).</param>
        /// <returns>The blended colour.</returns>
        private static Colour BlendWithWhite(Colour col, double percentage)
        {
            return Colour.FromRgb(col.R * percentage + 1 - percentage, col.G * percentage + 1 - percentage, col.B * percentage + 1 - percentage);
        }
    }
}
