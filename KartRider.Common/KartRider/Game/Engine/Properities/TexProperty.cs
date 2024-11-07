using System.Collections.Generic;
using System.IO;
using System.Text;
using KartLibrary.Game.Engine.Enums;
using KartLibrary.Game.Engine.Tontrollers;
using KartLibrary.IO;
using KartLibrary.Xml;
using Vortice.Direct3D11;

namespace KartLibrary.Game.Engine.Properities;

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

    public FloatTontroller? AlphaTontroller { get; private set; }

    public override string ClassName => "TexProperty";

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        u1 = reader.ReadInt32();
        if (reader.ReadByte() != 0)
        {
            u3 = reader.ReadField(decodedObjectMap, decodedFieldMap, (BinaryReader reader, Dictionary<short, KartObject>? decObjMap, Dictionary<short, object>? decFieldMap) => reader.ReadKRString());
        }

        u4 = reader.ReadInt32();
        u5 = reader.ReadInt32();
        u6 = reader.ReadInt32();
        u7 = reader.ReadInt32();
        u8 = reader.ReadInt32();
        u9 = reader.ReadInt32();
        if (reader.ReadByte() != 0)
        {
            uObj1 = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() != 0)
        {
            uObj2 = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() != 0)
        {
            uObj3 = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() != 0)
        {
            uObj4 = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() != 0)
        {
            uObj5 = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
        }

        u15 = reader.ReadSingle();
        if (reader.ReadByte() != 0)
        {
            AlphaTontroller = reader.ReadKartObject<FloatTontroller>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() != 0)
        {
            tmpTag = reader.ReadField(decodedObjectMap, decodedFieldMap, (BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap) => reader.ReadBinaryXmlTag(Encoding.Unicode));
        }
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
    }

    public override string ToString()
    {
        StringBuilder stringBuilder;
        StringBuilder stringBuilder2 = (stringBuilder = new StringBuilder());
        StringBuilder stringBuilder3 = stringBuilder;
        StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder);
        handler.AppendLiteral("<u1>");
        handler.AppendFormatted(u1);
        handler.AppendLiteral("(");
        handler.AppendFormatted(u1, "x8");
        handler.AppendLiteral(")</u1>");
        stringBuilder3.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder4 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder);
        handler.AppendLiteral("<u3>");
        handler.AppendFormatted(u3);
        handler.AppendLiteral("</u3>");
        stringBuilder4.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder5 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder);
        handler.AppendLiteral("<u4>");
        handler.AppendFormatted(u4);
        handler.AppendLiteral("(");
        handler.AppendFormatted(u4, "x8");
        handler.AppendLiteral(")</u4>");
        stringBuilder5.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder6 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder);
        handler.AppendLiteral("<u5>");
        handler.AppendFormatted(u5);
        handler.AppendLiteral("(");
        handler.AppendFormatted(u5, "x8");
        handler.AppendLiteral(")</u5>");
        stringBuilder6.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder7 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder);
        handler.AppendLiteral("<u6>");
        handler.AppendFormatted(u6);
        handler.AppendLiteral("(");
        handler.AppendFormatted(u6, "x8");
        handler.AppendLiteral(")</u6>");
        stringBuilder7.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder8 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder);
        handler.AppendLiteral("<u7>");
        handler.AppendFormatted(u7);
        handler.AppendLiteral("(");
        handler.AppendFormatted(u7, "x8");
        handler.AppendLiteral(")</u7>");
        stringBuilder8.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder9 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder);
        handler.AppendLiteral("<u8>");
        handler.AppendFormatted(u8);
        handler.AppendLiteral("(");
        handler.AppendFormatted(u8, "x8");
        handler.AppendLiteral(")</u8>");
        stringBuilder9.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder10 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder);
        handler.AppendLiteral("<u9>");
        handler.AppendFormatted(u9);
        handler.AppendLiteral("(");
        handler.AppendFormatted(u9, "x8");
        handler.AppendLiteral(")</u9>");
        stringBuilder10.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder11 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder);
        handler.AppendLiteral("<u15>");
        handler.AppendFormatted(u15);
        handler.AppendLiteral("</u15>");
        stringBuilder11.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder12 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder);
        handler.AppendLiteral("<tmpTag>");
        handler.AppendFormatted(tmpTag);
        handler.AppendLiteral("</tmpTag>");
        stringBuilder12.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder13 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder);
        handler.AppendLiteral("<uObj1>");
        handler.AppendFormatted(uObj1);
        handler.AppendLiteral("</uObj1>");
        stringBuilder13.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder14 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder);
        handler.AppendLiteral("<uObj2>");
        handler.AppendFormatted(uObj2);
        handler.AppendLiteral("</uObj2>");
        stringBuilder14.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder15 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder);
        handler.AppendLiteral("<uObj3>");
        handler.AppendFormatted(uObj3);
        handler.AppendLiteral("</uObj3>");
        stringBuilder15.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder16 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder);
        handler.AppendLiteral("<uObj4>");
        handler.AppendFormatted(uObj4);
        handler.AppendLiteral("</uObj4>");
        stringBuilder16.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder17 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder);
        handler.AppendLiteral("<uObj5>");
        handler.AppendFormatted(uObj5);
        handler.AppendLiteral("</uObj5>");
        stringBuilder17.AppendLine(ref handler);
        return stringBuilder2.ToString();
    }
}