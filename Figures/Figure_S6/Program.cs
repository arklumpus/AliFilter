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
using VectSharp.Plots;
using VectSharp.Raster;
using VectSharp.SVG;

namespace Figure_S6
{
    internal partial class Program
    {
        static void Main(string[] args)
        {
            // To ensure consistent formatting if the system language is not set to English.
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            Page figureS6a = CreateFigureS6a();
            Page figureS6b = CreateFigureS6b();
            Page figureS6c = CreateFigureS6c();

            Font partLetterFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 20);

            Page figureS6 = new Page(1, 1);

            figureS6.Graphics.FillText(0, 0, "a)", partLetterFont, Colours.Black);
            figureS6.Graphics.DrawGraphics(0, 0, figureS6a.Graphics);

            figureS6.Graphics.FillText(figureS6a.Width + 5, 0, "b)", partLetterFont, Colours.Black);
            figureS6.Graphics.DrawGraphics(figureS6a.Width, 0, figureS6b.Graphics);

            figureS6.Graphics.FillText(0, figureS6a.Height + 10, "c)", partLetterFont, Colours.Black);
            figureS6.Graphics.DrawGraphics(0, figureS6a.Height + 10, figureS6c.Graphics);

            figureS6.Crop();

            // Resize to a width of 17cm.
            Page finalFigureS6 = new Page(482, figureS6.Height * 482 / figureS6.Width);
            finalFigureS6.Background = Colours.White;
            finalFigureS6.Graphics.Scale(482 / figureS6.Width, 482 / figureS6.Width);
            finalFigureS6.Graphics.DrawGraphics(0, 0, figureS6.Graphics);

            Document doc = new Document();
            doc.Pages.Add(finalFigureS6);

            finalFigureS6.SaveAsSVG("Figure_S6.svg");
            finalFigureS6.SaveAsSVG("Figure_S6.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_S6.pdf");
            finalFigureS6.SaveAsPNG("Figure_S6.png", 600.0 / 72);
        }


