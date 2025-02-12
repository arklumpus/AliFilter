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

#ifndef ALIFILTER_H
#define ALIFILTER_H

#define ALIFILTER_FEATURE_COUNT 6

// Represents an AliFilter model.
typedef struct {
    // Threshold for the logistic model.
    double threshold;

    // Coefficients of the logistic model.
    double coefficients[6];

    // Intercept of the logistic model.
    double intercept;
} alifilter_model;

// Computes the alignment features for an alignment represented by an char array.
//   Parameters:
//     • const char* sequenceData: the alignment sequence data (it should contain sequenceCount * alignmentLength elements).
//     • int sequenceCount: the number of sequences in the alignment.
//     • int alignmentLength: the length of each sequence in the alignment.
//
//   Return value: a pointer to a double array containing alignmentLength * ALIFILTER_FEATURE_COUNT elements, representing
//                 the features for each column in the alignment. You should free() this pointer eventually.
double* alifilter_getAlignmentFeatures(const char* sequenceData, int sequenceCount, int alignmentLength);

// Parses an AliFilter JSON model file.
//   Parameters:
//     • const char* modelFile: the path to the JSON model file.
//     • alifilter_model* out_model: a pointer to an empty alifilter_model struct; if this method returns 0 or 5, the struct
//                                   will have been populated with the model data.
//
//   Return value:
//     • 0: success
//     • 1: error opening the file
//     • 2: the file does not start with a {
//     • 3: the file does not contain a "LogisticModel" element
//     • 4: error while reading the model coefficients
//     • 5: error while closing the file (but the model has been read successfully)
int alifilter_parseModel(const char* modelFile, alifilter_model* out_model);

// Computes scores for each alignment column, given pre-computed alignment features for each column.
//   Parameters:
//     • alifilter_model model: the model to use.
//     • double* alignmentFeatures: the pre-computed alignment features. This should point to an array containing alignmentLength * ALIFILTER_FEATURE_COUNT elements.
//     • int alignmentLength: number of columns for which alignment features are provided.
//
//   Return value: a pointer to a double array containing alignmentLength elements, representing the score
//                 for each column in the alignment. You should free() this pointer eventually.
double* alifilter_getScores(alifilter_model model, double* alignmentFeatures, int alignmentLength);

// Computes an alignment mask, given pre-computed scores for each column.
//   Parameters:
//     • alifilter_model model: the model to use.
//     • double* alignmentScores: the pre-computed alignment scores.This should point to an array containing alignmentLength elements.
//     • int alignmentLength: number of columns for which scores are provided.
//
//   Return value: a C string containing a sequence of 0s and 1s for columns that should deleted or preserved, respectively.
char* alifilter_getMaskFromScores(alifilter_model model, double* alignmentScores, int alignmentLength);

// Computes an alignment mask, given pre-computed alignment features for each column.
//   Parameters:
//     • alifilter_model model: the model to use.
//     • double* alignmentFeatures: the pre-computed alignment features. This should point to an array containing alignmentLength * ALIFILTER_FEATURE_COUNT elements.
//     • int alignmentLength: number of columns for which alignment features are provided.
//
//   Return value: a C string containing a sequence of 0s and 1s for columns that should deleted or preserved, respectively.
char* alifilter_getMaskFromFeatures(alifilter_model model, double* alignmentFeatures, int alignmentLength);

// Computes an alignment mask for an alignment represented by an char array.
//   Parameters:
//     • alifilter_model model: the model to use.
//     • const char* sequenceData: the alignment sequence data (it should contain sequenceCount * alignmentLength elements).
//     • int sequenceCount: the number of sequences in the alignment.
//     • int alignmentLength: the length of each sequence in the alignment.
//
//   Return value: a C string containing a sequence of 0s and 1s for columns that should deleted or preserved, respectively.
char* alifilter_getMask(alifilter_model model, const char* sequenceData, int sequenceCount, int alignmentLength);

#ifdef ALIFILTER_IMPLEMENTATION

#include <ctype.h>
#include <math.h>
#include <string.h>

#define MAX(x, y) (((x) > (y)) ? (x) : (y))
#define MIN(x, y) (((x) < (y)) ? (x) : (y))

