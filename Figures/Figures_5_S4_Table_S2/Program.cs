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

using VectSharp.SVG;
using VectSharp;
using VectSharp.PDF;
using VectSharp.Raster;

namespace Figures_5_S4_Table_S2
{
    internal static partial class Program
    {
        static void Main(string[] args)
        {
            // To ensure consistent formatting if the system language is not set to English.
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            // Print Table S2 to the console.
            CreateTableS2(Console.Out);

            // Create Figure 5.
            CreateFigure5();

            // Create Figure S4.
            CreateFigureS4();
        }

        static void CreateFigure5()
        {
            // Create the figure parts.
            Page figure5a = CreateFigure5a();
            Page figure5b = CreateFigure5b();

            // Draw the two figure parts on the same page.
            Page compositeFigure = new Page(figure5a.Width + figure5b.Width + 10, Math.Max(figure5a.Height, figure5b.Height));
            compositeFigure.Graphics.DrawGraphics(0, compositeFigure.Height * 0.5 - figure5a.Height * 0.5, figure5a.Graphics);
            compositeFigure.Graphics.DrawGraphics(figure5a.Width + 10, compositeFigure.Height * 0.5 - figure5b.Height * 0.5, figure5b.Graphics);

            // Draw the figure part letters.
            Font partLetterFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 18);
            compositeFigure.Graphics.FillText(0, 0, "a)", partLetterFont, Colours.Black);
            compositeFigure.Graphics.FillText(figure5a.Width + 15, 0, "b)", partLetterFont, Colours.Black);
            compositeFigure.Crop();

            // Resize to a width of 17cm.
            Page finalFigure5 = new Page(482, compositeFigure.Height * 482 / compositeFigure.Width);
            finalFigure5.Background = Colours.White;
            finalFigure5.Graphics.Scale(482 / compositeFigure.Width, 482 / compositeFigure.Width);
            finalFigure5.Graphics.DrawGraphics(0, 0, compositeFigure.Graphics);

            Document doc = new Document();
            doc.Pages.Add(finalFigure5);

            finalFigure5.SaveAsSVG("Figure_5.svg");
            finalFigure5.SaveAsSVG("Figure_5.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_5.pdf");
            finalFigure5.SaveAsPNG("Figure_5.png", 600.0 / 72);
        }
    }

}
