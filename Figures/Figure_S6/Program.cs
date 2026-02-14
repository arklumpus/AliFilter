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

using MathNet.Numerics.Statistics;
using System.Net.Http.Headers;
using VectSharp;
using VectSharp.PDF;
using VectSharp.Plots;
using VectSharp.Raster;
using VectSharp.SVG;

namespace Figure_S6
{
    internal partial class Program
    {
        static readonly Dictionary<string, (long fileSize, int medianLength)> AlignmentData = File.ReadLines("../../../Data/alignments.txt").Select(x => x.Split(" ")).Select(x => new KeyValuePair<string, (long, int)>(x[0], (long.Parse(x[1]), int.Parse(x[2])))).ToDictionary();


        static Program()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        static void Main(string[] args)
        {
            Page figS6a = CreateFigureS6a();
            Page figS6b = CreateFigureS6b();
            Page figS6c = CreateFigureS6c();
            Page legend = CreateLegend();

            Page compositePage = new Page(720, 670);

            compositePage.Graphics.DrawGraphics(0, 0, figS6a.Graphics);
            compositePage.Graphics.DrawGraphics(compositePage.Width - figS6b.Width, 0, figS6b.Graphics);
            compositePage.Graphics.DrawGraphics(0, compositePage.Height - figS6c.Height, figS6c.Graphics);
            compositePage.Graphics.DrawGraphics((compositePage.Width + figS6c.Width) * 0.5 - legend.Width * 0.5, compositePage.Height - figS6c.Height * 0.5 - legend.Height * 0.5, legend.Graphics);

            Font figurePartFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 20);

            compositePage.Graphics.FillText(0, 20, "a)", figurePartFont, Colours.Black, TextBaselines.Baseline);
            compositePage.Graphics.FillText(compositePage.Width - figS6b.Width, 20, "b)", figurePartFont, Colours.Black, TextBaselines.Baseline);
            compositePage.Graphics.FillText(0, compositePage.Height - figS6c.Height, "c)", figurePartFont, Colours.Black, TextBaselines.Baseline);

            // Resize to a width of 17cm.
            Page finalFigureS6 = new Page(482, compositePage.Height * 482 / compositePage.Width);
            finalFigureS6.Background = Colours.White;
            finalFigureS6.Graphics.Scale(482 / compositePage.Width, 482 / compositePage.Width);
            finalFigureS6.Graphics.DrawGraphics(0, 0, compositePage.Graphics);

            Document doc = new Document();
            doc.Pages.Add(finalFigureS6);

            finalFigureS6.SaveAsSVG("Figure_S6.svg");
            finalFigureS6.SaveAsSVG("Figure_S6.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_S6.pdf");
            finalFigureS6.SaveAsPNG("Figure_S6.png", 600.0 / 72);
        }


        static Page CreateLegend()
        {
            // Colour to use for each tool.
            Dictionary<string, Colour> toolColours = new Dictionary<string, Colour>()
            {
                { "alifilter", Colour.FromRgb(119, 170, 221) },
                { "bmge",    Colour.FromRgb(238, 136, 102) },
                { "clipkit",   Colour.FromRgb(187, 204, 51) },
                { "gblocks",    Colour.FromRgb(255, 170, 187) },
                { "silva",    Colour.FromRgb(128, 128, 128) },
            };

            // Symbols for each tool.
            GraphicsPath circle = new GraphicsPath().Arc(0, 0, 1, 0, 2 * Math.PI).Close();
            GraphicsPath square = new GraphicsPath().MoveTo(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1).Close();
            GraphicsPath diamond = new GraphicsPath().MoveTo(0, -1).LineTo(1, 0).LineTo(0, 1).LineTo(-1, 0).Close();
            GraphicsPath triangle = new GraphicsPath().MoveTo(0, -1).LineTo(-1, 1).LineTo(1, 1).Close();

            Dictionary<string, (GraphicsPath, bool, double)> toolShapes = new Dictionary<string, (GraphicsPath, bool, double)>()
            {
                { "alifilter", (triangle, true, 5) },
                { "bmge", (diamond, true, 5) },
                { "clipkit", (square, false, 3.5) },
                { "gblocks", (circle, false, 4.5) },
                { "silva", (circle, false, 5.5) },
            };

            Dictionary<string, string> toolNames = new Dictionary<string, string>()
            {
                { "alifilter", "AliFilter" },
                { "bmge", "BMGE" },
                { "clipkit", "ClipKIT" },
                { "gblocks", "Gblocks" },
                { "silva", "SSU / LSU" }
            };

            string[] tools = new string[] { "alifilter", "bmge", "clipkit", "gblocks", "silva" };

            Page pag = new Page(1, 1);
            Graphics gpr = pag.Graphics;

            Font fnt = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 16);

