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

using System.Text;
using VectSharp.SVG;
using VectSharp;
using VectSharp.Plots;

namespace AliFilter
{
    internal static class PlotUtilities
    {
        /// <summary>
        /// Pad a string to the specified length, adding whitespace both to the left and to the right.
        /// </summary>
        /// <param name="str">The string to pad.</param>
        /// <param name="length">The target length.</param>
        /// <returns>The padded string.</returns>
        public static string PadCenter(this string str, int length)
        {
            return str.PadLeft(str.Length + (length - str.Length) / 2, ' ').PadRight(length, ' ');
        }


        /// <summary>
        /// Ordinal days of the month.
        /// </summary>
        internal static Dictionary<int, string> DaysOfMonth = new Dictionary<int, string>()
        {
            { 1, "1^st^" },
            { 2, "2^nd^" },
            { 3, "3^rd^" },
            { 21, "21^st^" },
            { 22, "22^nd^" },
            { 23, "23^rd^" },
            { 31, "31^st^" },
        };

        /// <summary>
        /// Inserts a figure in a Markdown document.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> where the MarkDown document is being built.</param>
        /// <param name="figure">The figure to insert.</param>
        /// <param name="attributes">Attributes for the figure.</param>
        internal static void InsertFigure(StringBuilder builder, Page figure, string attributes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                figure.SaveAsSVG(ms, SVGContextInterpreter.TextOptions.DoNotEmbed);

                builder.Append("<img " + attributes + " src=\"data:image/svg+xml;base64,");

                builder.Append(Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length));
                builder.Append("\">");
            }
        }

        /// <summary>
        /// Iteratively refines the position of a set of points, relative to a set of attractors (while the points repel each other). Used to place the component loading labels on the plot. 
        /// </summary>
        /// <param name="startingPoints">The initial positions of the points.</param>
        /// <param name="attractors">The attractors (one for each point).</param>
        /// <param name="origin">The centre of the points.</param>
        /// <param name="attractorDistance">The target distance from the attractor.</param>
        /// <param name="repulsorDistance">The target distance between two points.</param>
        /// <param name="iterations">The number of iterations.</param>
        /// <returns>The refined point positions.</returns>
        internal static Point[] RefinePositions(IReadOnlyList<Point> startingPoints, IReadOnlyList<Point> attractors, Point origin, double attractorDistance, double repulsorDistance, int iterations)
        {
            double attractionScale = 0.001;
            double repulsionScale = 0.1;
            double damping = 0.995;
            double dt = 0.1;

            Point[] positions = startingPoints.ToArray();
            Point[] speeds = new Point[startingPoints.Count];
            Point[] accelerations = new Point[startingPoints.Count];

            for (int iter = 0; iter < iterations; iter++)
            {
                double totalMovement = 0;

                for (int i = 0; i < positions.Length; i++)
                {
                    double accelX = 0;
                    double accelY = 0;

                    for (int j = 0; j < positions.Length; j++)
                    {
                        if (i != j)
                        {
                            Point direction = new Point(positions[i].X - positions[j].X, positions[i].Y - positions[j].Y);
                            double distance = direction.Modulus();

                            if (distance < repulsorDistance)
                            {
                                accelX += direction.X / (distance * distance) * repulsionScale;
                                accelY += direction.Y / (distance * distance) * repulsionScale;
                            }
                            else
                            {
                                accelX += direction.X / (distance * distance * distance) * repulsionScale;
                                accelY += direction.Y / (distance * distance * distance) * repulsionScale;
                            }
                        }
                    }

                    {
                        Point direction = new Point(positions[i].X - origin.X, positions[i].Y - origin.Y);
                        double distance = direction.Modulus();
                        accelX += direction.X / (distance * distance) * repulsionScale;
                        accelY += direction.Y / (distance * distance) * repulsionScale;
                    }

                    {
                        Point direction = new Point(positions[i].X - attractors[i].X, positions[i].Y - attractors[i].Y);
                        double distance = direction.Modulus();

                        accelX += -direction.X / distance * (distance - attractorDistance) * attractionScale;
                        accelY += -direction.Y / distance * (distance - attractorDistance) * attractionScale;
                    }

                    double dx = speeds[i].X * dt + accelerations[i].X * dt * dt * 0.5;
                    double dy = speeds[i].Y * dt + accelerations[i].Y * dt * dt * 0.5;

                    totalMovement += Math.Sqrt(dx * dx + dy * dy);

                    positions[i] = new Point(positions[i].X + dx, positions[i].Y + dy);
                    speeds[i] = new Point(speeds[i].X * damping + accelerations[i].X, speeds[i].Y * damping + accelerations[i].Y);
                    accelerations[i] = new Point(accelX, accelY);
                }
            }

            return positions;
        }
    }

    /// <summary>
    /// Sigmoid coordinate system for logistic model results.
    /// </summary>
    internal class SigmoidCoordinateSystem : IContinuousInvertibleCoordinateSystem
    {
        public bool IsLinear => false;

        public double[] Resolution { get; }

        private LinearCoordinateSystem1D xAxis { get; }
        private LinearCoordinateSystem1D xAxisInverse { get; }

        private LinearCoordinateSystem1D yAxis { get; }
        private LinearCoordinateSystem1D yAxisInverse { get; }

        private double minX { get; }

        public double Exponent { get; }

        public SigmoidCoordinateSystem(double minX, double maxX, double exponent, double scaleX = 350, double scaleY = 250)
        {
            this.xAxis = new LinearCoordinateSystem1D(minX, maxX, scaleX);
            this.xAxisInverse = new LinearCoordinateSystem1D(0, scaleX, maxX - minX);
            this.yAxis = new LinearCoordinateSystem1D(1, 0, scaleY);
            this.yAxisInverse = new LinearCoordinateSystem1D(scaleY, 0, 1);
            this.Exponent = exponent;
            this.minX = minX;
            this.Resolution = new double[] { (maxX - minX) * 0.01, 0.01 };
        }

        public double[] GetAround(IReadOnlyList<double> point, IReadOnlyList<double> direction)
        {
            return new double[] { point[0] + 0.01 * direction[0], point[1] + 0.01 * direction[1] };
        }

        public bool IsDirectionStraight(IReadOnlyList<double> direction)
        {
            return direction[0] == 0 || direction[1] == 0;
        }

        public double[] ToDataCoordinates(Point plotPoint)
        {
            double y = yAxisInverse.ToPlotCoordinates(plotPoint.Y);

            if (y > 0 && y < 1)
            {
                y = 1.0 / (1.0 + Math.Pow(y / (1 - y), -Exponent));
            }

            return new double[] { minX + xAxisInverse.ToPlotCoordinates(plotPoint.X), y };
        }

        public Point ToPlotCoordinates(IReadOnlyList<double> dataPoint)
        {
            double y;

            if (dataPoint[1] <= 0 || dataPoint[1] >= 1)
            {
                y = yAxis.ToPlotCoordinates(dataPoint[1]);
            }
            else
            {
                y = yAxis.ToPlotCoordinates(1.0 / (1 + Math.Pow((1 - dataPoint[1]) / dataPoint[1], 1.0 / Exponent)));
            }


            return new Point(xAxis.ToPlotCoordinates(dataPoint[0]), y);
        }
    }
}
