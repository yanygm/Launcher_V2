using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using KartLibrary.Xml;
using KartLibrary.Record;
using KartLibrary.Consts;
using System.Numerics;
using KartLibrary.Game.Engine;

namespace KartLibrary.IO
{
    public static class BinaryReaderExt
    {
        public static string ReadText(this BinaryReader reader)
        {
            int count = reader.ReadInt32() << 1;
            byte[] data = reader.ReadBytes(count);
            return Encoding.Unicode.GetString(data);
        }

        public static string ReadText(this BinaryReader br, Encoding encoding, int Count)
        {
            byte[] data = br.ReadBytes(Count);
            return encoding.GetString(data);
        }

        public static string ReadText(this BinaryReader br, Encoding encoding)
        {
            int count = br.ReadInt32() << 1;
            byte[] data = br.ReadBytes(count);
            return encoding.GetString(data);
        }

        public static BinaryXmlTag ReadBinaryXmlTag(this BinaryReader br, Encoding encoding)
        {
            BinaryXmlTag tag = new BinaryXmlTag();
            tag.Name = br.ReadText(encoding);
            //Text
            tag.Text = br.ReadText(encoding);
            //Attributes
            int attCount = br.ReadInt32();
            for (int i = 0; i < attCount; i++)
                tag.SetAttribute(br.ReadText(encoding), br.ReadText(encoding));
            //SubTags
            int SubCount = br.ReadInt32();
            for (int i = 0; i < SubCount; i++)
                tag.Children.Add(br.ReadBinaryXmlTag(encoding));
            return tag;
        }

