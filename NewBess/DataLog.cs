using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewBess
{
    public class DataLog
    {
        public double elapsedTime { get; set; }
        public float CX { get; set; }
        public float CY { get; set; }
        public float CZ { get; set; } 

        public DataLog(double time, float x, float y, float z)
        {
            elapsedTime = time;
            CX = x;
            CY = y;
            CZ = z;
        }
    }
}
