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

#ifndef ALIFILTER_PHYLIP_H
#define ALIFILTER_PHYLIP_H

// Maximum length for the sequence names.
#define MAX_SEQUENCE_NAME_LENGTH 254

// Represents a sequence alignment.
typedef struct {
    // Names of the sequences in the alignment.
    char* sequenceNames;

    // Alignment sequence data (contains sequenceCount* alignmentLength elements).
    char* sequenceData;

    // Number of sequences in the alignment.
    int sequenceCount;

    // Length of the alignment.
    int alignmentLength; 
} alignment;

// Releases the memory held by an alignment.
//   Parameters:
//     • alignment* alignment: the alignment that should be freed.
void phylip_freeAlignment(alignment* alignment);

// Parses an alignment file in PHYLIP format.
//   Parameters:
//     • const char* phylipFile: path to the PHYLIP file.
//     • alignment* out_alignment: if the return value is 0 or 5, when this function returns this pointer will point to the parsed alignment.
//
//   Return value:
//     • 0: success
//     • 1: error opening the file
//     • 2: error while retrieving the number of sequences and the length of the alignment
//     • 3: could not allocate enough memory for the alignment
//     • 4: error while reading the alignment
//     • 5: error while closing the file (but the alignment has been read successfully and should be freed)
int phylip_parsePHYLIP(const char* phylipFile, alignment* out_alignment);

// Returns the name of a sequence in the alignment (or NULL if an invalid sequence index is specified).
//   Parameters:
//     • const alignment* alignment: the alignment from which the sequence name should be extracted.
//     • int sequence: the index of the sequence whose name should be extracted.
//
//   Return value: name of the sequence in the alignment, as a C string.
char* phylip_getSequenceName(const alignment* alignment, int sequence);



#ifdef ALIFILTER_PHYLIP_IMPLEMENTATION

#define STR_HELPER(x) #x
#define STR(x) STR_HELPER(x)

#include <stdio.h>
#include <stdlib.h>

void phylip_freeAlignment(alignment* alignment) {
    free(alignment->sequenceNames);
    alignment->sequenceNames = NULL;
    free(alignment->sequenceData);
    alignment->sequenceData = NULL;
}

int phylip_parsePHYLIP(const char* phylipFile, alignment* out_alignment) {
    
    // Access the alignment file.
    FILE* fileH = fopen(phylipFile, "r");
    if (fileH == NULL) {
        return 1;
    }

    int sequenceCount;
    int alignmentLength;

    // Read the number of sequences and alignment length from the PHYLIP file.
    int result = fscanf(fileH, "%d %d", &sequenceCount, &alignmentLength);

    if (result != 2 || sequenceCount < 2 || alignmentLength < 1) {
        fclose(fileH);
        return 2;
    }

    // Allocate memory for the alignment.
    char* sequenceNames = (char *)malloc(sequenceCount * (MAX_SEQUENCE_NAME_LENGTH + 1) * sizeof(*sequenceNames));
    char* sequenceData = (char *)malloc(sequenceCount * alignmentLength * sizeof(*sequenceData));

    if (sequenceNames == NULL || sequenceData == NULL) {
        free(sequenceNames);
        free(sequenceData);
        fclose(fileH);
        return 3;
    }

    // Read the sequences from the file.
    for (int i = 0; i < sequenceCount; i++) {
        // Read the sequence name.
        result = fscanf(fileH, "%"STR(MAX_SEQUENCE_NAME_LENGTH)"s", &sequenceNames[i * (MAX_SEQUENCE_NAME_LENGTH + 1)]);

        if (result == 1) {
            // Skip space characters.
            int c = fgetc(fileH);
            while (c == ' ') {
                c = fgetc(fileH);
            }

            if (c == EOF) {
                result = 0;
            }
            else {
                // At this point, c is the first character in the sequence.
                sequenceData[i * alignmentLength] = c;

                // Read the remaining characters in the sequence.
                size_t readChars = fread(&sequenceData[i * alignmentLength + 1], sizeof(char), alignmentLength - 1, fileH);

                if (readChars != (size_t)(alignmentLength - 1)) {
                    result = 0;
                }
            }
        }

        // Exit with an error code.
        if (result != 1) {
            free(sequenceNames);
            free(sequenceData);
            fclose(fileH);
            return 4;
        }
    }

    out_alignment->sequenceCount = sequenceCount;
    out_alignment->alignmentLength = alignmentLength;
    out_alignment->sequenceNames = sequenceNames;
    out_alignment->sequenceData = sequenceData;

    // Close the file.
    result = fclose(fileH);

    if (result != 0) {
        return 5;
    }
    else {
        return 0;
    }
}

char* phylip_getSequenceName(const alignment* alignment, int sequence) {
    if (sequence >= 0 && sequence < alignment->sequenceCount) {
        return &(alignment->sequenceNames[sequence * (MAX_SEQUENCE_NAME_LENGTH + 1)]);
    }
    else {
        return NULL;
    }
}

#endif
#endif