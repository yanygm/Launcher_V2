using System;
using System.Linq;
using ExcData;
using KartRider.IO.Packet;
using Set_Data;

namespace KartRider
{
	public static class GameSupport
	{
		public static void PcFirstMessage()
		{
			uint first_val = 418454259;
			uint second_val = 3259911425;
			using (OutPacket outPacket = new OutPacket("PcFirstMessage"))
			{
				outPacket.WriteUShort(SessionGroup.usLocale);
				outPacket.WriteUShort(1);
				outPacket.WriteUShort(SetGameOption.Version);
				outPacket.WriteString("http://kartupdate.tiancity.cn/patch/JDVDDSVTJVLGHXJ");
				outPacket.WriteUInt(first_val);
				outPacket.WriteUInt(second_val);
				outPacket.WriteByte(SessionGroup.nClientLoc);
				outPacket.WriteString("9wk/NSpInbhNJGTCHOvYH76HjtBwlUA7QKaxZlqwWu0=");
				outPacket.WriteBytes(new byte[31]);
				outPacket.WriteString("92Jw/2KaOSER68ywYfQoploG2FJgmhqCCBTSXaob5e8=");
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
				outPacket.WriteFloat(SetGameOption.Set_BGM);
				outPacket.WriteFloat(SetGameOption.Set_Sound);
				outPacket.WriteByte(SetGameOption.Main_BGM);
				outPacket.WriteByte(SetGameOption.Sound_effect);
				outPacket.WriteByte(SetGameOption.Full_screen);
				outPacket.WriteByte(SetGameOption.Unk1);
				outPacket.WriteByte(SetGameOption.Unk2);
				outPacket.WriteByte(SetGameOption.Unk3);
				outPacket.WriteByte(SetGameOption.Unk4);
				outPacket.WriteByte(SetGameOption.Unk5);
				outPacket.WriteByte(SetGameOption.Unk6);
				outPacket.WriteByte(SetGameOption.Unk7);
				outPacket.WriteByte(SetGameOption.Unk8);
				outPacket.WriteByte(SetGameOption.Unk9);
				outPacket.WriteByte(SetGameOption.Unk10);
				outPacket.WriteByte(SetGameOption.Unk11);
				outPacket.WriteByte(SetGameOption.BGM_Check);
				outPacket.WriteByte(SetGameOption.Sound_Check);
				outPacket.WriteByte(SetGameOption.Unk12);
				outPacket.WriteByte(SetGameOption.Unk13);
				outPacket.WriteByte(SetGameOption.GameType);
				outPacket.WriteByte(SetGameOption.SetGhost);
				outPacket.WriteByte(SetGameOption.SpeedType);
				outPacket.WriteByte(SetGameOption.Unk14);
				outPacket.WriteByte(SetGameOption.Unk15);
				outPacket.WriteByte(SetGameOption.Unk16);
				outPacket.WriteByte(SetGameOption.Unk17);
				outPacket.WriteByte(SetGameOption.Set_screen);
				outPacket.WriteByte(SetGameOption.Unk18);
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
					outPacket.WriteString(SetRider.Nickname);
					outPacket.WriteByte(0);
					outPacket.WriteShort(SetMyRoom.MyRoom);
					outPacket.WriteByte(SetMyRoom.MyRoomBGM);
					outPacket.WriteByte(SetMyRoom.UseRoomPwd);
					outPacket.WriteByte(0);
					outPacket.WriteByte(SetMyRoom.UseItemPwd);
					outPacket.WriteByte(SetMyRoom.TalkLock);
					outPacket.WriteString(SetMyRoom.RoomPwd);
					outPacket.WriteString("");
					outPacket.WriteString(SetMyRoom.ItemPwd);
					outPacket.WriteShort(SetMyRoom.MyRoomKart1);
					outPacket.WriteShort(SetMyRoom.MyRoomKart2);
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
				outPacket.WriteShort(SetMyRoom.MyRoom);
				outPacket.WriteByte(SetMyRoom.MyRoomBGM);
				outPacket.WriteByte(SetMyRoom.UseRoomPwd);
				outPacket.WriteByte(0);
				outPacket.WriteByte(SetMyRoom.UseItemPwd);
				outPacket.WriteByte(SetMyRoom.TalkLock);
				outPacket.WriteString(SetMyRoom.RoomPwd);
				outPacket.WriteString("");
				outPacket.WriteString(SetMyRoom.ItemPwd);
				outPacket.WriteShort(SetMyRoom.MyRoomKart1);
				outPacket.WriteShort(SetMyRoom.MyRoomKart2);
				RouterListener.MySession.Client.Send(outPacket);
			}
		}

		public static void PrCheckMyClubStatePacket()
		{
			using (OutPacket outPacket = new OutPacket("PrCheckMyClubStatePacket"))
			{
				if (SetRider.ClubMark_LOGO == 0)
				{
					outPacket.WriteInt(0);
					outPacket.WriteString("");
					outPacket.WriteInt(0);
					outPacket.WriteInt(0);
				}
				else
				{
					outPacket.WriteInt(SetRider.ClubCode);
					outPacket.WriteString(SetRider.ClubName);
					outPacket.WriteInt(SetRider.ClubMark_LOGO);
					outPacket.WriteInt(SetRider.ClubMark_LINE);
				}
				outPacket.WriteShort(5);//Grade
				outPacket.WriteString(SetRider.Nickname);
				outPacket.WriteInt(1);//ClubMember
				outPacket.WriteByte(5);//Level
				RouterListener.MySession.Client.Send(outPacket);
			}
		}

