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

using System.Diagnostics;
using System.Runtime.InteropServices;
using VectSharp;
using VectSharp.Plots;

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

        /// <summary>
        /// Draw the ROC curve.
        /// </summary>
        /// <param name="scores">Column scores.</param>
        /// <param name="trueMask">"True" (i.e., manual) alignment mask.</param>
        /// <param name="modelThreshold">Threshold value used by the model.</param>
        /// <param name="modelValues">When this method returns, this variable will hold the threshold value and the accuracy and MCC scores corresponding to the default model threshold.</param>
        /// <param name="optimalAValues">When this method returns, this variable will hold the threshold value and the accuracy and MCC scores corresponding to the threshold that optimises the accuracy.</param>
        /// <param name="optimalMCCValues">When this method returns, this variable will hold the threshold value and the accuracy and MCC scores corresponding to the threshold that optimises the MCC.</param>
        /// <returns>A <see cref="Page"/> containing the ROC plot.</returns>
        static Page DrawROCCurve(double[] scores, string trueMask, double modelThreshold, out (double threshold, double a, double mcc) modelValues, out (double threshold, double a, double mcc) optimalAValues, out (double threshold, double a, double mcc) optimalMCCValues)
        {
            // Compute the ROC curve.
            List<(double fpr, double tpr)> ROCCurve = ComputeROCCurve(scores, trueMask.Select(x => x switch { '0' => false, '1' => true, _ => throw new Exception("Invalid mask state") }).ToArray(), out List<double> thresholds, out List<double> accuracy, out List<double> mcc, out double auc);

            // Identify points corresponding to the optimal thresholds and to the current threshold.
            int currentThreshold = -1;
            int optimalAccuracy = -1;
            int optimalMCC = -1;
            double maxAccuracy = double.MinValue;
            double maxMCC = double.MinValue;

            for (int i = 0; i < ROCCurve.Count; i++)
            {
                if (accuracy[i] > maxAccuracy)
                {
                    maxAccuracy = accuracy[i];
                    optimalAccuracy = i;
                }

                if (mcc[i] > maxMCC)
                {
                    maxMCC = mcc[i];
                    optimalMCC = i;
                }

                if (i < ROCCurve.Count - 1 && thresholds[i] <= modelThreshold && thresholds[i + 1] >= modelThreshold)
                {
                    currentThreshold = i;
                }
            }

            // Create the ROC plot.
            Plot rocPlot = Plot.Create.LineChart(ROCCurve, xAxisTitle: "False positive rate", yAxisTitle: "True positive rate", width: 150, height: 100,
                axisLabelPresentationAttributes: new PlotElementPresentationAttributes()
                {
                    Stroke = null,
                    Fill = Colours.Black,
                    Font = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 8)
                },
                axisTitlePresentationAttributes: new PlotElementPresentationAttributes()
                {
                    Stroke = null,
                    Fill = Colours.Black,
                    Font = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.HelveticaBold), 10)
                },
                linePresentationAttributes: new PlotElementPresentationAttributes()
                {
                    Stroke = Colours.Black,
                    LineWidth = 1
                },
                axisPresentationAttributes: new PlotElementPresentationAttributes()
                {
                    Stroke = Colours.Black,
                    LineWidth = 0.5
                },
                gridPresentationAttributes: new PlotElementPresentationAttributes()
                {
                    Stroke = Colour.FromRgb(200, 200, 200),
                    LineWidth = 0.5
                }, axisArrowSize: 5);

            rocPlot.GetAll<ContinuousAxisTitle>().ElementAt(1).Position += 5;

            // Add the threshold points.
            rocPlot.AddPlotElement(new PlotElement<IReadOnlyList<double>>(rocPlot.GetFirst<IContinuousCoordinateSystem>(), (gpr, coord) =>
            {
                Point currentPt = coord.ToPlotCoordinates(new double[] { ROCCurve[currentThreshold].fpr, ROCCurve[currentThreshold].tpr });
                Point optimalAccuracyPt = coord.ToPlotCoordinates(new double[] { ROCCurve[optimalAccuracy].fpr, ROCCurve[optimalAccuracy].tpr });
                Point optimalMCCPt = coord.ToPlotCoordinates(new double[] { ROCCurve[optimalMCC].fpr, ROCCurve[optimalMCC].tpr });

                gpr.StrokePath(new GraphicsPath().MoveTo(currentPt.X - 3, currentPt.Y - 3).LineTo(currentPt.X + 3, currentPt.Y - 3).LineTo(currentPt.X, currentPt.Y + 3).Close(), Colours.White, 2, lineJoin: LineJoins.Round); 
                gpr.FillPath(new GraphicsPath().MoveTo(currentPt.X - 3, currentPt.Y - 3).LineTo(currentPt.X + 3, currentPt.Y - 3).LineTo(currentPt.X, currentPt.Y + 3).Close(), Gradients.ViridisColouring(modelThreshold));

                gpr.StrokePath(new GraphicsPath().MoveTo(optimalAccuracyPt.X - 3, optimalAccuracyPt.Y).LineTo(optimalAccuracyPt.X, optimalAccuracyPt.Y - 3).LineTo(optimalAccuracyPt.X + 3, optimalAccuracyPt.Y).LineTo(optimalAccuracyPt.X, optimalAccuracyPt.Y + 3).Close(), Colours.White, 2, lineJoin: LineJoins.Round);
                gpr.FillPath(new GraphicsPath().MoveTo(optimalAccuracyPt.X - 3, optimalAccuracyPt.Y).LineTo(optimalAccuracyPt.X, optimalAccuracyPt.Y - 3).LineTo(optimalAccuracyPt.X + 3, optimalAccuracyPt.Y).LineTo(optimalAccuracyPt.X, optimalAccuracyPt.Y + 3).Close(), Gradients.ViridisColouring(thresholds[optimalAccuracy]));

                if (optimalMCC != optimalAccuracy)
                {
                    gpr.StrokePath(new GraphicsPath().Arc(optimalMCCPt, 3, 0, 2 * Math.PI).Close(), Colours.White, 2, lineJoin: LineJoins.Round);
                    gpr.FillPath(new GraphicsPath().Arc(optimalMCCPt, 3, 0, 2 * Math.PI).Close(), Gradients.ViridisColouring(thresholds[optimalMCC]));
                }

                Font labelFont = new Font(FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica), 8);

                gpr.FillText(currentPt.X + 5, currentPt.Y, FormattedText.Format("Model threshold", FontFamily.StandardFontFamilies.Helvetica, 8), Colours.Black, TextBaselines.Middle);

                if (optimalMCC != optimalAccuracy)
                {
                    gpr.FillText(optimalAccuracyPt.X - 5, optimalAccuracyPt.Y + 9, FormattedText.Format("Optimal <i>A</i> threshold", FontFamily.StandardFontFamilies.Helvetica, 8), Colours.Black, TextBaselines.Middle);
                    gpr.FillText(optimalMCCPt.X + 5, optimalMCCPt.Y + 4, FormattedText.Format("Optimal <i>MCC</i> threshold", FontFamily.StandardFontFamilies.Helvetica, 8), Colours.Black, TextBaselines.Middle);
                }
                else
                {
                    gpr.FillText(optimalMCCPt.X + 5, optimalMCCPt.Y + 5, FormattedText.Format("Optimal threshold", FontFamily.StandardFontFamilies.Helvetica, 8), Colours.Black, TextBaselines.Middle);
                }

                FormattedText[] aucText = FormattedText.Format($"<i>AUC</i> = {auc.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}", FontFamily.StandardFontFamilies.Helvetica, 8).ToArray();

                gpr.FillText(coord.ToPlotCoordinates(new double[] { 1, 0 }) + new Point(- aucText.Measure().Width - 3, - 3), aucText, Colours.Black, TextBaselines.Bottom);

            }));

            // Return the accuracy and MCC values corresponding to the various thresholds.
            modelValues = (thresholds[currentThreshold], accuracy[currentThreshold], mcc[currentThreshold]);
            optimalAValues = (thresholds[optimalAccuracy], accuracy[optimalAccuracy], mcc[optimalAccuracy]);
            optimalMCCValues = (thresholds[optimalMCC], accuracy[optimalMCC], mcc[optimalMCC]);

            // Render the plot.
            Page renderedPlot = rocPlot.Render();
            renderedPlot.Crop();

            return renderedPlot;
        }

        /// <summary>
        /// Compute the ROC curve.
        /// </summary>
        /// <param name="scores">The column scores.</param>
        /// <param name="trueLabels">The "true" (i.e., manual) label for each column.</param>
        /// <param name="thresholds">When this method returns, this variable will contain the list of thresholds corresponding to the (fpr, tpr) pairs.</param>
        /// <param name="accuracy">When this method returns, this variable will contain the list of accuracy values corresponding to the (fpr, tpr) pairs.</param>
        /// <param name="mcc">When this method returns, this variable will contain the list of MCC values corresponding to the (fpr, tpr) pairs.</param>
        /// <param name="auc">When this method returns, this variable will contain the area under the ROC curve.</param>
        /// <returns>The ROC curve, as a list of (fpr, tpr) pairs (fpr: false positive rate [x axis], tpr: true positive rate [y axis]).</returns>
        static List<(double fpr, double tpr)> ComputeROCCurve(double[] scores, bool[] trueLabels, out List<double> thresholds, out List<double> accuracy, out List<double> mcc, out double auc)
        {
            // Sort the scores (and corresponding labels) in ascending order.
            (double, bool)[] sortedPredictions = scores.Select((x, i) => (x, trueLabels[i])).OrderBy(x => x.x).ThenBy(x => x.Item2 ? 1 : 0).ToArray();

            // Starting condition, with a threshold of 0: everything is positive (either true or false).
            int tp = trueLabels.Count(x => x);
            int tn = 0;
            int fp = trueLabels.Length - tp;
            int fn = 0;
            double currentThreshold = 0;

            // This list will contain the ROC curve.
            List<(double fpr, double tpr)> roc = new List<(double fpr, double tpr)>()
            {
                (1, 1)
            };

            // Threshold values, accuracy and MCC corresponding to each point of the ROC curve.
            thresholds = new List<double>() { 1 };
            accuracy = new List<double>() { (double)(tp + tn) / (tp + tn + fp + fn) };
            mcc = new List<double>() { ((double)tp * tn - (double)fp * fn) / Math.Sqrt(((double)tp + fp) * ((double)tp + fn) * ((double)tn + fp) * ((double)tn + fn)) };

            // Go through the sorted scores in ascending order, increasing the threshold after each point.
            for (int i = 0; i < sortedPredictions.Length; i++)
            {
                if (sortedPredictions[i].Item1 > currentThreshold)
                {
                    double tpr = tp == 0 ? 0 : (double)tp / (tp + fn);
                    double fpr = fp == 0 ? 0 : (double)fp / (fp + tn);

                    // TPR and FPR should be weakly monotonic.
                    Debug.Assert(tpr <= roc[^1].tpr);
                    Debug.Assert(fpr <= roc[^1].fpr);

                    roc.Add((fpr, tpr));

                    currentThreshold = sortedPredictions[i].Item1;
                    thresholds.Add(currentThreshold);
                }

                // By increasing the threshold, we have removed a positive point.
                // It was either a true positive (in which case we have added a false
                // negative), or a false positive (in which case we have added a true
                // negative).

                if (sortedPredictions[i].Item2) // It was a TP
                {
                    fn++;
                    tp--;
                }
                else // It was a FP
                {
                    tn++;
                    fp--;
                }

                // Compute the accuracy and MCC scores for the current threshold.
                accuracy.Add((double)(tp + tn) / (tp + tn + fp + fn));
                mcc.Add(((double)tp * tn - (double)fp * fn) / Math.Sqrt(((double)tp + fp) * ((double)tp + fn) * ((double)tn + fp) * ((double)tn + fn)));
            }

            // Final state: the threshold is 1, so everything is a negative (either true or false).
            roc.Add((0, 0));
            thresholds.Add(0);
            accuracy.Add((double)trueLabels.Count(x => !x) / trueLabels.Length);
            mcc.Add(0);

            // Compute the AUC using the trapezoidal rule.
            auc = 0;
            for (int i = roc.Count - 2; i >= 0; i--)
            {
                auc += (roc[i].fpr - roc[i + 1].fpr) * (roc[i].tpr + roc[i + 1].tpr) * 0.5;
            }

            return roc;
        }
    }
}
