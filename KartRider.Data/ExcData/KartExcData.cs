using KartRider;
using KartRider.Common.Network;
using KartRider.IO.Packet;
using Profile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace ExcData
{
    public static class KartExcData
    {
        public static Dictionary<string, List<Tune>> TuneLists = new Dictionary<string, List<Tune>>();
        public static Dictionary<string, List<Plant>> PlantLists = new Dictionary<string, List<Plant>>();
        public static Dictionary<string, List<Level>> LevelLists = new Dictionary<string, List<Level>>();
        public static Dictionary<string, List<Parts>> PartsLists = new Dictionary<string, List<Parts>>();
        public static Dictionary<string, List<Parts12>> Parts12Lists = new Dictionary<string, List<Parts12>>();
        public static Dictionary<string, List<Level12>> Level12Lists = new Dictionary<string, List<Level12>>();

        public static void Tune_ExcData(SessionGroup Parent, string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var TuneList = new List<Tune>();
            if (File.Exists(filename.TuneData_LoadFile))
            {
                TuneList = JsonHelper.DeserializeNoBom<List<Tune>>(filename.TuneData_LoadFile);
            }
            TuneLists.TryAdd(Nickname, TuneList);
            int range = 26;//分批次数
            int times = TuneList.Count / range + (TuneList.Count % range > 0 ? 1 : 0);
            for (int i = 0; i < times; i++)
            {
                var tempList = TuneList.GetRange(i * range, (i + 1) * range > TuneList.Count ? (TuneList.Count - i * range) : range);
                int TuneCount = tempList.Count;
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
                {
                    if (i == 0)
                    {
                        oPacket.WriteByte(1);
                    }
                    else
                    {
                        oPacket.WriteByte(0);
                    }
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteInt(TuneCount);
                    for (var f = 0; f < TuneCount; f++)
                    {
                        oPacket.WriteShort(3);
                        oPacket.WriteShort(tempList[f].ID);
                        oPacket.WriteShort(tempList[f].SN);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(tempList[f].Tune1);
                        oPacket.WriteShort(tempList[f].Tune2);
                        oPacket.WriteShort(tempList[f].Tune3);
                        oPacket.WriteShort(tempList[f].Slot1);
                        oPacket.WriteShort(tempList[f].Count1);
                        oPacket.WriteShort(tempList[f].Slot2);
                        oPacket.WriteShort(tempList[f].Count2);
                    }
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteInt(0);
                    Parent.Client.Send(oPacket);
                }
            }
        }

        public static void Plant_ExcData(SessionGroup Parent, string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var PlantList = new List<Plant>();
            if (File.Exists(filename.PlantData_LoadFile))
            {
                PlantList = JsonHelper.DeserializeNoBom<List<Plant>>(filename.PlantData_LoadFile);
            }
            PlantLists.TryAdd(Nickname, PlantList);
            int range = 26;//分批次数
            int times = PlantList.Count / range + (PlantList.Count % range > 0 ? 1 : 0);
            for (int i = 0; i < times; i++)
            {
                var tempList = PlantList.GetRange(i * range, (i + 1) * range > PlantList.Count ? (PlantList.Count - i * range) : range);
                int PlantCount = tempList.Count;
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
                {
                    oPacket.WriteByte(0);
                    if (i == 0)
                    {
                        oPacket.WriteByte(1);
                    }
                    else
                    {
                        oPacket.WriteByte(0);
                    }
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(PlantCount);
                    for (var f = 0; f < PlantCount; f++)
                    {
                        oPacket.WriteShort(tempList[f].ID);
                        oPacket.WriteShort(tempList[f].SN);
                        oPacket.WriteInt(4);
                        oPacket.WriteShort(tempList[f].Engine);
                        oPacket.WriteShort(tempList[f].EngineID);
                        oPacket.WriteShort(tempList[f].Handle);
                        oPacket.WriteShort(tempList[f].HandleID);
                        oPacket.WriteShort(tempList[f].Wheel);
                        oPacket.WriteShort(tempList[f].WheelID);
                        oPacket.WriteShort(tempList[f].Kit);
                        oPacket.WriteShort(tempList[f].KitID);
                    }
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteInt(0);
                    Parent.Client.Send(oPacket);
                }
            }
        }

        public static void Level_ExcData(SessionGroup Parent, string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var LevelList = new List<Level>();
            if (File.Exists(filename.LevelData_LoadFile))
            {
                LevelList = JsonHelper.DeserializeNoBom<List<Level>>(filename.LevelData_LoadFile);
            }
            LevelLists.TryAdd(Nickname, LevelList);
            int range = 26;//分批次数
            int times = LevelList.Count / range + (LevelList.Count % range > 0 ? 1 : 0);
            for (int i = 0; i < times; i++)
            {
                var tempList = LevelList.GetRange(i * range, (i + 1) * range > LevelList.Count ? (LevelList.Count - i * range) : range);
                int LevelCount = tempList.Count;
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
                {
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    if (i == 0)
                    {
                        oPacket.WriteByte(1);
                    }
                    else
                    {
                        oPacket.WriteByte(0);
                    }
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(LevelCount);
                    for (var f = 0; f < LevelCount; f++)
                    {
                        oPacket.WriteShort(tempList[f].ID);
                        oPacket.WriteShort(tempList[f].SN);
                        oPacket.WriteShort(tempList[f].Grade);
                        oPacket.WriteShort(tempList[f].Points);
                        oPacket.WriteShort(tempList[f].Level1);
                        oPacket.WriteShort(tempList[f].Level2);
                        oPacket.WriteShort(tempList[f].Level3);
                        oPacket.WriteShort(tempList[f].Level4);
                        oPacket.WriteShort(tempList[f].Effect); //코팅
                    }
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteInt(0);
                    Parent.Client.Send(oPacket);
                }
            }
        }

        public static void Parts_ExcData(SessionGroup Parent, string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var PartsList = new List<Parts>();
            if (File.Exists(filename.PartsData_LoadFile))
            {
                PartsList = JsonHelper.DeserializeNoBom<List<Parts>>(filename.PartsData_LoadFile);
            }
            PartsLists.TryAdd(Nickname, PartsList);
            int range = 26;//分批次数
            int times = PartsList.Count / range + (PartsList.Count % range > 0 ? 1 : 0);
            for (int i = 0; i < times; i++)
            {
                var tempList = PartsList.GetRange(i * range, (i + 1) * range > PartsList.Count ? (PartsList.Count - i * range) : range);
                int parts = tempList.Count;
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
                {
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    if (i == 0)
                    {
                        oPacket.WriteByte(1);
                    }
                    else
                    {
                        oPacket.WriteByte(0);
                    }
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(parts);
                    for (var f = 0; f < parts; f++)
                    {
                        oPacket.WriteShort(tempList[f].ID);
                        oPacket.WriteShort(tempList[f].SN);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(-1);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(tempList[f].Engine);
                        oPacket.WriteByte(tempList[f].EngineGrade);
                        oPacket.WriteShort(tempList[f].EngineValue);
                        oPacket.WriteShort(tempList[f].Handle);
                        oPacket.WriteByte(tempList[f].HandleGrade);
                        oPacket.WriteShort(tempList[f].HandleValue);
                        oPacket.WriteShort(tempList[f].Wheel);
                        oPacket.WriteByte(tempList[f].WheelGrade);
                        oPacket.WriteShort(tempList[f].WheelValue);
                        oPacket.WriteShort(tempList[f].Booster);
                        oPacket.WriteByte(tempList[f].BoosterGrade);
                        oPacket.WriteShort(tempList[f].BoosterValue);
                        oPacket.WriteShort(tempList[f].Coating);
                        oPacket.WriteByte(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(tempList[f].TailLamp);
                        oPacket.WriteByte(0);
                        oPacket.WriteShort(0);
                    }
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteInt(0);
                    Parent.Client.Send(oPacket);
                }
            }
        }

        public static void Level12_ExcData(SessionGroup Parent, string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var Level12List = new List<Level12>();
            if (File.Exists(filename.Level12Data_LoadFile))
            {
                Level12List = JsonHelper.DeserializeNoBom<List<Level12>>(filename.Level12Data_LoadFile);
            }
            Level12Lists.TryAdd(Nickname, Level12List);
            int range = 26;//分批次数
            int times = Level12List.Count / range + (Level12List.Count % range > 0 ? 1 : 0);
            for (int i = 0; i < times; i++)
            {
                var tempList = Level12List.GetRange(i * range, (i + 1) * range > Level12List.Count ? (Level12List.Count - i * range) : range);
                int Parts = tempList.Count;
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
                {
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    if (i == 0)
                    {
                        oPacket.WriteByte(1);
                    }
                    else
                    {
                        oPacket.WriteByte(0);
                    }
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(Parts);
                    for (var f = 0; f < Parts; f++)
                    {
                        oPacket.WriteShort(3);
                        oPacket.WriteShort(tempList[f].ID);
                        oPacket.WriteShort(tempList[f].SN);
                        oPacket.WriteShort(tempList[f].Grade);
                        oPacket.WriteShort(tempList[f].SkillPoints);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(4);
                        oPacket.WriteShort(4);
                        oPacket.WriteShort(tempList[f].Skill1);
                        oPacket.WriteShort(tempList[f].Skill2);
                        oPacket.WriteShort(tempList[f].Skill3);
                        oPacket.WriteShort(tempList[f].SkillGrade1);
                        oPacket.WriteShort(tempList[f].SkillGrade2);
                        oPacket.WriteShort(tempList[f].SkillGrade3);
                    }
                    oPacket.WriteByte(0);
                    oPacket.WriteInt(0);
                    Parent.Client.Send(oPacket);
                }
            }
        }

        public static void Parts12_ExcData(SessionGroup Parent, string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var Parts12List = new List<Parts12>();
            if (File.Exists(filename.Parts12Data_LoadFile))
            {
                Parts12List = JsonHelper.DeserializeNoBom<List<Parts12>>(filename.Parts12Data_LoadFile);
            }
            Parts12Lists.TryAdd(Nickname, Parts12List);
            int range = 26;//分批次数
            int times = Parts12List.Count / range + (Parts12List.Count % range > 0 ? 1 : 0);
            for (int i = 0; i < times; i++)
            {
                var tempList = Parts12List.GetRange(i * range, (i + 1) * range > Parts12List.Count ? (Parts12List.Count - i * range) : range);
                int parts12 = tempList.Count;
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
                {
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    if (i == 0)
                    {
                        oPacket.WriteByte(1);
                    }
                    else
                    {
                        oPacket.WriteByte(0);
                    }
                    oPacket.WriteInt(parts12);
                    for (var f = 0; f < parts12; f++)
                    {
                        oPacket.WriteShort(tempList[f].ID);
                        oPacket.WriteShort(tempList[f].SN);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(-1);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(tempList[f].Engine);
                        oPacket.WriteShort(1);
                        oPacket.WriteShort(tempList[f].Handle);
                        oPacket.WriteShort(1);
                        oPacket.WriteShort(tempList[f].Wheel);
                        oPacket.WriteShort(1);
                        oPacket.WriteShort(tempList[f].Booster);
                        oPacket.WriteShort(1);
                        oPacket.WriteShort(tempList[f].Coating);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(tempList[f].TailLamp);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(tempList[f].BoosterEffect);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(tempList[f].ExceedType);
                        oPacket.WriteShort(0);
                    }
                    Parent.Client.Send(oPacket);
                }
            }
        }

        public static void AddTuneList(string Nickname, short id, short sn, short tune1, short tune2, short tune3, short slot1, short count1, short slot2, short count2)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            TuneLists.TryAdd(Nickname, new List<Tune>());
            var TuneList = TuneLists[Nickname];
            var existingList = TuneList.FirstOrDefault(list => list.ID == id && list.SN == sn);
            if (existingList == null)
            {
                var newList = new Tune { ID = id, SN = sn, Tune1 = tune1, Tune2 = tune2, Tune3 = tune3, Slot1 = slot1, Count1 = count1, Slot2 = slot2, Count2 = count2 };
                TuneList.Add(newList);
                Save(filename.TuneData_LoadFile, TuneList);
            }
            else
            {
                existingList.Tune1 = tune1;
                existingList.Tune2 = tune2;
                existingList.Tune3 = tune3;
                existingList.Slot1 = slot1;
                existingList.Count1 = count1;
                existingList.Slot2 = slot2;
                existingList.Count2 = count2;
                Save(filename.TuneData_LoadFile, TuneList);
            }
        }

        public static void AddPlantList(string Nickname, short id, short sn, short item, short item_id)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            PlantLists.TryAdd(Nickname, new List<Plant>());
            var PlantList = PlantLists[Nickname];
            var existingList = PlantList.FirstOrDefault(list => list.ID == id && list.SN == sn);
            if (existingList == null)
            {
                var newList = new Plant { ID = id, SN = sn, Engine = 0, EngineID = 0, Handle = 0, HandleID = 0, Wheel = 0, WheelID = 0, Kit = 0, KitID = 0 };
                switch (item)
                {
                    case 43:
                        newList.Engine = item;
                        newList.EngineID = item_id;
                        break;
                    case 44:
                        newList.Handle = item;
                        newList.HandleID = item_id;
                        break;
                    case 45:
                        newList.Wheel = item;
                        newList.WheelID = item_id;
                        break;
                    case 46:
                        newList.Kit = item;
                        newList.KitID = item_id;
                        break;
                }
                PlantList.Add(newList);
                Save(filename.PlantData_LoadFile, PlantList);
            }
            else
            {
                switch (item)
                {
                    case 43:
                        existingList.Engine = item;
                        existingList.EngineID = item_id;
                        break;
                    case 44:
                        existingList.Handle = item;
                        existingList.HandleID = item_id;
                        break;
                    case 45:
                        existingList.Wheel = item;
                        existingList.WheelID = item_id;
                        break;
                    case 46:
                        existingList.Kit = item;
                        existingList.KitID = item_id;
                        break;
                }
                Save(filename.PlantData_LoadFile, PlantList);
            }
        }

        public static void AddLevelList(string Nickname, short id, short sn, short level, short point, short v1, short v2, short v3, short v4, short Effect)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            LevelLists.TryAdd(Nickname, new List<Level>());
            var LevelList = LevelLists[Nickname];
            var existingList = LevelList.FirstOrDefault(list => list.ID == id && list.SN == sn);
            if (existingList == null)
            {
                var newList = new Level { ID = id, SN = sn, Grade = level, Points = point, Level1 = v1, Level2 = v2, Level3 = v3, Level4 = v4, Effect = Effect };
                LevelList.Add(newList);
                Save(filename.LevelData_LoadFile, LevelList);
            }
            else
            {
                existingList.Grade = level;
                existingList.Points = point;
                existingList.Level1 = v1;
                existingList.Level2 = v2;
                existingList.Level3 = v3;
                existingList.Level4 = v4;
                existingList.Effect = Effect;
                Save(filename.LevelData_LoadFile, LevelList);
            }
        }

        public static void AddPartsList(string Nickname, short id, short sn, short Item_Cat_Id, short Item_Id, byte Grade, short PartsValue)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            Parts12Lists.TryAdd(Nickname, new List<Parts12>());
            var Parts12List = Parts12Lists[Nickname];
            PartsLists.TryAdd(Nickname, new List<Parts>());
            var PartsList = PartsLists[Nickname];
            var existing12List = Parts12List.FirstOrDefault(list => list.ID == id && list.SN == sn);
            if (Item_Cat_Id == 0)
            {
                if (existing12List == null)
                {
                    var newList = new Parts12 { ID = id, SN = sn, Engine = 0, Handle = 0, Wheel = 0, Booster = 0, Coating = 0, TailLamp = 0, BoosterEffect = 0, ExceedType = Item_Id };
                    Parts12List.Add(newList);
                    Save(filename.Parts12Data_LoadFile, Parts12List);
                }
                else
                {
                    existing12List.ExceedType = Item_Id;
                    Save(filename.Parts12Data_LoadFile, Parts12List);
                }
            }
            else if (Item_Cat_Id == 72 || Item_Cat_Id == 73 || Item_Cat_Id == 74 || Item_Cat_Id == 75 || Item_Cat_Id == 76 || Item_Cat_Id == 77 || Item_Cat_Id == 78)
            {
                if (existing12List == null)
                {
                    var newList = new Parts12 { ID = id, SN = sn, Engine = 0, Handle = 0, Wheel = 0, Booster = 0, Coating = 0, TailLamp = 0, BoosterEffect = 0, ExceedType = 0 };
                    switch (Item_Cat_Id)
                    {
                        case 72:
                            newList.Engine = Item_Id;
                            break;
                        case 73:
                            newList.Handle = Item_Id;
                            break;
                        case 74:
                            newList.Wheel = Item_Id;
                            break;
                        case 75:
                            newList.Booster = Item_Id;
                            break;
                        case 76:
                            newList.Coating = Item_Id;
                            break;
                        case 77:
                            newList.TailLamp = Item_Id;
                            break;
                        case 78:
                            newList.BoosterEffect = Item_Id;
                            break;
                    }
                    Parts12List.Add(newList);
                    Save(filename.Parts12Data_LoadFile, Parts12List);
                }
                else
                {
                    switch (Item_Cat_Id)
                    {
                        case 72:
                            existing12List.Engine = Item_Id;
                            break;
                        case 73:
                            existing12List.Handle = Item_Id;
                            break;
                        case 74:
                            existing12List.Wheel = Item_Id;
                            break;
                        case 75:
                            existing12List.Booster = Item_Id;
                            break;
                        case 76:
                            existing12List.Coating = Item_Id;
                            break;
                        case 77:
                            existing12List.TailLamp = Item_Id;
                            break;
                        case 78:
                            existing12List.BoosterEffect = Item_Id;
                            break;
                    }
                    Save(filename.Parts12Data_LoadFile, Parts12List);
                }
                return;
            }
            var existingList = PartsList.FirstOrDefault(list => list.ID == id && list.SN == sn);
            if (existingList == null)
            {
                var newList = new Parts { ID = id, SN = sn, Engine = 0, EngineGrade = 0, EngineValue = 0, Handle = 0, HandleGrade = 0, HandleValue = 0, Wheel = 0, WheelGrade = 0, WheelValue = 0, Booster = 0, BoosterGrade = 0, BoosterValue = 0, Coating = 0, TailLamp = 0 };
                switch (Item_Cat_Id)
                {
                    case 63:
                        newList.Engine = Item_Id;
                        newList.EngineGrade = Grade;
                        newList.EngineValue = PartsValue;
                        break;
                    case 64:
                        newList.Handle = Item_Id;
                        newList.HandleGrade = Grade;
                        newList.HandleValue = PartsValue;
                        break;
                    case 65:
                        newList.Wheel = Item_Id;
                        newList.WheelGrade = Grade;
                        newList.WheelValue = PartsValue;
                        break;
                    case 66:
                        newList.Booster = Item_Id;
                        newList.BoosterGrade = Grade;
                        newList.BoosterValue = PartsValue;
                        break;
                    case 68:
                        newList.Coating = Item_Id;
                        break;
                    case 69:
                        newList.TailLamp = Item_Id;
                        break;
                }
                PartsList.Add(newList);
                Save(filename.PartsData_LoadFile, PartsList);
            }
            else
            {
                switch (Item_Cat_Id)
                {
                    case 63:
                        existingList.Engine = Item_Id;
                        existingList.EngineGrade = Grade;
                        existingList.EngineValue = PartsValue;
                        break;
                    case 64:
                        existingList.Handle = Item_Id;
                        existingList.HandleGrade = Grade;
                        existingList.HandleValue = PartsValue;
                        break;
                    case 65:
                        existingList.Wheel = Item_Id;
                        existingList.WheelGrade = Grade;
                        existingList.WheelValue = PartsValue;
                        break;
                    case 66:
                        existingList.Booster = Item_Id;
                        existingList.BoosterGrade = Grade;
                        existingList.BoosterValue = PartsValue;
                        break;
                    case 68:
                        existingList.Coating = Item_Id;
                        break;
                    case 69:
                        existingList.TailLamp = Item_Id;
                        break;
                }
                Save(filename.PartsData_LoadFile, PartsList);
            }
        }

        public static void AddLevel12List(string Nickname, short id, short sn, short level, short field, short skill, short skilllevel, short point)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            Level12Lists.TryAdd(Nickname, new List<Level12>());
            var Level12List = Level12Lists[Nickname];
            var existingList = Level12List.FirstOrDefault(list => list.ID == id && list.SN == sn);
            if (existingList == null)
            {
                var newList = new Level12 { ID = id, SN = sn, Grade = level, SkillPoints = point, Skill1 = 0, SkillGrade1 = 0, Skill2 = 0, SkillGrade2 = 0, Skill3 = 0, SkillGrade3 = 0 };
                switch (field)
                {
                    case 1:
                        newList.Skill1 = skill;
                        newList.SkillGrade1 = skilllevel;
                        break;
                    case 2:
                        newList.Skill2 = skill;
                        newList.SkillGrade2 = skilllevel;
                        break;
                    case 3:
                        newList.Skill3 = skill;
                        newList.SkillGrade3 = skilllevel;
                        break;
                }
                Level12List.Add(newList);
                Save(filename.Level12Data_LoadFile, Level12List);
            }
            else
            {
                if (level != -1)
                    existingList.Grade = level;
                if (point != -1)
                    existingList.SkillPoints = point;
                switch (field)
                {
                    case 1:
                        if (skill != -1)
                            existingList.Skill1 = skill;
                        if (skilllevel != -1)
                            existingList.SkillGrade1 = skilllevel;
                        break;
                    case 2:
                        if (skill != -1)
                            existingList.Skill2 = skill;
                        if (skilllevel != -1)
                            existingList.SkillGrade2 = skilllevel;
                        break;
                    case 3:
                        if (skill != -1)
                            existingList.Skill3 = skill;
                        if (skilllevel != -1)
                            existingList.SkillGrade3 = skilllevel;
                        break;
                }
                Save(filename.Level12Data_LoadFile, Level12List);
            }
        }

        public static void Save<T>(string fileName, T obj)
        {
            File.WriteAllText(fileName, JsonHelper.Serialize(obj));
        }
    }

    public class Tune
    {
        public short ID { get; set; } = 0;
        public short SN { get; set; } = 0;
        public short Tune1 { get; set; } = 0;
        public short Tune2 { get; set; } = 0;
        public short Tune3 { get; set; } = 0;
        public short Slot1 { get; set; } = 0;
        public short Count1 { get; set; } = 0;
        public short Slot2 { get; set; } = 0;
        public short Count2 { get; set; } = 0;
    }

    public class Plant
    {
        public short ID { get; set; } = 0;
        public short SN { get; set; } = 0;
        public short Engine { get; set; } = 0;
        public short EngineID { get; set; } = 0;
        public short Handle { get; set; } = 0;
        public short HandleID { get; set; } = 0;
        public short Wheel { get; set; } = 0;
        public short WheelID { get; set; } = 0;
        public short Kit { get; set; } = 0;
        public short KitID { get; set; } = 0;
    }

    public class Level
    {
        public short ID { get; set; } = 0;
        public short SN { get; set; } = 0;
        public short Grade { get; set; } = 0;
        public short Points { get; set; } = 0;
        public short Level1 { get; set; } = 0;
        public short Level2 { get; set; } = 0;
        public short Level3 { get; set; } = 0;
        public short Level4 { get; set; } = 0;
        public short Effect { get; set; } = 0;
    }

    public class Parts
    {
        public short ID { get; set; } = 0;
        public short SN { get; set; } = 0;
        public short Engine { get; set; } = 0;
        public byte EngineGrade { get; set; } = 0;
        public short EngineValue { get; set; } = 0;
        public short Handle { get; set; } = 0;
        public byte HandleGrade { get; set; } = 0;
        public short HandleValue { get; set; } = 0;
        public short Wheel { get; set; } = 0;
        public byte WheelGrade { get; set; } = 0;
        public short WheelValue { get; set; } = 0;
        public short Booster { get; set; } = 0;
        public byte BoosterGrade { get; set; } = 0;
        public short BoosterValue { get; set; } = 0;
        public short Coating { get; set; } = 0;
        public short TailLamp { get; set; } = 0;
    }

    public class Parts12
    {
        public short ID { get; set; } = 0;
        public short SN { get; set; } = 0;
        public short Engine { get; set; } = 0;
        public short Handle { get; set; } = 0;
        public short Wheel { get; set; } = 0;
        public short Booster { get; set; } = 0;
        public short Coating { get; set; } = 0;
        public short TailLamp { get; set; } = 0;
        public short BoosterEffect { get; set; } = 0;
        public short ExceedType { get; set; } = 0;
    }

    public class Level12
    {
        public short ID { get; set; } = 0;
        public short SN { get; set; } = 0;
        public short Grade { get; set; } = 0;
        public short SkillPoints { get; set; } = 0;
        public short Skill1 { get; set; } = 0;
        public short SkillGrade1 { get; set; } = 0;
        public short Skill2 { get; set; } = 0;
        public short SkillGrade2 { get; set; } = 0;
        public short Skill3 { get; set; } = 0;
        public short SkillGrade3 { get; set; } = 0;
    }
}
