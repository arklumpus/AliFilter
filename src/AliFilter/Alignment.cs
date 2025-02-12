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

using AliFilter.AlignmentFormatting;

namespace AliFilter
{
    /// <summary>
    /// The type of alignment.
    /// </summary>
    public enum AlignmentType
    {
        /// <summary>
        /// DNA alignment.
        /// </summary>
        DNA,

        /// <summary>
        /// Protein alignment.
        /// </summary>
        Protein,

        /// <summary>
        /// Automatically detect if DNA or Protein.
        /// </summary>
        Autodetect
    }

    /// <summary>
    /// Represents a sequence alignment.
    /// </summary>
    public abstract class Alignment
    {
        /// <summary>
        /// Metadata associated with the alignment.
        /// </summary>
        public virtual IDictionary<string, object> Tags { get; }

        /// <summary>
        /// The characters allowed in sequences (e.g., ACGT), excluding the gap character -.
        /// </summary>
        public abstract char[] Characters { get; }

        /// <summary>
        /// The number of sequences in the alignment.
        /// </summary>
        public virtual int SequenceCount { get; }

        /// <summary>
        /// The length of each sequence in the alignment.
        /// </summary>
        public virtual int AlignmentLength { get; }

        /// <summary>
        /// Contains the raw sequence data.
        /// </summary>
        protected virtual char[] AlignmentData { get; }

        /// <summary>
        /// The names of the sequences in the alignment.
        /// </summary>
        public virtual string[] SequenceNames { get; }

        /// <summary>
        /// Contains the index of each sequence in the alignment.
        /// </summary>
        protected virtual IReadOnlyDictionary<string, int> SequenceNameIndices { get; }

        /// <summary>
        /// Gets the specified sequence.
        /// </summary>
        /// <param name="index">The index of the sequence to get.</param>
        /// <returns>The requested sequence, as a collection of <see langword="char"/>s.</returns>
        public virtual IEnumerable<char> GetSequence(int index)
        {
            if (index < 0 || index >= SequenceCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "The index must range between 0 (inclusive) and the number of sequences in the alignment (exclusive)!");
            }

            for (int i = 0; i < AlignmentLength; i++)
            {
                yield return AlignmentData[index * AlignmentLength + i];
            }
        }

        /// <summary>
        /// Gets the sequence with the specified name.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to get.</param>
        /// <returns>The requested sequence, as a collection of <see langword="char"/>s.</returns>
        public virtual IEnumerable<char> GetSequence(string sequenceName)
        {
            if (!SequenceNameIndices.TryGetValue(sequenceName, out int index))
            {
                throw new ArgumentOutOfRangeException(sequenceName, sequenceName, "The sequence '" + sequenceName + "' is not present in the alignment!");
            }

            return GetSequence(index);
        }

        /// <summary>
        /// Gets the sequence with the specified name.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to get.</param>
        /// <param name="sequence">When this method returns, this variable will contain the requested sequence, as a collection of <see langword="char"/>s, or <see langword="null"/> if the specified sequence was not present in the alignment.</param>
        /// <returns><see langword="true"/> if the specified sequence is contained in the alignment; <see langword="false"/> otherwise.</returns>
        public virtual bool TryGetSequence(string sequenceName, out IEnumerable<char> sequence)
        {
            if (!SequenceNameIndices.TryGetValue(sequenceName, out int index))
            {
                sequence = null;
                return false;
            }

            sequence = GetSequence(index);
            return true;
        }

        /// <summary>
        /// Returns the specified column from the alignment.
        /// </summary>
        /// <param name="index">The index of the column to get.</param>
        /// <returns>The requested column, as a collection of <see langword="char"/>s.</returns>
        public virtual IEnumerable<char> GetColumn(int index)
        {
            if (index < 0 || index >= AlignmentLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "The index must range between 0 (inclusive) and the length of the alignment (exclusive)!");
            }