        public static string ReadNullTerminatedText(this BinaryReader br, bool wideString)
        {
            StringBuilder stringBuilder = new StringBuilder(16);
            if (wideString)
            {
                char ch;
                while((ch = (char)br.ReadInt16()) != '\0')
                    stringBuilder.Append(ch);
            }
            else
            {
                char ch;
                while ((ch = (char)br.ReadByte()) != '\0')
                    stringBuilder.Append(ch);
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

    public static class KSVBinaryRExt
    {
        public static DateTime ReadKRDateTime(this BinaryReader br)
        {
            DateTime dt = new DateTime(1900, 1, 1);
            uint date = (uint)br.ReadUInt16();
            uint time = (uint)br.ReadUInt16() * 4;
            dt = dt.AddDays(date);
            dt = dt.AddSeconds(time);
            return dt;
        }
        public static string ReadKRString(this BinaryReader br)
        {
            int len = br.ReadInt32();
            byte[] strData = br.ReadBytes(len << 1);
            return Encoding.GetEncoding("UTF-16").GetString(strData);
        }

        public static KSVInfo ReadKSVInfo(this BinaryReader br)
        {
            KSVInfo ki = new KSVInfo();
            uint headerClassIdentifier = br.ReadUInt32();
            ki.RecordHeaderVersion = KSVStructVersion.GetHeaderVersion(headerClassIdentifier);
            ki.RecordTitle = br.ReadKRString();
            ki.RegionCode = (CountryCode)br.ReadInt16();
            ki.Unknown1_1 = br.ReadByte();
            ki.ContestType = (ContestType)br.ReadByte();
            ki.PlayerNameHash = br.ReadUInt32();
            ki.Unknown1_2 = br.ReadUInt32();
            ki.RecorderAccount = br.ReadKRString();
            ki.RecorderName = br.ReadKRString();
            ki.RecordingDate = br.ReadKRDateTime();
            ki.RecordChecksum = br.ReadUInt32();
            ki.IsOffical = br.ReadByte() == 1;
            ki.Description = br.ReadKRString();
            ki.TrackName = br.ReadKRString();
            ki.Unknown3 = br.ReadInt32();
            ki.BestTime = new TimeSpan(0, 0, 0, 0, br.ReadInt32());
            ki.ContestImg = br.ReadKRString();
            ki.Unknown4 = br.ReadInt32();
            ki.Unknown5 = br.ReadInt32();
            ki.Unknown6 = br.ReadByte();
            if (ki.RecordHeaderVersion >= 9)
                ki.Speed = (SpeedType)(br.ReadByte());
            int playerCount = br.ReadInt32();
            PlayerInfo[] players = new PlayerInfo[playerCount];
            for (int i = 0; i < playerCount; i++)
                players[i] = br.ReadPlayerInfo(ki.RecordHeaderVersion);
            ki.Players = players;
            uint recordClassIdentifier = br.ReadUInt32();
            ki.RecordVersion = KSVStructVersion.GetVersion(recordClassIdentifier);
            int recordCount = br.ReadInt32();
            RecordData[] records = new RecordData[recordCount];
            for (int i = 0; i < recordCount; i++)
                records[i] = br.ReadRecordData(ki.RecordHeaderVersion);
            ki.Records = records;
            return ki;
        }

        public static PlayerInfo ReadPlayerInfo(this BinaryReader br, int KSVHeaderVersion)
        {
            PlayerInfo pi = new PlayerInfo();
            pi.PlayerName = br.ReadKRString();
            pi.ClubName = br.ReadKRString();
            pi.Equipment = br.ReadPlayerEquipment(KSVHeaderVersion);
            return pi;
        }

        public static PlayerEquipment ReadPlayerEquipment(this BinaryReader br, int KSVHeaderVersion)
        {
            PlayerEquipment pe = new PlayerEquipment();
            pe.Character = br.ReadInt16();
            if (KSVHeaderVersion >= 10)
                pe.KartPaint = br.ReadInt16();
            pe.CharacterColor = br.ReadInt16();
            pe.Kart = br.ReadInt16();
            pe.Plate = br.ReadInt16();
            pe.Goggle = br.ReadInt16();
            pe.Balloon = br.ReadInt16();
            pe.Equ2 = br.ReadInt16();
            pe.Headband = br.ReadInt16();
            pe.Replay = br.ReadInt16();
            pe.Cane = br.ReadInt16();
            pe.Equ3 = br.ReadInt16();
            pe.Apparel = br.ReadInt16();
            pe.Equ4 = br.ReadInt16();
            pe.PlateText = br.ReadKRString();
            pe.Equ5 = br.ReadInt16();
            pe.Equ6 = br.ReadInt16();
            pe.Equ7 = br.ReadInt16();
            pe.Equ8 = br.ReadInt16();
            pe.Equ9 = br.ReadInt16();
            pe.Equ10 = br.ReadInt16();
            pe.Equ11 = br.ReadInt16();
            if (KSVHeaderVersion >= 11)
            {
                pe.Equ12 = br.ReadInt16();
                pe.Equ13 = br.ReadInt16();
            }
            return pe;
        }

        public static RecordData ReadRecordData(this BinaryReader br, int KSVHeaderVersion)
        {
            RecordData rd = new RecordData();
            int totalCount = br.ReadInt32();
            RecordStamp[] rss = new RecordStamp[totalCount];
            for (int i = 0; i < totalCount; i++)
            {
                rss[i] = br.ReadRecordStramp(KSVHeaderVersion);
            }
            rd.Stamps = rss;
            return rd;
        }

        public static RecordStamp ReadRecordStramp(this BinaryReader br, int KSVHeaderVersion)
        {
            RecordStamp rs = new RecordStamp();
            rs.Time = br.ReadInt16() * 100;
            rs.X = br.ReadInt16() * 0.1f;
            rs.Y = br.ReadInt16() * 0.1f;
            rs.Z = br.ReadInt16() * 0.1f;
            float angle_W = br.ReadInt16() * 0.01f;
            float angle_X = br.ReadInt16() * 0.01f;
            float angle_Y = br.ReadInt16() * 0.01f;
            float angle_Z = br.ReadInt16() * 0.01f;
            rs.Angle = new System.Numerics.Quaternion(angle_X, angle_Y, angle_Z, angle_W);
            rs.Status = br.ReadUInt16();
            return rs;
        }
    }

    public static class KartObjectExt
    {
        public static KartObject? ReadKartObject(this BinaryReader br, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            uint classStamp;
            KartObject output;
            if (decodedObjectMap is not null)
            {
                int isnull = br.ReadUInt16();
                if(isnull == 0x47BB)
                {
                    short objIndex = br.ReadInt16();
                    if (!decodedObjectMap.ContainsKey(objIndex))
                        throw new IndexOutOfRangeException();
                    output = decodedObjectMap[objIndex];
                }
                else
                {
                    classStamp = br.ReadUInt32();
                    short objIndex = br.ReadInt16();
                    output = KartObjectManager.CreateObject(classStamp);
                    output?.DecodeObject(br, decodedObjectMap, decodedFieldMap);
                    decodedObjectMap.Add(objIndex, output);
                }
            }
            else
            {
                classStamp = br.ReadUInt32();
                output = KartObjectManager.CreateObject(classStamp);
                output?.DecodeObject(br, decodedObjectMap, decodedFieldMap);
            }
            return output;
        }

        public static TBase ReadKartObject<TBase>(this BinaryReader br, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap) where TBase : KartObject, new()
        {
            uint classStamp;
            TBase output;
            if (decodedObjectMap is not null)
            {
                int isnull = br.ReadUInt16();
                if (isnull == 0x47BB)
                {
                    short objIndex = br.ReadInt16();
                    if (!decodedObjectMap.ContainsKey(objIndex))
                        throw new IndexOutOfRangeException();
                    if(decodedObjectMap[objIndex] is TBase decTBase)
                        output = decTBase;
                    else
                        throw new InvalidCastException();
                }
                else
                {
                    classStamp = br.ReadUInt32();
                    short objIndex = br.ReadInt16();
                    output = KartObjectManager.CreateObject<TBase>(classStamp);
                    output?.DecodeObject(br, decodedObjectMap, decodedFieldMap);
                    decodedObjectMap.Add(objIndex, output);
                }
            }
            else
            {
                classStamp = br.ReadUInt32();
                output = KartObjectManager.CreateObject<TBase>(classStamp);
                output?.DecodeObject(br, decodedObjectMap, decodedFieldMap);
            }
            return output;
        }

        // AA27 BB27
        public static T ReadField<T>(this BinaryReader br, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap, DecodeFieldFunc<T> decodeFieldFunc)
        {
            if(decodedFieldMap is not null)
            {
                ushort token = br.ReadUInt16();
                if (token == 0x27AA)
                {
                    short fieldObjIndex = br.ReadInt16();
                    T decodedField = decodeFieldFunc(br, decodedObjectMap, decodedFieldMap);
                    decodedFieldMap.Add(fieldObjIndex, decodedField);
                    return decodedField;
                }
                else if (token == 0x27BB)
                {
                    short fieldObjIndex = br.ReadInt16();
                    if (decodedFieldMap.ContainsKey(fieldObjIndex))
                        if (decodedFieldMap[fieldObjIndex] is T outField)
                            return outField;
                        else
                            throw new InvalidCastException();
                    else
                        throw new IndexOutOfRangeException();
                }
                else
                    throw new Exception();
            }
            else
            {
                return decodeFieldFunc(br, decodedObjectMap, decodedFieldMap);
            }
        }
    }

    public delegate T DecodeFieldFunc<T>(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap);
}
