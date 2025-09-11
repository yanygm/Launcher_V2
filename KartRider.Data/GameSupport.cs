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
                outPacket.WriteHexString("0140030000530123872D488B0E000078DAA556F957D34010FEE42A2D6DD3B4DCE27DDF8A07E201C85D95E301BFFB280DC833505E1B45FF7B67365D9B9421BB9597F792DD9D6FCE9DCCCC06804F0EBDEAA8611755CCC3A7D51E7EE01B1A3881474F15E91C415ADB328EE97D886562692040174B38A4858723ACD3FB9420E16E87DEBBF4ED3E1FA2A5F5948C52B4C2DE92519A86F6A563A66B49A9B4E8517F4E446B61994222D953EF814262B04250D6B59034899C6B216B12F92163E8C2D30AA13C3843C6F045E10547B4350A711DD1CE28A4C86139C04FDAF88A28A92F6562A096C4C13C11F6F05D693E21728D5434B045DF3F74E663A88F00155A06F4F8C433CC07FB443F22713E1146FAD5012F0FFE39311A3DD4668FC937C8DF7DFA1E2BE3C7E55C88832E672291D69015629A7005C21271853E1E138561570644254CBAEA24661043AE152D928C81D7F32AEC75A5B94A0775B2ED3795006DE10DE71C404BFACD9211A20379CB0CD5B6DE1EB7961A8FFC9D31834B72DEDFED8C4D9B79CF55D957A3BA5951E916AEDBA5DF1F4D84C9263D4896AD4D783826C2B60D717A64629313FB71279E68114F8A96263E2D5A1AF54CAE3BDBEA27AA12D3299ECBFF4914F262D82825EEFDE4B051669CE1658A187C150E2DFD55EB48F3BFCE468AC25A9359E3DF4844CD39C50605AACED689C855900B09830EC49FE5AD2D83D6303D91C890FC57BDFB1F66ADF97D32B3AF60DC333C21933EB09FBF146B83200D12502382A7B4869005D5232A6AEFE1A37B0EC326EDC2333677A647F5239FB615CCE6239B2DB2664F296805712E2702B4DFF3CC5F557E04E459CB471D8285308703B5F515A83DC48B853390B3D1582A0A5DA7ACBA8DD7AC7A8162588E8E6B6531502B8E202B0E5995FB681C5496FB681CF479D0A223F34DD589FE65D0A2336BF0D77CC48D45BAA05A9BEAB578FD9020EB59215A3BCDFEBA11DE3D0FD80D7A6ACDE4D2B04DE6E5312650C10F071B9D737C5D97600EF40A219086794EE358A30BE6DB5D6660376CEEA6CCD01ED8DCF52A437B61335E715B421F6C862C1E1190426733305F3BFAD1D924CC714306E661978B0806D03E93B23664210DA2234CCA215A5666F8280F537199659883E41233C7A0023A6D1353CCE6E222B57F9A45149194E69B0CF90B56B88724");
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
