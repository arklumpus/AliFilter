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
        public static void CreateFigure1()
        {
            // Read the alignment and the masks
            Dictionary<string, string> alignment = FASTA.Read("../../../Data/95.aligned.fas");
            Dictionary<string, string> masks = FASTA.Read("../../../Data/masks.txt");

            string manualMask = masks["Manual"];
            masks.Remove("Manual");

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
            // Colours for the masks.
            Colour[] maskColours = new Colour[]
            {
                    Colour.FromRgb(119, 170, 221), // AliFilter
                    Colour.FromRgb(238, 136, 102), // BMGE
                    Colour.FromRgb(238, 221, 136), // trimAl
                    Colour.FromRgb(255, 170, 187), // Gblocks
                    Colour.FromRgb(153, 221, 255), // Noisy
                    Colour.FromRgb(187, 204, 51),  // ClipKIT
            };


            Colour manualMaskColour = Colour.FromRgb(170, 170, 170);
            // Font for the mask names.
            Font maskFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 10);

            // Font for the outgroup.
            Font outgroupFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaOblique), 8);

            // Draw the alignment.
            Page alignmentPage = DrawAlignment(sortedSequences, alignment, residueWidth, sequenceHeight);

            // Draw the masks.
            Page manualMaskPage = DrawMask(manualMask, residueWidth, maskHeight, manualMaskColour);
            Page maskPage = DrawMasks(masks, residueWidth, maskHeight, maskColours);

            // Final page for the full figure.
            Page fullPage = new Page(482, alignmentPage.Height + 10 + manualMaskPage.Height + maskPage.Height);
            fullPage.Background = Colours.White;
            Graphics gpr = fullPage.Graphics;

            // Maximum width of the key labels.
            double labelWidth = Math.Max(masks.Keys.Select(x => maskFont.MeasureText(x).Width).Max(), outgroupFont.MeasureText("Outgroup").Width + 7);

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

            // Draw the masks.
            gpr.DrawGraphics(0, 0, maskPage.Graphics);
            gpr.Restore();

            {
                gpr.FillText(labelWidth - maskFont.MeasureText("Manual").Width, alignmentPage.Height + 5 + maskHeight * 0.5, "Manual", maskFont, Colours.Black, TextBaselines.Middle);

                int i = 0;
                foreach (KeyValuePair<string, string> kvp in masks)
                {
                    gpr.FillText(labelWidth - maskFont.MeasureText(kvp.Key).Width, alignmentPage.Height + 10 + manualMaskPage.Height + maskHeight * (i + 0.5), kvp.Key, maskFont, Colours.Black, TextBaselines.Middle);
                    i++;
                }
            }

            Document doc = new Document();
            doc.Pages.Add(fullPage);

            fullPage.SaveAsSVG("Figure_1.svg");
            fullPage.SaveAsSVG("Figure_1.notext.svg", SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            doc.SaveAsPDF("Figure_1.pdf");
            fullPage.SaveAsPNG("Figure_1.png", 600 / 72.0);
        }
    }
}
