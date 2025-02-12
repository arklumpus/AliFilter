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

using System.Runtime.InteropServices;
using VectSharp;

namespace Figures_1_S1_S2_S3
{
    internal partial class Program
    {
        /// <summary>
        /// Amino acid colours for drawing sequence alignments.
        /// </summary>
        static readonly Dictionary<char, Colour> AAColours = new Dictionary<char, Colour>(new Dictionary<char, string>() { { 'A', "rgb(25, 128, 230)" }, { 'C', "rgb(230, 128, 128)" }, { 'D', "rgb(204, 77, 204)" }, { 'E', "rgb(204, 77, 204)" }, { 'F', "rgb(25, 128, 230)" }, { 'G', "rgb(230, 153, 77)" }, { 'H', "rgb(25, 179, 179)" }, { 'I', "rgb(25, 128, 230)" }, { 'K', "rgb(230, 51, 25)" }, { 'L', "rgb(25, 128, 230)" }, { 'M', "rgb(25, 128, 230)" }, { 'N', "rgb(25, 204, 25)" }, { 'P', "rgb(204, 204, 0)" }, { 'Q', "rgb(25, 204, 25)" }, { 'R', "rgb(230, 51, 25)" }, { 'S', "rgb(25, 204, 25)" }, { 'T', "rgb(25, 204, 25)" }, { 'V', "rgb(25, 128, 230)" }, { 'W', "rgb(25, 128, 230)" }, { 'Y', "rgb(25, 179, 179)" }, { '-', "rgb(255, 255, 255)" }, { 'X', "rgb(255, 255, 255)" } }.Select(x => new KeyValuePair<char, Colour>(x.Key, Colour.FromCSSString(x.Value).Value)));

        /// <summary>
        /// Draw a protein sequence alignment.
        /// </summary>
        /// <param name="sortedSequences">The sequence names, sorted in a way to produce a pleasant result.</param>
        /// <param name="alignment">The sequence alignment.</param>
        /// <param name="residueWidth">The thickness (in graphics units) of each residue.</param>
        /// <param name="sequenceHeight">The height (in graphics units) of each sequence.</param>
        /// <param name="alignmentColour"></param>
        /// <returns>A <see cref="Page"/> on which the alignment has been drawn.</returns>
        static unsafe Page DrawAlignment(List<string> sortedSequences, Dictionary<string, string> alignment, double residueWidth, double sequenceHeight)
        {
            // To prevent the "bleeding" effect (and to reduce file size), draw the alignment as a raster image.

            // Allocate enough memory.
            DisposableIntPtr imageData = new DisposableIntPtr(Marshal.AllocHGlobal(alignment.Count * alignment.ElementAt(0).Value.Length * 4));

            // Create the raster image object.
            RasterImage alignmentImage = new RasterImage(ref imageData, alignment.ElementAt(0).Value.Length, alignment.Count, true, false);

            // Access the raw data.
            byte* dataPointer = (byte*)alignmentImage.ImageDataAddress;

            for (int i = 0; i < sortedSequences.Count; i++)
            {
                string sequence = alignment[sortedSequences[i]];

                for (int j = 0; j < sequence.Length; j++)
                {
                    // Set each pixel colour to the colour corresponding to the residue.
                    if (sequence[j] != '-')
                    {
                        Colour col = AAColours[sequence[j]];

                        dataPointer[(i * sequence.Length + j) * 4] = (byte)(col.R * 255);
                        dataPointer[(i * sequence.Length + j) * 4 + 1] = (byte)(col.G * 255);
                        dataPointer[(i * sequence.Length + j) * 4 + 2] = (byte)(col.B * 255);
                        dataPointer[(i * sequence.Length + j) * 4 + 3] = 255;
                    }
                    else
                    {
                        dataPointer[(i * sequence.Length + j) * 4] = 0;
                        dataPointer[(i * sequence.Length + j) * 4 + 1] = 0;
                        dataPointer[(i * sequence.Length + j) * 4 + 2] = 0;
                        dataPointer[(i * sequence.Length + j) * 4 + 3] = 0;
                    }
                }
            }

            // Create a page to contain the alignment and access its graphics surface.
            Page alignmentPage = new Page(1, 1);
            Graphics alignmentGpr = alignmentPage.Graphics;

            // Draw the alignment.
            alignmentGpr.DrawRasterImage(0, 0, residueWidth * alignment.ElementAt(0).Value.Length, sequenceHeight * alignment.Count, alignmentImage);

            // Crop the page to the alignment size.
            alignmentPage.Crop();

            return alignmentPage;
        }

        /// <summary>
        /// Draw a single alignment mask.
        /// </summary>
        /// <param name="mask">The mask string.</param>
        /// <param name="residueWidth">The thickness (in graphics units) of each residue.</param>
        /// <param name="maskHeight">The height (in graphics units) of the mask.</param>
        /// <param name="maskColour">The colour used to draw the mask.</param>
        /// <returns>A <see cref="Page"/> on which the mask has been drawn.</returns>
        static Page DrawMask(string mask, double residueWidth, double maskHeight, Colour maskColour)
        {
            // Create a page to contain the masks and access its graphics surface.
            Page maskPage = new Page(residueWidth * mask.Length, maskHeight);
            Graphics maskGpr = maskPage.Graphics;

            GraphicsPath maskPth = new GraphicsPath();

            // Identify blocks of preserved columns and draw them as rectangles.
            int currBlockStart = -1;
            for (int j = 0; j < mask.Length; j++)
            {
                if (mask[j] == '0')
                {
                    if (currBlockStart >= 0)
                    {
                        maskPth.MoveTo(residueWidth * currBlockStart, 0).LineTo(residueWidth * j, 0).LineTo(residueWidth * j, maskHeight).LineTo(residueWidth * currBlockStart, maskHeight).Close();
                        currBlockStart = -1;
                    }
                }
                else if (currBlockStart < 0)
                {
                    currBlockStart = j;
                }
            }

            if (currBlockStart >= 0)
            {
                maskPth.MoveTo(residueWidth * currBlockStart, 0).LineTo(residueWidth * mask.Length, 0).LineTo(residueWidth * mask.Length, maskHeight).LineTo(residueWidth * currBlockStart, maskHeight).Close();
            }

            maskGpr.FillPath(maskPth, maskColour);

            return maskPage;
        }

