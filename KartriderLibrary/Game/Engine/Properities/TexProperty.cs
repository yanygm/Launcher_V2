using KartLibrary.Game.Engine.Tontrollers;
using KartLibrary.IO;
using KartLibrary.Xml;
using KartLibrary.Game.Engine.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;
using System.IO;



namespace KartLibrary.Game.Engine.Properities
{
    [KartObjectImplement]
    public class TexProperty : KartObject
    {
        private D3DTextureOp TextureOp;
        private string _texName;
        private TextureAddressMode _addressU;
        private TextureAddressMode _addressV;

        private D3DTextureFilterType _minFilter;
        private D3DTextureFilterType _magFilter;
        private D3DTextureFilterType _mipFilter;

        private int _maxAnisotropy;

        private int u1;
        public string u3;
        private int u4;
        private int u5;
        private int u6;
        private int u7;
        private int u8;
        private int u9;
        private float u15;
        private BinaryXmlTag tmpTag;

        public FloatTontroller? uObj1;
        public FloatTontroller? uObj2;
        public FloatTontroller? uObj3;
        public FloatTontroller? uObj4;
        public FloatTontroller? uObj5;
        public FloatTontroller? AlphaTontroller { get; private set; } // FloatTontroller

        public override string ClassName => "TexProperty";

        public TexProperty()
        {

        }

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            u1 = reader.ReadInt32(); //colorOpAlphaOp, if == 1, colorOp = selectOp1, alphaOp = Mul, 
            if (reader.ReadByte() != 0)
            {
                u3 = reader.ReadField(decodedObjectMap, decodedFieldMap, (reader, decObjMap, decFieldMap) =>
                {
                    return reader.ReadKRString();
                });
            }
            u4 = reader.ReadInt32(); // ADDRESSU ee8110
            u5 = reader.ReadInt32(); // ADDRESSV
            u6 = reader.ReadInt32(); // MINFILTER if == 3, MAXANISOTROPY = u9
            u7 = reader.ReadInt32(); // MAGFILTER
            u8 = reader.ReadInt32(); // MIPFILTER
            u9 = reader.ReadInt32(); // MAXANISOTROPY
            if (reader.ReadByte() != 0)
                uObj1 = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() != 0)
                uObj2 = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() != 0)
                uObj3 = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() != 0)
                uObj4 = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() != 0)
                uObj5 = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
            u15 = reader.ReadSingle();
            if (reader.ReadByte() != 0)
                AlphaTontroller = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() != 0)
            {
                tmpTag = reader.ReadField<BinaryXmlTag>(decodedObjectMap, decodedFieldMap, (reader, decodedObjectMap, decodedFieldMap) =>
                {
                    return reader.ReadBinaryXmlTag(Encoding.Unicode);
                });
            }
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {

        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"<u1>{u1}({u1:x8})</u1>");
            stringBuilder.AppendLine($"<u3>{u3}</u3>");
            stringBuilder.AppendLine($"<u4>{u4}({u4:x8})</u4>");
            stringBuilder.AppendLine($"<u5>{u5}({u5:x8})</u5>");
            stringBuilder.AppendLine($"<u6>{u6}({u6:x8})</u6>");
            stringBuilder.AppendLine($"<u7>{u7}({u7:x8})</u7>");
            stringBuilder.AppendLine($"<u8>{u8}({u8:x8})</u8>");
            stringBuilder.AppendLine($"<u9>{u9}({u9:x8})</u9>");
            stringBuilder.AppendLine($"<u15>{u15}</u15>");
            stringBuilder.AppendLine($"<tmpTag>{tmpTag}</tmpTag>");

            stringBuilder.AppendLine($"<uObj1>{uObj1}</uObj1>");
            stringBuilder.AppendLine($"<uObj2>{uObj2}</uObj2>");
            stringBuilder.AppendLine($"<uObj3>{uObj3}</uObj3>");
            stringBuilder.AppendLine($"<uObj4>{uObj4}</uObj4>");
            stringBuilder.AppendLine($"<uObj5>{uObj5}</uObj5>");
            return stringBuilder.ToString();
        }
    }
}