        static Page CreateFigureS6a()
        {
            // Read the alignment length data.
            (string toolName, int totalLength, int distinct, int parsInformative)[] alignmentData = ReadAlignmentLengths().OrderByDescending(x => x.totalLength).ToArray();

            // Colour to use for each tool.
            Dictionary<string, Colour> toolColours = new Dictionary<string, Colour>()
            {
                { "raw", Colour.FromRgb(80, 80, 80) },
                { "alifilter", Colour.FromRgb(119, 170, 221) },
                { "bmge",    Colour.FromRgb(238, 136, 102) },
                { "trimal",    Colour.FromRgb(238, 221, 136) },
                { "gblocks",    Colour.FromRgb(255, 170, 187) },
                { "noisy",   Colour.FromRgb(153, 221, 255) },
                { "clipkit",   Colour.FromRgb(187, 204, 51) }
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

            Plot barChart = Plot.Create.StackedBarChart(new[] { ("", (IReadOnlyList<double>)new double[] { 0, 0, 350000 }) }.Concat(alignmentData.Select(x => (toolNames[x.toolName], (IReadOnlyList<double>)new double[] { x.parsInformative, x.distinct - x.parsInformative, x.totalLength - x.distinct }))).ToArray(), yAxisTitle: "Alignment length (residues)", width: 200);

            barChart.GetFirst<ContinuousAxisTicks>().StartPoint = new double[] { 1, barChart.GetFirst<ContinuousAxisTicks>().StartPoint[1] };
            barChart.GetFirst<ContinuousAxisTicks>().IntervalCount--;
            barChart.GetFirst<DataLabels<IReadOnlyList<double>>>().Alignment = TextAnchors.Right;
            barChart.GetFirst<DataLabels<IReadOnlyList<double>>>().Baseline = TextBaselines.Baseline;
            barChart.GetFirst<DataLabels<IReadOnlyList<double>>>().Rotation = (_, _) => -Math.PI / 6;
            barChart.GetFirst<DataLabels<IReadOnlyList<double>>>().Margin = (_, _) => new Point(0, 10);
            barChart.RemovePlotElement(barChart.GetFirst<ContinuousAxisLabels>());

            barChart.AddPlotElement(new DataLabels<IReadOnlyList<double>>(Enumerable.Range(0, 6).Select(x => new double[] { barChart.GetAll<ContinuousAxis>().ElementAt(1).StartPoint[0], x * 70000 }), barChart.GetFirst<IContinuousCoordinateSystem>())
            {
                Label = (i, x) => Math.Round(x[1] / 1000).ToString() + "k",
                Alignment = TextAnchors.Right,
                Margin = (_, _) => new Point(-7, 0)
            });

            barChart.GetAll<ContinuousAxisTitle>().ElementAt(1).Position += 20;

            barChart.AddPlotElement(new DataLabels<IReadOnlyList<double>>(alignmentData.Skip(1).Select((x, i) => new double[] { i + 2, x.totalLength }), barChart.GetFirst<IContinuousCoordinateSystem>())
            {
                Label = (_, x) => (x[1] / alignmentData[0].totalLength).ToString("0%"),
                Margin = (_, _) => new Point(-2, -5),
                Alignment = TextAnchors.Left,
                Rotation = (_, _) => -Math.PI / 4
            });

            StackedBars allBars = barChart.GetFirst<StackedBars>();
            barChart.RemovePlotElement(allBars);

            for (int i = 0; i < alignmentData.Length; i++)
            {
                StackedBars toolBar = new StackedBars(new double[][] { new double[] { i + 1, alignmentData[i].parsInformative, alignmentData[i].distinct - alignmentData[i].parsInformative, alignmentData[i].totalLength - alignmentData[i].distinct } }, barChart.GetFirst<IContinuousCoordinateSystem>())
                {
                    Margin = 0.25,
                    PresentationAttributes = new PlotElementPresentationAttributes[]
                    {
                        new PlotElementPresentationAttributes() { Stroke = null, Fill = toolColours[alignmentData[i].toolName] },
                        new PlotElementPresentationAttributes() { Stroke = null, Fill = BlendWithWhite(toolColours[alignmentData[i].toolName], 0.66) },
                        new PlotElementPresentationAttributes() { Stroke = null, Fill = BlendWithWhite(toolColours[alignmentData[i].toolName], 0.33) }
                    }
                };

                barChart.AddPlotElement(toolBar);
            }

            barChart.AddPlotElement(new PlotElement<IReadOnlyList<double>>(barChart.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                double y0 = coord.ToPlotCoordinates(new double[] { 0, 0 }).Y;
                double y1 = coord.ToPlotCoordinates(new double[] { 0, alignmentData[0].parsInformative }).Y;
                double y2 = coord.ToPlotCoordinates(new double[] { 0, alignmentData[0].distinct }).Y;
                double y3 = coord.ToPlotCoordinates(new double[] { 0, alignmentData[0].totalLength }).Y;

                double x0 = coord.ToPlotCoordinates(new double[] { 0.375, 0 }).X;
                double x1 = coord.ToPlotCoordinates(new double[] { -0.375, 0 }).X;

                Font labelFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 10);

                gpr.StrokePath(new GraphicsPath().MoveTo(x0, y0).LineTo(x0 * 0.8 + x1 * 0.2, y0).LineTo(x0 * 0.8 + x1 * 0.2, y1).LineTo(x0, y1), toolColours[alignmentData[0].toolName]);
                gpr.Save();
                gpr.Translate(x0 * 0.8 + x1 * 0.2, y1 - 5);
                gpr.Rotate(-Math.PI / 2);
                gpr.FillText(0, 6, "Parsimony-informative", labelFont, toolColours[alignmentData[0].toolName], TextBaselines.Baseline);
                gpr.Restore();

                gpr.StrokePath(new GraphicsPath().MoveTo(x0 * 0.6 + x1 * 0.4, y0).LineTo(x0 * 0.4 + x1 * 0.6, y0).LineTo(x0 * 0.4 + x1 * 0.6, y2).LineTo(x0 * 0.6 + x1 * 0.4, y2), BlendWithWhite(toolColours[alignmentData[0].toolName], 0.66));
                gpr.Save();
                gpr.Translate(x0 * 0.4 + x1 * 0.6, y2 - 5);
                gpr.Rotate(-Math.PI / 2);
                gpr.FillText(0, 4, "Distinct", labelFont, BlendWithWhite(toolColours[alignmentData[0].toolName], 0.66), TextBaselines.Baseline);
                gpr.Restore();

                gpr.StrokePath(new GraphicsPath().MoveTo(x0 * 0.2 + x1 * 0.8, y0).LineTo(x1, y0).LineTo(x1, y3).LineTo(x0 * 0.2 + x1 * 0.8, y3), BlendWithWhite(toolColours[alignmentData[0].toolName], 0.33));
                gpr.Save();
                gpr.Translate(x1, y3 + 5);
                gpr.Rotate(-Math.PI / 2);
                gpr.FillText(-labelFont.MeasureText("Total").Width, 10, "Total", labelFont, BlendWithWhite(toolColours[alignmentData[0].toolName], 0.33), TextBaselines.Baseline);
                gpr.Restore();

            }));

            return barChart.Render();
        }

        /// <summary>
        /// Read the alignment length data.
        /// </summary>
        /// <returns>The alignment length data as a collection of tuples including the tool name, the total
        /// alignment length, the number of distinct patterns, and the number of parsimony-informative sites.</returns>
        static IEnumerable<(string toolName, int totalLength, int distinct, int parsInformative)> ReadAlignmentLengths()
        {
            using (StreamReader sr = new StreamReader("../../../Data/alignment_length.txt"))
            {
                string line = sr.ReadLine();

                line = sr.ReadLine();

                while (line != null)
                {
                    string[] splitLine = line.Split('\t');

                    yield return (splitLine[0], int.Parse(splitLine[1]), int.Parse(splitLine[2]), int.Parse(splitLine[3]));
                    line = sr.ReadLine();
                }
            }
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
