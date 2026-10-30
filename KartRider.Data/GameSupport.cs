using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ExcData;
using KartRider.Common.Security;
using KartRider.IO.Packet;
using Profile;
using RiderData;
using KartRider.Logging;

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
        /// <summary>内部基础模式（双人组队归一化为 3/4）</summary>
        public byte GameType { get; set; }
        /// <summary>下发给客户端的原始模式号（双人组队为 67/68）</summary>
        public byte WireGameType { get; set; }
        /// <summary>是否双人组队频道</summary>
        public bool DuoTeam { get; set; }
    }

    public static class GameSupport
    {
        public static List<List<short>> Dictionary = new List<List<short>>();
        public static List<int> scenario = new List<int>();
        public static XmlNodeList QuestParams;
        public static XElement questInfo;
        public static Dictionary<byte, Channel> Channels = new Dictionary<byte, Channel>();

        /// <summary>去掉重复的 XML BOM（FF FE FF FE），避免 XDocument.Load 抛异常。</summary>
        public static byte[] StripXmlBom(byte[] data)
        {
            if (data == null) return Array.Empty<byte>();
            int offset = 0;
            while (data.Length - offset >= 4 && data[offset] == 0xFF && data[offset + 1] == 0xFE
                   && data[offset + 2] == 0xFF && data[offset + 3] == 0xFE)
            {
                offset += 2;
            }
            if (offset == 0) return data;
            byte[] result = new byte[data.Length - offset];
            Buffer.BlockCopy(data, offset, result, 0, result.Length);
            return result;
        }

        /// <summary>
        /// 统一处理客户端 XML 的编码差异，返回可直接交给 XDocument.Load / XmlDocument.Load 的 UTF-8 字节：
        /// 兼容 UTF-8（带/不带 BOM）、UTF-16LE / UTF-16BE（带/不带 BOM）、重复 BOM，
        /// 并把 XML 声明里的 encoding 改写成 utf-8。
        /// 否则像「声明 utf-16 但没有 BOM」的文件会直接抛
        /// “There is no Unicode byte order mark. Cannot switch to Unicode.”。
        /// 非 XML（二进制）内容原样返回。
        /// </summary>
        public static byte[] NormalizeXmlEncoding(byte[] data)
        {
            if (data == null || data.Length == 0) return data ?? Array.Empty<byte>();
            data = StripXmlBom(data);
            if (!NeedsEncodingNormalize(data)) return data;

            string text = DecodeXmlText(data);
            if (string.IsNullOrEmpty(text)) return data;

            // 不是 XML（例如二进制资源）就原样返回，避免改坏其它数据
            string trimmed = text.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            if (trimmed.Length == 0 || trimmed[0] != '<') return data;

            return new UTF8Encoding(false).GetBytes(RewriteDeclarationToUtf8(text));
        }

        /// <summary>是否需要做编码归一化（已经是无 BOM 的 UTF-8 且声明也是 utf-8 时直接跳过）。</summary>
        private static bool NeedsEncodingNormalize(byte[] data)
        {
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) return true;
            if (data.Length >= 2 && ((data[0] == 0xFF && data[1] == 0xFE) || (data[0] == 0xFE && data[1] == 0xFF))) return true;
            if (IsUtf16WithoutBom(data)) return true;

            int sample = Math.Min(data.Length, 256);
            string head = Encoding.ASCII.GetString(data, 0, sample);
            int encodingIndex = head.IndexOf("encoding", StringComparison.OrdinalIgnoreCase);
            if (encodingIndex < 0) return false;
            string rest = head.Substring(encodingIndex, Math.Min(head.Length - encodingIndex, 40)).ToLowerInvariant();
            return !rest.Contains("utf-8");
        }

        /// <summary>无 BOM 时用零字节比例判断是否为 UTF-16（XML 里 ASCII 居多，UTF-16 会有一半 0 字节）。</summary>
        private static bool IsUtf16WithoutBom(byte[] data)
        {
            int sample = Math.Min(data.Length, 512);
            if (sample < 4) return false;
            int zeros = 0;
            for (int i = 0; i < sample; i++)
            {
                if (data[i] == 0) zeros++;
            }
            return zeros * 8 >= sample;
        }

        /// <summary>按字节特征解码 XML 文本（BOM → 无 BOM 的 UTF-16 → UTF-8）。</summary>
        private static string DecodeXmlText(byte[] data)
        {
            int offset = 0;
            Encoding encoding;
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                encoding = new UTF8Encoding(false);
                offset = 3;
            }
            else if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
            {
                encoding = Encoding.Unicode;
                offset = 2;
            }
            else if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
            {
                encoding = Encoding.BigEndianUnicode;
                offset = 2;
            }
            else if (IsUtf16WithoutBom(data))
            {
                // 大端时首字节是 0（"<\0" 是 LE，"\0<" 是 BE）
                encoding = data[0] == 0 ? Encoding.BigEndianUnicode : Encoding.Unicode;
            }
            else
            {
                encoding = new UTF8Encoding(false);
            }

            try
            {
                return encoding.GetString(data, offset, data.Length - offset).TrimStart('\uFEFF');
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>把 XML 声明里的 encoding 统一改写为 utf-8（没有声明则原样返回）。</summary>
        private static string RewriteDeclarationToUtf8(string text)
        {
            int start = text.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return text;
            int end = text.IndexOf("?>", start, StringComparison.Ordinal);
            if (end < 0) return text;

            string declaration = text.Substring(start, end - start);
            int encodingIndex = declaration.IndexOf("encoding", StringComparison.OrdinalIgnoreCase);
            if (encodingIndex < 0) return text;

            int quoteStart = declaration.IndexOfAny(new[] { '"', '\'' }, encodingIndex);
            if (quoteStart < 0) return text;
            char quote = declaration[quoteStart];
            int quoteEnd = declaration.IndexOf(quote, quoteStart + 1);
            if (quoteEnd < 0) return text;

            string newDeclaration = declaration.Substring(0, quoteStart + 1) + "utf-8" + declaration.Substring(quoteEnd);
            return text.Substring(0, start) + newDeclaration + text.Substring(end);
        }

        public static bool IsDuoTeamChannel(byte channelIndex) =>
            Channels.TryGetValue(channelIndex, out var ch) && ch.DuoTeam;

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
            string updateUrl = ""; // PatchUrl 配置项已移除，客户端不再检查更新
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

        public static void PrGetGameOption(SessionGroup Parent, string Nickname)
        {
            var config = ProfileService.GetProfileConfig(Nickname);
            if (config?.GameOption == null)
            {
                Log.Info($"[PrGetGameOption] Warning: ProfileConfig or GameOption is null for {Nickname}");
                return;
            }
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
            if (QuestParams != null && QuestParams.Count > 0)
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
            if (questInfo != null)
            {
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
            if (config?.RiderItem == null)
            {
                // 档案缺失：使用 RiderItemData 默认初始数据补齐，保证包结构完整，避免畸形包影响其他玩家
                Log.Info($"[GetRider] Warning: ProfileConfig or RiderItem is null for {Nickname}, writing default rider data");
                WriteRiderItem(new RiderItemData(), outPacket);
                return;
            }
            WriteRiderItem(config.RiderItem, outPacket);
        }

        /// <summary>
        /// 按固定字段顺序写入装备数据（30×UShort + 1×Byte + 7×UShort）。
        /// </summary>
        static void WriteRiderItem(RiderItemData item, OutPacket outPacket)
        {
            outPacket.WriteUShort(item.Set_Character);
            outPacket.WriteUShort(item.Set_Paint);
            outPacket.WriteUShort(item.Set_Kart);
            outPacket.WriteUShort(item.Set_Plate);
            outPacket.WriteUShort(item.Set_Goggle);
            outPacket.WriteUShort(item.Set_Balloon);
            outPacket.WriteUShort(item.Set_Unknown1);
            outPacket.WriteUShort(item.Set_HeadBand);
            outPacket.WriteUShort(item.Set_HeadPhone);
            outPacket.WriteUShort(item.Set_HandGearL);
            outPacket.WriteUShort(item.Set_Unknown2);
            outPacket.WriteUShort(item.Set_Uniform);
            outPacket.WriteUShort(item.Set_Decal);
            outPacket.WriteUShort(item.Set_Pet);
            outPacket.WriteUShort(item.Set_FlyingPet);
            outPacket.WriteUShort(item.Set_Aura);
            outPacket.WriteUShort(item.Set_SkidMark);
            outPacket.WriteUShort(item.Set_SpecialKit);
            outPacket.WriteUShort(item.Set_RidColor);
            outPacket.WriteUShort(item.Set_BonusCard);
            outPacket.WriteUShort(item.Set_BossModeCard);
            outPacket.WriteUShort(item.Set_KartPlant1);
            outPacket.WriteUShort(item.Set_KartPlant2);
            outPacket.WriteUShort(item.Set_KartPlant3);
            outPacket.WriteUShort(item.Set_KartPlant4);
            outPacket.WriteUShort(item.Set_Unknown3);
            outPacket.WriteUShort(item.Set_FishingPole);
            outPacket.WriteUShort(item.Set_Tachometer);
            outPacket.WriteUShort(item.Set_Dye);
            outPacket.WriteUShort(item.Set_KartSN);
            outPacket.WriteByte(item.Set_Unknown4);
            outPacket.WriteUShort(item.Set_KartCoating);
            outPacket.WriteUShort(item.Set_KartTailLamp);
            outPacket.WriteUShort(item.Set_slotBg);
            outPacket.WriteUShort(item.Set_KartCoating12);
            outPacket.WriteUShort(item.Set_KartTailLamp12);
            outPacket.WriteUShort(item.Set_KartBoosterEffect12);
            outPacket.WriteUShort(item.Set_Unknown5);
        }

        public static void PrGetRiderInfo(string nickname, SessionGroup Parent)
        {
            uint UserID = ClientManager.GetUserNO(nickname);
            var config = ProfileService.GetProfileConfig(nickname);
            if (config?.Rider == null)
            {
                Log.Info($"[PrGetRiderInfo] Warning: ProfileConfig or Rider is null for {nickname}");
                return;
            }
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
            if (config?.Rider == null)
            {
                Log.Info($"[PrCheckMyClubStatePacket] Warning: ProfileConfig or Rider is null for {Nickname}");
                return;
            }
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

        public static void GetMsgrFriendList(OutPacket outPacket, int totalCount, List<KeyValuePair<uint, string>> segment)
        {
            outPacket.WriteInt(segment.Count);
            var OnlinePlayers = ClientManager.GetOnlinePlayers();
            foreach (var User in segment)
            {
                outPacket.WriteUInt(User.Key);
                outPacket.WriteString(User.Value);
                outPacket.WriteUInt(ProfileService.GetProfileConfig(User.Value)?.Rider?.RP ?? 0);
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
            var OnlinePlayers = ClientManager.GetOnlinePlayers()
                .Where(x => x != Parent.Client.Nickname)
                .OrderBy(x => Guid.NewGuid())
                .Take(10)
                .ToList();
            outPacket.WriteInt(OnlinePlayers.Count);
            foreach (var nickname in OnlinePlayers)
            {
                outPacket.WriteUInt(ClientManager.GetUserNO(nickname));
                outPacket.WriteString(nickname);
                outPacket.WriteUInt(ProfileService.GetProfileConfig(nickname)?.Rider?.RP ?? 0);
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