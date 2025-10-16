using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO.Packet;
using KartRider;
using ExcData;
using System.Xml;
using System.Linq;
using Profile;

namespace RiderData
{
    public static class NewRider
    {
        public static Dictionary<short, Dictionary<short, string>> items = new Dictionary<short, Dictionary<short, string>>();
        public static List<List<short>> NewKart = new List<List<short>>();
        private static readonly HashSet<short> excludedKeys = new HashSet<short>{ 3, 6, 10, 15, 19, 24, 25, 29, 33, 34, 35, 40, 41, 47, 48, 50, 51, 56, 57, 58, 60, 62, 63, 64, 65, 66, 72, 73, 74, 75 };
        private static readonly HashSet<short> ValidItemCatIds = new HashSet<short> { 1, 2, 4, 8, 11, 12, 13, 14, 16, 18, 20, 21, 26, 27, 28, 31, 52, 61, 70, 71 };

        public static void LoadItemData(SessionGroup Parent)
        {
            KartExcData.Tune_ExcData(Parent);
            KartExcData.Plant_ExcData(Parent);
            KartExcData.Level_ExcData(Parent);
            KartExcData.Parts_ExcData(Parent);
            KartExcData.Level12_ExcData(Parent);
            KartExcData.Parts12_ExcData(Parent);
            NewRider.XUniquePartsData(Parent);
            NewRider.XLegendPartsData(Parent);
            NewRider.XRarePartsData(Parent);
            NewRider.XNormalPartsData(Parent);
            NewRider.V1UniquePartsData(Parent);
            NewRider.V1LegendPartsData(Parent);
            NewRider.V1RarePartsData(Parent);
            NewRider.V1NormalPartsData(Parent);
            NewRider.partsEngine12(Parent);
            NewRider.partsHandle12(Parent);
            NewRider.partsWheel12(Parent);
            NewRider.partsBooster12(Parent);
            NewRider.Items(Parent);
            NewRider.NewKart1(Parent);
            NewRider.NewKart2(Parent);
            NewRider.NewRiderData(Parent);//라이더 인식
        }

        public static void NewRiderData(SessionGroup Parent)
        {
            using (OutPacket oPacket = new OutPacket("PrGetRider"))
            {
                oPacket.WriteByte(1);
                oPacket.WriteByte(0);
                oPacket.WriteString(ProfileService.ProfileConfig.Rider.Nickname);
                oPacket.WriteShort(0);
                oPacket.WriteShort(0);
                oPacket.WriteShort(ProfileService.ProfileConfig.Rider.Emblem1);
                oPacket.WriteShort(ProfileService.ProfileConfig.Rider.Emblem2);
                oPacket.WriteShort(0);
                GameSupport.GetRider(oPacket);
                oPacket.WriteShort(0);
                oPacket.WriteString(ProfileService.ProfileConfig.Rider.Card);
                oPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Lucci);
                oPacket.WriteUInt(ProfileService.ProfileConfig.Rider.RP);
                oPacket.WriteBytes(new byte[94]);
                Parent.Client.Send(oPacket);
            }
        }

        public static void NewKart1(SessionGroup Parent)
        {
            short sn = 1;
            int range = 100;//分批次数
            if (items.TryGetValue(3, out Dictionary<short, string> resultDict))
            {
                List<short> kart = new List<short>(resultDict.Keys);
                int times = kart.Count / range + (kart.Count % range > 0 ? 1 : 0);
                for (int i = 0; i < times; i++)
                {
                    var tempList = kart.GetRange(i * range, (i + 1) * range > kart.Count ? (kart.Count - i * range) : range);
                    int Count = tempList.Count;
                    using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                    {
                        outPacket.WriteByte(1);
                        outPacket.WriteInt(Count);
                        foreach (var Kart in tempList)
                        {
                            outPacket.WriteShort(3);
                            outPacket.WriteShort(Kart);
                            outPacket.WriteShort(sn);
                            outPacket.WriteShort(1);//数量
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(-1);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                        }
                        Parent.Client.Send(outPacket);
                    }
                }
            }
        }

        public static void NewKart2(SessionGroup Parent)
        {
            int range = 100;//分批次数
            int times = NewKart.Count / range + (NewKart.Count % range > 0 ? 1 : 0);
            for (int i = 0; i < times; i++)
            {
                var tempList = NewKart.GetRange(i * range, (i + 1) * range > NewKart.Count ? (NewKart.Count - i * range) : range);
                int Count = tempList.Count;
                using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                {
                    outPacket.WriteByte(1);
                    outPacket.WriteInt(Count);
                    foreach (var Kart in tempList)
                    {
                        outPacket.WriteShort(3);
                        outPacket.WriteShort(Kart[0]);
                        outPacket.WriteShort(Kart[1]);
                        outPacket.WriteShort(1);//数量
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(-1);
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(0);
                    }
                    Parent.Client.Send(outPacket);
                }
            }
        }

