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

namespace AliFilter.AlignmentFormatting
{
    /// <summary>
    /// An interface that describes a class that can read and write alignments in a certain format.
    /// </summary>
    public abstract class AlignmentFormatter
    {
        /// <summary>
        /// The alignment format supported by the class implementing this interface.
        /// </summary>
        public abstract AlignmentFileFormat Format { get; }

        /// <summary>
        /// Determines whether an alignment is in a format supported by the class implementing this interface.
        /// </summary>
        /// <param name="input">A <see cref="TextReader"/> from which the alignment is read.</param>
        /// <returns><see langword="true" /> if the alignment is in a format supported by the class implementing this interface, <see langword="false"/> otherwise.</returns>
        public abstract bool Is(TextReader input);

        /// <summary>
        /// Determines whether an alignment is in a format supported by the class implementing this interface.
        /// </summary>
        /// <param name="input">A <see cref="Stream"/> from which the alignment is read.</param>
        /// <returns><see langword="true" /> if the alignment is in a format supported by the class implementing this interface, <see langword="false"/> otherwise.</returns>
        public virtual bool Is(Stream input)
        {
            using (StreamReader reader = new StreamReader(input, leaveOpen: true))
            {
                return Is(reader);
            }
        }

        /// <summary>
        /// Determines whether an alignment is in a format supported by the class implementing this interface.
        /// </summary>
        /// <param name="fileName">The path to the file from which the alignment is read.</param>
        /// <returns><see langword="true" /> if the alignment is in a format supported by the class implementing this interface, <see langword="false"/> otherwise.</returns>
        public virtual bool Is(string fileName)
        {
            using (FileStream input = File.OpenRead(fileName))
            {
                return Is(input);
            }
        }

        /// <summary>
        /// Reads an alignment from a <see cref="TextReader"/> in the format supported by the class implementing this interface.
        /// </summary>
        /// <param name="input">A <see cref="TextReader"/> from which the alignment is read.</param>
        /// <param name="alignmentType">The alignment type.</param>
        /// <returns>An <see cref="Alignment"/> containing the sequences read from the <see cref="TextReader"/>.</returns>
        public abstract Alignment Read(TextReader input, AlignmentType alignmentType = AlignmentType.Autodetect);


        /// <summary>
        /// Reads an alignment from a <see cref="Stream"/> in the format supported by the class implementing this interface.
        /// </summary>
        /// <param name="input">A <see cref="Stream"/> from which the alignment is read.</param>
        /// <param name="alignmentType">The alignment type.</param>
        /// <returns>An <see cref="Alignment"/> containing the sequences read from the <see cref="TextReader"/>.</returns>
        public virtual Alignment Read(Stream input, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            using (StreamReader reader = new StreamReader(input))
            {
                return this.Read(reader, alignmentType);
            }
        }

        /// <summary>
        /// Reads an alignment from a file in the format supported by the class implementing this interface.
        /// </summary>
        /// <param name="fileName">The path to the file from which the alignment is read.</param>
        /// <param name="alignmentType">The alignment type.</param>
        /// <returns>An <see cref="Alignment"/> containing the sequences read from the <see cref="TextReader"/>.</returns>
        public virtual Alignment Read(string fileName, AlignmentType alignmentType = AlignmentType.Autodetect)
        {
            using (Stream input = File.OpenRead(fileName))
            {
                return this.Read(input, alignmentType);
            }
        }

        /// <summary>
        /// Saves an <see cref="Alignment"/> in the format supported by the class implementing this interface.
        /// </summary>
        /// <param name="output">A <see cref="TextWriter"/> on which the <paramref name="alignment"/> is written.</param>
        /// <param name="alignment">The <see cref="Alignment"/> to save.</param>
        public abstract void Write(TextWriter output, Alignment alignment);

        /// <summary>
        /// Saves an <see cref="Alignment"/> in the format supported by the class implementing this interface.
        /// </summary>
        /// <param name="output">A <see cref="Stream"/> on which the <paramref name="alignment"/> is written.</param>
        /// <param name="alignment">The <see cref="Alignment"/> to save.</param>
        public virtual void Write(Stream output, Alignment alignment)
        {
            using (StreamWriter writer = new StreamWriter(output))
            {
                this.Write(writer, alignment);
            }
        }

        /// <summary>
        /// Saves an <see cref="Alignment"/> in the format supported by the class implementing this interface.
        /// </summary>
        /// <param name="fileName">The path to the file on which the <paramref name="alignment"/> is written.</param>
        /// <param name="alignment">The <see cref="Alignment"/> to save.</param>
        public virtual void Write(string fileName, Alignment alignment)
        {
            using (Stream output = File.Create(fileName))
            {
                this.Write(output, alignment);
            }
        }
    }
}
