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

using AliFilter.Models;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AliFilter
{
    internal static partial class Utilities
    {
        // Read a model from a stream.
        internal static T ReadModel<T>(Stream modelStream) where T : FullModel
        {
            return JsonSerializer.Deserialize<T>(modelStream, (JsonTypeInfo<T>)ModelSerializerContext.Default.GetTypeInfo(typeof(T)));
        }

        // Read a model from a file.
        internal static T ReadModel<T>(string modelFile) where T : FullModel
        {
            using (FileStream fs = File.OpenRead(modelFile))
            {
                return ReadModel<T>(fs);
            }
        }

        // Read a model from a file.
        internal static T ReadModel<T>(Arguments arguments, TextWriter outputLog) where T : FullModel
        {
            outputLog?.WriteLine("Reading model from file " + Path.GetFullPath(arguments.InputModel) + "...");
            return ReadModel<T>(arguments.InputModel);
        }

        // Read a model or a mask from a file.
        internal static object ReadModelOrMask<T>(Arguments arguments, TextWriter outputLog) where T : FullModel
        {
            outputLog?.WriteLine("Reading model from file " + Path.GetFullPath(arguments.InputModel) + "...");

            using (FileStream fs = File.OpenRead(arguments.InputModel))
            {
                bool json = false;

                using (StreamReader sr = new StreamReader(fs, leaveOpen: true))
                {
                    int read = sr.Read();

                    while (read > 0 && char.IsWhiteSpace((char)read))
                    {
                        read = sr.Read();
                    }

                    if (read < 0)
                    {
                        outputLog?.WriteLine("The file does not contain a valid model or mask!");
                        return null;
                    }
                    else if ((char)read == '{')
                    {
                        json = true;
                    }
                    else
                    {
                        json = false;
                    }
                }

                fs.Seek(0, SeekOrigin.Begin);

                if (json)
                {
                    return JsonSerializer.Deserialize<T>(fs, (JsonTypeInfo<T>)ModelSerializerContext.Default.GetTypeInfo(typeof(T)));
                }
                else
                {
                    using (StreamReader sr2 = new StreamReader(fs))
                    {
                        string maskString = new string(sr2.ReadToEnd().Where(x => !char.IsWhiteSpace(x)).ToArray());
                        return new Mask(maskString);
                    }
                }
            }
        }
    }
}
