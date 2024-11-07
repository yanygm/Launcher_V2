using System;
using System.IO;
using System.Text;
using KartLibrary.Record;

namespace KartLibrary.IO;

public static class KSVBinaryWExt
{
    private static Random rd = new Random();

    public static void WriteKSVInfo(this BinaryWriter bw, KSVInfo ki)
    {
        uint headerClassIdentifier = KSVStructVersion.GetHeaderClassIdentifier(ki.RecordHeaderVersion);
        bw.Write(headerClassIdentifier);
        bw.WriteKRString(ki.RecordTitle);
        bw.Write((short)ki.RegionCode);
        bw.Write(ki.Unknown1_1);
        bw.Write((byte)ki.ContestType);
        uint playerNameHash = GetPlayerNameHash(ki.Players);
        bw.Write(playerNameHash);
        bw.Write(ki.Unknown1_2);
        bw.WriteKRString(ki.RecorderAccount);
        bw.WriteKRString(ki.RecorderName);
        bw.WriteKRDateTime(ki.RecordingDate);
        uint recordCheckSum = GetRecordCheckSum(ki.Records);
        bw.Write(recordCheckSum);
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
        {
            bw.Write((byte)ki.Speed);
        }

        PlayerInfo[] players = ki.Players;
        bw.Write(players.Length);
        PlayerInfo[] array = players;
        foreach (PlayerInfo pi in array)
        {
            bw.WritePlayerInfo(pi, ki.RecordHeaderVersion);
        }

        uint recordClassIdentifier = KSVStructVersion.GetRecordClassIdentifier(ki.RecordVersion);
        bw.Write(recordClassIdentifier);
        RecordData[] records = ki.Records;
        bw.Write(records.Length);
        RecordData[] array2 = records;
        foreach (RecordData recordData in array2)
        {
            bw.WriteRecordData(recordData, ki.RecordHeaderVersion);
        }
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
        {
            bw.Write(pe.KartPaint);
        }

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
        RecordStamp[] stamps = rd.Stamps;
        bw.Write(stamps.Length);
        RecordStamp[] array = stamps;
        foreach (RecordStamp data in array)
        {
            bw.WriteRecordStramp(data, KSVHeaderVersion);
        }
    }

    public static void WriteRecordStramp(this BinaryWriter bw, RecordStamp data, int KSVHeaderVersion)
    {
        bw.Write((short)(data.Time / 100));
        bw.Write((short)(data.X * 10f));
        bw.Write((short)(data.Y * 10f));
        bw.Write((short)(data.Z * 10f));
        bw.Write((short)(data.Angle.W * 100f));
        bw.Write((short)(data.Angle.X * 100f));
        bw.Write((short)(data.Angle.Y * 100f));
        bw.Write((short)(data.Angle.Z * 100f));
        bw.Write(data.Status);
    }

    public static void WriteKRDateTime(this BinaryWriter bw, DateTime dateTime)
    {
        DateTime dateTime2 = new DateTime(1900, 1, 1);
        uint num = (uint)(dateTime - dateTime2).TotalDays;
        uint num2 = (uint)(dateTime.Hour * 3600 + dateTime.Minute * 60 + dateTime.Second) >> 2;
        bw.Write((ushort)num);
        bw.Write((ushort)num2);
    }

    public static void WriteKRString(this BinaryWriter bw, string str)
    {
        int length = str.Length;
        byte[] bytes = Encoding.GetEncoding("UTF-16").GetBytes(str);
        bw.Write(length);
        bw.Write(bytes);
    }

    private static uint GetPlayerNameHash(PlayerInfo[] players)
    {
        uint num = 0u;
        foreach (PlayerInfo playerInfo in players)
        {
            byte[] bytes = Encoding.GetEncoding("UTF-16").GetBytes(playerInfo.PlayerName);
            uint num2 = Adler.Adler32(0u, bytes, 0, bytes.Length);
            num += num2;
        }

        return num;
    }

    private static uint GetRecordCheckSum(RecordData[] data)
    {
        uint num = 0u;
        uint num2 = 0u;
        for (int i = 0; i < data.Length; i++)
        {
            RecordStamp[] stamps = data[i].Stamps;
            for (int j = 0; j < stamps.Length; j++)
            {
                if ((j & 1) == 1)
                {
                    num += stamps[j].Status;
                }
                else
                {
                    num2 += stamps[j].Status;
                }
            }
        }

        return (num << 16) + num2;
    }
}