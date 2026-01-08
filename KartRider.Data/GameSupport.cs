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
                outPacket.WriteHexString("01 46 03 00 00 53 01 AE 8A EA 57 A4 0E 00 00 78 DA 9D 56 F9 57 D3 40 10 FE E4 2A 2D 6D D3 B4 DC A2 28 8A F7 89 F7 05 C8 5D 95 E3 01 BF FB 28 0D C8 33 50 5E 1B 45 FF 7B 67 36 5D 9B 94 21 BB E5 E5 BD 24 BB F3 CD B9 B3 33 B3 09 E0 B3 43 AF 3A 6A D8 43 15 0B F0 E9 6F 1F 3F F1 1D 0D 9C C2 A3 A7 8A 74 8E 20 AD 65 19 27 F4 3E C2 0A B1 34 10 A0 8B 25 1C D1 8F 87 63 6C D0 FB 8C 20 E1 6A 97 DE 7B F4 ED BE 18 A2 A5 F5 94 8C 52 B4 C2 DE 92 51 9A 86 F6 A5 63 A6 6B 49 A9 B4 E8 51 7F 4E 44 6B 61 99 42 22 D9 53 EF 81 42 62 B0 42 50 D6 B5 90 34 83 9C 6B 21 6B 06 F9 21 63 E8 C2 DD 0A A1 3C 38 43 C6 F0 45 E1 05 47 B4 35 0A 71 1D D1 CE 28 A4 C8 61 39 C4 2F 5A F8 8A 28 A9 2F 65 62 A0 96 C4 C1 3C 11 F6 F1 43 69 3E 25 72 8D 54 34 B0 4D DF BF B4 E7 63 A8 8F 00 15 FA 0D E8 F1 89 67 98 37 0E 88 7E 4C E2 7C 22 8C F4 AB 0D FE 3D FC EF C4 68 74 53 9B 3D 26 9F 20 7F 0F E8 7B A2 8C 1F 97 73 21 0E BA 9A 89 44 5A 43 56 89 69 C2 15 08 CB C4 15 FA 78 42 14 86 5D 1B 10 95 30 E9 BA 93 98 41 0C 99 2C 5A 24 19 03 6F E4 55 D8 EB 4A 73 95 36 EA 64 DB 1F 2A 01 DA C2 9B CE 05 80 96 F4 A9 92 11 A2 03 79 CB 0C D5 B6 DE 1E B7 96 1A 8F FC F4 98 C1 25 39 EF EF 74 C6 A6 CD BC EB AA EC AB 51 DD AC A8 74 0B FF DB A5 DF 1B 4D 84 C9 26 DD 4F 96 AD 4D 78 30 26 C2 76 0C 71 7A 68 62 93 13 FB 51 27 9E 68 11 8F 8B 96 26 3E 29 5A 1A F5 54 AE 3B 3B EA 12 55 89 E9 0C CF E4 7B 12 85 3C 1F 36 4A 89 7B 3F 33 6C 94 19 67 78 91 22 06 5F 85 43 4B 7F D9 DA D2 FC AF B2 91 A2 B0 DE 64 D6 F8 D7 12 51 73 BE 61 83 02 55 67 EB 44 E4 2A C8 85 84 41 87 E2 65 79 6B CB A0 35 BC 9B 48 64 48 BE 55 EF 2F C3 AC 35 7F 48 66 F6 15 8C 7B 86 27 64 D2 47 F6 F3 B7 62 6D 10 A4 41 02 6A 44 F0 94 D6 10 B2 A8 7A 44 45 AD 3D 7C 72 2F 60 D8 A2 55 B8 C7 E6 CE F6 A8 7E E4 D3 B2 82 B9 7C 64 B1 4D D6 EC 2B 05 AD 20 CE E7 44 80 F6 7B 81 F9 AB CA 8F 80 3C 6B F9 A8 43 B0 18 E6 70 A0 96 BE 02 B5 87 78 A9 70 0E 72 3E 1A CB 45 A1 EB 94 55 B7 F1 9A 55 2F 50 0C 2B D1 71 AD 2C 06 6A D5 11 64 C5 21 6B 72 1F 8D 83 CA 72 1F 8D 83 BE 0C 5A 74 64 3E A9 3A D1 BF 0E 5A 74 66 0D FE 96 8F B8 B1 44 07 54 6B 53 BD 1E AF 1F 12 64 23 2B 44 6B B7 D9 5F 37 C3 B3 E7 01 BB 41 4F AD 99 5C 1A B6 C5 BC 3C C6 04 2A F8 E1 60 A3 73 8E FB E4 15 98 03 BD 4A 08 A4 61 9E D3 38 D6 E8 82 F9 74 57 18 D8 0D 9B B3 29 33 B4 07 36 67 BD C6 D0 5E D8 8C 57 DC 96 D0 07 9B 21 8B 47 04 A4 D0 D9 0C CC C7 8E 7E 74 36 09 73 DC 90 81 79 D8 E5 22 82 01 B4 CF A4 AC 0D 59 48 83 E8 08 93 72 88 96 95 59 DE CA C3 54 5C E6 18 E6 20 B9 C4 CC 33 A8 80 A4 64 DB 62 88 8B CB CD 4D D3 CC 5C 84 FD 0C 38 C5 0C 25 24 5F 8E 4D 06 FD 03 66 8E 8A AF");
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
