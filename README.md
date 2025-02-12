# AliFilter: a machine learning approach to alignment filtering

<img src="AliFilter_banner.svg" height=200 align="right" />

[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.14861812.svg)](https://doi.org/10.5281/zenodo.14861812)

Sequence alignment filtering (or "trimming") consists in removing parts of a DNA or protein alignment to improve the performance of a downstream analysis (such as a phylogenetic reconstruction). Alignment columns are removed because they are deemed to be unsuitable for the analysis, e.g. because they are likely to be the result of mistakes introduced by the sequence alignment software, because they contain no information, or because they contain a high amount of noise.

Alignment filtering can be performed by manually inspecting alignments and identifying problematic alignment columns, or by using a variety of software tools (e.g., BMGE [[1]](https://doi.org/10.1186/1471-2148-10-210), ClipKIT [[2]](https://doi.org/10.1371/journal.pbio.3001007), Gblocks [[3]](https://doi.org/10.1093/OXFORDJOURNALS.MOLBEV.A026334), Noisy [[4]](https://doi.org/10.1186/1748-7188-3-7), trimAL [[5]](https://doi.org/10.1093/bioinformatics/btp348); see [Tan et al. 2015 [6]](https://doi.org/10.1093/sysbio/syv033) for a review of some of these). Compared to manual filtering, automated filtering tools have the advantage of being easily applicable to large datasets and producing consistent results; on the other hand, apart from some customisation settings, they are often a "black box" offering little control over which parts of the alignments are preserved or deleted. Manual filtering, on the other hand, is more time consuming and less reproducible, but allows for a more fine-tuned filtering approach.

**AliFilter** is a tool to automate a manual filtering approach. Using a machine learning algorithm, AliFilter can create a model from a small set of manually filtered alignments; the model can then be used to reproducibly filter many aligments, simulating the manual filtering approach. The program also comes with a pre-trained model that can be used to filter alignments out of the box.

AliFilter is a command-line tool available for Windows, macOS and Linux; it is distributed under a GPLv3 license. An API is also available, which allows programs written in C#, C/C++, Python, R, and JavaScript to use AliFilter models for alignment filtering.

## Quick usage guide

AliFilter does not require any installation; you just need to [download the latest program release](https://github.com/arklumpus/AliFilter/releases) for your operating system and you are good to go.

To filter an alignment with AliFilter, run the following command:

```
AliFilter -i alignment.fas -o output.fas
```

Here, `alignment.fas` is the input (unfiltered) alignment, while `output.fas` is the name of the file where the output (filtered) alignment will be saved. Alignments can be in FASTA or relaxed PHYLIP format. This command will use the default model implemented in AliFilter to filter the alignment.

If you wish to use a specific model, you can use the `-m` argument:

```
AliFilter -i alignment.fas -o output.fas -m <model>
```

Where `<model>` is either a standard model specification, or the path to a `model.json` file containing a custom trained model. A list of the standard model specifications is available [in the Wiki](https://github.com/arklumpus/AliFilter/wiki/Alignment-filtering#standard-models).

If you do not provide the `-i` or `-o` arguments, the program will read from the standard input or write to the standard output. This makes it possible to concatenate sequence alignment and filtering in a single line; for example, if you are using `mafft` to align the sequences:

```
mafft --auto unaligned.fas | AliFilter > filtered.fas
```

This command will directly create a file called `filtered.fas` containing the filtered sequence alignment.

AliFilter can also perform additional tasks, including training new models, comparing two alignments or masks, and combining multiple masks. See [the Wiki](https://github.com/arklumpus/AliFilter/wiki) for more details on all the features of the program.

## Citation

If you use AliFilter in your research, please cite it as:

```
TODO
```

## Building from source

Note that if you just wish to use the program, you can simply download the precompiled executables from the [release page](https://github.com/arklumpus/AliFilter/releases), rather than compiling the program.

If you wish to build AliFilter from source, you will need to install the [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). Afterwards, clone this repository (the source code is in the `src` folder) and execute the build script for your platform.

### Windows

On Windows (x64 only, for now), you will need to execute:

```cmd
BuildRelease-win-x64.cmd <subject> <pin>
```

Where `<subject>` is your code signing certificate subject and `<pin>` is your smart card pin. If you do not have a code signing certificate, you can enter random strings for these: the signing step will fail, but the executables will still be produced.

### Linux

The command you will need to execute on Linux systems depends on your architecture.

#### Linux-x64

```bash
chmod +x BuildRelease-linux-x64.sh
./BuildRelease-linux-x64.sh
```

#### Linux-arm64

```bash
chmod +x BuildRelease-linux-arm64.sh
./BuildRelease-linux-arm64.sh
```

### macOS

The command you need to execute on macOS also depends on your architecture. If you do not have a paid Apple Developer account, you can enter random strings for the various required arguments of the script; the code signing and notarization steps will fail, but the executable will still produced.

#### macOS-x64

```bash
chmod +x BuildRelease-mac-x64.sh
./BuildRelease-mac-x64.sh <Developer ID Application> <Apple ID> <App-specific password> <Developer team ID>
```

#### macOS-arm64

```bash
chmod +x BuildRelease-mac-arm64.sh
./BuildRelease-mac-arm64.sh <Developer ID Application> <Apple ID> <App-specific password> <Developer team ID>
```

## References

[1] Criscuolo, A., Gribaldo, S. BMGE (Block Mapping and Gathering with Entropy): a new software for selection of phylogenetic informative regions from multiple sequence alignments. BMC Evol Biol 10, 210 (2010). https://doi.org/10.1186/1471-2148-10-210

[2] Steenwyk JL, Buida TJ III, Li Y, Shen X-X, Rokas A (2020) ClipKIT: A multiple sequence alignment trimming software for accurate phylogenomic inference. PLoS Biol 18(12): e3001007. https://doi.org/10.1371/journal.pbio.3001007

[3] Castresana, J. (2000). Selection of Conserved Blocks from Multiple Alignments for Their Use in Phylogenetic Analysis. Molecular Biology and Evolution, 17(4), 540–552. https://doi.org/10.1093/OXFORDJOURNALS.MOLBEV.A026334

[4] Dress, A.W., Flamm, C., Fritzsch, G. et al. Noisy: Identification of problematic columns in multiple sequence alignments. Algorithms Mol Biol 3, 7 (2008). https://doi.org/10.1186/1748-7188-3-7

[5] Salvador Capella-Gutiérrez, José M. Silla-Martínez, Toni Gabaldón, trimAl: a tool for automated alignment trimming in large-scale phylogenetic analyses, Bioinformatics, Volume 25, Issue 15, August 2009, Pages 1972–1973, https://doi.org/10.1093/bioinformatics/btp348

[6] Ge Tan, Matthieu Muffato, Christian Ledergerber, Javier Herrero, Nick Goldman, Manuel Gil, Christophe Dessimoz, Current Methods for Automated Filtering of Multiple Sequence Alignments Frequently Worsen Single-Gene Phylogenetic Inference, Systematic Biology, Volume 64, Issue 5, September 2015, Pages 778–791, https://doi.org/10.1093/sysbio/syv033
