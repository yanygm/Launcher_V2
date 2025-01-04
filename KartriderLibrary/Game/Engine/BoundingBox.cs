using KartLibrary.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine
{
    public struct BoundingBox
    {
        public Vector3 MinPosition { get; set; }

        public Vector3 MaxPosition { get; set; }

        public BoundingBox(Vector3 minPos, Vector3 maxPos)
        {
            MinPosition = minPos;
            MaxPosition = maxPos;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<BoundingBox>");
            stringBuilder.ConstructPropertyString(1, "MinPosition", MinPosition);
            stringBuilder.ConstructPropertyString(1, "MaxPosition", MaxPosition);
            stringBuilder.AppendLine("</BoundingBox>");
            return stringBuilder.ToString();
        }
    }
}