        public static void Items(SessionGroup Parent)
        {
            foreach (var category in items)
            {
                short itemCatId = category.Key;
                if (!excludedKeys.Contains(itemCatId))
                {
                    List<List<ushort>> items = new List<List<ushort>>();
                    foreach (var item in category.Value)
                    {
                        short sn = 0;
                        ushort num = ProfileService.ProfileConfig.Rider.SlotChanger;
                        short id = item.Key;
                        if (ValidItemCatIds.Contains(itemCatId))
                        {
                            num = 1;
                        }
                        if (itemCatId == 7)
                        {
                            if (id == 3 || id == 4)
                            {
                                List<ushort> add = new List<ushort> { (ushort)id, (ushort)sn, 1 };
                                items.Add(add);
                            }
                            else
                            {
                                List<ushort> add = new List<ushort> { (ushort)id, (ushort)sn, num };
                                items.Add(add);
                            }
                        }
                        else if (itemCatId == 14)
                        {
                            if (id == 22 || id == 23 || id == 31 || id == 37 || id == 53 || id == 57 || id == 99)
                            {
                                List<ushort> add = new List<ushort> { (ushort)id, (ushort)sn, num };
                                items.Add(add);
                            }
                        }
                        else if(itemCatId == 23)
                        {
                            if (id == 1)
                            {
                                List<ushort> add = new List<ushort> { (ushort)id, (ushort)sn, num };
                                items.Add(add);
                            }
                            else if (id == 3 || id == 89 || id == 97 || id == 98 || id == 99 || id == 100 || id == 106)
                            {
                                List<ushort> add = new List<ushort> { (ushort)id, (ushort)sn, 1 };
                                items.Add(add);
                            }
                        }
                        else if (itemCatId == 28)
                        {
                            if (id != 50 && id != 37)
                            {
                                List<ushort> add = new List<ushort> { (ushort)id, (ushort)sn, num };
                                items.Add(add);
                            }
                        }
                        else
                        {
                            List<ushort> add = new List<ushort> { (ushort)id, (ushort)sn, num };
                            items.Add(add);
                        }
                    }
                    LoRpGetRiderItemPacket(Parent, itemCatId, items);
                }
            }
        }

        public static void partsEngine12(SessionGroup Parent)
        {
            if (items.TryGetValue(72, out Dictionary<short, string> resultDict))
            {
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
                {
                    int count = resultDict.Count;
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(count);
                    foreach (var kvp in resultDict)
                    {
                        short id = (short)kvp.Key;
                        oPacket.WriteShort(72);
                        oPacket.WriteShort(id);
                        oPacket.WriteShort(0);
                        oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                        oPacket.WriteByte(0);
                        oPacket.WriteByte(0);
                        oPacket.WriteShort(-1);
                        oPacket.WriteShort(-1);
                        oPacket.WriteByte(1);
                        if (id < 11)
                        {
                            oPacket.WriteByte(4);
                        }
                        else if (id < 21)
                        {
                            oPacket.WriteByte(3);
                        }
                        else if (id < 31)
                        {
                            oPacket.WriteByte(2);
                        }
                        else if (id < 41)
                        {
                            oPacket.WriteByte(1);
                        }
                        oPacket.WriteShort(V2Spec.Get12Parts(id));
                    }
                    Parent.Client.Send(oPacket);
                }
            }
        }

        public static void partsHandle12(SessionGroup Parent)
        {
            if (items.TryGetValue(73, out Dictionary<short, string> resultDict))
            {
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
                {
                    int count = resultDict.Count;
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(count);
                    foreach (var kvp in resultDict)
                    {
                        short id = (short)kvp.Key;
                        oPacket.WriteShort(73);
                        oPacket.WriteShort(id);
                        oPacket.WriteShort(0);
                        oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                        oPacket.WriteByte(0);
                        oPacket.WriteByte(0);
                        oPacket.WriteShort(-1);
                        oPacket.WriteShort(-1);
                        oPacket.WriteByte(1);
                        if (id < 11)
                        {
                            oPacket.WriteByte(4);
                        }
                        else if (id < 21)
                        {
                            oPacket.WriteByte(3);
                        }
                        else if (id < 31)
                        {
                            oPacket.WriteByte(2);
                        }
                        else if (id < 41)
                        {
                            oPacket.WriteByte(1);
                        }
                        oPacket.WriteShort(V2Spec.Get12Parts(id));
                    }
                    Parent.Client.Send(oPacket);
                }
            }
        }

