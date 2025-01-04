using KartLibrary.Consts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Record
{
    public class KSVInfo
    {

        public int RecordHeaderVersion { get; set; }
        public string RecordTitle { get; set; } = "";
        public CountryCode RegionCode { get; set; }
        public byte Unknown1_1 { get; set; }
        public ContestType ContestType { get; set; } //byte
        public uint PlayerNameHash { get; set; }
        public uint Unknown1_2 { get; set; }
        public string RecorderAccount { get; set; } = "";
        public string RecorderName { get; set; } = "";
        public DateTime RecordingDate { get; set; } = new DateTime(1900, 1, 1);
        public uint RecordChecksum { get; set; }
        public bool IsOffical { get; set; } = false;
        public string Description { get; set; } = "";
        public string TrackName { get; set; } = "";
        public int Unknown3 { get; set; }
        public TimeSpan BestTime { get; set; }
        public string ContestImg { get; set; } = "";//Old Property
        public int Unknown4 { get; set; }
        public int Unknown5 { get; set; }
        public byte Unknown6 { get; set; }
        public SpeedType Speed { get; set; } = SpeedType.SuperSpeed; //byte
        public PlayerInfo[] Players { get; set; } = new PlayerInfo[0];
        public int RecordVersion { get; set; }
        public RecordData[] Records { get; set; } = new RecordData[0];

    }
}
