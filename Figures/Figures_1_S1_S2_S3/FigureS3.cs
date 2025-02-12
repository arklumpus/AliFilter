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

using MathNet.Numerics.Statistics;
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
        public static void CreateFigureS3()
        {
            // Read the alignment and the masks
            Dictionary<string, string> alignment = FASTA.Read("../../../Data/95.aligned.fas");
            Dictionary<string, string> allMasks = FASTA.Read("../../../Data/masks_mistakes.txt");

            // Thresholds from the validated models.
            double[] thresholds = new double[] { 0.36, 0.36, 0.36, 0.36, 0.36, 0.55, 0.55, 0.46, 0.46, 0.55, 0.54, 0.48, 0.38, 0.54, 0.54, 0.53, 0.53, 0.54, 0.54, 0.48, 0.49, 0.43, 0.53, 0.53, 0.42, 0.44, 0.52, 0.49, 0.49, 0.44, 0.45, 0.52, 0.5, 0.52, 0.45, 0.47, 0.47, 0.47, 0.5, 0.47, 0.51, 0.48, 0.48, 0.52, 0.51, 0.49, 0.49, 0.49, 0.5, 0.5, 0.5, 0.5, 0.5, 0.51, 0.5 };

            string manualMask = allMasks["Manual"];
            allMasks.Remove("Manual");

            string alifilterMask = allMasks["AliFilter"];
            allMasks.Remove("AliFilter");

            Dictionary<string, double[]> masks = new Dictionary<string, double[]>(allMasks.Select(x => new KeyValuePair<string, double[]>(x.Key, x.Value.Split(" ").Select(y => double.Parse(y, System.Globalization.CultureInfo.InvariantCulture)).ToArray())));

            // For enhanced visual clarity, build a neighbour-joining tree from the alignment and sort the sequences accordingly.
            TreeNode njTree = NeighborJoining.BuildTree(alignment, evolutionModel: EvolutionModel.BLOSUM62);

            // Re-root the tree.
            njTree = njTree.GetRootedTree(njTree.GetLastCommonAncestor("Candidatus.Obscuribacter.phosphatis", "Heliobacterium_modesticaldum"));

            // Outgroup taxa.
            List<string> outgroup = njTree.GetLastCommonAncestor("Candidatus.Obscuribacter.phosphatis", "Heliobacterium_modesticaldum").GetLeafNames();

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
            Page maskPage = DrawFuzzyMasks(masks.Keys.OrderBy(x => double.Parse(x.Split("_")[1].Split(".")[0..^1].Aggregate((a, b) => a + "." + b), System.Globalization.CultureInfo.InvariantCulture)).ThenBy(x => int.Parse(x.Split(".")[^1])).ToList(), masks, residueWidth, maskHeight * 0.2, colouring);

            // Final page for the full figure.
            Page fullPage = new Page(482, alignmentPage.Height + 15 + manualMaskPage.Height + aliFilterMaskPage.Height + maskPage.Height + maskHeight + 5 + 30);
            fullPage.Background = Colours.White;
            Graphics gpr = fullPage.Graphics;

            // Maximum width of the key labels.
            //double labelWidth = Math.Max(masks.Keys.Select(x => maskFont.MeasureText(x).Width).Max(), outgroupFont.MeasureText("Outgroup").Width + 7);
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

            // Labels for the masks.
            {
                gpr.FillText(labelWidth - maskFont.MeasureText("Manual").Width, alignmentPage.Height + 5 + maskHeight * 0.5, "Manual", maskFont, Colours.Black, TextBaselines.Middle);

                gpr.FillText(labelWidth - maskFont.MeasureText("AliFilter").Width, alignmentPage.Height + 5 + manualMaskPage.Height + 5 + maskHeight * 0.5, "AliFilter", maskFont, Colours.Black, TextBaselines.Middle);

                for (int i = 0; i < 11; i++)
                {
                    string text = (i * 0.05).ToString("0%");

                    gpr.FillText(labelWidth - maskFont.MeasureText(text).Width, alignmentPage.Height + 15 + manualMaskPage.Height + aliFilterMaskPage.Height + maskHeight * (i + 0.5), text, maskFont, Colours.Black, TextBaselines.Middle);
                }

                gpr.Save();

                gpr.Translate(labelWidth - maskFont.MeasureText("50%").Width - 10, alignmentPage.Height + 15 + manualMaskPage.Height + aliFilterMaskPage.Height + maskPage.Height * 0.5);
                gpr.Rotate(-Math.PI / 2);

                gpr.FillText(-maskFont.MeasureText("% Mistakes").Width * 0.5, 0, "% Mistakes", maskFont, Colours.Black, TextBaselines.Bottom);

                gpr.Restore();
            }

            // Preservation score colour scale.
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
                    double q1 = thresholds.LowerQuartile();
                    double medianThreshold = thresholds.Median();
                    double q3 = thresholds.UpperQuartile();

                    double left = x0 + thresholds.Min() * width;
                    double x1 = x0 + q1 * width;
                    double x2 = x0 + medianThreshold * width;
                    double x3 = x0 + q3 * width;
                    double right = x0 + thresholds.Max() * width;


                    gpr.StrokePath(new GraphicsPath().MoveTo(left, -12 - maskHeight * 0.25).LineTo(left, -12 + maskHeight * 0.25).MoveTo(left, -12).LineTo(right, -12).MoveTo(right, -12 - maskHeight * 0.25).LineTo(right, -12 + maskHeight * 0.25), scoreBrush);
                    gpr.FillRectangle(x1, -12 - maskHeight * 0.25, x3 - x1, maskHeight * 0.5, scoreBrush);
                    gpr.StrokePath(new GraphicsPath().MoveTo(x2, -12 - maskHeight * 0.25).LineTo(x2, -12 + maskHeight * 0.25), Colours.White);

                    gpr.FillText(left - maskFont.MeasureText("Model thresholds").Width - 5, -12, "Model thresholds", maskFont, Colours.Black, TextBaselines.Middle);
                }

                gpr.Restore();
            }

            Document doc = new Document();
            doc.Pages.Add(fullPage);

            fullPage.SaveAsSVG("Figure_S3.svg");
            fullPage.SaveAsSVG("Figure_S3.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_S3.pdf");
            fullPage.SaveAsPNG("Figure_S3.png", 600 / 72.0);
        }
    }
}
