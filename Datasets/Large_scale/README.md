# Datasets for large-scale benchmarks

The datasets used for large-scale benchmarks are too large for this repository and are provided separately.

## SILVA Ref NR99

The SSU and LSU Ref NR99 alignments from release 138.2 of the SILVA database ([[1]](https://doi.org/10.1093/NAR/GKS1219)) can be downloaded from the ARB-SILVA server:

* SSU: [SILVA_138.2_SSURef_tax_silva_full_align_trunc.fasta.gz&nbsp;&nbsp;&nbsp;&nbsp;![DOI: 10.82364/138.2/SSU/Ref/FASTA/aligned](https://img.shields.io/badge/DOI-10.82364%2F138.2%2FSSU%2FRef%2FFASTA%2Faligned-blue)](https://doi.org/10.82364/138.2/SSU/Ref/FASTA/aligned)
* LSU: [SILVA_138.2_LSURef_tax_silva_full_align_trunc.fasta.gz&nbsp;&nbsp;&nbsp;&nbsp;![DOI: 10.82364/138.2/LSU/Ref/FASTA/aligned](https://img.shields.io/badge/DOI-10.82364%2F138.2%2FLSU%2FRef%2FFASTA%2Faligned-blue)](https://doi.org/10.82364/138.2/LSU/Ref/FASTA/aligned)

These alignments were modified by replacing `.` gap characters with `-` using `sed`:

```bash
sed "s/\./-/g" original_alignment > modified_alignment
```

## GTDB bac120 alignments

The alignments for the bac120 markers from release 226 of the GTDB database ([[2]](https://doi.org/10.1093/NAR/GKAB776)) are archived on Zenodo:

* bac120: [bac120_r226_aligned.zip](https://zenodo.org/records/18518536/files/bac120_r226_aligned.zip?download=1)&nbsp;&nbsp;&nbsp;&nbsp;[![DOI: 10.5281/zenodo.18518536](https://zenodo.org/badge/DOI/10.5281/zenodo.18518536.svg)](https://doi.org/10.5281/zenodo.18518536)

## References

[1] Quast, C., Pruesse, E., Yilmaz, P., Gerken, J., Schweer, T., Yarza, P., Peplies, J., & Glöckner, F. O. (2013). The SILVA ribosomal RNA gene database project: improved data processing and web-based tools. Nucleic Acids Research, 41(D1), D590–D596. https://doi.org/10.1093/NAR/GKS1219

[2] Parks, D. H., Chuvochina, M., Rinke, C., Mussig, A. J., Chaumeil, P. A., & Hugenholtz, P. (2022). GTDB: an ongoing census of bacterial and archaeal diversity through a phylogenetically consistent, rank normalized and complete genome-based taxonomy. Nucleic Acids Research, 50(D1), D785–D794. https://doi.org/10.1093/NAR/GKAB776