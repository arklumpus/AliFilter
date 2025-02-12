# AliFilter datasets

This folder contains the datasets that were used to train AliFilter models and assess the performance of the program. Each subfolder represents one of the primary datasets:

1. `1_Cyanobacteria_Phylogenomic` contains a dataset consisting of 139 alignments of manually curated cyanobacterial genes from a previously published study [[1]](https://doi.org/10.1093/sysbio/syae025). Each alignment contains approximately 200 amino acid sequences from Cyanobacteria and some outgroup genomes (Vampirovibrionia, Serycytochromatia, and other Terrabacteria).

2. `2_Cyanobacteria_BUSCO` consists of 40 alignments randomly selected out of a set of 773 cyanobacterial single copy orthologs retrieved by BUSCO v5.4.3 [[2]](https://doi.org/10.1093/molbev/msab199) with the `cyanobacteria_odb10` dataset. Each alignment contains approximately 3000 amino acid sequences from genomes of Cyanobacteria.

3. `3_Rhodobacteraceae_BUSCO` consists of 40 alignments, randomly selected out of 833 single copy orthologs retrieved by BUSCO v5.4.3 [[2]](https://doi.org/10.1093/molbev/msab199) with the `rhodobacterales_odb10` dataset. Each alignment contains approximately 150 amino acid sequences from genomes of Rhodobacteraceae.

4. `4_Prokaryotes_COG` consists of 54 alignments of the vertically evolving single-copy marker genes identified in [[3]](https://doi.org/10.7554/eLife.66695), sampled from 350 archaea and 350 bacteria representing the breadth of prokaryotic diversity.

5. `5_Collembola_BUSCO` consists of 40 alignments, selected using the `--suggest` function of AliFilter out of 1,013 single copy orthologs retrieved by BUSCO v5.4.3 [[2]](https://doi.org/10.1093/molbev/msab199) with the arthropoda_odb10 dataset. Each alignment has a variable set of taxa from a total of 149 species.

6. `6_Formicidae_BUSCO` consists of 40 alignments, selected using the `--suggest` function of AliFilter out of 5,991 single copy orthologs retrieved by BUSCO v5.4.3 [[2]](https://doi.org/10.1093/molbev/msab199) with the hymenoptera_odb10 dataset. Each alignment contains a variable set of taxa from a total of 71 species.

7. `7_Heliconiini_OG` consists of 40 alignments, selected using the `--suggest` function of AliFilter out of 3,393 single copy orthologs identified in [[4]](https://doi.org/10.1038/s41467-023-41412-5). Alignments contain a variable set of taxa from a total of 63 species.

8. `8_rRNA_SILVA` contains 58 alignments of small subunit (SSU) and large subunit (LSU) rRNA sequences, retrieved from the SILVA database [[5]](https://doi.org/10.1093/NAR/GKS1219). Each alignment contains between 42 and 2000 rRNA sequences from a single group (phylum, class, or order) of prokaryotes or eukaryotes.

Each subfolder contains further subfolders with the alignment files:

* `AliFilter_Filtered` contains alignments filtered using the default AliFilter model.
* `BMGE_Filtered` contains alignments filtered using BMGE [[6]](https://doi.org/10.1186/1471-2148-10-210) with the default settings.
* `ClipKIT_Filtered` contains alignments filtered using ClipKIT [[7]](https://doi.org/10.1371/journal.pbio.3001007) with the default settings.
* `ERRM_Filtered` contains alignments manually filtered by Edmund R. R. Moody (only for datasets 2, 3, and 4).
* `FC_Filtered` contains alignments manually filtered by Francesco Cicconardi (only for datasets 5, 6, and 7).
* `GB_Filtered` contains alignments manually filtered by Giorgio Bianchini.
* `Gblocks_Filtered` contains alignments filtered using Gblocks [[8]](https://doi.org/10.1093/OXFORDJOURNALS.MOLBEV.A026334) with the `b1` and `b2` parameters set to $0.5 \cdot n$ (where $n$ is the number of sequences in the alignment), the `b3` parameter set to `1`, `b4` set to `6`, and `b5` to `h`.
* `Noisy_Filtered` contains alignments filtered using Noisy [[9]](https://doi.org/10.1186/1748-7188-3-7) with the default settings.
* `Raw_Alignments` contains the raw alignment files.
* `trimAl_Filtered` contains alignments filtered using trimAl [[10]](https://doi.org/10.1093/bioinformatics/btp348) with the `automated1` option.

Furthermore, each dataset folder also contains three text files (`Training_set.txt`, `Validation_set.txt`, and `Test_set.txt`), which list the alignments used for model training, validation and testing (except for dataset 1, which was entirely used as a test set).

## References

[1] Bianchini, G., Hagemann, M., & Sánchez-Baracaldo, P. (2024). Stochastic Character Mapping, Bayesian Model Selection, and Biosynthetic Pathways Shed New Light on the Evolution of Habitat Preference in Cyanobacteria. Systematic Biology, syae025. https://doi.org/10.1093/sysbio/syae025

[2] Mosè Manni, Matthew R Berkeley, Mathieu Seppey, Felipe A Simão, Evgeny M Zdobnov, BUSCO Update: Novel and Streamlined Workflows along with Broader and Deeper Phylogenetic Coverage for Scoring of Eukaryotic, Prokaryotic, and Viral Genomes, Molecular Biology and Evolution, Volume 38, Issue 10, October 2021, Pages 4647–4654, https://doi.org/10.1093/molbev/msab199

[3] Moody, E. R. R., Mahendrarajah, T. A., Dombrowski, N., Clark, J. W., Petitjean, C., Offre, P., Szöllősi, G. J., Spang, A., & Williams, T. A. (2022). An estimate of the deepest branches of the tree of life from ancient vertically evolving genes. ELife, 11. https://doi.org/10.7554/ELIFE.66695

[4] Cicconardi, F., Milanetti, E., Pinheiro de Castro, E. C., Mazo-Vargas, A., van Belleghem, S. M., Ruggieri, A. A., Rastas, P., Hanly, J., Evans, E., Jiggins, C. D., Owen McMillan, W., Papa, R., di Marino, D., Martin, A., & Montgomery, S. H. (2023). Evolutionary dynamics of genome size and content during the adaptive radiation of Heliconiini butterflies. Nature Communications 2023 14:1, 14(1), 1–24. https://doi.org/10.1038/s41467-023-41412-5

[5] Quast, C., Pruesse, E., Yilmaz, P., Gerken, J., Schweer, T., Yarza, P., Peplies, J., & Glöckner, F. O. (2013). The SILVA ribosomal RNA gene database project: improved data processing and web-based tools. Nucleic Acids Research, 41(D1), D590–D596. https://doi.org/10.1093/NAR/GKS1219

[6] Criscuolo, A., Gribaldo, S. BMGE (Block Mapping and Gathering with Entropy): a new software for selection of phylogenetic informative regions from multiple sequence alignments. BMC Evol Biol 10, 210 (2010). https://doi.org/10.1186/1471-2148-10-210

[7] Steenwyk JL, Buida TJ III, Li Y, Shen X-X, Rokas A (2020) ClipKIT: A multiple sequence alignment trimming software for accurate phylogenomic inference. PLoS Biol 18(12): e3001007. https://doi.org/10.1371/journal.pbio.3001007

[8] Castresana, J. (2000). Selection of Conserved Blocks from Multiple Alignments for Their Use in Phylogenetic Analysis. Molecular Biology and Evolution, 17(4), 540–552. https://doi.org/10.1093/OXFORDJOURNALS.MOLBEV.A026334

[9] Dress, A.W., Flamm, C., Fritzsch, G. et al. Noisy: Identification of problematic columns in multiple sequence alignments. Algorithms Mol Biol 3, 7 (2008). https://doi.org/10.1186/1748-7188-3-7

[10] Salvador Capella-Gutiérrez, José M. Silla-Martínez, Toni Gabaldón, trimAl: a tool for automated alignment trimming in large-scale phylogenetic analyses, Bioinformatics, Volume 25, Issue 15, August 2009, Pages 1972–1973, https://doi.org/10.1093/bioinformatics/btp348
