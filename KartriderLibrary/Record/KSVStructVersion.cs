using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KartLibrary.IO;
using System.Threading.Tasks;

namespace KartLibrary.Record
{
    public static class KSVStructVersion
    {
        private static Dictionary<uint, int> HeaderVersions = new Dictionary<uint, int>();//key: hash value:version

        private static Dictionary<uint, int> Versions = new Dictionary<uint, int>();//key: hash value:version

        private static void generateHeaderVers()
        {
            for (int i = 0; i <= 14; i++)
            {
                string headerVersion = $"KartRecord{i}Header";
                string version = $"KartRecord{i}";
                byte[] headerData = Encoding.UTF8.GetBytes(headerVersion);
                byte[] versionData = Encoding.UTF8.GetBytes(version);
                HeaderVersions.Add(Adler.Adler32(0, headerData, 0, headerData.Length), i);
                Versions.Add(Adler.Adler32(0, versionData, 0, versionData.Length), i);
            }
        }

        public static int GetHeaderVersion(uint HeaderClassIdentifier)
        {
            if (HeaderVersions.Count == 0)
                generateHeaderVers();
            return HeaderVersions[HeaderClassIdentifier];
        }

        public static int GetVersion(uint RecordClassIdentifier)
        {
            if (Versions.Count == 0)
                generateHeaderVers();
            return Versions[RecordClassIdentifier];
        }

        public static uint GetHeaderClassIdentifier(int headerVersion)
        {
            string headerVer = $"KartRecord{headerVersion}Header";
            byte[] headerData = Encoding.UTF8.GetBytes(headerVer);
            return Adler.Adler32(0, headerData, 0, headerData.Length);
        }

        public static uint GetRecordClassIdentifier(int recordVersion)
        {
            string headerVer = $"KartRecord{recordVersion}";
            byte[] headerData = Encoding.UTF8.GetBytes(headerVer);
            return Adler.Adler32(0, headerData, 0, headerData.Length);
        }
    }
}
