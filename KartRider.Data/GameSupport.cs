using System;
using System.Collections.Generic;
using System.Linq;
using ExcData;
using KartRider.IO.Packet;
using Profile;

namespace KartRider
{
    public static class GameSupport
    {
        public static void PcFirstMessage()
        {
            uint first_val = 2130265355;
            uint second_val = 3323984468;
            using (OutPacket outPacket = new OutPacket("PcFirstMessage"))
            {
                outPacket.WriteUShort(SessionGroup.usLocale);
                outPacket.WriteUShort(1);
                outPacket.WriteUShort(ProfileService.ProfileConfig.GameOption.Version);
                outPacket.WriteString("http://kartupdate.tiancity.cn/patch/MNUDSNRSVQBBFYM");
                outPacket.WriteUInt(first_val);
                outPacket.WriteUInt(second_val);
                outPacket.WriteByte(SessionGroup.nClientLoc);
                outPacket.WriteString("yzwh4TvSQK8knRdV6FqXJfUFXw7VXWvK9Tm+4hPGbmE=");
                outPacket.WriteBytes(new byte[31]);
                outPacket.WriteString("Tkd+Hpj7uDwGwHYB+klvMh2Hmg7D38i5RqXuE4pFVhc=");
                RouterListener.MySession.Client.Send(outPacket);
            }
            RouterListener.MySession.Client._RIV = first_val ^ second_val;
            RouterListener.MySession.Client._SIV = first_val ^ second_val;
            return;
        }

        public static void OnDisconnect()
        {
            RouterListener.MySession.Client.Disconnect();
        }

        public static void SpRpLotteryPacket()
        {
            using (OutPacket outPacket = new OutPacket("SpRpLotteryPacket"))
            {
                outPacket.WriteHexString("05 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
                RouterListener.MySession.Client.Send(outPacket);
            }
        }

        public static void PrGetGameOption()
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
                RouterListener.MySession.Client.Send(outPacket);
            }
        }

        public static void ChRpEnterMyRoomPacket()
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
                    RouterListener.MySession.Client.Send(outPacket);
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
                    RouterListener.MySession.Client.Send(outPacket);
                }
            }
        }

        public static void RmNotiMyRoomInfoPacket()
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
                RouterListener.MySession.Client.Send(outPacket);
            }
        }

        public static void PrCheckMyClubStatePacket()
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
                RouterListener.MySession.Client.Send(outPacket);
            }
        }

        public static void ChRequestChStaticReplyPacket()
        {
            using (OutPacket outPacket = new OutPacket("ChRequestChStaticReplyPacket"))
            {
                outPacket.WriteHexString("014E0300005301A499DF77F90E000078DAA556F957D34010FEE42A2D6DD38B533C51BC2F3C100F40EEAA1C0FF8DD4769409E81F2DA28FADF3BB3E9DAA40CD92DBEBC97ECEE7C73EE646636017C72E855470D7BA862011EADF6F103DFD0C0295C7AAA486608D2DA967142EF23AC104B033EBA58C2112D5C1C6383DE67040976BBF4DEA36FF7C5102DADA76894A215F6168DD234B42F19315D4B4A24458FFA33225A0B4BE562C9AE7A0FE462831580D2790B4953C8E42D644D213B680C5D705A21940B67D018BE303CE788B686217947B4330C2970580EF193369E224AEA8BA908A825B19425C23EBE2BCDA744AE918A06B6E9FB87CE3C0CF611A0424B9F1E8F7886F8E080E8C724CE23C270BF3AE0E5E13F2746C287DAEC51F906F97B40DF1365FC989C0B51D0D55428D21AB24A4CE37981B04C5C818F274461D8B501510993AE3BB119C4901B058B2463E0CDAC0A7B5D69AED2419D6CFB4D25405B78CBB900D0927EBB6884E8404E98A1DAD63B63D652A391BF3B6A7049CEFBC9CED8B499F7F22AFB6A54372B2ADD8275BBF4FB23B130D9A407F1B2B5090F4745D88E214E8F4C6C72623FEEC4132DE249C1D2C4A7054BA39EC9756747FD4455623AC373F93F09435E0C19A544BD9F1A32CA8C32BC4C1083A7C2A1A5BF6A1D69FED7E95051586F326BFC1B89A839A7D9205FD5D93A11B90A722161D0A1F8B3BCB565D01A66C66319E2FFAA779761D69ADFC7337B0AC63DC31532E903FBF94BB13608D220013522B84A6B0059543DA2A2F62E3EE62F60D8A25D70C6E6CEF6A87EE4D1B682B96C68B34DD6EC2B05AD20CE674480F67B81F9ABCA0F9F3C6BF9A843B018E4B0AFB69E02B5877829770E723E1ACB05A1EB9455B7719B55CF570C2BE171AD2C066AD5116445216B721F8D82CA721F8D823E972C3A32DF549DE85F4A169D5983BF66436E2CD105D5DA54AF47EB8704D9480BD1DA6DF6D7CDE0EE79C06ED0536B2697866D312F8F31BE0A7E30D8E89C6347AEC01CE855422009F39CC6B14617CCB7BBC2C06ED8DC4D99A13DB0B9EB3586F6C266BCE2B6843ED80C593C222081CE6660BE76F4A3B34998E38614CCC32E17110CA07D26656D48431A4487999441B8ACCCF25116A6E232C73007F125669E413974DA26A6992D8FFFA9FD332CA280B834DF624811971BBD2699B904FB91728219FE0286D899A5");
                RouterListener.MySession.Client.Send(outPacket);
            }
        }

        public static void PrDynamicCommand()
        {
            using (OutPacket outPacket = new OutPacket("PrDynamicCommand"))
            {
                outPacket.WriteByte(0);
                RouterListener.MySession.Client.Send(outPacket);
            }
        }

        public static void PrQuestUX2ndPacket(OutPacket outPacket)
        {
            int All_Quest = KartExcData.quest.Count;
            outPacket.WriteInt(1);
            outPacket.WriteInt(1);
            outPacket.WriteInt(All_Quest);
            foreach (var item in KartExcData.quest)
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
            outPacket.WriteShort(0);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_RidColor);
            outPacket.WriteShort(ProfileService.ProfileConfig.RiderItem.Set_BonusCard);
            outPacket.WriteShort(0);//bossModeCard
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
            outPacket.WriteShort(0);
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
                int index = random.Next(KartExcData.itemProb_indi.Count);
                short skill = KartExcData.itemProb_indi[index];
                skill = GameSupport.GetItemSkill(skill);
                return skill;
            }
            else if (gameType == 4)
            {
                Random random = new Random();
                int index = random.Next(KartExcData.itemProb_team.Count);
                short skill = KartExcData.itemProb_team[index];
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

        public static void AddItemSkill(short skill)
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
                RouterListener.MySession.Client.Send(oPacket);
            }
        }

        public static void AttackedSkill(byte type, byte uni, short skill)
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
                RouterListener.MySession.Client.Send(oPacket);
            }
        }
    }
}