        public static void partsWheel12(SessionGroup Parent)
        {
            if (items.TryGetValue(74, out Dictionary<short, string> resultDict))
            {
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
                {
                    int count = resultDict.Count;
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(count);
                    foreach (var kvp in resultDict)
                    {
                        short id = (short)kvp.Key;
                        oPacket.WriteShort(74);
                        oPacket.WriteShort(id);
                        oPacket.WriteShort(0);
                        oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                        oPacket.WriteByte(0);
                        oPacket.WriteByte(0);
                        oPacket.WriteShort(-1);
                        oPacket.WriteShort(-1);
                        oPacket.WriteByte(1);
                        if (id < 11)
                        {
                            oPacket.WriteByte(4);
                        }
                        else if (id < 21)
                        {
                            oPacket.WriteByte(3);
                        }
                        else if (id < 31)
                        {
                            oPacket.WriteByte(2);
                        }
                        else if (id < 41)
                        {
                            oPacket.WriteByte(1);
                        }
                        oPacket.WriteShort(V2Spec.Get12Parts(id));
                    }
                    Parent.Client.Send(oPacket);
                }
            }
        }

        public static void partsBooster12(SessionGroup Parent)
        {
            if (items.TryGetValue(75, out Dictionary<short, string> resultDict))
            {
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
                {
                    int count = resultDict.Count;
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(count);
                    foreach (var kvp in resultDict)
                    {
                        short id = (short)kvp.Key;
                        oPacket.WriteShort(75);
                        oPacket.WriteShort(id);
                        oPacket.WriteShort(0);
                        oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                        oPacket.WriteByte(0);
                        oPacket.WriteByte(0);
                        oPacket.WriteShort(-1);
                        oPacket.WriteShort(-1);
                        oPacket.WriteByte(1);
                        if (id < 11)
                        {
                            oPacket.WriteByte(4);
                        }
                        else if (id < 21)
                        {
                            oPacket.WriteByte(3);
                        }
                        else if (id < 31)
                        {
                            oPacket.WriteByte(2);
                        }
                        else if (id < 41)
                        {
                            oPacket.WriteByte(1);
                        }
                        oPacket.WriteShort(V2Spec.Get12Parts(id));
                    }
                    Parent.Client.Send(oPacket);
                }
            }
        }

