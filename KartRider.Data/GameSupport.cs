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
				outPacket.WriteHexString("01240300005301307118D6FF0D000078DAA55669531341107D728584249B830041BCEF5BF1403C403912A28216F0DD22C9122917422551F4DFDB3D9B31BBA1D999686DD5EECCF4EBD7C7CC76CF1680F70EBD5A68620F75ACC0A3510DDFF1156D1CC3A5A78E788A20BD690547F43E408954DAE86088190E68E0E2105BF43E21883FDBA5F71E7D87CF8668B691BC91451B1CCD1BD934742C1E725D33C5E26244E32911ADC9129948B1ABDE1399C864F9A064D682691EA9AC05D73CD20563EAFCD52AA15C380563FA82F08C23FA1A84641DD1CF2024C76969E0074D3C2594CCE71321508F71324D821ABE29CBC7246E928936B6E9FB9BD63C14C60850A561871E8F74A678619FE48744E791607A5C2DF0B0F1378899E0A276BB28EF207FF7E97BA49C9F95CF4218743E11C8B4869449692E2B08D649CB8FF188240CBB30211A61D14527F20431E452CEE29031F0725AA5BDA52CD769A145BEFDA212A03DBCE29C01E8B15FCD1B213A91D7CC50EDEBF5596BD670E66F140D21C9E7FEE6606ADACD5B5975FA9A5437ABEAB8F9E37EF6DB339130D9A53BD1DCDA85BB4511B663C8D33D939A7CB0EF0F1289A67890B374F161CED2A94772DDD9513F519D944EF058FE4F82902753469670F4F35346CEB0C2D31829782A1D9AFD596F49EB3F4F068AC2665759E35F4842ADB9C00E75549D6D9190AB2017120635C49FE5A5AD82B6B03817A910FD57BDFA17656DF975B4B2A760DC335CE124BDE1387F2AD53641DA44D02481ABACFA9055D523AA6AEEE26DF60C852F34F3D7D8DDA511D58F3C9A56B19C0E4CB6C99B9A32D04BE2BB9408D071AFB07E5DC5D1A1C87A31EA14ACFA67B8A3A69E02F5A7782D730A723A1BEB39A1EB5454B771BB55AFA3144AC1EB5A454C54D911B8C2900DB98F864115B98F86411F262D3A32EF548BE41F272D3AB3067F4A07C258A30D6AF699DE0CD70F09B29514B2B5DBEDAF9FFDBDE70B769B9E66F7706918B39F8339976542200EF3558CD389219837B0C4C061D8A4BFC2D011D86CE7064347617383E2CE8331D8DCA3F816801806BBE6F2CE621C835D76396F48C07C9FE53A8109F45F3BD91A9290EE9AD32C4A2158399678298D41ABF702AB39F89F92BCC8147F00E4177131");
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
					outPacket.WriteInt(0);
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
