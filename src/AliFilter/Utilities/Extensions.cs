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

namespace AliFilter
{
    internal static partial class Utilities
    {
        /// <summary>
        /// Obtain the elements at the specified indices in the <paramref name="enumerable"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the <paramref name="enumerable"/>.</typeparam>
        /// <param name="enumerable">The collection of items from which the elements will be taken.</param>
        /// <param name="indices">The indices of the elements to take. This collection should be sorted in ascending order.</param>
        /// <returns>The elements with the specified indices.</returns>
        public static IEnumerable<T> ElementsAt<T>(this IEnumerable<T> enumerable, IEnumerable<int> indices)
        {
            IEnumerator<int> enumerator = indices.GetEnumerator();
            bool keepGoing = enumerator.MoveNext();

            int index = 0;

            foreach (T t in enumerable)
            {
                if (!keepGoing)
                {
                    yield break;
                }

                if (index == enumerator.Current)
                {
                    yield return t;
                    keepGoing = enumerator.MoveNext();
                }

                index++;
            }

            if (keepGoing)
            {
                throw new IndexOutOfRangeException();
            }
        }
    }
}