            for (int i = 0; i < tools.Length; i++)
            {
                Colour col = toolColours[tools[i]];
                GraphicsPath shape = toolShapes[tools[i]].Item1;
                bool filled = toolShapes[tools[i]].Item2;
                double shapeSize = toolShapes[tools[i]].Item3 * 1.33;

                gpr.Save();
                gpr.Translate(5, i * 24);
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

                gpr.FillText(20, i * 24 + 5.5, toolNames[tools[i]], fnt, Colours.Black, TextBaselines.Baseline);
            }

            

            pag.Crop();
            return pag;
        }
        


        static Dictionary<string, (double[] runtime, long[] ram, int filteredLength)> ReadResults(string tool)
        {
            return File.ReadLines($"../../../Data/stats.{tool}.txt").Select(x => x.Split(" ")).Select(x => new KeyValuePair<string, (double[], long[], int)>(x[0], (x[1].Split(";").Select(y => double.Parse(y)).ToArray(), x[2].Split(";").Select(y => long.Parse(y) * 1024).ToArray(), int.Parse(x[3])))).ToDictionary();
        }

    }

    // A non-linear coordinate system for the runtime axis (necessary because the times we need to display range from seconds to hours).
    class TimeCoordinateSystem : IContinuousInvertibleCoordinateSystem
    {
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public bool IsLinear => false;

        public TimeCoordinateSystem(double minX, double minY, double maxX, double maxY, double width, double height)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            Width = width;
            Height = height;
        }

        public double[] Resolution => new double[] { (MaxX - MinX) * 0.01, (MaxY - MinY) * 0.01 };

        public double[] GetAround(IReadOnlyList<double> point, IReadOnlyList<double> direction)
        {
            double magnitude = Math.Sqrt(direction[0] * direction[0] + direction[1] * direction[1]);

            return new double[] { point[0] + direction[0] / magnitude * 0.01, point[1] + direction[1] / magnitude * 0.01 };
        }

        public bool IsDirectionStraight(IReadOnlyList<double> direction)
        {
            return direction[0] == 0 || direction[1] == 0;
        }

        public double[] ToDataCoordinates(Point plotPoint)
        {
            double y;

            if ((Height - plotPoint.Y) / Height * 2 < 1)
            {
                y = ((Height - plotPoint.Y) / Height * 2) * 1000;
            }
            else if (((Height - plotPoint.Y) / Height * 2) < 2)
            {
                y = (((Height - plotPoint.Y) / Height * 2) - 1) * 60000 + 1000;
            }
            else
            {
                y = (((Height - plotPoint.Y) / Height * 2) - 2) * 3600000 + 61000;
            }

            return new double[] { MinX + plotPoint.X / Width * (MaxX - MinX), y };
        }

        public Point ToPlotCoordinates(IReadOnlyList<double> dataPoint)
        {
            double y;

            if (dataPoint[1] < 1000)
            {
                y = dataPoint[1] / 1000.0;
            }
            else if (dataPoint[1] < 60000)
            {
                y = 1 + (dataPoint[1] - 1000) / 60000.0;
            }
            else
            {
                y = 2 + (dataPoint[1] - 61000) / 3600000.0;
            }

            return new Point((dataPoint[0] - MinX) / (MaxX - MinX) * Width, Height - y / 2 * Height);
        }
    }
}
