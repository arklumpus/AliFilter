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
    /// <summary>
    /// Draws a progress bar.
    /// </summary>
    internal class ProgressBar
    {
        /// <summary>
        /// The <see cref="TextWriter"/> on which the progress bar is drawn (e.g., the standard output).
        /// </summary>
        private TextWriter Output { get; }

        /// <summary>
        /// The last drawn progress value.
        /// </summary>
        private int LastProgress { get; set; } = 0;

        /// <summary>
        /// Create a new <see cref="ProgressBar"/> writing on the specified <paramref name="output"/> <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="output">The <see cref="TextWriter"/> on which the progress bar will be drawn (e.g., the standard output).</param>
        public ProgressBar(TextWriter output)
        {
            this.Output = output;
        }

        /// <summary>
        /// Initialise the progress bar.
        /// </summary>
        public void Start()
        {
            Output.WriteLine("  0% ┌─────── 20% ───── 40% ───── 60% ───── 80% ──────┐ 100%");
            LastProgress = 0;

            Output.Write("     ");
            Output.Flush();
        }

        /// <summary>
        /// Update the current progress value.
        /// </summary>
        /// <param name="progress">The current progress, ranging from 0 to 1.</param>
        public void Progress(double progress)
        {
            lock (this)
            {
                int currProgress = (int)Math.Round(progress * 50);

                if (currProgress > LastProgress)
                {
                    if (LastProgress == 0)
                    {
                        Output.Write("╘");
                        Output.Write(new string('═', currProgress - LastProgress - 1));
                    }
                    else
                    {
                        Output.Write(new string('═', currProgress - LastProgress));
                    }


                    Output.Flush();

                    LastProgress = currProgress;
                }
            }
        }

        /// <summary>
        /// Complete the progress bar.
        /// </summary>
        public void Finish()
        {
            Progress(1);
            Output.Write("\b╛");
            Output.WriteLine();
            Output.WriteLine();
        }
    }
}
