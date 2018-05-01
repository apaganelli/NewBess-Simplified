using Microsoft.Kinect;
using System;

namespace NewBess
{
    class DTW
    {
        CameraSpacePoint[] x, y;
        double[,] dist;
        double[,] cost;

        public double minCost { get; set; } 
        public int stepsH { get; set; }
        public int stepsV { get; set; }
        public int stepsD { get; set; }

        int lenX;
        int lenY;

        public DTW(CameraSpacePoint[] p_x, CameraSpacePoint[] p_y)
        {
            lenX = p_x.Length;
            lenY = p_y.Length;

            dist = new double[lenX, lenY];
            cost = new double[lenX, lenY];

            // x = new CameraSpacePoint[lenX];
            // y = new CameraSpacePoint[lenY];

            x = p_x;
            y = p_y;

            BuildCostMatrix(p_x, p_y, cost);
            BuildDistanceMatrix(cost, dist);
            minCost = FindMinimalCost(dist);
        }


        public DTW(double[] x, double[] y)
        {
            lenX = x.Length;
            lenY = y.Length;

            dist = new double[lenX, lenY];
            cost = new double[lenX, lenY];

            BuildCostMatrix(x, y, cost);
            BuildDistanceMatrix(cost, dist);
            minCost = FindMinimalCost(dist);
        }

        public void BuildCostMatrix(double[] x, double[] y, double[,] matrix)
        {
            for (int i = 0; i < lenX; i++)
            {
                for (int j = 0; j < lenY; j++)
                {
                    matrix[i, j] = Math.Abs(x[i] - y[j]);
                }
            }
        }

        public void BuildCostMatrix(CameraSpacePoint[] x, CameraSpacePoint[] y, double[,] matrix)
        {
            for (int i = 0; i < lenX; i++)
            {
                for (int j = 0; j < lenY; j++)
                {
                    matrix[i, j] = EuclideanDistance3d(x[i], y[j]);
                }
            }
        }


        public void BuildDistanceMatrix(double[,] c, double[,] d)
        {
            d[0, 0] = c[0, 0];

            for (int i = 1; i < lenY; i++)
            {
                d[0, i] = d[0, i - 1] + c[0, i];
            }

            for (int j = 1; j < lenX; j++)
            {
                d[j, 0] = d[j - 1, 0] + c[j, 0];
            }

            for (int i = 1; i < lenX; i++)
            {
                for (int j = 1; j < lenY; j++)
                {
                    d[i, j] = Math.Min(d[i - 1, j - 1], Math.Min(d[i - 1, j], d[i, j - 1])) + c[i, j];
                }
            }
        }

        public double FindMinimalCost(double[,] d)
        {
            int i = lenX - 1;
            int j = lenY - 1;

            double sum = d[i, j];
            stepsH = stepsV = stepsD = 0;

            while (i > 0 || j > 0)
            {
                if (i == 0)
                {
                    sum += d[i, j - 1];
                    stepsH++;
                    j--;
                }
                else if (j == 0)
                {
                    sum += d[i - 1, j];
                    stepsV++;
                    i--;
                }
                else if (d[i - 1, j - 1] < Math.Min(d[i - 1, j], d[i, j - 1]))
                {
                    sum += d[i - 1, j - 1];
                    stepsD++;
                    i--;
                    j--;
                }
                else if (d[i - 1, j] < d[i, j - 1])
                {
                    sum += d[i - 1, j];
                    i--;
                    stepsV++;
                }
                else
                {
                    sum += d[i, j - 1];
                    j--;
                    stepsH++;
                }
            }

            return sum;
        }

        /// <summary>
        /// Returns the Euclidean distante betweeen two 3D vectors p1 - p2.
        /// </summary>
        /// <param name="p1">First 3D vector</param>
        /// <param name="p2">Second 3D vector</param>
        /// <returns></returns>
        public double EuclideanDistance3d(CameraSpacePoint p1, CameraSpacePoint p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
        }
    }
}
