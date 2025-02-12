#    AliFilter: A Machine Learning Approach to Alignment Filtering
#
#    by Giorgio Bianchini, Rui Zhu, Francesco Cicconardi, Edmund RR Moody
#
#    Copyright (C) 2024  Giorgio Bianchini
#
#    This program is free software: you can redistribute it and/or modify
#    it under the terms of the GNU General Public License as published by
#    the Free Software Foundation, version 3.
#
#    This program is distributed in the hope that it will be useful,
#    but WITHOUT ANY WARRANTY; without even the implied warranty of
#    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#    GNU General Public License for more details.
#
#    You should have received a copy of the GNU General Public License
#    along with this program.  If not, see <https://www.gnu.org/licenses/>.
    
import math
import numpy
import json
import array
import Bio.Align

class AliFilterModel:
    def __init__(self, modelFilePath):
        fileH = open(modelFilePath)
        jsonModel = json.load(fileH)
        fileH.close()
        
        self.logisticModelCoefficients = numpy.transpose(numpy.array(jsonModel['LogisticModel']['Coefficients'], ndmin=2))
        self.logisticModelIntercept = jsonModel['LogisticModel']['Intercept']
        
        if 'FastThreshold' in jsonModel:
            self.threshold = jsonModel['FastThreshold']
        else:
            self.threshold = 0.5
    
    def getScores(self, features):
        return array.array('d', map(lambda x: 1 / (1 + math.exp(-(x + self.logisticModelIntercept))), numpy.squeeze(numpy.dot(features, self.logisticModelCoefficients))))
    
    def getMaskFromScores(self, scores):
        return ''.join(map(lambda x: '1' if x >= self.threshold else '0', scores))
    
    def getMaskFromFeatures(self, features):
        return self.getMaskFromScores(self.getScores(features))
    
    def getMask(self, alignment):
        return self.getMaskFromFeatures(alignment.getAlignmentFeatures())
    
    def filter(self, alignment):
        scores = self.getScores(alignment.getAlignmentFeatures())
        return Bio.Align.MultipleSeqAlignment(map(lambda seq: Bio.SeqRecord.SeqRecord(Bio.Seq.Seq(''.join(seq.seq[i] for i in range(len(scores)) if scores[i] >= self.threshold)), seq.id, seq.name, seq.description, seq.dbxrefs, seq.features, seq.annotations, seq.letter_annotations), alignment))

def __alifilter_computeColumnFeatures(alignment, column, feature_matrix):
    gapCount = 0
    counts = numpy.zeros(26, 'i')
    validChars = 0
    
    for i in range(len(alignment)):
        c = alignment[i][column]
        
        if (c == '-'):
            gapCount += 1
        else:
            C = ord(c.upper())
            if C >= 65 and C <= 90:
                validChars += 1
                counts[C - 65] += 1
    
    # % Gaps
    feature_matrix[column, 0] = gapCount / len(alignment)
    
    # % Identity and entropy
    if validChars > 0:
        maxId = max(counts)
        entropy = sum(map(lambda x: 0 if x <= 0 else - x / validChars * math.log(x / validChars), counts))
        
        feature_matrix[column, 1] = maxId / len(alignment)
        feature_matrix[column, 3] = entropy
    else:
        feature_matrix[column, 1] = 0
        feature_matrix[column, 3] = 0
    
    # Distance from extremity
    feature_matrix[column, 2] = min(column, alignment.get_alignment_length() - 1 - column)
    
    # % Gaps +- 1 and % Gaps +- 2 are not computed here.

def __alifilter_getAlignmentFeatures(alignment):
    alignmentLength = alignment.get_alignment_length()
    features = numpy.zeros([alignmentLength, 6])
    
    # Compute % Gaps, % Identity, Distance from extremity and Entropy.
    for i in range(alignmentLength):
        __alifilter_computeColumnFeatures(alignment, i, features)
    
    # Compute % Gaps +- 1 and +- 2
    for i in range(alignmentLength):
        # % Gaps +- 1
        features[i, 4] = (((features[i - 1, 0] if i > 0 else 0) +
                           features[i, 0] +
                           (features[i + 1, 0] if i < alignmentLength - 1 else 0)) /
                           (1 + (1 if i > 0 else 0) + (1 if i < alignmentLength - 1 else 0)))
        
        # % Gaps +- 2
        features[i, 5] = (((features[i - 2, 0] if i > 1 else 0) +
                           (features[i - 1, 0] if i > 0 else 0) +
                           features[i, 0] +
                           (features[i + 1, 0] if i < alignmentLength - 1 else 0) +
                           (features[i + 2, 0] if i < alignmentLength - 2 else 0)) /
                           (1 + (2 if i > 1 else (1 if i > 0 else 0)) + (2 if i < alignmentLength - 2 else (1 if i < alignmentLength - 1 else 0))))
    
    return features

Bio.Align.MultipleSeqAlignment.getAlignmentFeatures = __alifilter_getAlignmentFeatures
Bio.Align.MultipleSeqAlignment.filter = lambda self, model: model.filter(self)
