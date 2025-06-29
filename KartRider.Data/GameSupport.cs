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
				outPacket.WriteHexString("01560300005301F0A2C9B5320F000078DAA556F957D34010FEE46E699BA60797788BE281071E8817C85D95E301BFFB280DC833505E5B45FF7B67365D9B9421BBC597F792DD9D6FCE9DCCCC26804F0EBD6AA8620F152CC0A7D53E7EE01BEA3885474F058934415ADB124EE87D841562A9A3812E9670440B0FC7D8A0F7194182DD2EBDF7E8DB7D31444BEBC91BA56885BD79A3340DED4B444CD792FA13A247036911AD8525B3B1644FBD07B3B1C10A4029D742D234D2AE85AC69648AC6D005A7654279708AC6F085E15947B4350C711DD1CE3024C76139C44FDAF88A28A9CF2723A096C4428608FBF8AE349F12B94A2AEAD8A6EF1F3AF351EC234099960D7A7CE219E28303A21F93389F08C303EA809787FF9C18091F6AB347E51BE4EF017D4F94F163722E44415793A1486BC82A318DBB026199B8021F4F88C2B06B83A212265D776233882137721649C6C09B1915F69AD25CA1831AD9F69B4A80B6F0967301A025FD76DE08D181BC63866A5BEF8E594B8D467E62D4E0929CF7F73A63D366DE7755F655A96E9655BA05EB76E99323B130D9A407F1B2B5090F4745D88E214E8F4C6C72623FEEC4132D622A6769E2939CA5514FE5BAB3A37EA20A319DE199FC9F8421CF878C52A2DE4F0F196546195EF41383AFC2A1A5BF6C1D69FE57A95051586F326BFC6B89A83967D8A086AAB335227215E442C2A043F1677963CBA035CC8EC732C4FF556F2FC3AC35BF8B67F6158C7B862764D27BF6F39762AD13A44E02AA44F094D600B2A87A4459ED3D7C702F60D8A25D70C6E67EEC51FDC8A76D197399D0669BACD9570A5A419C4F8B00EDF702F357941F0DF2ACE5A30EC16290C30DB5F515A83DC44BD97390F3D158CE095DA7A4BA8DD7AC7A0DC5B0121ED74A62A0561D415614B226F7D128A824F7D128E873C1A223F34DD588FEA560D19935F86B26E4C6125D50B54DF57AB47E48908D9410ADDD667FDD0CEE9E07EC3A3DD5667269D816F3F218D350C10F061B9D733CA75D8139D0AB844002E6398D638D2E986F778581DDB0B99B12437B6073D76B0CED85CD78C56D097DB019B27844403F3A9B81F9DA3180CE26618E1B92300FBB5C443088F69994B5210569101D66521AE1B2F2918F32301597398639882F31F30CCAA2D33631C36C2EFEA7F6CFB2881CE2D27C8B21795C6E629B60E6026C478B2986177199416C9259FF02C4FBA2F1");
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
	}
}
