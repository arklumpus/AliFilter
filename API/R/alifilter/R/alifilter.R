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

require(ape)
require(jsonlite)

#' Computes the alignment features for a DNA or protein alignment (`AAbin` or
#' `DNAbin`).
#'
#' @param alignment
#'   A sequence alignment as an object of class `AAbin` or `DNAbin` (from
#'   package ape).
#'
#' @return
#'   A two-dimensional list of mode numeric containing the features computed for
#'   each alignment column.
#'
#' @examples
#'   data(afExample)
#'   features <- getAlignmentFeatures(aaAlignment)
#'
#' @export
getAlignmentFeatures <- function(alignment) {

  # Check that these are DNA or AA sequences.
  if (!inherits(alignment, c("AAbin", "DNAbin"))) {
    stop(paste("Invalid alignment type \"", class(alignment),
               "\". Expected AAbin or DNAbin.", sep=""))
  }

  # Check that the sequences are aligned.
  sequenceLengths <- lengths(alignment, use.names = FALSE)
  if (max(sequenceLengths) != min(sequenceLengths)) {
    stop(paste("The sequences do not all have the same length!"))
  }

  if (inherits(alignment, "AAbin")) {
    return(alifilter_rcpp_features(as.list(alignment)))
  }
  else {
    return(alifilter_rcpp_features(lapply(as.character(as.list(alignment)), sapply,
                                          charToRaw)))
  }

}

#' Parse an AliFilter model serialized as a JSON file.
#'
#' @param modelFile
#'   The path to a JSON file containing a trained or validated AliFilter model.
#'
#' @return
#'   An object of class `alifilter_model` containing the logistic model
#'   coefficients and the preservation threshold.
#'
#' @examples
#' \dontrun{
#'   model <- parseAliFilterModel("path/to/model.json")
#' }
#' @export
parseAliFilterModel <- function(modelFile) {

  deserializedModel <- jsonlite::fromJSON(modelFile)

  model <- list()
  model$threshold <- 0.5

  if (!is.null(deserializedModel$FastThreshold))
  {
    model$threshold <- deserializedModel$FastThreshold
  }

  model$logisticModel <- deserializedModel$LogisticModel

  attr(model, "class") = "alifilter_model"

  return(model)
}

#' Compute column preservation scores, given pre-computed alignment features and
#' an AliFilter model.
#'
#' @param alignmentFeatures
#'   The pre-computed alignment features (from the `getAlignmentFeatures`
#'   function)
#'
#' @param model
#'   An object of class `alifilter_model`, representing the model to use to
#'   compute the column preservation scores.
#'
#' @return
#'   A vector of mode numeric containing the column preservation scores (from
#'   0 to 1).
#'
#' @examples
#'   data(afExample)
#'   features <- getAlignmentFeatures(aaAlignment)
#'   getColumnScores(features, afModel)
#'
#' @export
getColumnScores <- function(alignmentFeatures, model) {
  # Check that the model is valid
  if (!inherits(model, "alifilter_model")) {
    stop(paste("Invalid model type \"", class(model),
               "\". Expected alifilter_model.", sep=""))
  }

  scores <- 1.0 / (1.0 + exp(-(model$logisticModel[[1]] %*% alignmentFeatures +
                                 model$logisticModel[[2]])))

  return(scores);
}

#' Get an alignment mask from pre-computed column scores.
#'
#' @param scores
#'   The pre-computed column preservation scores (from the `getColumnScores`
#'   function)
#'
#' @param model
#'   An object of class `alifilter_model`, containing the preservation threshold
#'   used to convert the column scores into the mask.
#'
#' @return
#'   A string containing `1` for columns that should be preserved and `0` for
#'   columns that should be deleted.
#'
#' @examples
#'   data(afExample)
#'   features <- getAlignmentFeatures(aaAlignment)
#'   scores <- getColumnScores(features, afModel)
#'   getMaskFromColumnScores(scores, afModel)
#'
#' @export
getMaskFromColumnScores <- function(scores, model) {
  # Check that the model is valid
  if (!inherits(model, "alifilter_model")) {
    stop(paste("Invalid model type \"", class(model),
               "\". Expected alifilter_model.", sep=""))
  }

  return(paste(as.character(as.numeric(scores >= model$threshold)),
               collapse=""))
}

#' Get an alignment mask from pre-computed alignment features.
#'
#' @param alignmentFeatures
#'   The pre-computed alignment features (from the `getAlignmentFeatures`
#'   function)
#'
#' @param model
#'   An object of class `alifilter_model`, specifying the AliFilter model.
#'
#' @return
#'   A string containing `1` for columns that should be preserved and `0` for
#'   columns that should be deleted.
#'
#' @examples
#'   data(afExample)
#'   features <- getAlignmentFeatures(aaAlignment)
#'   getMaskFromAlignmentFeatures(features, afModel)
#'
#' @export
getMaskFromAlignmentFeatures <- function(alignmentFeatures, model) {
  return (getMaskFromColumnScores(getColumnScores(alignmentFeatures, model),
                                  model))
}

#' Get an alignment mask from an alignment (`AAbin` or `DNAbin`) using the
#' specified model.
#'
#' @param alignment
#'   A sequence alignment as an object of class `AAbin` or `DNAbin` (from
#'   package ape).
#'
#' @param model
#'   An object of class `alifilter_model`, specifying the AliFilter model.
#'
#' @return
#'   A string containing `1` for columns that should be preserved and `0` for
#'   columns that should be deleted.
#'
#' @examples
#'   data(afExample)
#'   getMask(aaAlignment, afModel)
#'
#' @export
getMask <- function(alignment, model) {
  return (getMaskFromColumnScores(getColumnScores(
    getAlignmentFeatures(alignment), model), model))
}

#' Filter an alignment (`DNAbin` or `AAbin`) using the specified AliFilter
#' model.
#'
#' @param alignment
#'   A sequence alignment as an object of class `AAbin` or `DNAbin` (from
#'   package ape).
#'
#' @param model
#'   An object of class `alifilter_model`, specifying the AliFilter model.
#'
#' @return
#'   An alignment (`DNAbin` or `AAbin`) containing only the columns that have
#'   been preserved according to the AliFilter model.
#'
#' @examples
#'   data(afExample)
#'   filterAlignment(aaAlignment, afModel)
#'
#' @export
filterAlignment <- function(alignment, model) {
  scores <- getColumnScores(getAlignmentFeatures(alignment), model)
  return(as.matrix(alignment)[,scores >= model$threshold])
}
