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

using PhyloTree.TreeBuilding;
using PhyloTree;
using VectSharp.SVG;
using VectSharp;
using VectSharp.PDF;
using VectSharp.Raster;

namespace Figures_1_S1_S2_S3
{
    internal partial class Program
    {
        public static void CreateFigureS1()
        {
            // Read the alignment and the masks
            Dictionary<string, string> alignment = FASTA.Read("../../../Data/27.aligned.fas");

            // Threshold from the validated model.
            double threshold = 0.36;

            // The masks have been added directly as "sequences" at the end of the alignment.
            string manualMask = alignment["Manual"];
            alignment.Remove("Manual");

            string alifilterMask = alignment["AliFilter"];
            alignment.Remove("AliFilter");

            double[] alifilterScores = alignment["AliFilter_Float"].Split(" ").Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            alignment.Remove("AliFilter_Float");

            // For enhanced visual clarity, build a neighbour-joining tree from the alignment and sort the sequences accordingly.
            TreeNode njTree = NeighborJoining.BuildTree(alignment, evolutionModel: EvolutionModel.BLOSUM62);

            // Re-root the tree.
            njTree = njTree.GetRootedTree(njTree.GetLastCommonAncestor("Candidatus.Obscuribacter.phosphatis", "Candidatus.Caenarcanum.bioreactoricola"));

            // Outgroup taxa.
            List<string> outgroup = njTree.GetLastCommonAncestor("Candidatus.Obscuribacter.phosphatis", "Candidatus.Caenarcanum.bioreactoricola").GetLeafNames();

            // Get the sorted sequence names.
            List<string> sortedSequences = njTree.GetLeafNames();

            // Width of a single residue in the alignment.
            double residueWidth = 2;
            // Height of each sequence in the alignment.
            double sequenceHeight = 1;
            // Height of each mask in the alignment.
            double maskHeight = 15;

            Colour manualMaskColour = Colour.FromRgb(170, 170, 170);
            // Font for the mask names.
            Font maskFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 10);