// Computes features for a single column.
void alifilter_computeColumnFeatures(const char* sequenceData, int sequenceCount, int alignmentLength, int column, double* out_features) {
    int gapCount = 0;
    int counts['Z' - 'A' + 1] = { 0 };
    int validChars = 0;

    for (int i = 0; i < sequenceCount; i++) {
        char c = sequenceData[i * alignmentLength + column];

        if (c == '-') {
            gapCount++;
        }
        else {
            char C = toupper(c);
            if (C >= 'A' && C <= 'Z') {
                validChars++;
                counts[C - 'A']++;
            }
        }
    }

    // % Gaps
    out_features[0] = (double)gapCount / sequenceCount;

    // % Identity and entropy
    if (validChars > 0) {
        int maxId = 0;
        double entropy = 0;

        for (int i = 0; i < 'Z' - 'A' + 1; i++) {
            maxId = MAX(maxId, counts[i]);

            if (counts[i] > 0)
            {
                entropy += - (double)counts[i] / validChars * log((double)counts[i] / validChars);
            }
        }

        out_features[1] = (double)maxId / sequenceCount;
        out_features[3] = entropy;
    }
    else {
        out_features[1] = 0;
        out_features[3] = 0;
    }

    // Distance from extremity
    out_features[2] = MIN(column, alignmentLength - 1 - column);

    // % Gaps +- 1 and % Gaps +- 2 are not computed here.
}

double* alifilter_getAlignmentFeatures(const char* sequenceData, int sequenceCount, int alignmentLength) {
    double* features = (double*)malloc(ALIFILTER_FEATURE_COUNT * alignmentLength * sizeof(*features));

    if (features == NULL)
    {
        return NULL;
    }

    // Compute % Gaps, % Identity, Distance from extremity and Entropy.
    for (int i = 0; i < alignmentLength; i++) {
        alifilter_computeColumnFeatures(sequenceData, sequenceCount, alignmentLength, i, &features[i * ALIFILTER_FEATURE_COUNT]);
    }

    // Compute % Gaps +- 1 and +- 2
    for (int i = 0; i < alignmentLength; i++) {
        // % Gaps +- 1
        features[ALIFILTER_FEATURE_COUNT * i + 4] = ((i > 0 ? features[ALIFILTER_FEATURE_COUNT * (i - 1) + 0] : 0) +
                                                     features[ALIFILTER_FEATURE_COUNT * i + 0] +
                                                     (i < alignmentLength - 1 ? features[ALIFILTER_FEATURE_COUNT * (i + 1) + 0] : 0)) /
                                                     (1 + (i > 0 ? 1 : 0) + (i < alignmentLength - 1 ? 1 : 0));
        
        // % Gaps +- 2
        features[ALIFILTER_FEATURE_COUNT * i + 5] = ((i > 1 ? features[ALIFILTER_FEATURE_COUNT * (i - 2) + 0] : 0) +
                                                     (i > 0 ? features[ALIFILTER_FEATURE_COUNT * (i - 1) + 0] : 0) +
                                                     features[ALIFILTER_FEATURE_COUNT * i + 0] +
                                                     (i < alignmentLength - 1 ? features[ALIFILTER_FEATURE_COUNT * (i + 1) + 0] : 0) +
                                                     (i < alignmentLength - 2 ? features[ALIFILTER_FEATURE_COUNT * (i + 2) + 0] : 0)) /
                                                     (1 + (i > 1 ? 2 : i > 0 ? 1 : 0) + (i < alignmentLength - 2 ? 2 : i < alignmentLength - 1 ? 1 : 0));
    }

    return features;
}

