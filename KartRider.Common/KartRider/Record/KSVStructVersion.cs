using System.Collections.Generic;
using System.Text;
using KartLibrary.IO;

namespace KartLibrary.Record;

public static class KSVStructVersion
{
    private static Dictionary<uint, int> HeaderVersions = new Dictionary<uint, int>();

    private static Dictionary<uint, int> Versions = new Dictionary<uint, int>();

    private static void generateHeaderVers()
    {
        for (int i = 0; i <= 14; i++)
        {
            string s = $"KartRecord{i}Header";
            string s2 = $"KartRecord{i}";
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            byte[] bytes2 = Encoding.UTF8.GetBytes(s2);
            HeaderVersions.Add(Adler.Adler32(0u, bytes, 0, bytes.Length), i);
            Versions.Add(Adler.Adler32(0u, bytes2, 0, bytes2.Length), i);
        }
    }

    public static int GetHeaderVersion(uint HeaderClassIdentifier)
    {
        if (HeaderVersions.Count == 0)
        {
            generateHeaderVers();
        }

        return HeaderVersions[HeaderClassIdentifier];
    }

    public static int GetVersion(uint RecordClassIdentifier)
    {
        if (Versions.Count == 0)
        {
            generateHeaderVers();
        }

        return Versions[RecordClassIdentifier];
    }

    public static uint GetHeaderClassIdentifier(int headerVersion)
    {
        string s = $"KartRecord{headerVersion}Header";
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        return Adler.Adler32(0u, bytes, 0, bytes.Length);
    }

    public static uint GetRecordClassIdentifier(int recordVersion)
    {
        string s = $"KartRecord{recordVersion}";
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        return Adler.Adler32(0u, bytes, 0, bytes.Length);
    }
}