        /// <summary>
        /// Draw multiple masks on the same <see cref="Page"/>.
        /// </summary>
        /// <param name="masks">The masks to draw.</param>
        /// <param name="residueWidth">The thickness (in graphics units) of each residue.</param>
        /// <param name="maskHeight">The height (in graphics units) of each mask.</param>
        /// <param name="maskColours">The colours used to draw each mask.</param>
        /// <returns>A <see cref="Page"/> on which the masks have been drawn.</returns>
        static Page DrawMasks(Dictionary<string, string> masks, double residueWidth, double maskHeight, Colour[] maskColours)
        {
            // Create a page to contain the masks and access its graphics surface.
            Page maskPage = new Page(residueWidth * masks.First().Value.Length, masks.Count * maskHeight);
            Graphics maskGpr = maskPage.Graphics;

            {
                int i = 0;
                foreach (KeyValuePair<string, string> kvp in masks)
                {
                    string sequence = kvp.Value;

                    GraphicsPath maskPth = new GraphicsPath();

                    // Identify blocks of preserved columns and draw them as rectangles.
                    int currBlockStart = -1;
                    for (int j = 0; j < sequence.Length; j++)
                    {
                        if (sequence[j] == '0')
                        {
                            if (currBlockStart >= 0)
                            {
                                maskPth.MoveTo(residueWidth * currBlockStart, maskHeight * i).LineTo(residueWidth * j, maskHeight * i).LineTo(residueWidth * j, maskHeight * (i + 1)).LineTo(residueWidth * currBlockStart, maskHeight * (i + 1)).Close();
                                currBlockStart = -1;
                            }
                        }
                        else if (currBlockStart < 0)
                        {
                            currBlockStart = j;
                        }
                    }

                    if (currBlockStart >= 0)
                    {
                        maskPth.MoveTo(residueWidth * currBlockStart, maskHeight * i).LineTo(residueWidth * sequence.Length, maskHeight * i).LineTo(residueWidth * sequence.Length, maskHeight * (i + 1)).LineTo(residueWidth * currBlockStart, maskHeight * (i + 1)).Close();
                    }

                    maskGpr.FillPath(maskPth, maskColours[i]);
                    i++;
                }
            }

            return maskPage;
        }

        /// <summary>
        /// Draw "fuzzy" masks.
        /// </summary>
        /// <param name="sortedMasks">The sorted mask names.</param>
        /// <param name="masks">The masks to draw.</param>
        /// <param name="residueWidth">The thickness (in graphics units) of each residue.</param>
        /// <param name="maskHeight">The height (in graphics units) of each mask.</param>
        /// <param name="colouring">A function returning the colour to use for each mask value.</param>
        /// <returns>The <see cref="Page"/> on which the masks have been drawn.</returns>
        static unsafe Page DrawFuzzyMasks(List<string> sortedMasks, Dictionary<string, double[]> masks, double residueWidth, double maskHeight, Func<double, Colour> colouring)
        {
            // To prevent the "bleeding" effect (and to reduce file size), draw the masks as a raster image.

            // Allocate enough memory.
            DisposableIntPtr imageData = new DisposableIntPtr(Marshal.AllocHGlobal(masks.Count * masks.ElementAt(0).Value.Length * 4));

            // Create the raster image object.
            RasterImage maskImage = new RasterImage(ref imageData, masks.ElementAt(0).Value.Length, masks.Count, true, false);

            // Access the raw data.
            byte* dataPointer = (byte*)maskImage.ImageDataAddress;

            for (int i = 0; i < sortedMasks.Count; i++)
            {
                double[] sequence = masks[sortedMasks[i]];

                for (int j = 0; j < sequence.Length; j++)
                {
                    Colour col = colouring(sequence[j]);

                    dataPointer[(i * sequence.Length + j) * 4] = (byte)(col.R * 255);
                    dataPointer[(i * sequence.Length + j) * 4 + 1] = (byte)(col.G * 255);
                    dataPointer[(i * sequence.Length + j) * 4 + 2] = (byte)(col.B * 255);
                    dataPointer[(i * sequence.Length + j) * 4 + 3] = 255;
                }
            }

            // Create a page to contain the alignment and access its graphics surface.
            Page maskPage = new Page(1, 1);
            Graphics alignmentGpr = maskPage.Graphics;

            // Draw the masks.
            alignmentGpr.DrawRasterImage(0, 0, residueWidth * masks.ElementAt(0).Value.Length, maskHeight * masks.Count, maskImage);

            // Crop the page to the alignment size.
            maskPage.Crop();

            return maskPage;
        }
    }
}
