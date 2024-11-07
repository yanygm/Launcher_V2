using System.IO;
using System.Numerics;
using System.Text;
using KartLibrary.Game.Engine;
using KartLibrary.Xml;

namespace KartLibrary.IO;

public static class BinaryReaderExt
{
    public static string ReadText(this BinaryReader reader)
    {
        int count = reader.ReadInt32() << 1;
        byte[] bytes = reader.ReadBytes(count);
        return Encoding.Unicode.GetString(bytes);
    }

    public unsafe static int MyReadInt32(this BinaryReader reader)
    {
        int result;
        fixed (byte* ptr = reader.ReadBytes(4))
        {
            result = *(int*)ptr;
        }

        return result;
    }

    public static string ReadText(this BinaryReader br, Encoding encoding, int Count)
    {
        byte[] bytes = br.ReadBytes(Count);
        return encoding.GetString(bytes);
    }

    public static string ReadText(this BinaryReader br, Encoding encoding)
    {
        int count = br.ReadInt32() << 1;
        byte[] bytes = br.ReadBytes(count);
        return encoding.GetString(bytes);
    }

    public static BinaryXmlTag ReadBinaryXmlTag(this BinaryReader br, Encoding encoding)
    {
        BinaryXmlTag binaryXmlTag = new BinaryXmlTag();
        binaryXmlTag.Name = br.ReadText(encoding);
        binaryXmlTag.Text = br.ReadText(encoding);
        int num = br.ReadInt32();
        for (int i = 0; i < num; i++)
        {
            binaryXmlTag.SetAttribute(br.ReadText(encoding), br.ReadText(encoding));
        }

        int num2 = br.ReadInt32();
        for (int j = 0; j < num2; j++)
        {
            binaryXmlTag.Children.Add(br.ReadBinaryXmlTag(encoding));
        }

        return binaryXmlTag;
    }

    public static string ReadNullTerminatedText(this BinaryReader br, bool wideString)
    {
        StringBuilder stringBuilder = new StringBuilder(16);
        if (wideString)
        {
            char value;
            while ((value = (char)br.ReadInt16()) != 0)
            {
                stringBuilder.Append(value);
            }
        }
        else
        {
            char value2;
            while ((value2 = (char)br.ReadByte()) != 0)
            {
                stringBuilder.Append(value2);
            }
        }

        return stringBuilder.ToString();
    }

    public static Vector2 ReadVector2(this BinaryReader br)
    {
        float x = br.ReadSingle();
        float y = br.ReadSingle();
        return new Vector2(x, y);
    }

    public static Vector3 ReadVector3(this BinaryReader br)
    {
        float x = br.ReadSingle();
        float y = br.ReadSingle();
        float z = br.ReadSingle();
        return new Vector3(x, y, z);
    }

    public static Vector4 ReadVector4(this BinaryReader br)
    {
        float x = br.ReadSingle();
        float y = br.ReadSingle();
        float z = br.ReadSingle();
        float w = br.ReadSingle();
        return new Vector4(x, y, z, w);
    }

    public static BoundingBox ReadBoundBox(this BinaryReader br)
    {
        Vector3 minPos = br.ReadVector3();
        Vector3 maxPos = br.ReadVector3();
        return new BoundingBox(minPos, maxPos);
    }
}