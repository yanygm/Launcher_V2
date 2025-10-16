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
                outPacket.WriteUShort(SessionGroup.usLocale);
                outPacket.WriteUShort(1);
                outPacket.WriteUShort(ProfileService.ProfileConfig.GameOption.Version);
                outPacket.WriteString("https://github.com/yanygm/Launcher_V2");
                outPacket.WriteUInt(key.first_val);
                outPacket.WriteUInt(key.second_val);
                outPacket.WriteByte(SessionGroup.nClientLoc);
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

        public static void PrGetGameOption(SessionGroup Parent)
        {
            using (OutPacket outPacket = new OutPacket("PrGetGameOption"))
            {
                outPacket.WriteFloat(ProfileService.ProfileConfig.GameOption.Set_BGM);
                outPacket.WriteFloat(ProfileService.ProfileConfig.GameOption.Set_Sound);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.Main_BGM);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.Sound_effect);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.Full_screen);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.ShowMirror);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.ShowOtherPlayerNames);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.ShowOutlines);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.ShowShadows);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.HighLevelEffect);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.MotionBlurEffect);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.MotionDistortionEffect);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.HighEndOptimization);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.AutoReady);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.PropDescription);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.VideoQuality);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.BGM_Check);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.Sound_Check);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.ShowHitInfo);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.AutoBoost);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.GameType);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.SetGhost);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.SpeedType);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.RoomChat);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.DrivingChat);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.ShowAllPlayerHitInfo);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.ShowTeamColor);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.Set_screen);
                outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.HideCompetitiveRank);
                outPacket.WriteBytes(new byte[79]);
                Parent.Client.Send(outPacket);
            }
        }

        public static void ChRpEnterMyRoomPacket(SessionGroup Parent)
        {
            if (GameType.EnterMyRoomType == 0)
            {
                using (OutPacket outPacket = new OutPacket("ChRpEnterMyRoomPacket"))
                {
                    outPacket.WriteString(ProfileService.ProfileConfig.Rider.Nickname);
                    outPacket.WriteByte(0);
                    outPacket.WriteShort(ProfileService.ProfileConfig.MyRoom.MyRoom);
                    outPacket.WriteByte(ProfileService.ProfileConfig.MyRoom.MyRoomBGM);
                    outPacket.WriteByte(ProfileService.ProfileConfig.MyRoom.UseRoomPwd);
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(ProfileService.ProfileConfig.MyRoom.UseItemPwd);
                    outPacket.WriteByte(ProfileService.ProfileConfig.MyRoom.TalkLock);
                    outPacket.WriteString(ProfileService.ProfileConfig.MyRoom.RoomPwd);
                    outPacket.WriteString("");
                    outPacket.WriteString(ProfileService.ProfileConfig.MyRoom.ItemPwd);
                    outPacket.WriteShort(ProfileService.ProfileConfig.MyRoom.MyRoomKart1);
                    outPacket.WriteShort(ProfileService.ProfileConfig.MyRoom.MyRoomKart2);
                    Parent.Client.Send(outPacket);
                }
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("ChRpEnterMyRoomPacket"))
                {
                    outPacket.WriteString("");
                    outPacket.WriteByte(GameType.EnterMyRoomType);
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

        public static void RmNotiMyRoomInfoPacket(SessionGroup Parent)
        {
            using (OutPacket outPacket = new OutPacket("RmNotiMyRoomInfoPacket"))
            {
                outPacket.WriteShort(ProfileService.ProfileConfig.MyRoom.MyRoom);
                outPacket.WriteByte(ProfileService.ProfileConfig.MyRoom.MyRoomBGM);
                outPacket.WriteByte(ProfileService.ProfileConfig.MyRoom.UseRoomPwd);
                outPacket.WriteByte(0);
                outPacket.WriteByte(ProfileService.ProfileConfig.MyRoom.UseItemPwd);
                outPacket.WriteByte(ProfileService.ProfileConfig.MyRoom.TalkLock);
                outPacket.WriteString(ProfileService.ProfileConfig.MyRoom.RoomPwd);
                outPacket.WriteString("");
                outPacket.WriteString(ProfileService.ProfileConfig.MyRoom.ItemPwd);
                outPacket.WriteShort(ProfileService.ProfileConfig.MyRoom.MyRoomKart1);
                outPacket.WriteShort(ProfileService.ProfileConfig.MyRoom.MyRoomKart2);
                Parent.Client.Send(outPacket);
            }
        }

        public static void PrCheckMyClubStatePacket(SessionGroup Parent)
        {
            using (OutPacket outPacket = new OutPacket("PrCheckMyClubStatePacket"))
            {
                if (ProfileService.ProfileConfig.Rider.ClubMark_LOGO == 0)
                {
                    outPacket.WriteInt(0);
                    outPacket.WriteString("");
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                }
                else
                {
                    outPacket.WriteInt(ProfileService.ProfileConfig.Rider.ClubCode);
                    outPacket.WriteString(ProfileService.ProfileConfig.Rider.ClubName);
                    outPacket.WriteInt(ProfileService.ProfileConfig.Rider.ClubMark_LOGO);
                    outPacket.WriteInt(ProfileService.ProfileConfig.Rider.ClubMark_LINE);
                }
                outPacket.WriteShort(5);//Grade
                outPacket.WriteString(ProfileService.ProfileConfig.Rider.Nickname);
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
                outPacket.WriteHexString("01 46 03 00 00 53 01 FD 8C C8 42 B0 0E 00 00 78 DA A5 56 F9 57 D3 40 10 FE E4 2A 2D 6D D3 B4 DC E2 7D DF 8A 07 E2 01 C8 5D 95 E3 01 BF FB 28 0D C8 33 50 5E 1B 45 FF 7B 67 36 5D 9B 94 21 BB 95 97 F7 92 EC CE 37 E7 CE CE CC 06 80 4F 0E BD EA A8 61 17 55 CC C3 A7 BF 3D FC C0 37 34 70 02 8F 9E 2A D2 39 82 B4 96 65 1C D3 FB 10 CB C4 D2 40 80 2E 96 70 48 3F 1E 8E B0 4E EF 53 82 84 AB 1D 7A EF D2 B7 FB 7C 88 96 D6 53 32 4A D1 0A 7B 4B 46 69 1A DA 97 8E 99 AE 25 A5 D2 A2 47 FD 39 11 AD 85 65 0A 89 64 4F BD 07 0A 89 C1 0A 41 59 D7 42 D2 24 72 AE 85 AC 49 E4 87 8C A1 0B 77 2B 84 F2 E0 0C 19 C3 17 85 17 1C D1 D6 28 C4 75 44 3B A3 90 22 87 E5 00 3F 69 E1 2B A2 A4 BE 94 89 81 5A 12 07 F3 44 D8 C3 77 A5 F9 84 C8 35 52 D1 C0 16 7D FF D0 9E 8F A1 3E 02 54 E8 37 A0 C7 27 9E 61 DE D8 27 FA 11 89 F3 89 30 D2 AF 36 F8 F7 E0 9F 13 A3 D1 4D 6D F6 98 7C 82 FC DD A7 EF B1 32 7E 5C CE 85 38 E8 72 26 12 69 0D 59 21 A6 09 57 20 2C 11 57 E8 E3 31 51 18 76 65 40 54 C2 A4 AB 4E 62 06 31 E4 5A D1 22 C9 18 78 3D AF C2 5E 57 9A AB B4 51 27 DB 7E 53 09 D0 16 DE 70 CE 01 B4 A4 DF 2C 19 21 3A 90 B7 CC 50 6D EB ED 71 6B A9 F1 C8 DF 19 33 B8 24 E7 FD DD CE D8 B4 99 F7 5C 95 7D 35 AA 9B 15 95 6E E1 7F BB F4 FB A3 89 30 D9 A4 07 C9 B2 B5 09 0F C7 44 D8 B6 21 4E 8F 4C 6C 72 62 3F EE C4 13 2D E2 49 D1 D2 C4 A7 45 4B A3 9E C9 75 67 5B 5D A2 2A 31 9D E2 B9 7C 4F A2 90 17 C3 46 29 71 EF 27 87 8D 32 E3 0C 2F 53 C4 E0 AB 70 68 E9 AF 5A 5B 9A FF 75 36 52 14 D6 9A CC 1A FF 46 22 6A CE 29 36 28 50 75 B6 4E 44 AE 82 5C 48 18 74 20 5E 96 B7 B6 0C 5A C3 F4 44 22 43 F2 AD 7A F7 3F CC 5A F3 FB 64 66 5F C1 B8 67 78 42 26 7D 60 3F 7F 29 D6 06 41 1A 24 A0 46 04 4F 69 0D 21 0B AA 47 54 D4 DA C3 47 F7 1C 86 4D 5A 85 7B 6C EE 4C 8F EA 47 3E 2D 2B 98 CD 47 16 5B 64 CD 9E 52 D0 0A E2 5C 4E 04 68 BF E7 99 BF AA FC 08 C8 B3 96 8F 3A 04 0B 61 0E 07 6A E9 2B 50 7B 88 17 0B 67 20 67 A3 B1 54 14 BA 4E 59 75 1B AF 59 F5 02 C5 B0 1C 1D D7 CA 62 A0 56 1C 41 56 1C B2 2A F7 D1 38 A8 2C F7 D1 38 E8 F3 A0 45 47 E6 93 AA 13 FD CB A0 45 67 D6 E0 AF F9 88 1B 8B 74 40 B5 36 D5 6B F1 FA 21 41 D6 B3 42 B4 76 9A FD 75 23 3C 7B 1E B0 1B F4 D4 9A C9 A5 61 9B CC CB 63 4C A0 82 1F 0E 36 3A E7 B8 4F 5E 82 39 D0 2B 84 40 1A E6 39 8D 63 8D 2E 98 4F 77 99 81 DD B0 39 9B 32 43 7B 60 73 D6 AB 0C ED 85 CD 78 C5 6D 09 7D B0 19 B2 78 44 40 0A 9D CD C0 7C EC E8 47 67 93 30 C7 0D 19 98 87 5D 2E 22 18 40 FB 4C CA DA 90 85 34 88 8E 30 29 87 68 59 99 E1 AD 3C 4C C5 65 96 61 0E 92 4B CC 1C 83 0A E8 B4 4D 4C 31 9B 8B 8B D4 FE 69 16 51 44 52 9A 6F 32 A4 84 E4 8B B2 C1 A0 BF 51 78 8C FE");
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

        public static void GetRider(OutPacket outPacket)
        {
            var KartAndSN = new { Kart = ProfileService.ProfileConfig.RiderItem.Set_Kart, SN = ProfileService.ProfileConfig.RiderItem.Set_KartSN };
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Character);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Paint);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Kart);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Plate);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Goggle);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Balloon);
            outPacket.WriteShort(0);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_HeadBand);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_HeadPhone);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_HandGearL);
            outPacket.WriteShort(0);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Uniform);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Decal);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Pet);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_FlyingPet);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Aura);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_SkidMark);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_SpecialKit);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_RidColor);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_BonusCard);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_BossModeCard);
            var existingPlant = KartExcData.PlantList.FirstOrDefault(list => list[0] == KartAndSN.Kart && list[1] == KartAndSN.SN);
            if (existingPlant != null)
            {
                outPacket.WriteShort(existingPlant[3]);
                outPacket.WriteShort(existingPlant[7]);
                outPacket.WriteShort(existingPlant[5]);
                outPacket.WriteShort(existingPlant[9]);
            }
            else
            {
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
            }
            outPacket.WriteShort(0);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_FishingPole);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Tachometer);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_Dye);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_KartSN);
            outPacket.WriteByte(0);
            var existingParts = KartExcData.PartsList.FirstOrDefault(list => list[0] == KartAndSN.Kart && list[1] == KartAndSN.SN);
            if (existingParts != null)
            {
                outPacket.WriteShort(existingParts[14]);
                outPacket.WriteShort(existingParts[15]);
            }
            else
            {
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
            }
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_slotBg);
            var existingParts12 = KartExcData.Parts12List.FirstOrDefault(list => list[0] == KartAndSN.Kart && list[1] == KartAndSN.SN);
            if (existingParts12 != null)
            {
                outPacket.WriteShort(existingParts12[14]);
                outPacket.WriteShort(existingParts12[15]);
                outPacket.WriteShort(existingParts12[16]);
            }
            else
            {
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
            }
        }

        public static short RandomItemSkill(byte gameType)
        {
            if (gameType == 2)
            {
                Random random = new Random();
                int index = random.Next(MultyPlayer.itemProb_indi.Count);
                short skill = MultyPlayer.itemProb_indi[index];
                skill = GameSupport.GetItemSkill(skill);
                return skill;
            }
            else if (gameType == 4)
            {
                Random random = new Random();
                int index = random.Next(MultyPlayer.itemProb_team.Count);
                short skill = MultyPlayer.itemProb_team[index];
                skill = GameSupport.GetItemSkill(skill);
                return skill;
            }
            return 0;
        }

        public static short GetItemSkill(short skill)
        {
            List<short> skills = V2Spec.GetSkills();
            for (int i = 0; i < skills.Count; i++)
            {
                if (V2Spec.itemSkill.TryGetValue(skills[i], out var Level) &&
                    Level.TryGetValue(skill, out var LevelSkill))
                {
                    return LevelSkill;
                }
            }
            if (MultyPlayer.kartConfig.SkillChange.TryGetValue(ProfileService.ProfileConfig.RiderItem.Set_Kart, out var changes) &&
                changes.TryGetValue(skill, out var changesSkill))
            {
                return changesSkill;
            }
            return skill;
        }

        public static void AddItemSkill(SessionGroup Parent, short skill)
        {
            skill = GameSupport.GetItemSkill(skill);
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

        public static void AttackedSkill(SessionGroup Parent, byte type, byte uni, short skill)
        {
            skill = GameSupport.GetItemSkill(skill);
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
