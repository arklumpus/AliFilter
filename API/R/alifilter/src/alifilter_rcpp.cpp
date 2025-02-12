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

#include <Rcpp.h>
#include <ctype.h>
#include <math.h>
#include <string.h>

#define MAX(x, y) (((x) > (y)) ? (x) : (y))
#define MIN(x, y) (((x) < (y)) ? (x) : (y))

#define ALIFILTER_FEATURE_COUNT 6

using namespace Rcpp;

void alifilter_getAlignmentFeatures(List sequenceData, double * out_features);

// [[Rcpp::export]]
NumericVector alifilter_rcpp_features(List seqs) {
  NumericVector features(Dimension(ALIFILTER_FEATURE_COUNT, ((RawVector)seqs[0]).size()));
  alifilter_getAlignmentFeatures(seqs, &features[0]);
  return features;
}

// Computes features for a single column.
void alifilter_computeColumnFeatures(List sequenceData, int sequenceCount, int alignmentLength, int column, double * out_features) {
  int gapCount = 0;
  int counts['Z' - 'A' + 1] = { 0 };
  int validChars = 0;

  for (int i = 0; i < sequenceCount; i++) {
    unsigned char c = ((RawVector)sequenceData[i])[column];

    if (c == '-') {
      gapCount++;
    }
    else {
      unsigned char C = toupper(c);
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

// Computes the alignment features for an alignment.
//   Parameters:
//     • List sequenceData: the alignment sequence data.
//     • double * out_features: the computed features will be stored at this pointer
//                              The pointer should be able to address at least ALIFILTER_FEATURE_COUNT * <alignment length> values.
void alifilter_getAlignmentFeatures(List sequenceData, double * out_features) {
  int sequenceCount = sequenceData.size();
  int alignmentLength = ((RawVector)sequenceData[0]).size();

  // Compute % Gaps, % Identity, Distance from extremity and Entropy.
  for (int i = 0; i < alignmentLength; i++) {
    alifilter_computeColumnFeatures(sequenceData, sequenceCount, alignmentLength, i, &out_features[i * ALIFILTER_FEATURE_COUNT]);
  }

  // Compute % Gaps +- 1 and +- 2
  for (int i = 0; i < alignmentLength; i++) {
    // % Gaps +- 1
    out_features[ALIFILTER_FEATURE_COUNT * i + 4] = ((i > 0 ? out_features[ALIFILTER_FEATURE_COUNT * (i - 1) + 0] : 0) +
                                                    out_features[ALIFILTER_FEATURE_COUNT * i + 0] +
                                                    (i < alignmentLength - 1 ? out_features[ALIFILTER_FEATURE_COUNT * (i + 1) + 0] : 0)) /
                                                    (1 + (i > 0 ? 1 : 0) + (i < alignmentLength - 1 ? 1 : 0));

    // % Gaps +- 2
    out_features[ALIFILTER_FEATURE_COUNT * i + 5] = ((i > 1 ? out_features[ALIFILTER_FEATURE_COUNT * (i - 2) + 0] : 0) +
                                                    (i > 0 ? out_features[ALIFILTER_FEATURE_COUNT * (i - 1) + 0] : 0) +
                                                    out_features[ALIFILTER_FEATURE_COUNT * i + 0] +
                                                    (i < alignmentLength - 1 ? out_features[ALIFILTER_FEATURE_COUNT * (i + 1) + 0] : 0) +
                                                    (i < alignmentLength - 2 ? out_features[ALIFILTER_FEATURE_COUNT * (i + 2) + 0] : 0)) /
                                                    (1 + (i > 1 ? 2 : i > 0 ? 1 : 0) + (i < alignmentLength - 2 ? 2 : i < alignmentLength - 1 ? 1 : 0));
  }
}
