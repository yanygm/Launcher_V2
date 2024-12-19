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
				//RouterListener.MySession.Client.Send(outPacket);
			}
			return;
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
				outPacket.WriteHexString("01 EF 02 00 00 53 01 6B 3F 87 3C ED 0C 00 00 78 DA 9D 56 69 73 12 41 10 6D 13 C2 0D CB 72 29 31 DE F7 AD 78 6B 12 35 E1 10 AB B4 AC C4 EF 96 84 0D A6 B2 84 14 A0 D1 7F 6F 77 2F 03 BB A4 D9 19 2C AA 76 77 BA 5F BF 3E E8 99 E9 16 00 7C B0 F0 31 84 11 38 F0 03 5C 7C FF 84 16 AF 7A F8 3E 82 0E 1C 40 22 C3 90 63 14 3A 28 50 E2 06 1A 90 E1 12 31 1C 8C 6D BE E0 F3 64 C2 F0 8D 59 7B B0 3C 1F A2 D8 22 05 2D 8B 72 B8 52 D0 B2 29 68 34 11 08 5D 31 C5 12 62 46 F1 8C 88 56 64 C9 5C A8 DA E1 67 2A 17 5A 2C 0F 94 B6 0D 98 AA 90 B1 0D B8 AA 90 2D 69 4B E7 49 DB 88 72 C0 2A 69 CB E7 87 E7 2C 31 56 3F C4 B6 C4 38 FD 90 3C 95 A5 0B BF 70 E1 B2 52 72 5F 48 06 40 53 C6 62 16 15 7B D8 9A E4 F9 18 D5 7D 74 31 84 1D 7C FF E5 B6 2D 45 11 D0 C6 CF 11 FE 5C B4 29 93 60 1F F5 3D A4 73 51 71 36 CE 02 FA EC 4E 92 38 E7 17 AA B0 2B F2 3F 48 EF 7D 7C 1F 71 F0 AB 72 2F 04 41 E7 93 BE 4A 2B 48 13 8D D6 6C 41 51 47 2B 2F C7 23 D4 10 EC 42 4A 74 42 AA 8B 56 68 07 11 E4 52 DE A0 C9 08 78 39 CB 65 1F B0 E7 0E 0A 06 18 DB 1F F8 3E 89 F0 8A 35 07 30 65 BF 5A D0 42 54 21 AF E9 A1 2A D6 EB AB C6 AC C1 CA DF A8 68 52 92 FB FE E6 62 66 2A CC 5B 36 77 5F 1F 0E F1 49 ED E6 7D CF B2 DF 0E 87 29 B6 3B 15 11 B6 AB 49 F9 AE CE 4C EE D1 7B 79 43 6F F7 F3 86 FC 0F E4 D3 60 97 5B BB 83 46 27 F0 50 EE 5E 3F E4 51 59 CB 12 4C E4 71 59 CB 19 34 78 12 43 03 97 8B AF D8 AB 53 91 B2 7F 9A F6 6D D5 CF 63 63 85 7F 26 29 95 E5 73 0A 68 C4 A7 DF 00 95 74 36 D1 F6 26 50 57 6C E1 17 A6 06 CA C3 CB B5 50 83 F0 5E 7F F5 3F C6 CA F3 EB 70 63 97 61 74 92 3B 42 27 BD A1 3C 7F B3 E9 10 21 43 24 E8 A3 C2 61 AF 1E 64 9B 4F EE 36 AF 1D 78 6B CF 31 F8 8A 2B 4F 46 E1 AE 47 F8 96 70 71 D9 86 8D AC 6F B1 83 D1 EC B1 83 69 11 37 33 22 40 E5 FD 8E 5A 74 80 7E A8 79 B6 10 D6 47 C0 61 80 E1 3D B9 E8 70 AA 34 36 4D CB A0 AA B4 95 3B 35 5B 9D AE C6 76 5E B8 0B 5A 7C 07 38 E3 B3 68 C4 06 35 FF 10 D5 12 0B 55 B7 04 AE 20 A4 21 DF 6E 41 50 53 BE DD 82 A0 8F 45 83 7B 92 FE A9 01 ED B0 A2 C1 7D A9 C0 9F B2 BE 34 6A F8 07 F5 67 5C 93 FE 0C E8 93 AD 23 02 12 A0 9F 60 28 5F 58 02 7D 85 6B 04 5C 06 93 FA 34 09 1A 01 93 7A 37 08 BA 02 26 83 07 9D F2 10 05 93 F1 83 2E 4F 88 C1 62 D3 21 95 16 E2 B0 D8 8C 48 75 83 24 E8 C7 40 DA C8 90 82 D9 69 8D BC 41 1A FC FB 77 9D 44 19 D0 ED E2 0D 82 65 21 7C 2F 6F 12 E8 1F 49 74 3F 6C");
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
