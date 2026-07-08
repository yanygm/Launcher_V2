using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ExcData;
using KartRider.Common.Security;
using KartRider.IO.Packet;
using Profile;
using RiderData;

namespace KartRider
{
    public class Keys
    {
        public uint first_val { get; set; }
        public uint second_val { get; set; }
        public string key1 { get; set; }
        public string key2 { get; set; }
    }

    public class Channel
    {
        public string Name { get; set; }
        public byte CreateSpeed { get; set; }
        public byte GameType { get; set; }
    }

    public static class GameSupport
    {
        public static List<List<short>> Dictionary = new List<List<short>>();
        public static List<int> scenario = new List<int>();
        public static XmlNodeList QuestParams;
        public static XElement questInfo;
        public static Dictionary<byte, Channel> Channels = new Dictionary<byte, Channel>();

        public static Keys[] keys = new Keys[]
        {
            new Keys { first_val = 2919676295, second_val = 263300380, key1 = "QyvKvO60jogWDupzJ7gm0kRQdooFjWRjSjlq0gu/x2k=", key2 = "GXQstj1A95XiHvjrOGuPkzdyL+7qxETl/cPlUZk2KA4=" },
            new Keys { first_val = 3595571486, second_val = 2168420743, key1 = "+B1K8NAOvJd3cXFieRWTkRNj2rlv2qVmALSUdXFpNl0=", key2 = "TwKtPFLx+3AuKg5PFa021r3hKyFDK2sFBzQJJCI26wA=" },
            new Keys { first_val = 3059596768, second_val = 1772034572, key1 = "DI5gSCYZrEcZjR4fma5gSevvLBGSzKMoOPl7ZHDmfgA=", key2 = "bLV2VEcHkS8SrZVuPwitWN+I2851xwVEr+UBEzcYz+8=" },
            new Keys { first_val = 1412439591, second_val = 684842217, key1 = "nd65IIry0ZcguC7Ra8Ufby5xJmqMaXNXojL3OidbrsE=", key2 = "EmEHRGaDmK6Yz0GxPOVtloXvzSdYyNaQdIA/OWQez/U=" },
            new Keys { first_val = 1183929409, second_val = 4001694798, key1 = "jQre/0PRqRZ0oFW1u4jx1rj41LP+clRw2EhJ96Tfo0I=", key2 = "Hkk73+2YbVVquYu44C5jzbUwQ9XiBAs9QOdarBWspwE=" },
            new Keys { first_val = 2031112783, second_val = 2190302224, key1 = "5wpYhubc/NxIqTklY0UoZNu7ZaCRr8Zypw32i1PiHfs=", key2 = "HxjlBMdgLG97tWeLkzJ/1eWpNfDLz56z3FQTl72AecU=" },
            new Keys { first_val = 3640782532, second_val = 2489762877, key1 = "ComjZh2R0y82PVv25nzqrcqnusvQbGfngimO69PO7bc=", key2 = "pQ04kPHlUS67of2l4D3rukfTsJrSB15G4NtoAx+X8ec=" },
            new Keys { first_val = 912740103, second_val = 3754337362, key1 = "A7H8oUUAoWg65+rFF8h9xcr/aiYwecEfNQyGNF5WHhs=", key2 = "ycsTsKSzTxbOraG5PrjtBWP81YCor02tCxJquIl+5NM=" }
        };

        public static uint PcFirstMessageAsync(SessionGroup Parent)
        {
            Random random = new Random();
            int index = random.Next(keys.Length);
            Keys key = keys[index];
            string updateUrl = ProfileService.SettingConfig.PatchUrl;
            ushort ClientVersion = ProfileService.SettingConfig.ClientVersion;
            using (OutPacket outPacket = new OutPacket("PcFirstMessage"))
            {
                outPacket.WriteUShort(ProfileService.SettingConfig.LocaleID);
                outPacket.WriteUShort(1);
                outPacket.WriteUShort(ClientVersion);
                outPacket.WriteString(updateUrl);
                outPacket.WriteUInt(key.first_val);
                outPacket.WriteUInt(key.second_val);
                outPacket.WriteByte((byte)ProfileService.SettingConfig.nClientLoc);
                outPacket.WriteString(key.key1);
                outPacket.WriteBytes(new byte[31]);
                outPacket.WriteString(key.key2);
                Parent.Client.Send(outPacket);
            }
            return key.first_val ^ key.second_val;
        }

