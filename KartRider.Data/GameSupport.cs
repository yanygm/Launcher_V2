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
                int count = 3;
                int[] time = new int[] { 1547597728, 1707244048, 1862052984 };
                outPacket.WriteInt(count);
                for (int i = 0; i < count; i++)
                {
                    outPacket.WriteInt();
                    outPacket.WriteInt(time[i]);
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
                outPacket.WriteHexString("01 37 03 00 00 53 01 37 78 83 6C 36 0E 00 00 78 DA 9D 56 F9 57 DA 40 10 FE EA 0D 02 21 80 07 D6 DE F7 DD DA FB 52 EB 4D 5B 95 A7 FE DE 27 12 AD AF 51 7C 90 D6 F6 BF EF CC 86 2D 09 8E D9 A5 2F EF 25 BB 3B DF 9C 3B 99 99 4D 00 9F 1C 7A 35 D1 C0 2E EA 58 80 4F AB 3D FC C0 37 B4 70 02 8F 9E 3A 52 59 82 74 B6 15 1C D3 FB 10 2B C4 D2 42 80 3E 96 70 48 0B 0F 47 D8 A0 F7 29 41 C2 DD 0E BD 77 E9 DB 7F 3E 44 4B 1B 28 1A A5 68 85 83 45 A3 34 0D 1D 4A C5 4C D7 92 86 53 A2 47 23 59 11 AD 85 A5 F3 89 64 4F BD 47 F3 89 C1 0A 41 19 D7 42 D2 0C B2 AE 85 AC 19 E4 C6 8C A1 0B 4F 6B 84 F2 E0 8C 19 C3 17 85 E7 1D D1 D6 28 C4 75 44 3B A3 90 02 87 E5 00 3F 69 E3 2B A2 A4 BE 98 8E 81 3A 12 4B 39 22 EC E1 BB D2 7C 42 E4 06 A9 68 61 8B BE 7F E8 CC C7 D8 10 01 6A B4 0C E8 F1 89 67 9C 0F F6 89 7E 44 E2 7C 22 4C 8C A8 03 5E 1E FC 73 62 32 7A A8 CD 2E CB 37 C8 DF 7D FA 1E 2B E3 A7 E4 5C 88 83 2E A6 23 91 D6 90 55 62 9A 76 05 C2 32 71 85 3E 1E 13 85 61 97 46 45 25 4C BA EC 24 66 10 43 AE 14 2C 92 8C 81 57 73 2A EC 4D A5 B9 4E 07 4D B2 ED 37 95 00 6D E1 35 E7 1C 40 47 FA F5 A2 11 A2 03 79 C3 0C D5 B6 DE 9C B2 96 1A 8F FC AD B2 C1 25 39 EF 6F F7 C6 A6 CD BC E3 AA EC 6B 50 DD AC A9 74 0B D7 DD D2 EF 4E 26 C2 64 93 EE 25 CB D6 26 DC 2F 8B B0 6D 43 9C 1E 98 D8 E4 C4 7E D8 8B 27 5A C4 A3 82 A5 89 8F 0B 96 46 3D 91 EB CE B6 FA 89 EA C4 74 8A A7 F2 7F 12 85 3C 1B 37 4A 89 7B 3F 33 6E 94 19 67 78 3E 4C 0C BE 0A 87 96 FE A2 73 A4 F9 5F 66 22 45 61 BD CD AC F1 AF 24 A2 E6 7C CD 06 05 AA CE 36 89 C8 55 90 0B 09 83 0E C4 9F E5 8D 2D 83 D6 F0 76 3A 91 21 F9 AF 7A F7 3F CC 5A F3 FB 64 66 5F C1 B8 67 78 42 26 7D 60 3F 7F 29 D6 16 41 5A 24 A0 41 04 4F 69 0D 21 8B AA 47 D4 D4 DE C3 47 F7 1C 86 2A ED C2 33 36 77 76 40 F5 23 9F B6 35 CC E5 22 9B 2D B2 66 4F 29 E8 04 71 3E 2B 02 B4 DF 0B CC 5F 57 7E 04 E4 59 C7 47 1D 82 C5 30 87 03 B5 F5 15 A8 3B C4 4B F9 33 90 B3 D1 58 2E 08 5D A7 A2 BA 8D D7 AE 7A 81 62 58 89 8E 6B 15 31 50 AB 8E 20 2B 0E 59 93 FB 68 1C 54 91 FB 68 1C F4 B9 64 D1 91 F9 A6 9A 44 FF 52 B2 E8 CC 1A FC 35 17 71 63 89 2E A8 D1 A5 7A 3D 5E 3F 24 C8 46 46 88 D6 4E BB BF 6E 86 77 CF 03 76 8B 9E 46 3B B9 34 AC CA BC 3C C6 04 2A F8 E1 60 A3 73 8E 73 F1 02 CC 81 5E 25 04 52 30 CF 69 1C 6B F4 C1 7C BB 2B 0C EC 87 CD DD 54 18 3A 00 9B BB 5E 63 E8 20 6C C6 2B 6E 4B 18 82 CD 90 C5 23 02 86 D1 DB 0C CC D7 8E 11 F4 36 09 73 DC 90 86 79 D8 E5 8B C3 28 BA 67 52 D6 86 0C A4 41 74 82 49 59 44 CB CA 2C 1F E5 60 2A 2E 73 0C 73 90 5C 62 E6 19 94 47 52 B2 55 19 E2 22 39 5D 37 19 F4 17 7A B9 78 38");
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