            for (int i = 0; i < SequenceCount; i++)
            {
                yield return AlignmentData[i * AlignmentLength + index];
            }
        }

        /// <summary>
        /// Create a new <see cref="Alignment"/>.
        /// </summary>
        /// <param name="sequences">The sequences in the alignment.</param>
        protected Alignment(IReadOnlyList<(string name, IEnumerable<char> sequence)> sequences)
        {
            this.SequenceCount = sequences.Count;
            this.SequenceNames = new string[SequenceCount];
            Dictionary<string, int> sequenceNameIndices = new Dictionary<string, int>(SequenceCount);
            this.Tags = new Dictionary<string, object>();

            for (int i = 0; i < SequenceCount; i++)
            {
                IEnumerable<char> currSeq = sequences[i].sequence;

                if (this.AlignmentData == null)
                {
                    if (currSeq.TryGetNonEnumeratedCount(out int count))
                    {
                        this.AlignmentLength = count;
                    }
                    else
                    {
                        char[] tempSeq = currSeq.ToArray();
                        currSeq = tempSeq;
                        this.AlignmentLength = tempSeq.Length;
                    }

                    this.AlignmentData = new char[this.AlignmentLength * this.SequenceCount];
                }

                this.SequenceNames[i] = sequences[i].name;
                sequenceNameIndices[sequences[i].name] = i;

                int j = 0;

                foreach (char c in currSeq)
                {
                    this.AlignmentData[this.AlignmentLength * i + j] = c;
                    j++;
                }

                if (j != this.AlignmentLength)
                {
                    throw new ArgumentException("The sequences do not all have the same length!");
                }
            }

            this.SequenceNameIndices = sequenceNameIndices;
        }

        /// <summary>
        /// Create a new <see cref="Alignment"/>.
        /// </summary>
        /// <param name="sequences">The sequences in the alignment.</param>
        protected Alignment(IReadOnlyList<KeyValuePair<string, IEnumerable<char>>> sequences)
        {
            this.SequenceCount = sequences.Count;
            this.SequenceNames = new string[SequenceCount];
            Dictionary<string, int> sequenceNameIndices = new Dictionary<string, int>(SequenceCount);
            this.Tags = new Dictionary<string, object>();

            for (int i = 0; i < SequenceCount; i++)
            {
                IEnumerable<char> currSeq = sequences[i].Value;

                if (this.AlignmentData == null)
                {
                    if (currSeq.TryGetNonEnumeratedCount(out int count))
                    {
                        this.AlignmentLength = count;
                    }
                    else
                    {
                        char[] tempSeq = currSeq.ToArray();
                        currSeq = tempSeq;
                        this.AlignmentLength = tempSeq.Length;
                    }

                    this.AlignmentData = new char[this.AlignmentLength * this.SequenceCount];
                }

                this.SequenceNames[i] = sequences[i].Key;
                sequenceNameIndices[sequences[i].Key] = i;

                int j = 0;

                foreach (char c in currSeq)
                {
                    this.AlignmentData[this.AlignmentLength * i + j] = c;
                    j++;
                }

                if (j != this.AlignmentLength)
                {
                    throw new ArgumentException("The sequences do not all have the same length!");
                }
            }

            this.SequenceNameIndices = sequenceNameIndices;
        }

        /// <summary>
        /// Create a new <see cref="Alignment"/>.
        /// </summary>
        /// <param name="sequenceCount">The number of sequences in the alignment.</param>
        /// <param name="sequences">The sequences in the alignment.</param>
        protected Alignment(int sequenceCount, IEnumerable<(string name, IEnumerable<char> sequence)> sequences)
        {
            this.SequenceCount = sequenceCount;
            this.SequenceNames = new string[SequenceCount];
            Dictionary<string, int> sequenceNameIndices = new Dictionary<string, int>(SequenceCount);
            this.Tags = new Dictionary<string, object>();

            int i = 0;
            foreach ((string name, IEnumerable<char> sequence) in sequences)
            {
                IEnumerable<char> currSeq = sequence;

                if (this.AlignmentData == null)
                {
                    if (currSeq.TryGetNonEnumeratedCount(out int count))
                    {
                        this.AlignmentLength = count;
                    }
                    else
                    {
                        char[] tempSeq = currSeq.ToArray();
                        currSeq = tempSeq;
                        this.AlignmentLength = tempSeq.Length;
                    }

                    this.AlignmentData = new char[this.AlignmentLength * this.SequenceCount];
                }

                this.SequenceNames[i] = name;
                sequenceNameIndices[name] = i;

                int j = 0;

                foreach (char c in currSeq)
                {
                    this.AlignmentData[this.AlignmentLength * i + j] = c;
                    j++;
                }

                if (j != this.AlignmentLength)
                {
                    throw new ArgumentException("The sequences do not all have the same length!");
                }

                i++;
            }

            this.SequenceNameIndices = sequenceNameIndices;
        }

        /// <summary>
        /// Create a new <see cref="Alignment"/>.
        /// </summary>
        /// <param name="sequenceCount">The number of sequences in the alignment.</param>
        /// <param name="sequences">The sequences in the alignment.</param>
        protected Alignment(int sequenceCount, IEnumerable<KeyValuePair<string, IEnumerable<char>>> sequences)
        {
            this.SequenceCount = sequenceCount;
            this.SequenceNames = new string[SequenceCount];
            Dictionary<string, int> sequenceNameIndices = new Dictionary<string, int>(SequenceCount);
            this.Tags = new Dictionary<string, object>();

            int i = 0;
            foreach (KeyValuePair<string, IEnumerable<char>> seq in sequences)
            {
                IEnumerable<char> currSeq = seq.Value;

                if (this.AlignmentData == null)
                {
                    if (currSeq.TryGetNonEnumeratedCount(out int count))
                    {
                        this.AlignmentLength = count;
                    }
                    else
                    {
                        char[] tempSeq = currSeq.ToArray();
                        currSeq = tempSeq;
                        this.AlignmentLength = tempSeq.Length;
                    }

                    this.AlignmentData = new char[this.AlignmentLength * this.SequenceCount];
                }

                this.SequenceNames[i] = seq.Key;
                sequenceNameIndices[seq.Key] = i;

                int j = 0;

                foreach (char c in currSeq)
                {
                    this.AlignmentData[this.AlignmentLength * i + j] = c;
                    j++;
                }

                if (j != this.AlignmentLength)
                {
                    throw new ArgumentException("The sequences do not all have the same length!");
                }

                i++;
            }

            this.SequenceNameIndices = sequenceNameIndices;
        }


        /// <summary>
        /// Create a new empty <see cref="Alignment"/>.
        /// </summary>
        /// <param name="sequenceCount">The number of sequences in the alignment.</param>
        /// <param name="alignmentLength">The length of each sequence in the alignment.</param>
        protected Alignment(int sequenceCount, int alignmentLength)
        {
            this.SequenceCount = sequenceCount;
            this.AlignmentLength = alignmentLength;
            this.SequenceNameIndices = new Dictionary<string, int>(SequenceCount);
            this.SequenceNames = new string[sequenceCount];
            this.AlignmentData = new char[alignmentLength * sequenceCount];
            this.Tags = new Dictionary<string, object>();
        }

        /// <summary>
        /// Determines the type of a sequence (DNA or protein).
        /// </summary>
        /// <param name="sequence">The sequence whose tipe is to be determined.</param>
        /// <returns>The inferred sequence type.</returns>
        public static AlignmentType DetectSequenceType(IEnumerable<char> sequence)
        {
            AlignmentType alignmentType = AlignmentType.DNA;

            foreach (char c in sequence)
            {
                if (!(c < 65 || c == 'A' || c == 'a' || c == 'C' || c == 'c' || c == 'G' || c == 'g' || c == 'T' || c == 't' || c == 'U' || c == 'u' || c == 'N' || c == 'n'))
                {
                    alignmentType = AlignmentType.Protein;
                    break;
                }
            }

            return alignmentType;
        }

        /// <summary>
        /// Create an <see cref="Alignment"/> from a collection of sequences.
        /// </summary>
        /// <param name="sequences">The sequences in the alignment.</param>
        /// <param name="alignmentType">The type of sequences in the alignment.</param>
        /// <returns>A <see cref="DNAAlignment"/> or a <see cref="ProteinAlignment"/> created from the specified sequences.</returns>
        public static Alignment Create(IReadOnlyList<(string name, IEnumerable<char> sequence)> sequences, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            if (alignmentType == AlignmentType.Autodetect)
            {
                int ind = 0;
                IEnumerable<char> sequence = sequences[0].sequence;

                while (!sequence.Any(x => x != '0' && x != '1') && ind < sequences.Count - 1)
                {
                    ind++;
                    sequence = sequences[ind].sequence;
                }

                alignmentType = DetectSequenceType(sequence);
            }

            if (alignmentType == AlignmentType.DNA)
            {
                return new DNAAlignment(sequences);
            }
            else if (alignmentType == AlignmentType.Protein)
            {
                return new ProteinAlignment(sequences);
            }
            else
            {
                throw new ArgumentException("Invalid alignment type!");
            }
        }

        /// <summary>
        /// Create an <see cref="Alignment"/> from a collection of sequences.
        /// </summary>
        /// <param name="sequences">The sequences in the alignment.</param>
        /// <param name="alignmentType">The type of sequences in the alignment.</param>
        /// <returns>A <see cref="DNAAlignment"/> or a <see cref="ProteinAlignment"/> created from the specified sequences.</returns>
        public static Alignment Create(IReadOnlyList<KeyValuePair<string, IEnumerable<char>>> sequences, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            if (alignmentType == AlignmentType.Autodetect)
            {
                int ind = 0;
                IEnumerable<char> sequence = sequences[0].Value;

                while (!sequence.Any(x => x != '0' && x != '1') && ind < sequences.Count - 1)
                {
                    ind++;
                    sequence = sequences[ind].Key;
                }

                alignmentType = DetectSequenceType(sequence);
            }

            if (alignmentType == AlignmentType.DNA)
            {
                return new DNAAlignment(sequences);
            }
            else if (alignmentType == AlignmentType.Protein)
            {
                return new ProteinAlignment(sequences);
            }
            else
            {
                throw new ArgumentException("Invalid alignment type!");
            }
        }

        /// <summary>
        /// Create an <see cref="Alignment"/> from a collection of sequences.
        /// </summary>
        /// <param name="sequenceCount">The number of sequences in the alignment.</param>
        /// <param name="sequences">The sequences in the alignment.</param>
        /// <param name="alignmentType">The type of sequences in the alignment.</param>
        /// <returns>A <see cref="DNAAlignment"/> or a <see cref="ProteinAlignment"/> created from the specified sequences.</returns>
        public static Alignment Create(int sequenceCount, IEnumerable<(string name, IEnumerable<char> sequence)> sequences, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            IEnumerable<(string name, IEnumerable<char> sequence)> finalEnumerable;

            if (alignmentType == AlignmentType.Autodetect)
            {
                IEnumerator<(string name, IEnumerable<char> sequence)> enumerator = sequences.GetEnumerator();

                List<(string name, IEnumerable<char> sequence)> enumerated = new List<(string name, IEnumerable<char> sequence)>();

                IEnumerable<char> sequence = "";
                if (enumerator.MoveNext())
                {
                    sequence = enumerator.Current.sequence;
                    enumerated.Add(enumerator.Current);
                }

                while (!sequence.Any(x => x != '0' && x != '1') && enumerator.MoveNext())
                {
                    sequence = enumerator.Current.sequence;
                    enumerated.Add(enumerator.Current);
                }

                alignmentType = DetectSequenceType(sequence);

                IEnumerable<(string, IEnumerable<char>)> enumerate()
                {
                    while (enumerator.MoveNext())
                    {
                        yield return enumerator.Current;
                    }
                }

                finalEnumerable = enumerated.Concat(enumerate());
            }
            else
            {
                finalEnumerable = sequences;
            }

            if (alignmentType == AlignmentType.DNA)
            {
                return new DNAAlignment(sequenceCount, finalEnumerable);
            }
            else if (alignmentType == AlignmentType.Protein)
            {
                return new ProteinAlignment(sequenceCount, finalEnumerable);
            }
            else
            {
                throw new ArgumentException("Invalid alignment type!");
            }
        }

        /// <summary>
        /// Create an <see cref="Alignment"/> from a collection of sequences.
        /// </summary>
        /// <param name="sequenceCount">The number of sequences in the alignment.</param>
        /// <param name="sequences">The sequences in the alignment.</param>
        /// <param name="alignmentType">The type of sequences in the alignment.</param>
        /// <returns>A <see cref="DNAAlignment"/> or a <see cref="ProteinAlignment"/> created from the specified sequences.</returns>
        public static Alignment Create(int sequenceCount, IEnumerable<KeyValuePair<string, IEnumerable<char>>> sequences, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            IEnumerable<KeyValuePair<string, IEnumerable<char>>> finalEnumerable;

            if (alignmentType == AlignmentType.Autodetect)
            {
                IEnumerator<KeyValuePair<string, IEnumerable<char>>> enumerator = sequences.GetEnumerator();

                List<KeyValuePair<string, IEnumerable<char>>> enumerated = new List<KeyValuePair<string, IEnumerable<char>>>();

                IEnumerable<char> sequence = "";
                if (enumerator.MoveNext())
                {
                    sequence = enumerator.Current.Value;
                    enumerated.Add(enumerator.Current);
                }

                while (!sequence.Any(x => x != '0' && x != '1') && enumerator.MoveNext())
                {
                    sequence = enumerator.Current.Value;
                    enumerated.Add(enumerator.Current);
                }

                alignmentType = DetectSequenceType(sequence);

                IEnumerable<KeyValuePair<string, IEnumerable<char>>> enumerate()
                {
                    while (enumerator.MoveNext())
                    {
                        yield return enumerator.Current;
                    }
                }

                finalEnumerable = enumerated.Concat(enumerate());
            }
            else
            {
                finalEnumerable = sequences;
            }

            if (alignmentType == AlignmentType.DNA)
            {
                return new DNAAlignment(sequenceCount, finalEnumerable);
            }
            else if (alignmentType == AlignmentType.Protein)
            {
                return new ProteinAlignment(sequenceCount, finalEnumerable);
            }
            else
            {
                throw new ArgumentException("Invalid alignment type!");
            }
        }

        /// <summary>
        /// Returns a copy of this <see cref="Alignment"/>.
        /// </summary>
        /// <returns>A copy of this <see cref="Alignment"/>.</returns>
        public abstract Alignment Clone();

        /// <summary>
        /// Creates a subset of this alignment, including only the specified sequences.
        /// </summary>
        /// <param name="sequenceIndices">The indices of the sequences to include.</param>
        /// <returns>An alignment containing only the specified sequences.</returns>
        public abstract Alignment Subset(IReadOnlyList<int> sequenceIndices);

        /// <summary>
        /// Creates a subset of this alignment, including only the specified sequences.
        /// </summary>
        /// <param name="sequences">The sequences to include.</param>
        /// <returns>An alignment containing only the specified sequences.</returns>
        public virtual Alignment Subset(IReadOnlyList<string> sequences)
        {
            return this.Subset(sequences.Select(x => this.SequenceNameIndices[x]).ToList());
        }

        /// <summary>
        /// Returns a copy of the alignment, where the specified sequences have been removed.
        /// </summary>
        /// <param name="sequencesToRemove">The indices of the sequences to remove.</param>
        /// <returns>A copy of the alignment, where the specified sequences have been removed.</returns>
        public abstract Alignment RemoveSequences(IEnumerable<int> sequencesToRemove);

        /// <summary>
        /// Returns a copy of the alignment, where the specified sequences have been removed.
        /// </summary>
        /// <param name="sequencesToRemove">The indices of the sequences to remove.</param>
        /// <returns>A copy of the alignment, where the specified sequences have been removed.</returns>
        public virtual Alignment RemoveSequences(params int[] sequencesToRemove)
        {
            return this.RemoveSequences((IEnumerable<int>)sequencesToRemove);
        }

        /// <summary>
        /// Returns a copy of the alignment, where the specified sequences have been removed.
        /// </summary>
        /// <param name="sequencesToRemove">The names of the sequences to remove.</param>
        /// <returns>A copy of the alignment, where the specified sequences have been removed.</returns>
        public virtual Alignment RemoveSequences(IEnumerable<string> sequencesToRemove)
        {
            return this.RemoveSequences(sequencesToRemove.Select(x => SequenceNameIndices[x]));
        }

        /// <summary>
        /// Returns a copy of the alignment, where the specified sequences have been removed.
        /// </summary>
        /// <param name="sequencesToRemove">The names of the sequences to remove.</param>
        /// <returns>A copy of the alignment, where the specified sequences have been removed.</returns>
        public virtual Alignment RemoveSequences(params string[] sequencesToRemove)
        {
            return this.RemoveSequences((IEnumerable<string>)sequencesToRemove);
        }

        /// <summary>
        /// Cleans this <see cref="Alignment"/> by removing invalid characters.
        /// </summary>
        public virtual void Clean()
        {
            HashSet<char> validCharacters = new HashSet<char>(this.Characters);

            for (int i = 0; i < this.AlignmentData.Length; i++)
            {
                char c = Char.ToUpperInvariant(this.AlignmentData[i]);

                if (validCharacters.Contains(c))
                {
                    this.AlignmentData[i] = c;
                }
                else
                {
                    this.AlignmentData[i] = '-';
                }
            }
        }

        /// <summary>
        /// Ensures that two alignments contain the same set of sequences.
        /// </summary>
        /// <param name="alignment1">The first alignment.</param>
        /// <param name="alignment2">The second alignment.</param>
        /// <param name="removedFrom1">When this method returns, this variable will contain a list of the sequences that were present in the first alignment but not in the second (and were thus removed).</param>
        /// <param name="removedFrom2">When this method returns, this variable will contain a list of the sequences that were present in the second alignment but not in the first (and were thus removed).</param>
        /// <returns>A tuple of subsetted alignments containing only the sequence in common between <paramref name="alignment1"/> and <paramref name="alignment2"/>.</returns>
        public static (Alignment, Alignment) MakeConsistent(Alignment alignment1, Alignment alignment2, out string[] removedFrom1, out string[] removedFrom2)
        {
            removedFrom1 = alignment1.SequenceNames.Where(x => !alignment2.SequenceNameIndices.ContainsKey(x)).ToArray();
            removedFrom2 = alignment2.SequenceNames.Where(x => !alignment1.SequenceNameIndices.ContainsKey(x)).ToArray();
            string[] inBoth = alignment1.SequenceNames.Where(alignment2.SequenceNameIndices.ContainsKey).ToArray();

            Alignment tbr1 = alignment1.Subset(inBoth);
            Alignment tbr2 = alignment2.Subset(inBoth);

            return (tbr1, tbr2);
        }

        /// <summary>
        /// Create a bootstrap replicate of this alignment, by randomly resampling alignment rows.
        /// </summary>
        /// <remarks>Note that this is different from the bootstrap normally performed during phylogenetic analyses, as in that case columns are resampled instead.</remarks>
        /// <param name="random">The random number generator used to resampled the alignment rows. If this is <see langword="null"/>, a new <see cref="Random"/> is created.</param>
        /// <returns>A bootstrapped alignment.</returns>
        public Alignment Bootstrap(Random random = null)
        {
            random ??= new Random();

            return Alignment.Create(this.SequenceCount, Enumerable.Range(0, this.SequenceCount).Select(_ => { int ind = random.Next(0, this.SequenceCount); return (this.SequenceNames[ind], this.GetSequence(ind)); }), this is DNAAlignment ? AlignmentType.DNA : this is ProteinAlignment ? AlignmentType.Protein : AlignmentType.Autodetect);
        }

        /// <summary>
        /// Filter the alignment by applying the specified <paramref name="mask"/> to it.
        /// </summary>
        /// <param name="mask">The mask used to filter the alignment.</param>
        /// <returns>The filtered alignment.</returns>
        public abstract Alignment Filter(Mask mask);

        /// <summary>
        /// Create a new alignment reading data from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="alignmentStream">The <see cref="Stream"/> from which to read data.</param>
        /// <param name="format">When this method returns, this variable will contain the format of the alignment that has been read.</param>
        /// <param name="alignmentType">The type of alignment, or <see cref="AlignmentType.Autodetect"/> (the default) to detect automatically.</param>
        /// <param name="leaveOpen">Whether the <see cref="Stream"/> should be left open after the alignment has been read.</param>
        /// <returns>The <see cref="Alignment"/> that has been read from the <see cref="Stream"/>.</returns>
        public static Alignment FromStream(Stream alignmentStream, out AlignmentFileFormat format, AlignmentType alignmentType = AlignmentType.Autodetect, bool leaveOpen = false)
        {
            AlignmentFileFormat? formatTbr = null;
            Alignment tbr = FormatUtilities.ReadAlignment(alignmentStream, ref formatTbr, alignmentType, leaveOpen);
            format = formatTbr.Value;
            return tbr;
        }

        /// <summary>
        /// Create a new alignment reading data from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="alignmentStream">The <see cref="Stream"/> from which to read data.</param>
        /// <param name="format">The alignment file format (e.g., FASTA or PHYLIP).</param>
        /// <param name="alignmentType">The type of alignment, or <see cref="AlignmentType.Autodetect"/> (the default) to detect automatically.</param>
        /// <param name="leaveOpen">Whether the <see cref="Stream"/> should be left open after the alignment has been read.</param>
        /// <returns>The <see cref="Alignment"/> that has been read from the <see cref="Stream"/>.</returns>
        public static Alignment FromStream(Stream alignmentStream, AlignmentFileFormat format, AlignmentType alignmentType = AlignmentType.Autodetect, bool leaveOpen = false)
        {
            AlignmentFileFormat? formatTbr = format;
            Alignment tbr = FormatUtilities.ReadAlignment(alignmentStream, ref formatTbr, alignmentType, leaveOpen);
            return tbr;
        }

        /// <summary>
        /// Create a new alignment reading data from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="alignmentStream">The <see cref="Stream"/> from which to read data.</param>
        /// <param name="alignmentType">The type of alignment, or <see cref="AlignmentType.Autodetect"/> (the default) to detect automatically.</param>
        /// <param name="leaveOpen">Whether the <see cref="Stream"/> should be left open after the alignment has been read.</param>
        /// <returns>The <see cref="Alignment"/> that has been read from the <see cref="Stream"/>.</returns>
        public static Alignment FromStream(Stream alignmentStream, AlignmentType alignmentType = AlignmentType.Autodetect, bool leaveOpen = false) => FromStream(alignmentStream, out _, alignmentType, leaveOpen);

        /// <summary>
        /// Create a new alignment reading data from a file.
        /// </summary>
        /// <param name="alignmentFile">The file containing the alignment.</param>
        /// <param name="format">When this method returns, this variable will contain the format of the alignment that has been read.</param>
        /// <param name="alignmentType">The type of alignment, or <see cref="AlignmentType.Autodetect"/> (the default) to detect automatically.</param>
        /// <returns>The <see cref="Alignment"/> that has been read from the <see cref="Stream"/>.</returns>
        public static Alignment FromFile(string alignmentFile, out AlignmentFileFormat format, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            AlignmentFileFormat? formatTbr = null;
            Alignment tbr = FormatUtilities.ReadAlignment(alignmentFile, ref formatTbr, alignmentType);
            format = formatTbr.Value;
            return tbr;
        }

        /// <summary>
        /// Create a new alignment reading data from a file.
        /// </summary>
        /// <param name="alignmentFile">The file containing the alignment.</param>
        /// <param name="format">The alignment file format (e.g., FASTA or PHYLIP).</param>
        /// <param name="alignmentType">The type of alignment, or <see cref="AlignmentType.Autodetect"/> (the default) to detect automatically.</param>
        /// <returns>The <see cref="Alignment"/> that has been read from the <see cref="Stream"/>.</returns>
        public static Alignment FromFile(string alignmentFile, AlignmentFileFormat format, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            AlignmentFileFormat? formatTbr = format;
            Alignment tbr = FormatUtilities.ReadAlignment(alignmentFile, ref formatTbr, alignmentType);
            return tbr;
        }

        /// <summary>
        /// Create a new alignment reading data from a file.
        /// </summary>
        /// <param name="alignmentFile">The file containing the alignment.</param>
        /// <param name="alignmentType">The type of alignment, or <see cref="AlignmentType.Autodetect"/> (the default) to detect automatically.</param>
        /// <returns>The <see cref="Alignment"/> that has been read from the <see cref="Stream"/>.</returns>
        public static Alignment FromFile(string alignmentFile, AlignmentType alignmentType = AlignmentType.Autodetect) => FromFile(alignmentFile, out _, alignmentType);

        /// <summary>
        /// Save the <see cref="Alignment"/> to a <see cref="Stream"/> in the specified format.
        /// </summary>
        /// <param name="outputStream">The output <see cref="Stream"/>.</param>
        /// <param name="outputFormat">The output format.</param>
        public void Save(Stream outputStream, AlignmentFileFormat outputFormat = AlignmentFileFormat.FASTA)
        {
            FormatUtilities.GetAlignmentFormat(outputFormat).Write(outputStream, this);
        }

        /// <summary>
        /// Save the <see cref="Alignment"/> to a file in the specified format.
        /// </summary>
        /// <param name="outputFile">The path to the output file.</param>
        /// <param name="outputFormat">The output format.</param>
        public void Save(string outputFile, AlignmentFileFormat outputFormat = AlignmentFileFormat.FASTA)
        {
            FormatUtilities.GetAlignmentFormat(outputFormat).Write(outputFile, this);
        }
    }

    /// <summary>
    /// Represents a DNA sequence alignment.
    /// </summary>
    public class DNAAlignment : Alignment
    {
        /// <summary>
        /// The nucleotide characters.
        /// </summary>
        public static readonly char[] Nucleotides = new char[5] { 'A', 'C', 'G', 'T', 'U' };

        /// <inheritdoc/>
        public override char[] Characters => Nucleotides;

        private DNAAlignment(int sequenceCount, int alignmentLength) : base(sequenceCount, alignmentLength)
        {
            this.Tags["AlignmentType"] = AlignmentType.DNA;
        }

        /// <summary>
        /// Create a new <see cref="DNAAlignment"/>.
        /// </summary>
        /// <param name="sequences">The sequences in the alignment.</param>
        public DNAAlignment(IReadOnlyList<(string name, IEnumerable<char> sequence)> sequences) : base(sequences)
        {
            this.Tags["AlignmentType"] = AlignmentType.DNA;
        }

        /// <summary>
        /// Create a new <see cref="DNAAlignment"/>.
        /// </summary>
        /// <param name="sequences">The sequences in the alignment.</param>
        public DNAAlignment(IReadOnlyList<KeyValuePair<string, IEnumerable<char>>> sequences) : base(sequences)
        {
            this.Tags["AlignmentType"] = AlignmentType.DNA;
        }

        /// <summary>
        /// Create a new <see cref="DNAAlignment"/>.
        /// </summary>
        /// <param name="sequenceCount">The number of sequences in the alignment.</param>
        /// <param name="sequences">The sequences in the alignment.</param>
        public DNAAlignment(int sequenceCount, IEnumerable<(string name, IEnumerable<char> sequence)> sequences) : base(sequenceCount, sequences)
        {
            this.Tags["AlignmentType"] = AlignmentType.DNA;
        }

        /// <summary>
        /// Create a new <see cref="DNAAlignment"/>.
        /// </summary>
        /// <param name="sequenceCount">The number of sequences in the alignment.</param>
        /// <param name="sequences">The sequences in the alignment.</param>
        public DNAAlignment(int sequenceCount, IEnumerable<KeyValuePair<string, IEnumerable<char>>> sequences) : base(sequenceCount, sequences)
        {
            this.Tags["AlignmentType"] = AlignmentType.DNA;
        }

        /// <inheritdoc/>
        public override DNAAlignment Clone()
        {
            DNAAlignment clone = new DNAAlignment(this.SequenceCount, this.AlignmentLength);

            for (int i = 0; i < this.SequenceCount; i++)
            {
                clone.SequenceNames[i] = this.SequenceNames[i];
                ((Dictionary<string, int>)clone.SequenceNameIndices)[this.SequenceNames[i]] = i;
            }

            for (int i = 0; i < this.AlignmentData.Length; i++)
            {
                clone.AlignmentData[i] = this.AlignmentData[i];
            }

            foreach (KeyValuePair<string, object> kvp in this.Tags)
            {
                if (kvp.Value is ICloneable cloneable)
                {
                    clone.Tags[kvp.Key] = cloneable.Clone();
                }
                else
                {
                    clone.Tags[kvp.Key] = kvp.Value;
                }
            }

            return clone;
        }

        /// <inheritdoc/>
        public override DNAAlignment RemoveSequences(IEnumerable<int> sequencesToRemove)
        {
            HashSet<int> actualSequencesToRemove = new HashSet<int>(sequencesToRemove);

            return new DNAAlignment(this.SequenceCount - actualSequencesToRemove.Count, this.SequenceNames.Select((x, i) => (x, i)).Where(x => !actualSequencesToRemove.Contains(x.i)).Select(x => (x.x, this.GetSequence(x.i))));
        }

        /// <inheritdoc/>
        public override DNAAlignment Subset(IReadOnlyList<int> sequenceIndices)
        {
            DNAAlignment tbr = new DNAAlignment(sequenceIndices.Count, this.AlignmentLength);

            for (int i = 0; i < sequenceIndices.Count; i++)
            {
                tbr.SequenceNames[i] = this.SequenceNames[sequenceIndices[i]];
                ((Dictionary<string, int>)tbr.SequenceNameIndices)[this.SequenceNames[sequenceIndices[i]]] = i;
                for (int j = 0; j < this.AlignmentLength; j++)
                {
                    tbr.AlignmentData[i * this.AlignmentLength + j] = this.AlignmentData[sequenceIndices[i] * this.AlignmentLength + j];
                }
            }

            return tbr;
        }

        /// <inheritdoc/>
        public override DNAAlignment Filter(Mask mask)
        {
            List<int> maskedPositions = new List<int>();

            for (int i = 0; i < mask.Length; i++)
            {
                if (mask.MaskedStates[i])
                {
                    maskedPositions.Add(i);
                }
            }

            DNAAlignment tbr = new DNAAlignment(this.SequenceCount, Enumerable.Range(0, this.SequenceCount).Select(x => (this.SequenceNames[x], this.GetSequence(x).ElementsAt(maskedPositions))));

            return tbr;
        }
    }

    /// <summary>
    /// Represents a protein sequence alignment.
    /// </summary>
    public class ProteinAlignment : Alignment
    {
        /// <summary>
        /// The amino acid characters.
        /// </summary>
        public static readonly char[] AminoAcids = new char[22] { 'A', 'R', 'N', 'D', 'C', 'Q', 'E', 'G', 'H', 'I', 'L', 'K', 'M', 'F', 'P', 'S', 'T', 'W', 'Y', 'V', 'O', 'U' };

        /// <inheritdoc/>
        public override char[] Characters => AminoAcids;

        private ProteinAlignment(int sequenceCount, int alignmentLength) : base(sequenceCount, alignmentLength)
        {
            this.Tags["AlignmentType"] = AlignmentType.Protein;
        }

        /// <summary>
        /// Create a new <see cref="ProteinAlignment"/>.
        /// </summary>
        /// <param name="sequences">The sequences in the alignment.</param>
        public ProteinAlignment(IReadOnlyList<(string name, IEnumerable<char> sequence)> sequences) : base(sequences)
        {
            this.Tags["AlignmentType"] = AlignmentType.Protein;
        }

        /// <summary>
        /// Create a new <see cref="ProteinAlignment"/>.
        /// </summary>
        /// <param name="sequences">The sequences in the alignment.</param>
        public ProteinAlignment(IReadOnlyList<KeyValuePair<string, IEnumerable<char>>> sequences) : base(sequences)
        {
            this.Tags["AlignmentType"] = AlignmentType.Protein;
        }


        /// <summary>
        /// Create a new <see cref="ProteinAlignment"/>.
        /// </summary>
        /// <param name="sequenceCount">The number of sequences in the alignment.</param>
        /// <param name="sequences">The sequences in the alignment.</param>
        public ProteinAlignment(int sequenceCount, IEnumerable<(string name, IEnumerable<char> sequence)> sequences) : base(sequenceCount, sequences)
        {
            this.Tags["AlignmentType"] = AlignmentType.Protein;
        }

        /// <summary>
        /// Create a new <see cref="ProteinAlignment"/>.
        /// </summary>
        /// <param name="sequenceCount">The number of sequences in the alignment.</param>
        /// <param name="sequences">The sequences in the alignment.</param>
        public ProteinAlignment(int sequenceCount, IEnumerable<KeyValuePair<string, IEnumerable<char>>> sequences) : base(sequenceCount, sequences)
        {
            this.Tags["AlignmentType"] = AlignmentType.Protein;
        }

        /// <inheritdoc/>
        public override ProteinAlignment Clone()
        {
            ProteinAlignment clone = new ProteinAlignment(this.SequenceCount, this.AlignmentLength);

            for (int i = 0; i < this.SequenceCount; i++)
            {
                clone.SequenceNames[i] = this.SequenceNames[i];
                ((Dictionary<string, int>)clone.SequenceNameIndices)[this.SequenceNames[i]] = i;
            }

            for (int i = 0; i < this.AlignmentData.Length; i++)
            {
                clone.AlignmentData[i] = this.AlignmentData[i];
            }

            foreach (KeyValuePair<string, object> kvp in this.Tags)
            {
                if (kvp.Value is ICloneable cloneable)
                {
                    clone.Tags[kvp.Key] = cloneable.Clone();
                }
                else
                {
                    clone.Tags[kvp.Key] = kvp.Value;
                }
            }

            return clone;
        }

        /// <inheritdoc/>
        public override ProteinAlignment RemoveSequences(IEnumerable<int> sequencesToRemove)
        {
            HashSet<int> actualSequencesToRemove = new HashSet<int>(sequencesToRemove);

            return new ProteinAlignment(this.SequenceCount - actualSequencesToRemove.Count, this.SequenceNames.Select((x, i) => (x, i)).Where(x => !actualSequencesToRemove.Contains(x.i)).Select(x => (x.x, this.GetSequence(x.i))));
        }

        /// <inheritdoc/>
        public override Alignment Subset(IReadOnlyList<int> sequenceIndices)
        {
            ProteinAlignment tbr = new ProteinAlignment(sequenceIndices.Count, this.AlignmentLength);

            for (int i = 0; i < sequenceIndices.Count; i++)
            {
                tbr.SequenceNames[i] = this.SequenceNames[sequenceIndices[i]];
                ((Dictionary<string, int>)tbr.SequenceNameIndices)[this.SequenceNames[sequenceIndices[i]]] = i;
                for (int j = 0; j < this.AlignmentLength; j++)
                {
                    tbr.AlignmentData[i * this.AlignmentLength + j] = this.AlignmentData[sequenceIndices[i] * this.AlignmentLength + j];
                }
            }

            return tbr;
        }

        /// <inheritdoc/>
        public override ProteinAlignment Filter(Mask mask)
        {
            List<int> maskedPositions = new List<int>();

            for (int i = 0; i < mask.Length; i++)
            {
                if (mask.MaskedStates[i])
                {
                    maskedPositions.Add(i);
                }
            }

            ProteinAlignment tbr = new ProteinAlignment(this.SequenceCount, Enumerable.Range(0, this.SequenceCount).Select(x => (this.SequenceNames[x], this.GetSequence(x).ElementsAt(maskedPositions))));

            return tbr;
        }
    }
}
