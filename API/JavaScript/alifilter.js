/*
    AliFilter: A Machine Learning Approach to Alignment Filtering

    by Giorgio Bianchini, Rui Zhu, Francesco Cicconardi, Edmund RR Moody

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

"use strict";

/**
 * Wrapper for AliFilter functions.
 */
var aliFilter = {

    /**
     * This signature is compared with the feature signature contained in the model file, to
     * ensure that the model is applied to the same set of features that were used to train it.
     */
    FEATURE_SIGNATURE: "8D2F81A3625201DA548E08978D516E0C",
    
    /**
     * Number of features.
     */
    FEATURE_COUNT: 6,
    
    /**
     * Compute alignment features from a set of aligned sequences.
     * @param {Array<string>} sequences The aligned sequences, provided as an array of strings that have the same length.
     * @returns {Array<Array<number>>} An array with the same length as each sequence, where each element is an array containing {@link aliFilter.FEATURE_COUNT} floating point numbers, corresponding to the computed features.
     */
    getAlignmentFeatures: function(sequences)
    {
        // Function to compute features for a single column.
        let _computeColumnFeatures = function(sequences, columnIndex)
        {
            let gapCount = 0;
            
            // Counts for each letter in the alphabet.
            let counts = [];
            for (let i = 65; i <= 90; i++)
            {
                counts[String.fromCharCode(i)] = 0;
            }

            let validChars = 0;
            let out_features = new Array(aliFilter.FEATURE_COUNT);

            for (let i = 0; i < sequences.length; i++)
            {
                let c = sequences[i][columnIndex];

                if (c == '-')
                {
                    gapCount++;
                }
                else
                {
                    let C = c.toUpperCase();
                    if (C >= 'A' && C <= 'Z') {
                        validChars++;
                        counts[C]++;
                    }
                }
            }

            // % Gaps
            out_features[0] = gapCount / sequences.length;

            // % Identity and entropy
            if (validChars > 0)
            {
                let maxId = 0;
                let entropy = 0;

                for (let i in counts)
                {
                    maxId = Math.max(maxId, counts[i]);

                    if (counts[i] > 0)
                    {
                        entropy += - counts[i] / validChars * Math.log(counts[i] / validChars);
                    }
                }

                out_features[1] = maxId / sequences.length;
                out_features[3] = entropy;
            }
            else
            {
                out_features[1] = 0;
                out_features[3] = 0;
            }

            // Distance from extremity
            out_features[2] = Math.min(columnIndex, alignmentLength - 1 - columnIndex);

            // % Gaps +- 1 and % Gaps +- 2 are not computed here.

            return out_features;
        };

        // Assume all sequences have the same length.
        let alignmentLength = sequences[0].length;

        // Compute features for each column.
        let alignmentFeatures = new Array(alignmentLength);
        for (let i = 0; i < alignmentLength; i++)
        {
            alignmentFeatures[i] = _computeColumnFeatures(sequences, i);
        }

        // Compute % Gaps +- 1 and +- 2.
        for (let i = 0; i < alignmentLength; i++)
        {
            // % Gaps +- 1
            alignmentFeatures[i][4] = (
                                        (i > 0 ? alignmentFeatures[i - 1][0] : 0) +
                                        alignmentFeatures[i][0] +
                                        (i < alignmentLength - 1 ? alignmentFeatures[i + 1][0] : 0)
                                      ) /
                                      (1 + (i > 0 ? 1 : 0) + (i < alignmentLength - 1 ? 1 : 0));
            
            // % Gaps +- 2
            alignmentFeatures[i][5] = (
                                        (i > 1 ? alignmentFeatures[i - 2][0] : 0) +
                                        (i > 0 ? alignmentFeatures[i - 1][0] : 0) +
                                        alignmentFeatures[i][0] +
                                        (i < alignmentLength - 1 ? alignmentFeatures[i + 1][0] : 0) +
                                        (i < alignmentLength - 2 ? alignmentFeatures[i + 2][0] : 0)
                                      ) /
                                      (1 + (i > 1 ? 2 : i > 0 ? 1 : 0) + (i < alignmentLength - 2 ? 2 : i < alignmentLength - 1 ? 1 : 0));
        }

        return alignmentFeatures;
    },

    /**
     * Load an AliFilter JSON model file from the specified URL.
     * @param {string} modelFileURL The URL to the model file.
     * @returns {Object} An AliFilter model object, containing the model parameters and functions to apply the model.
     */
    loadModelFromURL: async function(modelFileURL)
    {
        return await fetch(modelFileURL).then(response => response.text()).then(text => aliFilter.loadModelFromJSON(text));
    },

    /**
     * Load an AliFilter JSON model from the specified JSON string.
     * @param {string} modelJson The serialised model in JSON format.
     * @returns {Object} An AliFilter model object, containing the model parameters and functions to apply the model.
     */
    loadModelFromJSON: function(modelJson)
    {
        // Parse the JSON model.
        let model = JSON.parse(modelJson);
        
        // Reject the model if the feature signature does not match.
        if (model.FeatureSignature !== aliFilter.FEATURE_SIGNATURE)
        {
            throw new Error("The AliFilter model uses a different set of features than those defined in this implementation!");
        }

        // Create a random GUID for the model.
        let objUrl = URL.createObjectURL(new Blob([]));
        model.Id = objUrl.slice(-36);
        URL.revokeObjectURL(objUrl);

        // Default threshold for unvalidated models.
        if (!model.FastThreshold)
        {
            model.FastThreshold = 0.5;
        }

        /**
         * Compute column preservation scores from a set of alignment features. The alignment features can be computed by using {@link aliFilter.getAlignmentFeatures}.
         * @param {Array<Array<number>>} alignmentFeatures The pre-computed alignment features.
         * @returns {Array<number>} An array where each element (ranging between 0 and 1) is the preservation score for the corresponding column.
         */
        model.getScores = function(alignmentFeatures)
        {
            let scores = new Float64Array(alignmentFeatures.length);

            for (let i = 0; i < alignmentFeatures.length; i++)
            {
                scores[i] = this.LogisticModel.Intercept;

                for (let j = 0; j < aliFilter.FEATURE_COUNT; j++)
                {
                    scores[i] += alignmentFeatures[i][j] * this.LogisticModel.Coefficients[j];
                }

                scores[i] = 1.0 / (1.0 + Math.exp(-scores[i]));
            }
            
            return scores;
        };

        /**
         * Get an alignment mask from a set of preservation scores.
         * @param {Array<number>} scores The column preservation scores.
         * @returns {string} A string where each character is either "0" (if the column score is smaller than the model's FastThreshold property) or "1" (if the column score is greater than or equal to the FastThreshold).
         */
        model.getMaskFromScores = function(scores)
        {
            return scores.map(x => x >= this.FastThreshold ? "1" : "0").join("");
        };

        /**
         * Compute column preservation scores from a set of alignment features, then use those preservation scores to create an alignment mask.
         * @param {Array<Array<number>>} alignmentFeatures The pre-computed alignment features.
         * @returns {string} A string where each character is either "0" (if the column score is smaller than the model's FastThreshold property) or "1" (if the column score is greater than or equal to the FastThreshold).
         */
        model.getMaskFromFeatures = function(alignmentFeatures)
        {
            return this.getMaskFromScores(this.getScores(alignmentFeatures));
        };

        /**
         * Compute alignment features and preservation scores from a set of aligned sequences, then use them to create an alignment mask.
         * @param {Array<string>} sequences The aligned sequences, provided as an array of strings that have the same length.
         * @returns {string} A string where each character is either "0" (if the column score is smaller than the model's FastThreshold property) or "1" (if the column score is greater than or equal to the FastThreshold).
         */
        model.getMask = function(sequences)
        {
            return this.getMaskFromScores(this.getScores(aliFilter.getAlignmentFeatures(sequences)));
        };

        return model;
    },
};