        public static void XUniquePartsData(SessionGroup Parent)
        {
            using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
            {
                oPacket.WriteInt(1);
                oPacket.WriteInt(1);
                oPacket.WriteInt(40);
                byte Grade = 1;
                //-----------------------------------------------------------------X 유니크 파츠
                for (short i = 1053; i <= 1080; i += 3)
                {
                    oPacket.WriteShort(63);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1053; i <= 1080; i += 3)
                {
                    oPacket.WriteShort(64);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1053; i <= 1080; i += 3)
                {
                    oPacket.WriteShort(65);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1053; i <= 1080; i += 3)
                {
                    oPacket.WriteShort(66);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                Parent.Client.Send(oPacket);
            }
        }

        public static void XLegendPartsData(SessionGroup Parent)
        {
            using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
            {
                oPacket.WriteInt(1);
                oPacket.WriteInt(1);
                oPacket.WriteInt(40);
                byte Grade = 2;
                //-----------------------------------------------------------------X 레전드 파츠
                for (short i = 1005; i <= 1050; i += 5)
                {
                    oPacket.WriteShort(63);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1005; i <= 1050; i += 5)
                {
                    oPacket.WriteShort(64);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1005; i <= 1050; i += 5)
                {
                    oPacket.WriteShort(65);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1005; i <= 1050; i += 5)
                {
                    oPacket.WriteShort(66);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                Parent.Client.Send(oPacket);
            }
        }

        public static void XRarePartsData(SessionGroup Parent)
        {
            using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
            {
                oPacket.WriteInt(1);
                oPacket.WriteInt(1);
                oPacket.WriteInt(40);
                byte Grade = 3;
                //-----------------------------------------------------------------X 레어 파츠
                for (short i = 910; i <= 1000; i += 10)
                {
                    oPacket.WriteShort(63);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 910; i <= 1000; i += 10)
                {
                    oPacket.WriteShort(64);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 910; i <= 1000; i += 10)
                {
                    oPacket.WriteShort(65);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 910; i <= 1000; i += 10)
                {
                    oPacket.WriteShort(66);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                Parent.Client.Send(oPacket);
            }
        }

        public static void XNormalPartsData(SessionGroup Parent)
        {
            using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
            {
                oPacket.WriteInt(1);
                oPacket.WriteInt(1);
                oPacket.WriteInt(40);
                byte Grade = 4;
                //-----------------------------------------------------------------X 일반 파츠
                for (short i = 810; i <= 900; i += 10)
                {
                    oPacket.WriteShort(63);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 810; i <= 900; i += 10)
                {
                    oPacket.WriteShort(64);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 810; i <= 900; i += 10)
                {
                    oPacket.WriteShort(65);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 810; i <= 900; i += 10)
                {
                    oPacket.WriteShort(66);
                    oPacket.WriteShort(1);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                Parent.Client.Send(oPacket);
            }
        }

        //-----------------------------------------------------------------------------------------------V1 파츠 관련
        public static void V1UniquePartsData(SessionGroup Parent)
        {
            using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
            {
                oPacket.WriteInt(1);
                oPacket.WriteInt(1);
                oPacket.WriteInt(40);
                byte Grade = 1;
                //-----------------------------------------------------------------V1 유니크 파츠
                for (short i = 1153; i <= 1180; i += 3)
                {
                    oPacket.WriteShort(63);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1053; i <= 1080; i += 3)
                {
                    oPacket.WriteShort(64);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1153; i <= 1180; i += 3)
                {
                    oPacket.WriteShort(65);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1053; i <= 1080; i += 3)
                {
                    oPacket.WriteShort(66);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                Parent.Client.Send(oPacket);
            }
        }

        public static void V1LegendPartsData(SessionGroup Parent)
        {
            using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
            {
                oPacket.WriteInt(1);
                oPacket.WriteInt(1);
                oPacket.WriteInt(40);
                byte Grade = 2;
                //-----------------------------------------------------------------V1 레전드 파츠
                for (short i = 1105; i <= 1150; i += 5)
                {
                    oPacket.WriteShort(63);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1005; i <= 1050; i += 5)
                {
                    oPacket.WriteShort(64);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1105; i <= 1150; i += 5)
                {
                    oPacket.WriteShort(65);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1005; i <= 1050; i += 5)
                {
                    oPacket.WriteShort(66);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                Parent.Client.Send(oPacket);
            }
        }

        public static void V1RarePartsData(SessionGroup Parent)
        {
            using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
            {
                oPacket.WriteInt(1);
                oPacket.WriteInt(1);
                oPacket.WriteInt(40);
                byte Grade = 3;
                //-----------------------------------------------------------------V1 레어 파츠
                for (short i = 1010; i <= 1100; i += 10)
                {
                    oPacket.WriteShort(63);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 910; i <= 1000; i += 10)
                {
                    oPacket.WriteShort(64);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 1010; i <= 1100; i += 10)
                {
                    oPacket.WriteShort(65);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 910; i <= 1000; i += 10)
                {
                    oPacket.WriteShort(66);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                Parent.Client.Send(oPacket);
            }
        }

        public static void V1NormalPartsData(SessionGroup Parent)
        {
            using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
            {
                oPacket.WriteInt(1);
                oPacket.WriteInt(1);
                oPacket.WriteInt(40);
                byte Grade = 4;
                //-----------------------------------------------------------------V1 일반 파츠
                for (short i = 910; i <= 1000; i += 10)
                {
                    oPacket.WriteShort(63);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 810; i <= 900; i += 10)
                {
                    oPacket.WriteShort(64);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 910; i <= 1000; i += 10)
                {
                    oPacket.WriteShort(65);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                for (short i = 810; i <= 900; i += 10)
                {
                    oPacket.WriteShort(66);
                    oPacket.WriteShort(2);
                    oPacket.WriteShort(0);
                    oPacket.WriteUShort(ProfileService.ProfileConfig.Rider.SlotChanger);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteShort(-1);
                    oPacket.WriteShort(-1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(Grade);
                    oPacket.WriteShort(i);
                }
                Parent.Client.Send(oPacket);
            }
        }

        public static void LoRpGetRiderItemPacket(SessionGroup Parent, short itemCat, List<List<ushort>> item)
        {
            int range = 100;//分批次数
            int times = item.Count / range + (item.Count % range > 0 ? 1 : 0);
            for (int i = 0; i < times; i++)
            {
                var tempList = item.GetRange(i * range, (i + 1) * range > item.Count ? (item.Count - i * range) : range);
                using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
                {
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(tempList.Count);
                    for (int f = 0; f < tempList.Count; f++)
                    {
                        oPacket.WriteShort(itemCat);
                        oPacket.WriteUShort(tempList[f][0]);
                        oPacket.WriteUShort(tempList[f][1]);
                        oPacket.WriteUShort(tempList[f][2]);
                        oPacket.WriteByte((byte)((Program.PreventItem ? 1 : 0)));
                        oPacket.WriteByte(0);
                        oPacket.WriteShort(-1);
                        oPacket.WriteShort(0);
                        oPacket.WriteByte(0);
                        oPacket.WriteByte(0);
                        oPacket.WriteShort(0);
                    }
                    Parent.Client.Send(oPacket);
                }
            }
        }
    }
}
