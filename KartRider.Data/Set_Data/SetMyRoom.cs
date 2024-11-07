using System;
using System.IO;

namespace Set_Data
{
	public static class SetMyRoom
	{
		public static short MyRoom = 0;
		public static byte MyRoomBGM = 0;
		public static byte UseRoomPwd = 0;
		public static byte UseItemPwd = 0;
		public static byte TalkLock = 1;
		public static string RoomPwd = "";
		public static string ItemPwd = "";
		public static short MyRoomKart1 = 0;
		public static short MyRoomKart2 = 0;

		public static void Save_SetMyRoom()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoom + FileName.Extension, false))
			{
				streamWriter.Write(SetMyRoom.MyRoom);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoomBGM + FileName.Extension, false))
			{
				streamWriter.Write(SetMyRoom.MyRoomBGM);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_UseRoomPwd + FileName.Extension, false))
			{
				streamWriter.Write(SetMyRoom.UseRoomPwd);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_UseItemPwd + FileName.Extension, false))
			{
				streamWriter.Write(SetMyRoom.UseItemPwd);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_TalkLock + FileName.Extension, false))
			{
				streamWriter.Write(SetMyRoom.TalkLock);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_RoomPwd + FileName.Extension, false))
			{
				streamWriter.Write(SetMyRoom.RoomPwd);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_ItemPwd + FileName.Extension, false))
			{
				streamWriter.Write(SetMyRoom.ItemPwd);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoomKart1 + FileName.Extension, false))
			{
				streamWriter.Write(SetMyRoom.MyRoomKart1);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoomKart2 + FileName.Extension, false))
			{
				streamWriter.Write(SetMyRoom.MyRoomKart2);
			}
		}

		public static void Load_SetMyRoom()
		{
			string Load_MyRoom = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoom + FileName.Extension;
			if (File.Exists(Load_MyRoom))
			{
				string textValue = System.IO.File.ReadAllText(Load_MyRoom);
				SetMyRoom.MyRoom = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_MyRoom, false))
				{
					streamWriter.Write(SetMyRoom.MyRoom);
				}
			}
			//-------------------------------------------------------------------------
			string Load_MyRoomBGM = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoomBGM + FileName.Extension;
			if (File.Exists(Load_MyRoomBGM))
			{
				string textValue = System.IO.File.ReadAllText(Load_MyRoomBGM);
				SetMyRoom.MyRoomBGM = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_MyRoomBGM, false))
				{
					streamWriter.Write(SetMyRoom.MyRoomBGM);
				}
			}
			//-------------------------------------------------------------------------
			string Load_UseRoomPwd = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_UseRoomPwd + FileName.Extension;
			if (File.Exists(Load_UseRoomPwd))
			{
				string textValue = System.IO.File.ReadAllText(Load_UseRoomPwd);
				SetMyRoom.UseRoomPwd = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_UseRoomPwd, false))
				{
					streamWriter.Write(SetMyRoom.UseRoomPwd);
				}
			}
			//-------------------------------------------------------------------------
			string Load_UseItemPwd = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_UseItemPwd + FileName.Extension;
			if (File.Exists(Load_UseItemPwd))
			{
				string textValue = System.IO.File.ReadAllText(Load_UseItemPwd);
				SetMyRoom.UseItemPwd = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_UseItemPwd, false))
				{
					streamWriter.Write(SetMyRoom.UseItemPwd);
				}
			}
			//-------------------------------------------------------------------------
			string Load_TalkLock = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_TalkLock + FileName.Extension;
			if (File.Exists(Load_TalkLock))
			{
				string textValue = System.IO.File.ReadAllText(Load_TalkLock);
				SetMyRoom.TalkLock = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_TalkLock, false))
				{
					streamWriter.Write(SetMyRoom.TalkLock);
				}
			}
			//-------------------------------------------------------------------------
			string Load_RoomPwd = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_RoomPwd + FileName.Extension;
			if (File.Exists(Load_RoomPwd))
			{
				string textValue = System.IO.File.ReadAllText(Load_RoomPwd);
				SetMyRoom.RoomPwd = textValue;
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_RoomPwd, false))
				{
					streamWriter.Write(SetMyRoom.RoomPwd);
				}
			}
			//-------------------------------------------------------------------------
			string Load_ItemPwd = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_ItemPwd + FileName.Extension;
			if (File.Exists(Load_ItemPwd))
			{
				string textValue = System.IO.File.ReadAllText(Load_ItemPwd);
				SetMyRoom.ItemPwd = textValue;
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_ItemPwd, false))
				{
					streamWriter.Write(SetMyRoom.ItemPwd);
				}
			}
			//-------------------------------------------------------------------------
			string Load_MyRoomKart1 = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoomKart1 + FileName.Extension;
			if (File.Exists(Load_MyRoomKart1))
			{
				string textValue = System.IO.File.ReadAllText(Load_MyRoomKart1);
				SetMyRoom.MyRoomKart1 = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_MyRoomKart1, false))
				{
					streamWriter.Write(SetMyRoom.MyRoomKart1);
				}
			}
			//-------------------------------------------------------------------------
			string Load_MyRoomKart2 = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoomKart2 + FileName.Extension;
			if (File.Exists(Load_MyRoomKart2))
			{
				string textValue = System.IO.File.ReadAllText(Load_MyRoomKart2);
				SetMyRoom.MyRoomKart2 = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_MyRoomKart2, false))
				{
					streamWriter.Write(SetMyRoom.MyRoomKart2);
				}
			}
		}
	}
}