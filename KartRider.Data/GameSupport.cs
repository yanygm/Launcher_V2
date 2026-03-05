using System;
using System.Collections.Generic;
using System.Linq;
using ExcData;
using KartRider.IO.Packet;
using Profile;

namespace KartRider
{
    public class Keys
    {
        public uint first_val { get; set; }
        public uint second_val { get; set; }
        public string key1 { get; set; }
        public string key2 { get; set; }
    }

    public static class GameSupport
    {
        public static List<List<short>> Dictionary = new List<List<short>>();
        public static List<int> scenario = new List<int>();
        public static List<int> quest = new List<int>();
        public static int seasonId = 0;

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

        public static uint PcFirstMessage(SessionGroup Parent)
        {
            Random random = new Random();
            int index = random.Next(keys.Length);
            Keys key = keys[index];
            using (OutPacket outPacket = new OutPacket("PcFirstMessage"))
            {
                outPacket.WriteUShort(ProfileService.SettingConfig.LocaleID);
                outPacket.WriteUShort(1);
                outPacket.WriteUShort(ProfileService.SettingConfig.ClientVersion);
                outPacket.WriteString("https://github.com/yanygm/Launcher_V2");
                outPacket.WriteUInt(key.first_val);
                outPacket.WriteUInt(key.second_val);
                outPacket.WriteByte((byte)ProfileService.SettingConfig.nClientLoc);
                outPacket.WriteString(key.key1);
                int[] time = new int[] { 1547597728, 1707244048, 1862052984 };
                outPacket.WriteInt(time.Length);
                foreach (int t in time)
                {
                    outPacket.WriteInt();
                    outPacket.WriteInt(t);
                }
                outPacket.WriteBytes(new byte[3]);
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
            using (OutPacket outPacket = new OutPacket("PrGetGameOption"))
            {
                outPacket.WriteFloat(ProfileService.ProfileConfigs[Nickname].GameOption.Set_BGM);
                outPacket.WriteFloat(ProfileService.ProfileConfigs[Nickname].GameOption.Set_Sound);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.Main_BGM);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.Sound_effect);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.Full_screen);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.ShowMirror);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.ShowOtherPlayerNames);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.ShowOutlines);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.ShowShadows);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.HighLevelEffect);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.MotionBlurEffect);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.MotionDistortionEffect);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.HighEndOptimization);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.AutoReady);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.PropDescription);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.VideoQuality);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.BGM_Check);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.Sound_Check);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.ShowHitInfo);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.AutoBoost);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.GameType);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.SetGhost);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.SpeedType);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.RoomChat);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.DrivingChat);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.ShowAllPlayerHitInfo);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.ShowTeamColor);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.Set_screen);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].GameOption.HideCompetitiveRank);
                outPacket.WriteBytes(new byte[79]);
                Parent.Client.Send(outPacket);
            }
        }

        public static void ChRpEnterMyRoomPacket(SessionGroup Parent, string Nickname)
        {
            if (ProfileService.ProfileConfigs[Nickname].Rider.EnterMyRoomType == 0)
            {
                using (OutPacket outPacket = new OutPacket("ChRpEnterMyRoomPacket"))
                {
                    outPacket.WriteString(Nickname);
                    outPacket.WriteByte(0);
                    outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].MyRoom.MyRoom);
                    outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].MyRoom.MyRoomBGM);
                    outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].MyRoom.UseRoomPwd);
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].MyRoom.UseItemPwd);
                    outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].MyRoom.TalkLock);
                    outPacket.WriteString(ProfileService.ProfileConfigs[Nickname].MyRoom.RoomPwd);
                    outPacket.WriteString("");
                    outPacket.WriteString(ProfileService.ProfileConfigs[Nickname].MyRoom.ItemPwd);
                    outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].MyRoom.MyRoomKart1);
                    outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].MyRoom.MyRoomKart2);
                    Parent.Client.Send(outPacket);
                }
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("ChRpEnterMyRoomPacket"))
                {
                    outPacket.WriteString("");
                    outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].Rider.EnterMyRoomType);
                    outPacket.WriteShort(0);
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(1);
                    outPacket.WriteString("");//RoomPwd
                    outPacket.WriteString("");
                    outPacket.WriteString("");//ItemPwd 
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(0);
                    Parent.Client.Send(outPacket);
                }
            }
        }

        public static void RmNotiMyRoomInfoPacket(SessionGroup Parent, string Nickname)
        {
            using (OutPacket outPacket = new OutPacket("RmNotiMyRoomInfoPacket"))
            {
                outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].MyRoom.MyRoom);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].MyRoom.MyRoomBGM);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].MyRoom.UseRoomPwd);
                outPacket.WriteByte(0);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].MyRoom.UseItemPwd);
                outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].MyRoom.TalkLock);
                outPacket.WriteString(ProfileService.ProfileConfigs[Nickname].MyRoom.RoomPwd);
                outPacket.WriteString("");
                outPacket.WriteString(ProfileService.ProfileConfigs[Nickname].MyRoom.ItemPwd);
                outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].MyRoom.MyRoomKart1);
                outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].MyRoom.MyRoomKart2);
                Parent.Client.Send(outPacket);
            }
        }

        public static void PrCheckMyClubStatePacket(SessionGroup Parent, string Nickname)
        {
            using (OutPacket outPacket = new OutPacket("PrCheckMyClubStatePacket"))
            {
                if (ProfileService.ProfileConfigs[Nickname].Rider.ClubMark_LOGO == 0)
                {
                    outPacket.WriteInt(0);
                    outPacket.WriteString("");
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                }
                else
                {
                    outPacket.WriteInt(ProfileService.ProfileConfigs[Nickname].Rider.ClubCode);
                    outPacket.WriteString(ProfileService.ProfileConfigs[Nickname].Rider.ClubName);
                    outPacket.WriteInt(ProfileService.ProfileConfigs[Nickname].Rider.ClubMark_LOGO);
                    outPacket.WriteInt(ProfileService.ProfileConfigs[Nickname].Rider.ClubMark_LINE);
                }
                outPacket.WriteShort(5);//Grade
                outPacket.WriteString(Nickname);
                outPacket.WriteInt(1);//ClubMember
                outPacket.WriteByte(5);//Level
                outPacket.WriteHexString("A2 0E 90 AB 9A 99");
                Parent.Client.Send(outPacket);
            }
        }

        public static void ChRequestChStaticReplyPacket(SessionGroup Parent)
        {
            using (OutPacket outPacket = new OutPacket("ChRequestChStaticReplyPacket"))
            {
                outPacket.WriteHexString("01 31 03 00 00 53 01 5F 72 2D 73 11 0E 00 00 78 DA 9D 56 67 53 DB 40 10 7D A1 19 1B DB 72 A3 98 90 DE 7B 42 7A 03 42 77 12 CA 00 DF 33 D8 16 0E 13 81 19 5B 09 C9 BF CF EE C9 17 4B 66 D1 9D 33 9A 91 EE 6E DF D6 5B ED EE 16 80 4F 0E BD 5A 68 62 1F 75 2C C2 A3 55 0D 3F F0 0D 6D 9C C0 A5 A7 8E 64 86 20 DD 6D 05 C7 F4 3E C4 2A B1 B4 E1 63 80 25 1C D2 C2 C5 11 36 E9 7D 4A 90 60 B7 47 EF 7D FA 0E 9E 0F D1 D2 86 8A 46 29 5A E1 70 D1 28 4D 43 47 92 11 D3 B5 A4 44 52 F4 68 34 23 A2 B5 B0 54 2E 96 EC AA F7 58 2E 36 58 01 28 9D B7 90 34 8B 4C DE 42 D6 2C B2 E3 C6 D0 05 A7 55 42 B9 70 C6 8D E1 0B C3 73 8E 68 6B 18 92 77 44 3B C3 90 02 87 A5 81 9F B4 F1 14 51 52 5F 4C 45 40 5D 89 A5 2C 11 6A F8 AE 34 9F 10 B9 49 2A DA D8 A1 EF 1F 3A F3 30 3E 42 80 2A 2D 7D 7A 3C E2 99 E0 83 03 A2 1F 91 38 8F 08 93 A3 EA 80 97 8D 7F 4E 4C 85 0F B5 D9 65 F9 06 F9 7B 40 DF 63 65 FC B4 9C 0B 51 D0 C5 54 28 D2 1A B2 46 4C 33 79 81 B0 42 5C 81 8F C7 44 61 D8 A5 31 51 09 93 2E 3B B1 19 C4 90 2B 05 8B 24 63 E0 D5 AC 0A 7B 4B 69 AE D3 41 8B 6C FB 4D 25 40 5B 78 CD 39 07 D0 95 7E BD 68 84 E8 40 DE 30 43 B5 AD 37 A7 AD A5 46 23 7F AB 6C 70 49 CE FB DB FD B1 69 33 EF E4 55 F6 35 A9 6E 56 55 BA 05 EB 5E E9 77 A7 62 61 B2 49 F7 E2 65 6B 13 EE 97 45 D8 AE 21 4E 0F 4C 6C 72 62 3F EC C7 13 2D E2 51 C1 D2 C4 C7 05 4B A3 9E C8 75 67 57 FD 44 75 62 3A C5 53 F9 3F 09 43 9E 4D 18 A5 44 BD 9F 9D 30 CA 8C 32 3C 4F 10 83 A7 C2 A1 A5 BF E8 1E 69 FE 97 E9 50 51 D8 E8 30 6B FC 2B 89 A8 39 5F B3 41 BE AA B3 2D 22 72 15 E4 42 C2 A0 86 F8 B3 BC B1 65 D0 1A DE CE C4 32 C4 FF 55 EF FE 87 59 6B 7E 1F CF EC 29 18 F7 0C 57 C8 A4 0F EC E7 2F C5 DA 26 48 9B 04 34 89 E0 2A AD 01 64 49 F5 88 AA DA BB F8 98 3F 87 61 9B 76 C1 19 9B 3B 37 A4 FA 91 47 DB 2A E6 B3 A1 CD 0E 59 53 53 0A BA 41 5C C8 88 00 ED F7 22 F3 D7 95 1F 3E 79 D6 F5 51 87 60 29 C8 61 5F 6D 3D 05 EA 0D F1 72 EE 0C E4 6C 34 56 0A 42 D7 A9 A8 6E E3 76 AA 9E AF 18 56 C3 E3 5A 45 0C D4 9A 23 C8 8A 42 D6 E5 3E 1A 05 55 E4 3E 1A 05 7D 2E 59 74 64 BE A9 16 D1 BF 94 2C 3A B3 06 7F CD 86 DC 58 A6 0B 6A F6 A8 DE 88 D6 0F 09 B2 99 16 A2 B5 D7 E9 AF 5B C1 DD F3 80 DD A6 A7 D9 49 2E 0D DB 66 5E 1E 63 7C 15 FC 60 B0 D1 39 C7 A1 B9 00 73 A0 D7 08 81 24 CC 73 1A 0B C4 00 CC B7 BB CA C0 41 D8 DC 4D 85 A1 43 B0 B9 EB 75 86 0E C3 66 BC E2 B6 84 11 D8 0C 59 3C 22 20 81 FE 66 60 BE 76 8C A2 BF 49 98 E3 86 14 CC C3 2E 17 11 8C A1 77 26 65 6D 48 43 1A 44 27 99 94 41 B8 AC CC F1 51 16 A6 E2 32 CF 30 07 F1 25 66 81 41 39 C4 25 DB 36 43 FE 02 81 3E 72 60");
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
            int All_Quest = quest.Count;
            outPacket.WriteInt(1);
            outPacket.WriteInt(1);
            outPacket.WriteInt(All_Quest);
            foreach (var item in quest)
            {
                outPacket.WriteInt(item);
                outPacket.WriteInt(item);
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

        public static void GetRider(SessionGroup Parent, string Nickname, OutPacket outPacket)
        {
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Character);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Paint);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Kart);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Plate);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Goggle);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Balloon);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Unknown1);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_HeadBand);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_HeadPhone);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_HandGearL);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Unknown2);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Uniform);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Decal);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Pet);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_FlyingPet);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Aura);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_SkidMark);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_SpecialKit);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_RidColor);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_BonusCard);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_BossModeCard);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartPlant1);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartPlant2);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartPlant3);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartPlant4);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Unknown3);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_FishingPole);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Tachometer);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Dye);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartSN);
            outPacket.WriteByte(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Unknown4);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartCoating);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartTailLamp);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_slotBg);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartCoating12);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartTailLamp12);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartBoosterEffect12);
            outPacket.WriteShort(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Unknown5);
        }

        public static short RandomItemSkill(string Nickname, byte gameType)
        {
            if (gameType == 2)
            {
                Random random = new Random();
                int index = random.Next(MultyPlayer.itemProb_indi.Count);
                short skill = MultyPlayer.itemProb_indi[index];
                skill = GameSupport.GetItemSkill(Nickname, skill);
                return skill;
            }
            else if (gameType == 4)
            {
                Random random = new Random();
                int index = random.Next(MultyPlayer.itemProb_team.Count);
                short skill = MultyPlayer.itemProb_team[index];
                skill = GameSupport.GetItemSkill(Nickname, skill);
                return skill;
            }
            return 0;
        }

        public static short GetItemSkill(string Nickname, short skill)
        {
            List<short> skills = V2Specs.GetSkills(Nickname);
            for (int i = 0; i < skills.Count; i++)
            {
                if (V2Specs.itemSkill.TryGetValue(skills[i], out var Level) &&
                    Level.TryGetValue(skill, out var LevelSkill))
                {
                    return LevelSkill;
                }
            }
            if (MultyPlayer.kartConfig.SkillChange.TryGetValue(ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Kart, out var changes) &&
                changes.TryGetValue(skill, out var changesSkill))
            {
                return changesSkill;
            }
            return skill;
        }

        public static void AddItemSkill(SessionGroup Parent, string Nickname, short skill)
        {
            skill = GameSupport.GetItemSkill(Nickname, skill);
            using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
            {
                oPacket.WriteInt();
                oPacket.WriteUInt(4294967295);
                oPacket.WriteByte(10);
                oPacket.WriteHexString("001000");
                oPacket.WriteShort(skill);
                oPacket.WriteByte(1);
                oPacket.WriteBytes(new byte[3]);
                oPacket.WriteByte(2);
                oPacket.WriteShort(skill);
                oPacket.WriteBytes(new byte[5]);
                Parent.Client.Send(oPacket);
            }
        }

        public static void AttackedSkill(SessionGroup Parent, string Nickname, byte type, byte uni, short skill)
        {
            skill = GameSupport.GetItemSkill(Nickname, skill);
            using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
            {
                oPacket.WriteInt();
                oPacket.WriteUInt();
                oPacket.WriteByte(type);
                oPacket.WriteByte(uni);
                oPacket.WriteShort(skill);
                oPacket.WriteByte(1);
                oPacket.WriteShort();
                oPacket.WriteByte(2);
                oPacket.WriteShort(skill);
                oPacket.WriteBytes(new byte[5]);
                Parent.Client.Send(oPacket);
            }
        }
    }
}