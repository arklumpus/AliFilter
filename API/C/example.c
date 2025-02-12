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

#include <stdio.h>

// A simple parser for relaxed PHYLIP alignments.
// You do not need this if you have another way of reading sequence alignments.
#define ALIFILTER_PHYLIP_IMPLEMENTATION
#include "phylip.h"

// AliFilter methods.
#define ALIFILTER_IMPLEMENTATION
#include "alifilter.h"

// Example 1: directly compute the mask from the alignment.
int example1(char* argv[]);

// Example 2: first compute alignment features, then compute the mask.
int example2(char* argv[]);

// Example 3: first compute alignment features, then compute column scores, then compute the mask.
int example3(char* argv[]);

int main(int argc, char* argv[]) {
    if (argc != 3)
    {
        fprintf(stderr, "\nWrong number of arguments!\n    Usage:\n        example <path to alignment file> <path to model file>\n\n");
        return 64;
    }

    // Example 1: directly compute the mask from the alignment.
    return example1(argv);
    
    // Example 2: first compute alignment features, then compute the mask.
    // return example2(argv);
    
    // Example 3: first compute alignment features, then compute column scores, then compute the mask.
    // return example3(argv);
}

int example1(char* argv[]) {
    // Example 1: directly compute the mask from the alignment.

    // Declare variables.
    alignment sequenceAlignment;
    alifilter_model model;
    char* mask;
    int error_code;

    // Read the alignment file.
    error_code = phylip_parsePHYLIP(argv[1], &sequenceAlignment);
    if (error_code != 0) {
        fprintf(stderr, "Error %d while reading the alignment file!\n", error_code);
        return 1;
    }

    // Read the model file.
    error_code = alifilter_parseModel(argv[2], &model);
    if (error_code != 0) {
        fprintf(stderr, "Error %d while reading the model file!\n", error_code);
        return 1;
    }

    // Create the mask.
    mask = alifilter_getMask(model, sequenceAlignment.sequenceData, sequenceAlignment.sequenceCount, sequenceAlignment.alignmentLength);
    if (mask == NULL) {
        fprintf(stderr, "Error while creating the alignment mask!\n");
        return 1;
    }
    
    // Print the mask.
    fprintf(stdout, "%s\n", mask);

    // Free memory
    free(mask);
    phylip_freeAlignment(&sequenceAlignment);

    return 0;
}

int example2(char* argv[]) {
    // Example 2: first compute alignment features, then compute the mask.

    // Declare variables.
    alignment sequenceAlignment;
    alifilter_model model;
    double* alignmentFeatures;
    char* mask;
    int error_code;

    // Read the alignment file.
    error_code = phylip_parsePHYLIP(argv[1], &sequenceAlignment);
    if (error_code != 0) {
        fprintf(stderr, "Error %d while reading the alignment file!\n", error_code);
        return 1;
    }

    // Read the model file.
    error_code = alifilter_parseModel(argv[2], &model);
    if (error_code != 0) {
        fprintf(stderr, "Error %d while reading the model file!\n", error_code);
        return 1;
    }

    // Compute the alignment features.
    alignmentFeatures = alifilter_getAlignmentFeatures(sequenceAlignment.sequenceData, sequenceAlignment.sequenceCount, sequenceAlignment.alignmentLength);
    if (alignmentFeatures == NULL) {
        fprintf(stderr, "Error while computing alignment features!\n");
        return 1;
    }

    // Create the mask from the features.
    mask = alifilter_getMaskFromFeatures(model, alignmentFeatures, sequenceAlignment.alignmentLength);
    if (mask == NULL) {
        fprintf(stderr, "Error while creating the alignment mask!\n");
        return 1;
    }
    
    // Print the mask.
    fprintf(stdout, "%s\n", mask);

    // Free memory
    free(mask);
    free(alignmentFeatures);
    phylip_freeAlignment(&sequenceAlignment);

    return 0;
}

int example3(char* argv[]) {
    // Example 3: first compute alignment features, then compute column scores, then compute the mask.

    // Declare variables.
    alignment sequenceAlignment;
    alifilter_model model;
    double* alignmentFeatures;
    double* columnScores;
    char* mask;
    int error_code;

    // Read the alignment file.
    error_code = phylip_parsePHYLIP(argv[1], &sequenceAlignment);
    if (error_code != 0) {
        fprintf(stderr, "Error %d while reading the alignment file!\n", error_code);
        return 1;
    }

    // Read the model file.
    error_code = alifilter_parseModel(argv[2], &model);
    if (error_code != 0) {
        fprintf(stderr, "Error %d while reading the model file!\n", error_code);
        return 1;
    }

    // Compute the alignment features.
    alignmentFeatures = alifilter_getAlignmentFeatures(sequenceAlignment.sequenceData, sequenceAlignment.sequenceCount, sequenceAlignment.alignmentLength);
    if (alignmentFeatures == NULL) {
        fprintf(stderr, "Error while computing alignment features!\n");
        return 1;
    }

    // Compute the column scores.
    columnScores = alifilter_getScores(model, alignmentFeatures, sequenceAlignment.alignmentLength);
    if (columnScores == NULL) {
        fprintf(stderr, "Error while computing column scores!\n");
        return 1;
    }

    // Create the mask from the features.
    mask = alifilter_getMaskFromScores(model, columnScores, sequenceAlignment.alignmentLength);
    if (mask == NULL) {
        fprintf(stderr, "Error while creating the alignment mask!\n");
        return 1;
    }
    
    // Print the mask.
    fprintf(stdout, "%s\n", mask);

    // Free memory
    free(mask);
    free(columnScores);
    free(alignmentFeatures);
    phylip_freeAlignment(&sequenceAlignment);

    return 0;
}
