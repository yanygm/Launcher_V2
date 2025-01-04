using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using KartLibrary.Xml;
using KartLibrary.Record;

namespace KartLibrary.IO
{
    public static class BinaryWriterExt
    {
        public static void WriteString(this BinaryWriter br, Encoding encoding,string Text)
        {
            byte[] data = encoding.GetBytes(Text);
            br.Write(Text.Length);
            br.Write(data);
            data = null;
        }

        public static void Write(this BinaryWriter br, Encoding encoding, string Key,string Value)
        {
            br.WriteString(encoding, Key);
            br.WriteString(encoding,Value);
        }

        public static void WriteNullTerminatedText(this BinaryWriter br, string text, bool wideString)
        {
            if (!wideString)
            {
                byte[] encData = Encoding.ASCII.GetBytes(text);
                br.Write(encData);
                br.Write((byte)0x00);
            }
            else
            {
                byte[] encData = Encoding.Unicode.GetBytes(text);
                br.Write(encData);
                br.Write((short)0x00);
            }
        }
    }

    public static class KSVBinaryWExt
    {
        public static void WriteKSVInfo(this BinaryWriter bw, KSVInfo ki)
        {
            uint headerClassIdentifier = KSVStructVersion.GetHeaderClassIdentifier(ki.RecordHeaderVersion);
            bw.Write(headerClassIdentifier);
            bw.WriteKRString(ki.RecordTitle);
            bw.Write((short)ki.RegionCode);
            bw.Write(ki.Unknown1_1);
            bw.Write((byte)ki.ContestType);
            uint PlayerNameHash = GetPlayerNameHash(ki.Players);
            bw.Write(PlayerNameHash);
            bw.Write(ki.Unknown1_2);
            bw.WriteKRString(ki.RecorderAccount);
            bw.WriteKRString(ki.RecorderName);
            bw.WriteKRDateTime(ki.RecordingDate);
            uint RecordCheckSum = GetRecordCheckSum(ki.Records);
            bw.Write(RecordCheckSum);
            bw.Write(ki.IsOffical);
            bw.WriteKRString(ki.Description);
            bw.WriteKRString(ki.TrackName);
            bw.Write(ki.Unknown3);
            bw.Write((int)ki.BestTime.TotalMilliseconds);
            bw.WriteKRString(ki.ContestImg);
            bw.Write(ki.Unknown4);
            bw.Write(ki.Unknown5);
            bw.Write(ki.Unknown6);
            if (ki.RecordHeaderVersion >= 9)
                bw.Write((byte)ki.Speed);
            PlayerInfo[] players = ki.Players;
            bw.Write(players.Length);
            foreach (PlayerInfo player in players)
                bw.WritePlayerInfo(player, ki.RecordHeaderVersion);
            uint recordClassIdentifier = KSVStructVersion.GetRecordClassIdentifier(ki.RecordVersion);
            bw.Write(recordClassIdentifier);
            RecordData[] records = ki.Records;
            bw.Write(records.Length);
            foreach (RecordData rd in records)
                bw.WriteRecordData(rd, ki.RecordHeaderVersion);
        }

        public static void WritePlayerInfo(this BinaryWriter bw, PlayerInfo pi, int KSVHeaderVersion)
        {
            bw.WriteKRString(pi.PlayerName);
            bw.WriteKRString(pi.ClubName);
            bw.WritePlayerEquipment(pi.Equipment, KSVHeaderVersion);
        }

        public static void WritePlayerEquipment(this BinaryWriter bw, PlayerEquipment pe, int KSVHeaderVersion)
        {
            bw.Write(pe.Character);
            if (KSVHeaderVersion >= 10)
                bw.Write(pe.KartPaint);
            bw.Write(pe.CharacterColor);
            bw.Write(pe.Kart);
            bw.Write(pe.Plate);
            bw.Write(pe.Goggle);
            bw.Write(pe.Balloon);
            bw.Write(pe.Equ2);
            bw.Write(pe.Headband);
            bw.Write(pe.Replay);
            bw.Write(pe.Cane);
            bw.Write(pe.Equ3);
            bw.Write(pe.Apparel);
            bw.Write(pe.Equ4);
            bw.WriteKRString(pe.PlateText);
            bw.Write(pe.Equ5);
            bw.Write(pe.Equ6);
            bw.Write(pe.Equ7);
            bw.Write(pe.Equ8);
            bw.Write(pe.Equ9);
            bw.Write(pe.Equ10);
            bw.Write(pe.Equ11);
            if (KSVHeaderVersion >= 11)
            {
                bw.Write(pe.Equ12);
                bw.Write(pe.Equ13);
            }
        }

        public static void WriteRecordData(this BinaryWriter bw, RecordData rd, int KSVHeaderVersion)
        {
            RecordStamp[] rss = rd.Stamps;
            bw.Write(rss.Length);
            foreach (RecordStamp r in rss)
            {
                bw.WriteRecordStramp(r, KSVHeaderVersion);
            }
        }
        static Random rd = new Random();
        public static void WriteRecordStramp(this BinaryWriter bw, RecordStamp data, int KSVHeaderVersion)
        {
            bw.Write((short)(data.Time / 100));
            bw.Write((short)(data.X * 10));
            bw.Write((short)(data.Y * 10));
            bw.Write((short)(data.Z * 10));

            bw.Write((short)((data.Angle.W) * 100));
            bw.Write((short)(data.Angle.X * 100));
            bw.Write((short)(data.Angle.Y * 100));
            bw.Write((short)((data.Angle.Z) * 100));
            /*
            bw.Write((short)((data.angle_W) * 100));
            bw.Write((short)(data.angle_X * 100));
            bw.Write((short)(data.angle_Y * 100));
            bw.Write((short)((data.angle_Z) * 100));
            */
            bw.Write(data.Status);
        }

        public static void WriteKRDateTime(this BinaryWriter bw, DateTime dateTime)
        {
            DateTime dt = new DateTime(1900, 1, 1);
            TimeSpan ts = dateTime - dt;
            uint date = (uint)ts.TotalDays;
            uint time = (uint)(dateTime.Hour * 3600 + dateTime.Minute * 60 + dateTime.Second) >> 2;
            bw.Write((ushort)date);
            bw.Write((ushort)time);
        }
        public static void WriteKRString(this BinaryWriter bw, string str)
        {
            int len = str.Length;
            byte[] strData = Encoding.GetEncoding("UTF-16").GetBytes(str);
            bw.Write(len);
            bw.Write(strData);
        }

        private static uint GetPlayerNameHash(PlayerInfo[] players)
        {
            uint output = 0;
            foreach (PlayerInfo player in players)
            {
                byte[] dataStr = Encoding.GetEncoding("UTF-16").GetBytes(player.PlayerName);
                uint strHash = Adler.Adler32(0, dataStr, 0, dataStr.Length);
                output += strHash;
            }
            return output;
        }
        private static uint GetRecordCheckSum(RecordData[] data)
        {
            uint oddSum = 0, evenSum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                RecordStamp[] curStamps = data[i].Stamps;
                for (int j = 0; j < curStamps.Length; j++)
                {
                    if ((j & 1) == 1)
                    {
                        oddSum += (uint)curStamps[j].Status;
                    }
                    else
                    {
                        evenSum += (uint)curStamps[j].Status;
                    }
                }
            }
            return (oddSum << 16) + evenSum;
        }
    }
}
