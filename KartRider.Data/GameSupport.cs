using System;
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
				outPacket.WriteString("http://kartupdate.tiancity.cn/patch/TPWFPZJPVQPLNWT");
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
				outPacket.WriteHexString("05 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00");
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
				outPacket.WriteHexString("013103000053019377617E280E0000789CA556F957D34010FE844269699B5E148A78DFB7E28178807214AA527DC0EF3EDA06E41928AFADA2FFBD339BAE4DCA249BEACB7BC9EECE37DF1C3B99DD1A80F716BDDA68610F4DACC0A15103DFF1151D9CC0A6A789449A20FD6915C7F43E4485543AE86284190E6960E308357A9F12C49DEDD27B8FBEA3C110CD162B1859B4C1B160E8A06FE3099FEB9A299E10239A488B684D96CC868A6DF59ECC8626CB05A5721198E691CE45E09A4766CA983A77B54E281B56305C5BF0C2B396E8AB1792B3443FBD903CA7E5003F68E228A164BE90F481FA8CC50C091AF8A62C9F90B845263AD8A6EF6F5A7330354E803A0DBBF438A453E2857D921F119D4382E909B5C0C383BF41CC7817B5DB657907F9BB4FDF63E5FCAC5C0B7ED0F9A427D31AB2414A733941B04E5A6E8CC72461D88549D1088B2ECABBA24B832197F2118A8C8197332AED6D65B9490B6DF2ED17B500EDE1152B00D067BF5A30427422AF99A1DAD7EBB39159FD99BF51368424D7FDCDE1D4B49BB772AAFA5AD437EBAADCDCF120FBED995098ECD29D706EEDC2DDB208DB31E4E99E494D2EECFBC344A2291EE423BAF8D004D48C8FE4BEB3A37EA226299DE2B1FC9F78214F4A46167FF4F32523A75FE1699C141C950ECDFEACBFA4F59FA73C4D61ABA7ACF12F24A1D65C6087BAAACFB649C85D901B09830EC49FE56554056D61712E5421FCAF7AF52FCADAF2EB706547C1F8CCB0854A7AC371FE54AA1D827488A045025B597521ABEA8CA8ABB98DB7B900852F3473D7D8DDA5983A8F1C9AD6B19CF14CB6C99B8632D04FE2BBB408D071AFB07E53C5D1A5C8FA31EA14ACBA35DC555347810653BC963D03399B8DF5BC70EA54D56963F7BA5E572954BCD7B5AA98A80D4BE0F24336E573D40FAACAE7A81FF4A118E144E69D6A93FC63516494C19F329E30D668835A03A6B7FCFD4382D45242B6767BE7EB6777EFF982DDA1A7D52B2E0DE3E8CFC19CCB0D422001F3558C093102F3065618388A28E9AF32348628DBB9C9D0B100A87F0BF8E4C178800303972D86C631DC3597771613214AD23595F38624CCF759EE1398C4E0B593AD2105E9AE39CDA234BC9D6389973218B67B2FB09A85FF69C98B4C9185B9B8B718F8078C897794F6CFA57F");
				RouterListener.MySession.Client.Send(outPacket);
			}
		}

		public static void PrDynamicCommand()
		{
			using (OutPacket outPacket = new OutPacket("PrDynamicCommand"))
			{
				outPacket.WriteByte(0);
				//outPacket.WriteInt(0);
				RouterListener.MySession.Client.Send(outPacket);
			}
		}

		public static void PrQuestUX2ndPacket()
		{
			//questGroupIndex='2' seasonId='17' kartPassSetId='1' unk='0' id='13'
			//EX) 217010013
			int NormalQuest = 13;
			int KartPassQuest = 0;
			int All_Quest = NormalQuest + KartPassQuest;
			using (OutPacket outPacket = new OutPacket("PrQuestUX2ndPacket"))
			{
				outPacket.WriteInt(1);
				outPacket.WriteInt(1);
				outPacket.WriteInt(All_Quest);
				for (int i = 1392; i <= 1405; i++)
				{
					if (i != 1401)
					{
						outPacket.WriteInt(i);
						outPacket.WriteInt(i);
						outPacket.WriteInt(0);
						outPacket.WriteInt(0);
						outPacket.WriteInt(0);
						outPacket.WriteInt(0);
						outPacket.WriteInt(2);
						outPacket.WriteInt(0);
						outPacket.WriteByte(0);
					}
				}
				RouterListener.MySession.Client.Send(outPacket);
			}
		}
	}
}