		public static void ChRequestChStaticReplyPacket()
		{
			using (OutPacket outPacket = new OutPacket("ChRequestChStaticReplyPacket"))
			{
				outPacket.WriteHexString("01550300005301FBA2778C320F000078DAA556F957D34010FEE46E699BA60797788B27A2E2817881DC55391EF0BB8FD2803C03E5B555F4BF7766D3B54919B25B7C792FD9DDF9E6DCC9CC6C02F8E4D0AB862AF650C1027C5AEDE307BEA18E5378F454904813A4B52DE184DE475821963A1AE8620947B4F0708C0D7A9F1124D8EDD27B8FBEDD1743B4B49EBC518A56D89B374AD3D0BE44C4742DA93F217A349016D15A58321B4BF6D47B301B1BAC0094722D244D23ED5AC89A46A6680C5D705A269407A7680C5F189E75445BC310D711ED0C43721C9643FCA48DAF8892FA7C32026A492C6488B08FEF4AF32991ABA4A28E6DFAFEA1331FC53E029469D9A0C7279E213E3820FA3189F389303CA00E7879F8CF8991F0A1367B54BE41FE1ED0F744193F26E742147435198AB486AC12D3B82B1096892BF0F184280CBB36282A61D2752736831872236791640CBC995161AF29CD153AA8916DBFA904680B6F3917005AD26FE78D101DC83B66A8B6F5EE98B5D468E427460D2EC9797FAF33366DE67D57655F95EA6659A55BB06E97FE602416269BF4305EB636E1D1A808DB31C4E9B1894D4EECC94E3CD1229EE42C4D9CCA591AF554AE3B3BEA27AA10D3199EC9FF4918F27CC82825EAFDF490516694E1453F31F82A1C5AFACBD691E67F950A1585F526B3C6BF96889A73860D6AA83A5B232257412E240C3A147F9637B60C5AC3EC782C43FC5FF5F632CC5AF3BB78665FC1B867784226BD673F7F29D63A41EA24A04A044F690D208BAA4794D5DEC307F702862DDA05676CEEC71ED58F7CDA963197096DB6C99A7DA5A015C4F9B408D07E2F307F45F9D120CF5A3EEA102C0639DC505B5F81DA43BC943D07391F8DE59CD0754AAADB78CDAAD7500C2BE171AD24066AD5116445216B721F8D824A721F8D823E172C3A32DF548DE85F0A169D5983BF66426E2CD10555DB54AF47EB8704D94809D1DA6DF6D7CDE0EE79C0AED3536D2697866D312F8F310D15FC60B0D139C773DA159803BD4A0824609ED338D6E882F9765718D80D9BBB2931B4073677BDC6D05ED88C57DC96D0079B218B4704F4A3B31998AF1D03E86C12E6B82109F3B0CB450483689F49591B529006D16126A5112E2B1FF92803537199639883F81233CFA02C3A6D1333CCE6E27F6AFF2C8BC8212ECDB71892876DD39F6278019799402699B588CBCD8613CCFC179BA9A2FC");
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

		public static void PrQuestUX2ndPacket()
		{
			int All_Quest = KartExcData.quest.Count;
			using (OutPacket outPacket = new OutPacket("PrQuestUX2ndPacket"))
			{
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
				RouterListener.MySession.Client.Send(outPacket);
			}
		}

		public static void GetRider(OutPacket outPacket)
		{
			var KartAndSN = new { Kart = SetRiderItem.Set_Kart, SN = SetRiderItem.Set_KartSN };
			outPacket.WriteShort(SetRiderItem.Set_Character);
			outPacket.WriteShort(SetRiderItem.Set_Paint);
			outPacket.WriteShort(SetRiderItem.Set_Kart);
			outPacket.WriteShort(SetRiderItem.Set_Plate);
			outPacket.WriteShort(SetRiderItem.Set_Goggle);
			outPacket.WriteShort(SetRiderItem.Set_Balloon);
			outPacket.WriteShort(0);
			outPacket.WriteShort(SetRiderItem.Set_HeadBand);
			outPacket.WriteShort(SetRiderItem.Set_HeadPhone);
			outPacket.WriteShort(SetRiderItem.Set_HandGearL);
			outPacket.WriteShort(0);
			outPacket.WriteShort(SetRiderItem.Set_Uniform);
			outPacket.WriteShort(SetRiderItem.Set_Decal);
			outPacket.WriteShort(SetRiderItem.Set_Pet);
			outPacket.WriteShort(SetRiderItem.Set_FlyingPet);
			outPacket.WriteShort(SetRiderItem.Set_Aura);
			outPacket.WriteShort(SetRiderItem.Set_SkidMark);
			outPacket.WriteShort(0);
			outPacket.WriteShort(SetRiderItem.Set_RidColor);
			outPacket.WriteShort(SetRiderItem.Set_BonusCard);
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
			outPacket.WriteShort(SetRiderItem.Set_Tachometer);
			outPacket.WriteShort(SetRiderItem.Set_Dye);
			outPacket.WriteShort(SetRiderItem.Set_KartSN);
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
			outPacket.WriteShort(SetRiderItem.Set_slotBg);
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

		public static short GetItemSkill(short kart)
    	{
			Random random = new Random();
			int index = random.Next(KartExcData.itemProb_indi.Count);
			short skill = KartExcData.itemProb_indi[index];
			if (MultyPlayer.skillChange.TryGetValue(kart, out var changes) && 
				changes.TryGetValue(skill, out var changesSkill))
			{
				return changesSkill;
			}
			return skill;
		}

		public static void AddItemSkill(short skill)
		{
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
