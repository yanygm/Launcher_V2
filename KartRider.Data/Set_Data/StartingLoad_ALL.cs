using System;
using System.IO;

namespace Set_Data
{
	public static class StartingLoad_ALL
	{
		public static void StartingLoad()
		{
			string folderPath_SetRiderItem = FileName.SetRiderItem_LoadFile;
			DirectoryInfo di_SetRiderItem = new DirectoryInfo(folderPath_SetRiderItem);
			if (di_SetRiderItem.Exists == false)
			{
				di_SetRiderItem.Create();
			}        
			string folderPath_MyRoom = FileName.SetMyRoom_LoadFile;
			DirectoryInfo di_MyRoom = new DirectoryInfo(folderPath_MyRoom);
			if (di_MyRoom.Exists == false)
			{
				di_MyRoom.Create();
			}
			string folderPath_GameOption = FileName.SetGameOption_LoadFile;
			DirectoryInfo di_GameOption = new DirectoryInfo(folderPath_GameOption);
			if (di_GameOption.Exists == false)
			{
				di_GameOption.Create();
			}
			string folderPath_config = FileName.config_LoadFile;
			DirectoryInfo di_config = new DirectoryInfo(folderPath_config);
			if (di_config.Exists == false)
			{
				di_config.Create();
			}
			SetRider.Load_SetRider();
			SetRiderItem.Load_SetRiderItem();
			SetMyRoom.Load_SetMyRoom();
			SetGameOption.Load_SetGameOption();
			Set_ETC.Load_ALL();
			config.Load_ALL();
		}
	}
}