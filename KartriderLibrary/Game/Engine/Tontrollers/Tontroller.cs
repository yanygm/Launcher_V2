using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Tontrollers
{
    public class Tontroller : KartObject
    {
        public override string ClassName => "Tontroller";

        private int u1;
        public int u2;
        private int u3;
        private int u4;
        private int u5;
        private int u6;
        public int startTime;
        public int endTime;

        public Tontroller()
        {

        }

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            u1 = reader.ReadInt32();
            u2 = reader.ReadInt32();
            u3 = reader.ReadInt32();
            u4 = reader.ReadInt32();
            u5 = reader.ReadInt32();
            u6 = reader.ReadInt32();
            startTime = reader.ReadInt32();
            endTime = reader.ReadInt32();
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }
    }
}
