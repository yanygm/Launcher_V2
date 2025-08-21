using System;
using System.Collections.Generic;
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
				outPacket.WriteHexString("01550300005301789FB41D1E0F000078DAA556F957D34010FEE46E699BA6079778A1789F78201E80DC55391EF0BB8FD2803C03E5B555F4BF7766D3B54919B25B7C792FC9EE7C73EEECCC6C02F8E4D0AB862AF650C1027CFADBC70F7C431DA7F0E8A9209126486B59C209BD8FB0422C7534D0C5128EE8C7C33136E87D469060B54BEF3DFA765F0CD1D27AF246295A616FDE284D43FB1211D3B5A4FE84E8D1405A446B61C96C2CD953EFC16C6CB00250CAB5903485B46B216B0A99A23174C16E99501E9CA2317C6178D6116D0D435C47B4330CC971580EF19316BE224AEAF3C908A825B19021C23EBE2BCDA744AE928A3AB6E9FB87F67C14FB0850A6DF063D3EF10CF1C601D18F499C4F84E101B5C1BF87FF9C18096F6AB347E513E4EF017D4F94F163722E44415793A1486BC82A318DBB026199B8021F4F88C2B06B83A212265D776233882137721649C6C09B1915F69AD25CA18D1AD9F69B4A80B6F0967301A0257D226F84E840DE3643B5AD77C6ACA546233F396A7049CEFBBB9DB16933EFB92AFBAA5437CB2ADD82FF76E9F7476261B2490FE2656B131E8E8AB01D439C1E99D8E4C47EDC89275AC4939CA5894F7396463D93EBCE8EBA4415623AC373F99E84212F868C52A2DE4F0D196546195EF61383AFC2A1A5BF6A6D69FED7A95051586F326BFC1B89A839A7D9A086AAB335227215E442C2A043F1B2BCB565D01A66C66319E26FD5BBCB306BCDEFE3997D05E39EE10999F481FDFCA558EB04A993802A113CA535802CAA1E51566B0F1FDD0B18B66815ECB1B9B33DAA1FF9B42C632E135A6C9335FB4A412B88F36911A0FD5E60FE8AF2A3419EB57CD421580C72B8A196BE02B58778297B0E723E1ACB39A1EB9454B7F19A55AFA11856C2E35A490CD4AA23C88A42D6E43E1A0595E43E1A057D2E5874643EA91AD1BF142C3AB3067FCD84DC58A203AAB6A95E8FD60F09B29112A2B5DBECAF9BC1D9F3805DA7A7DA4C2E0DDB625E1E631A2AF8C160A3738EE7B42B30077A951048C03CA771ACD105F3E9AE30B01B36675362680F6CCE7A8DA1BDB019AFB82DA10F3643168F08E8476733301F3B06D0D924CC714312E661978B0806D13E93B236A4200DA2C34C4A235C5666792B03537199639883F81233CFA02C3A6D13D3CCE6E27F6AFF0C8BC8212ECDB71892C7E526B649662EC07EFA9C608622E2AFE52683FE022CD29F79");
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
			if (MultyPlayer.skillChange.TryGetValue(SetRiderItem.Set_Kart, out var changes) &&
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