            // Font for the outgroup.
            Font outgroupFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaOblique), 8);

            // Draw the alignment.
            Page alignmentPage = DrawAlignment(sortedSequences, alignment, residueWidth, sequenceHeight);

            // Colouring scheme for the fuzzy masks.
            Func<double, Colour> colouring = Gradients.ViridisColouring;
            GradientStops colouringGradient = Gradients.Viridis;

            // Draw the masks.
            Page manualMaskPage = DrawMask(manualMask, residueWidth, maskHeight, manualMaskColour);
            Page aliFilterMaskPage = DrawMask(alifilterMask, residueWidth, maskHeight, Colour.FromRgb(119, 170, 221));
            Page maskPage = DrawFuzzyMasks(new List<string>() { "AliFilter_Float" }, new Dictionary<string, double[]>() { { "AliFilter_Float", alifilterScores } }, residueWidth, maskHeight, colouring);

            // Final page for the full figure.
            Page fullPage = new Page(482, alignmentPage.Height + 15 + manualMaskPage.Height + aliFilterMaskPage.Height + maskPage.Height + maskHeight + 5 + 30);
            fullPage.Background = Colours.White;
            Graphics gpr = fullPage.Graphics;

            // Maximum width of the key labels.
            double labelWidth = outgroupFont.MeasureText("Outgroup").Width + 7;

            // Available width for the alignment and masks.
            double xScale = (fullPage.Width - 5 - labelWidth) / alignmentPage.Width;

            // Assume that the outgroup sequences are at the top.
            int maxOutgroup = outgroup.Select(x => sortedSequences.IndexOf(x)).Max();
            gpr.FillRectangle(labelWidth, 0, fullPage.Width - labelWidth, maxOutgroup * sequenceHeight, Colour.FromRgb(229, 229, 229));
            gpr.FillRectangle(labelWidth - 4, 0, 4, maxOutgroup * sequenceHeight, Colour.FromRgb(187, 187, 187));
            gpr.FillText(labelWidth - 7 - outgroupFont.MeasureText("Outgroup").Width, maxOutgroup * sequenceHeight * 0.5, "Outgroup", outgroupFont, Colours.Black, TextBaselines.Middle);

            gpr.Save();
            gpr.Translate(labelWidth + 5, 0);
            gpr.Scale(xScale, 1);

            // Draw the alignment.
            gpr.DrawGraphics(0, 0, alignmentPage.Graphics);

            gpr.Translate(0, alignmentPage.Height + 5);

            // Draw the manual mask.
            gpr.DrawGraphics(0, 0, manualMaskPage.Graphics);

            gpr.Translate(0, manualMaskPage.Height + 5);

            // Draw the AliFilter mask.
            gpr.DrawGraphics(0, 0, aliFilterMaskPage.Graphics);

            gpr.Translate(0, aliFilterMaskPage.Height + 5);

            // Draw the masks.
            gpr.DrawGraphics(0, 0, maskPage.Graphics);
            gpr.Restore();


            {
                gpr.FillText(labelWidth - maskFont.MeasureText("Manual").Width, alignmentPage.Height + 5 + maskHeight * 0.5, "Manual", maskFont, Colours.Black, TextBaselines.Middle);

                gpr.FillText(labelWidth - maskFont.MeasureText("AliFilter").Width, alignmentPage.Height + 5 + manualMaskPage.Height + 5 + maskHeight * 0.5, "AliFilter", maskFont, Colours.Black, TextBaselines.Middle);

                gpr.FillText(labelWidth - maskFont.MeasureText("Scores").Width, alignmentPage.Height + 5 + manualMaskPage.Height + 5 + maskPage.Height + 5 + maskHeight * 0.5, "Scores", maskFont, Colours.Black, TextBaselines.Middle);
            }

            {
                gpr.Save();
                gpr.Translate(5, alignmentPage.Height + 20 + manualMaskPage.Height + aliFilterMaskPage.Height + maskPage.Height + 15);

                gpr.FillText(-5, maskHeight * 0.5, "Preservation score", maskFont, Colours.Black, TextBaselines.Middle);

                double width = 350;
                double x0 = maskFont.MeasureText("Preservation score").Width;

                for (int i = 0; i <= 10; i++)
                {
                    string text = (i * 0.1).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

                    double x = x0 + i * 0.1 * width;

                    gpr.StrokePath(new GraphicsPath().MoveTo(x, -3).LineTo(x, maskHeight + 3), Colours.Black);

                    gpr.FillText(x - maskFont.MeasureText(text.ToString()).Width * 0.5, maskHeight + 6, text, maskFont, Colours.Black);
                }

                LinearGradientBrush scoreBrush = new LinearGradientBrush(new VectSharp.Point(x0, 0), new VectSharp.Point(x0 + width, 0), colouringGradient);

                gpr.FillRectangle(x0, 0, width, maskHeight, scoreBrush);
                gpr.StrokeRectangle(x0, 0, width, maskHeight, scoreBrush, 1.5);

                {
                    double left = x0 + threshold * width;

                    gpr.FillPath(new GraphicsPath().MoveTo(left, -12 + maskHeight * 0.25).LineTo(left - maskHeight * 0.25, -12 - maskHeight * 0.25).LineTo(left + maskHeight * 0.25, -12 - maskHeight * 0.25).Close(), colouring(threshold));

                    gpr.FillText(left - maskFont.MeasureText("Model threshold").Width - 8, -12, "Model threshold", maskFont, Colours.Black, TextBaselines.Middle);
                }

                gpr.Restore();
            }

            Document doc = new Document();
            doc.Pages.Add(fullPage);

            fullPage.SaveAsSVG("Figure_S1.svg");
            fullPage.SaveAsSVG("Figure_S1.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_S1.pdf");
            fullPage.SaveAsPNG("Figure_S1.png", 600 / 72.0);
        }
    }
}
