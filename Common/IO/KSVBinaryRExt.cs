using System;
using System.IO;
using System.Numerics;
using System.Text;
using KartLibrary.Consts;
using KartLibrary.Record;

namespace KartLibrary.IO;

public static class KSVBinaryRExt
{
    public static DateTime ReadKRDateTime(this BinaryReader br)
    {
        DateTime dateTime = new DateTime(1900, 1, 1);
        uint num = br.ReadUInt16();
        uint num2 = (uint)(br.ReadUInt16() * 4);
        return dateTime.AddDays(num).AddSeconds(num2);
    }

    public static string ReadKRString(this BinaryReader br)
    {
        int num = br.ReadInt32();
        byte[] bytes = br.ReadBytes(num << 1);
        return Encoding.GetEncoding("UTF-16").GetString(bytes);
    }

    public static KSVInfo ReadKSVInfo(this BinaryReader br)
    {
        KSVInfo kSVInfo = new KSVInfo();
        uint headerClassIdentifier = br.ReadUInt32();
        kSVInfo.RecordHeaderVersion = KSVStructVersion.GetHeaderVersion(headerClassIdentifier);
        kSVInfo.RecordTitle = br.ReadKRString();
        kSVInfo.RegionCode = (CountryCode)br.ReadInt16();
        kSVInfo.Unknown1_1 = br.ReadByte();
        kSVInfo.ContestType = (ContestType)br.ReadByte();
        kSVInfo.PlayerNameHash = br.ReadUInt32();
        kSVInfo.Unknown1_2 = br.ReadUInt32();
        kSVInfo.RecorderAccount = br.ReadKRString();
        kSVInfo.RecorderName = br.ReadKRString();
        kSVInfo.RecordingDate = br.ReadKRDateTime();
        kSVInfo.RecordChecksum = br.ReadUInt32();
        kSVInfo.IsOffical = br.ReadByte() == 1;
        kSVInfo.Description = br.ReadKRString();
        kSVInfo.TrackName = br.ReadKRString();
        kSVInfo.Unknown3 = br.ReadInt32();
        kSVInfo.BestTime = new TimeSpan(0, 0, 0, 0, br.ReadInt32());
        kSVInfo.ContestImg = br.ReadKRString();
        kSVInfo.Unknown4 = br.ReadInt32();
        kSVInfo.Unknown5 = br.ReadInt32();
        kSVInfo.Unknown6 = br.ReadByte();
        if (kSVInfo.RecordHeaderVersion >= 9)
        {
            kSVInfo.Speed = (SpeedType)br.ReadByte();
        }

        int num = br.ReadInt32();
        PlayerInfo[] array = new PlayerInfo[num];
        for (int i = 0; i < num; i++)
        {
            array[i] = br.ReadPlayerInfo(kSVInfo.RecordHeaderVersion);
        }

        kSVInfo.Players = array;
        uint recordClassIdentifier = br.ReadUInt32();
        kSVInfo.RecordVersion = KSVStructVersion.GetVersion(recordClassIdentifier);
        int num2 = br.ReadInt32();
        RecordData[] array2 = new RecordData[num2];
        for (int j = 0; j < num2; j++)
        {
            array2[j] = br.ReadRecordData(kSVInfo.RecordHeaderVersion);
        }

        kSVInfo.Records = array2;
        return kSVInfo;
    }

    public static PlayerInfo ReadPlayerInfo(this BinaryReader br, int KSVHeaderVersion)
    {
        PlayerInfo result = new PlayerInfo();
        result.PlayerName = br.ReadKRString();
        result.ClubName = br.ReadKRString();
        result.Equipment = br.ReadPlayerEquipment(KSVHeaderVersion);
        return result;
    }

    public static PlayerEquipment ReadPlayerEquipment(this BinaryReader br, int KSVHeaderVersion)
    {
        PlayerEquipment playerEquipment = new PlayerEquipment();
        playerEquipment.Character = br.ReadInt16();
        if (KSVHeaderVersion >= 10)
        {
            playerEquipment.KartPaint = br.ReadInt16();
        }

        playerEquipment.CharacterColor = br.ReadInt16();
        playerEquipment.Kart = br.ReadInt16();
        playerEquipment.Plate = br.ReadInt16();
        playerEquipment.Goggle = br.ReadInt16();
        playerEquipment.Balloon = br.ReadInt16();
        playerEquipment.Equ2 = br.ReadInt16();
        playerEquipment.Headband = br.ReadInt16();
        playerEquipment.Replay = br.ReadInt16();
        playerEquipment.Cane = br.ReadInt16();
        playerEquipment.Equ3 = br.ReadInt16();
        playerEquipment.Apparel = br.ReadInt16();
        playerEquipment.Equ4 = br.ReadInt16();
        playerEquipment.PlateText = br.ReadKRString();
        playerEquipment.Equ5 = br.ReadInt16();
        playerEquipment.Equ6 = br.ReadInt16();
        playerEquipment.Equ7 = br.ReadInt16();
        playerEquipment.Equ8 = br.ReadInt16();
        playerEquipment.Equ9 = br.ReadInt16();
        playerEquipment.Equ10 = br.ReadInt16();
        playerEquipment.Equ11 = br.ReadInt16();
        if (KSVHeaderVersion >= 11)
        {
            playerEquipment.Equ12 = br.ReadInt16();
            playerEquipment.Equ13 = br.ReadInt16();
        }

        return playerEquipment;
    }

    public static RecordData ReadRecordData(this BinaryReader br, int KSVHeaderVersion)
    {
        RecordData result = new RecordData();
        int num = br.ReadInt32();
        RecordStamp[] array = new RecordStamp[num];
        for (int i = 0; i < num; i++)
        {
            array[i] = br.ReadRecordStramp(KSVHeaderVersion);
        }

        result.Stamps = array;
        return result;
    }

    public static RecordStamp ReadRecordStramp(this BinaryReader br, int KSVHeaderVersion)
    {
        RecordStamp result = new RecordStamp();
        result.Time = br.ReadInt16() * 100;
        result.X = (float)br.ReadInt16() * 0.1f;
        result.Y = (float)br.ReadInt16() * 0.1f;
        result.Z = (float)br.ReadInt16() * 0.1f;
        float w = (float)br.ReadInt16() * 0.01f;
        float x = (float)br.ReadInt16() * 0.01f;
        float y = (float)br.ReadInt16() * 0.01f;
        float z = (float)br.ReadInt16() * 0.01f;
        result.Angle = new Quaternion(x, y, z, w);
        result.Status = br.ReadUInt16();
        return result;
    }
}