        public static void OnDisconnect(SessionGroup Parent)
        {
            Parent.Client.Disconnect();
        }

        public static void SpRpLotteryPacket(SessionGroup Parent)
        {
            using (OutPacket outPacket = new OutPacket("SpRpLotteryPacket"))
            {
                outPacket.WriteHexString("05 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
                Parent.Client.Send(outPacket);
            }
        }

        public static void PrGetGameOption(SessionGroup Parent, string Nickname)
        {
            var config = ProfileService.GetProfileConfig(Nickname);
            using (OutPacket outPacket = new OutPacket("PrGetGameOption"))
            {
                outPacket.WriteFloat(config.GameOption.Set_BGM);
                outPacket.WriteFloat(config.GameOption.Set_Sound);
                outPacket.WriteByte(config.GameOption.Main_BGM);
                outPacket.WriteByte(config.GameOption.Sound_effect);
                outPacket.WriteByte(config.GameOption.Full_screen);
                outPacket.WriteByte(config.GameOption.ShowMirror);
                outPacket.WriteByte(config.GameOption.ShowOtherPlayerNames);
                outPacket.WriteByte(config.GameOption.ShowOutlines);
                outPacket.WriteByte(config.GameOption.ShowShadows);
                outPacket.WriteByte(config.GameOption.HighLevelEffect);
                outPacket.WriteByte(config.GameOption.MotionBlurEffect);
                outPacket.WriteByte(config.GameOption.MotionDistortionEffect);
                outPacket.WriteByte(config.GameOption.HighEndOptimization);
                outPacket.WriteByte(config.GameOption.AutoReady);
                outPacket.WriteByte(config.GameOption.PropDescription);
                outPacket.WriteByte(config.GameOption.VideoQuality);
                outPacket.WriteByte(config.GameOption.BGM_Check);
                outPacket.WriteByte(config.GameOption.Sound_Check);
                outPacket.WriteByte(config.GameOption.ShowHitInfo);
                outPacket.WriteByte(config.GameOption.AutoBoost);
                outPacket.WriteByte(config.GameOption.GameType);
                outPacket.WriteByte(config.GameOption.SetGhost);
                outPacket.WriteByte(config.GameOption.SpeedType);
                outPacket.WriteByte(config.GameOption.RoomChat);
                outPacket.WriteByte(config.GameOption.DrivingChat);
                outPacket.WriteByte(config.GameOption.ShowAllPlayerHitInfo);
                outPacket.WriteByte(config.GameOption.ShowTeamColor);
                outPacket.WriteByte(config.GameOption.Set_screen);
                // outPacket.WriteByte(config.GameOption.HideCompetitiveRank);
                outPacket.WriteString(config.GameOption.QuickMsg.GetValueOrDefault(0) ?? "", false);
                outPacket.WriteString(config.GameOption.QuickMsg.GetValueOrDefault(1) ?? "", false);
                outPacket.WriteString(config.GameOption.QuickMsg.GetValueOrDefault(2) ?? "", false);
                outPacket.WriteString(config.GameOption.QuickMsg.GetValueOrDefault(3) ?? "", false);
                outPacket.WriteString(config.GameOption.QuickMsg.GetValueOrDefault(4) ?? "", false);
                outPacket.WriteString(config.GameOption.QuickMsg.GetValueOrDefault(5) ?? "", false);
                outPacket.WriteString(config.GameOption.QuickMsg.GetValueOrDefault(6) ?? "", false);
                outPacket.WriteString(config.GameOption.QuickMsg.GetValueOrDefault(7) ?? "", false);
                outPacket.WriteString(config.GameOption.QuickMsg.GetValueOrDefault(8) ?? "", false);
                outPacket.WriteString(config.GameOption.QuickMsg.GetValueOrDefault(9) ?? "", false);
                outPacket.WriteString(config.GameOption.TeamQuickMsg.GetValueOrDefault(0) ?? "", false);
                outPacket.WriteString(config.GameOption.TeamQuickMsg.GetValueOrDefault(1) ?? "", false);
                outPacket.WriteString(config.GameOption.TeamQuickMsg.GetValueOrDefault(2) ?? "", false);
                outPacket.WriteString(config.GameOption.TeamQuickMsg.GetValueOrDefault(3) ?? "", false);
                outPacket.WriteString(config.GameOption.TeamQuickMsg.GetValueOrDefault(4) ?? "", false);
                outPacket.WriteString(config.GameOption.TeamQuickMsg.GetValueOrDefault(5) ?? "", false);
                outPacket.WriteString(config.GameOption.TeamQuickMsg.GetValueOrDefault(6) ?? "", false);
                outPacket.WriteString(config.GameOption.TeamQuickMsg.GetValueOrDefault(7) ?? "", false);
                outPacket.WriteString(config.GameOption.TeamQuickMsg.GetValueOrDefault(8) ?? "", false);
                outPacket.WriteString(config.GameOption.TeamQuickMsg.GetValueOrDefault(9) ?? "", false);
                Parent.Client.Send(outPacket);
            }
        }

        public static void ChRequestChStaticReplyPacket(SessionGroup Parent)
        {
            byte[] ChannelData = Array.Empty<byte>();
            using (OutPacket outPacket = new OutPacket("ChRequestChStaticReplyPacket"))
            {
                using (OutPacket oPacket = new OutPacket())
                {
                    oPacket.WriteInt(Channels.Count);
                    foreach (var channel in Channels)
                    {
                        oPacket.WriteByte((byte)(channel.Key + 1));
                        oPacket.WriteString(channel.Value.Name);
                    }

                    oPacket.WriteInt(Channels.Count);
                    foreach (var channel in Channels)
                    {
                        oPacket.WriteByte(channel.Key);
                        oPacket.WriteByte();
                        oPacket.WriteString(channel.Value.Name);
                        oPacket.WriteByte((byte)(channel.Key + 1));
                        oPacket.WriteInt();
                    }
                    ChannelData = oPacket.ToArray();
                }
                outPacket.WriteBool(true);
                byte[] ChannelDataEncode = KREncodedBlock.Encode(ChannelData, KREncodedBlock.EncodeFlag.ZLib, null);
                outPacket.WriteInt(ChannelDataEncode.Length);
                outPacket.WriteBytes(ChannelDataEncode);
                Parent.Client.Send(outPacket);
            }
        }

        public static void PrDynamicCommand(SessionGroup Parent)
        {
            using (OutPacket outPacket = new OutPacket("PrDynamicCommand"))
            {
                outPacket.WriteByte(0);
                Parent.Client.Send(outPacket);
            }
        }

        public static void PrQuestUX2ndPacket(OutPacket outPacket)
        {
            List<uint> quest = new List<uint>();
            if (QuestParams.Count > 0)
            {
                foreach (XmlNode xn in QuestParams)
                {
                    XmlElement xe = (XmlElement)xn;
                    uint id = uint.Parse(xe.GetAttribute("id"));
                    if (!(quest.Contains(id)))
                    {
                        quest.Add(id);
                    }
                }
            }

            List<uint> ids = new List<uint>();
            string period = questInfo.Attribute("seasonPeriod").Value;
            var isInTime = IsCurrentTimeInPeriod(period);
            if (isInTime != null)
            {
                string seasonId = questInfo.Attribute("seasonId").Value;
                foreach (int group in isInTime)
                {
                    for (int index = 1; index <= 3; index++)
                    {
                        string groupStr = group.ToString("D2");  // 1 → 01
                        string indexStr = index.ToString("D2");  // 1 → 01
                        // 拼接：14500 + 组号 + 序号
                        uint id = uint.Parse($"1{seasonId}00{groupStr}{indexStr}");
                        ids.Add(id);
                    }
                }
            }

            int All_Quest = quest.Count + ids.Count;
            outPacket.WriteInt(All_Quest);
            foreach (var item in quest)
            {
                outPacket.WriteUInt(item);
                outPacket.WriteUInt(item);
                outPacket.WriteInt(0);
                outPacket.WriteShort(-1);
                outPacket.WriteShort(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(1);
                outPacket.WriteInt(0);
                outPacket.WriteByte(0);
            }
            foreach (var item in ids)
            {
                outPacket.WriteUInt(item);
                outPacket.WriteUInt(item);
                outPacket.WriteInt(0);
                outPacket.WriteShort(-1);
                outPacket.WriteShort(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(1);
                outPacket.WriteInt(0);
                outPacket.WriteByte(0);
            }
        }

        public static void GetRider(string Nickname, OutPacket outPacket)
        {
            var config = ProfileService.GetProfileConfig(Nickname);
            outPacket.WriteUShort(config.RiderItem.Set_Character);
            outPacket.WriteUShort(config.RiderItem.Set_Paint);
            outPacket.WriteUShort(config.RiderItem.Set_Kart);
            outPacket.WriteUShort(config.RiderItem.Set_Plate);
            outPacket.WriteUShort(config.RiderItem.Set_Goggle);
            outPacket.WriteUShort(config.RiderItem.Set_Balloon);
            outPacket.WriteUShort(config.RiderItem.Set_Unknown1);
            outPacket.WriteUShort(config.RiderItem.Set_HeadBand);
            outPacket.WriteUShort(config.RiderItem.Set_HeadPhone);
            outPacket.WriteUShort(config.RiderItem.Set_HandGearL);
            outPacket.WriteUShort(config.RiderItem.Set_Unknown2);
            outPacket.WriteUShort(config.RiderItem.Set_Uniform);
            outPacket.WriteUShort(config.RiderItem.Set_Decal);
            outPacket.WriteUShort(config.RiderItem.Set_Pet);
            outPacket.WriteUShort(config.RiderItem.Set_FlyingPet);
            outPacket.WriteUShort(config.RiderItem.Set_Aura);
            outPacket.WriteUShort(config.RiderItem.Set_SkidMark);
            outPacket.WriteUShort(config.RiderItem.Set_SpecialKit);
            outPacket.WriteUShort(config.RiderItem.Set_RidColor);
            outPacket.WriteUShort(config.RiderItem.Set_BonusCard);
            outPacket.WriteUShort(config.RiderItem.Set_BossModeCard);
            outPacket.WriteUShort(config.RiderItem.Set_KartPlant1);
            outPacket.WriteUShort(config.RiderItem.Set_KartPlant2);
            outPacket.WriteUShort(config.RiderItem.Set_KartPlant3);
            outPacket.WriteUShort(config.RiderItem.Set_KartPlant4);
            outPacket.WriteUShort(config.RiderItem.Set_Unknown3);
            outPacket.WriteUShort(config.RiderItem.Set_FishingPole);
            outPacket.WriteUShort(config.RiderItem.Set_Tachometer);
            outPacket.WriteUShort(config.RiderItem.Set_Dye);
            outPacket.WriteUShort(config.RiderItem.Set_KartSN);
            outPacket.WriteByte(config.RiderItem.Set_Unknown4);
            outPacket.WriteUShort(config.RiderItem.Set_KartCoating);
            outPacket.WriteUShort(config.RiderItem.Set_KartTailLamp);
            outPacket.WriteUShort(config.RiderItem.Set_slotBg);
            outPacket.WriteUShort(config.RiderItem.Set_KartCoating12);
            outPacket.WriteUShort(config.RiderItem.Set_KartTailLamp12);
            outPacket.WriteUShort(config.RiderItem.Set_KartBoosterEffect12);
            outPacket.WriteUShort(config.RiderItem.Set_Unknown5);
        }

        public static void PrGetRiderInfo(string nickname, SessionGroup Parent)
        {
            uint UserID = ClientManager.GetUserNO(nickname);
            var config = ProfileService.GetProfileConfig(nickname);
            using (OutPacket outPacket = new OutPacket("PrGetRiderInfo"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteUInt(UserID);
                outPacket.WriteString(nickname);
                outPacket.WriteString(nickname);
                outPacket.WriteDateTime(DateTime.Now);
                GameSupport.GetRider(nickname, outPacket);
                outPacket.WriteString(config.Rider.Card);
                outPacket.WriteUInt(config.Rider.RP);
                outPacket.WriteInt(0);
                outPacket.WriteByte(RiderSchool.catLevel);//Licenses
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteBytes(new byte[17]);
                outPacket.WriteShort(config.Rider.Emblem1);
                outPacket.WriteShort(config.Rider.Emblem2);
                outPacket.WriteShort(0);
                outPacket.WriteString(config.Rider.RiderIntro);
                outPacket.WriteInt(config.Rider.Premium);
                outPacket.WriteByte(1);
                if (config.Rider.Premium == 0)
                    outPacket.WriteInt(0);
                else if (config.Rider.Premium == 1)
                    outPacket.WriteInt(10000);
                else if (config.Rider.Premium == 2)
                    outPacket.WriteInt(30000);
                else if (config.Rider.Premium == 3)
                    outPacket.WriteInt(60000);
                else if (config.Rider.Premium == 4)
                    outPacket.WriteInt(120000);
                else if (config.Rider.Premium == 5)
                    outPacket.WriteInt(200000);
                else
                    outPacket.WriteInt(0);
                if (config.Rider.ClubMark_LOGO == 0)
                {
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteString("");
                }
                else
                {
                    outPacket.WriteInt(config.Rider.ClubCode);
                    outPacket.WriteInt(config.Rider.ClubMark_LOGO);
                    outPacket.WriteInt(config.Rider.ClubMark_LINE);
                    outPacket.WriteString(config.Rider.ClubName);
                }
                outPacket.WriteInt(0);
                outPacket.WriteByte(config.Rider.Ranker);
                outPacket.WriteBytes(new byte[33]);
                Parent.Client.Send(outPacket);
            }
        }

        public static void PrCheckMyClubStatePacket(SessionGroup Parent, string Nickname)
        {
            var config = ProfileService.GetProfileConfig(Nickname);
            var server = MultyPlayer.GetServerEndPoint(Parent);
            using (OutPacket outPacket = new OutPacket("PrCheckMyClubStatePacket"))
            {
                if (config.Rider.ClubMark_LOGO == 0)
                {
                    outPacket.WriteInt(0);
                    outPacket.WriteString("");
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                }
                else
                {
                    outPacket.WriteInt(config.Rider.ClubCode);
                    outPacket.WriteString(config.Rider.ClubName);
                    outPacket.WriteInt(config.Rider.ClubMark_LOGO);
                    outPacket.WriteInt(config.Rider.ClubMark_LINE);
                }
                outPacket.WriteShort(5);//Grade
                outPacket.WriteString(Nickname);
                outPacket.WriteInt(0);//ClubMember
                outPacket.WriteByte(5);//Level
                outPacket.WriteEndPoint(server.Address, (ushort)(ProfileService.SettingConfig.ServerPort + 2));
                Parent.Client.Send(outPacket);
            }
        }

        public static void GetMsgrFriendList(SessionGroup Parent, OutPacket outPacket)
        {
            outPacket.WriteInt(ClientManager.UserNOToNickname.Count - 1);
            var OnlinePlayers = ClientManager.GetOnlinePlayers();
            foreach (var User in ClientManager.UserNOToNickname.Where(u => u.Value != Parent.Client.Nickname))
            {
                outPacket.WriteUInt(User.Key);
                outPacket.WriteString(User.Value);
                outPacket.WriteUInt(ProfileService.GetProfileConfig(User.Value).Rider.RP);
                outPacket.WriteHexString("00 00 00 00 00 00");
                if (OnlinePlayers.Contains(User.Value))
                {
                    outPacket.WriteByte(1);
                }
                else
                {
                    outPacket.WriteByte(0);
                }
                outPacket.WriteHexString("00 00 00 00 00");
            }
        }

        public static void RefreshRecommendFriendList(SessionGroup Parent, OutPacket outPacket)
        {
            var OnlinePlayers = ClientManager.GetOnlinePlayers();
            outPacket.WriteInt(OnlinePlayers.Count - 1);
            foreach (var nickname in OnlinePlayers.Where(x => x != Parent.Client.Nickname))
            {
                outPacket.WriteUInt(ClientManager.GetUserNO(nickname));
                outPacket.WriteString(nickname);
                outPacket.WriteUInt(ProfileService.GetProfileConfig(nickname).Rider.RP);
                outPacket.WriteHexString("00 00 00 00 00 00");
                outPacket.WriteHexString("F2 06 00 00 00 00");
            }
        }

        public static List<short> GetTuns(List<short> tunes, short Item)
        {
            short[] speed = { 103, 203, 303, 403, 503, 603, 703, 803, 903 };
            short[] item = { 10103, 10203, 10303, 10401, 10503, 10603, 10703, 10803, 10901, 11001, 11103, 11201, 11301, 11403, 11501, 11601, 11701, 11803, 11903, 12003 };
            short[] All = new short[speed.Length + item.Length];
            Array.Copy(speed, 0, All, 0, speed.Length);
            Array.Copy(item, 0, All, speed.Length, item.Length);

            short[] sourceArray;
            if (Item == 6)
            {
                sourceArray = speed;
            }
            else if (Item == 4)
            {
                sourceArray = item;
            }
            else
            {
                sourceArray = All;
            }

            // ============= 核心逻辑 =============
            Random random = new Random();

            // 1. 获取当前列表中已存在的非0数字（不能重复使用）
            HashSet<short> usedNumbers = new HashSet<short>(tunes.Where(x => x != 0));

            // 2. 从源数组中筛选出【可用的候选数字】：不在原列表中
            var availableNumbers = sourceArray.Where(num => !usedNumbers.Contains(num)).ToList();

            // 3. 找到所有需要填充的 0 的索引
            var zeroIndexes = tunes
                .Select((value, index) => new { value, index })
                .Where(x => x.value == 0)
                .Select(x => x.index)
                .ToList();

            // 4. 随机不重复填充每个 0 位置
            foreach (int index in zeroIndexes)
            {
                // 随机取一个可用数字
                int randomIndex = random.Next(availableNumbers.Count);
                short selectedNum = availableNumbers[randomIndex];

                // 填充
                tunes[index] = selectedNum;

                // 从候选池中移除，保证不重复
                availableNumbers.RemoveAt(randomIndex);
            }

            // 输出结果查看
            return tunes;
        }

        private static List<int> IsCurrentTimeInPeriod(string period)
        {
            try
            {
                var times = period.Split('~');
                string startStr = times[0];
                string endStr = times[1];

                DateTime startTime = DateTime.Parse(startStr);
                DateTime endTime = DateTime.Parse(endStr);
                DateTime now = DateTime.Now;

                if (now < startTime || now > endTime)
                    return null;

                int days = (now.Date - startTime.Date).Days;

                // 判断属于哪一段
                if (days < 7)
                    return new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
                else if (days < 13)
                    return new List<int>() { 7, 8, 9, 10, 11, 12, 13 };
                else if (days < 19)
                    return new List<int>() { 13, 14, 15, 16, 17, 18, 19 };
                else
                    return new List<int>() { 19, 20, 21 };
            }
            catch
            {
                return null;
            }
        }
    }
}