int alifilter_parseModel(const char* modelFile, alifilter_model* out_model) {
    // Access the model file.
    FILE* fileH = fopen(modelFile, "r");
    if (fileH == NULL) {
        return 1;
    }

    char buf[255];

    int result = fscanf(fileH, "%255s", buf);

    // The model file is a JSON file, so it should start with a {
    if (result != 1 || strcmp(buf, "{") != 0) {
        fclose(fileH);
        return 2;
    }

    out_model->threshold = 0.5;

    // Move through the file until we get to the section with the logistic model.
    while (result == 1 && strcmp(buf, "\"LogisticModel\":") != 0) {
        result = fscanf(fileH, "%255s", buf);
        
        // If the model has been validated, there will be a field specifying the threshold to use.
        // In this implementation, bootstrap replicates are not supported, so we only care about the "fast" threshold.
        if (result == 1 && strcmp(buf, "\"FastThreshold\":") == 0) {
            result = fscanf(fileH, "%255s", buf);

            if (result == 1 && buf[strlen(buf) - 1] == ',') {
                buf[strlen(buf) - 1] = '\0';
                out_model->threshold = strtod(buf, NULL);
            }
        }
    }

    if (result != 1 || strcmp(buf, "\"LogisticModel\":") != 0) {
        fclose(fileH);
        return 3;
    }

    result = fscanf(fileH, "%255s", buf);
    if (result != 1 || strcmp(buf, "{") != 0) {
        fclose(fileH);
        return 4;
    }

    result = fscanf(fileH, "%255s", buf);
    if (result != 1 || strcmp(buf, "\"Coefficients\":") != 0) {
        fclose(fileH);
        return 4;
    }

    result = fscanf(fileH, "%255s", buf);
    if (result != 1 || strcmp(buf, "[") != 0) {
        fclose(fileH);
        return 4;
    }

    for (int i = 0; i < ALIFILTER_FEATURE_COUNT; i++) {
        result = fscanf(fileH, "%lf%*[, \t\r\n]", &(out_model->coefficients[i]));
        if (result != 1) {
            fclose(fileH);
            return 4;
        }
    }

    result = fscanf(fileH, "%255s", buf);
    if (result != 1 || strcmp(buf, "],") != 0) {
        fclose(fileH);
        return 4;
    }

    result = fscanf(fileH, "%255s", buf);
    if (result != 1 || strcmp(buf, "\"Intercept\":") != 0) {
        fclose(fileH);
        return 4;
    }

    result = fscanf(fileH, "%lf", &(out_model->intercept));
    if (result != 1) {
        fclose(fileH);
        return 4;
    }

    // Close the file.
    result = fclose(fileH);

    if (result != 0) {
        return 5;
    }
    else {
        return 0;
    }
}

double* alifilter_getScores(alifilter_model model, double* alignmentFeatures, int alignmentLength) {
    double* scores = (double*)malloc(alignmentLength * sizeof(*scores));

    if (scores == NULL) {
        return NULL;
    }

    for (int i = 0; i < alignmentLength; i++) {
        scores[i] = model.intercept;
        for (int j = 0; j < ALIFILTER_FEATURE_COUNT; j++) {
            scores[i] += alignmentFeatures[i * ALIFILTER_FEATURE_COUNT + j] * model.coefficients[j];
        }
        scores[i] = 1.0 / (1.0 + exp(-scores[i]));
    }

    return scores;
}

char* alifilter_getMaskFromScores(alifilter_model model, double* alignmentScores, int alignmentLength) {
    char* mask = (char*)malloc((alignmentLength + 1) * sizeof(*mask));

    if (mask == NULL) {
        return NULL;
    }

    for (int i = 0; i < alignmentLength; i++) {
        mask[i] = (alignmentScores[i] >= model.threshold ? '1' : '0');
    }

    mask[alignmentLength] = '\0';

    return mask;
}

char* alifilter_getMaskFromFeatures(alifilter_model model, double* alignmentFeatures, int alignmentLength) {
    double* scores = alifilter_getScores(model, alignmentFeatures, alignmentLength);

    if (scores == NULL) {
        return NULL;
    }

    char* mask = alifilter_getMaskFromScores(model, scores, alignmentLength);
    free(scores);

    return mask;
}

char* alifilter_getMask(alifilter_model model, const char* sequenceData, int sequenceCount, int alignmentLength) {    
    double* features = alifilter_getAlignmentFeatures(sequenceData, sequenceCount, alignmentLength);
    if (features == NULL) {
        return NULL;
    }
    
    char* mask = alifilter_getMaskFromFeatures(model, features, alignmentLength);
    free(features);

    return mask;
}

#endif
#endif