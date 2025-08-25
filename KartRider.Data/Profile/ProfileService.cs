using KartRider;
using Newtonsoft.Json;
using RiderData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Profile
{
    public class ProfileService
    {
        private readonly static string config_path = AppDomain.CurrentDomain.BaseDirectory + @"Profile\" + "Launcher.json";
        public static ProfileConfig ProfileConfig { get; set; } = new ProfileConfig();
        public static void Save()
        {
            var jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.Indented,
            };

            using (StreamWriter streamWriter = new StreamWriter(config_path, false))
            {
                streamWriter.Write(Newtonsoft.Json.JsonConvert.SerializeObject(ProfileConfig, jsonSettings));
            }
        }
        public static void Load()
        {
            if (File.Exists(config_path))
            {
                string config_str = System.IO.File.ReadAllText(config_path);
                ProfileConfig = JsonConvert.DeserializeObject<ProfileConfig>(config_str);

                Loaded();
            }
            else
            {
                LoadOldData();
                using (StreamWriter streamWriter = new StreamWriter(config_path, false))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(ProfileConfig));
                }
                DeleteOldFiles();
            }
        }
        private static void Loaded()
        {
            if (ProfileConfig.ServerSetting.PreventItem_Use == 0)
            {
                Program.PreventItem = false;
            }
            else
            {
                Program.PreventItem = true;
            }

            if (ProfileConfig.ServerSetting.SpeedPatch_Use == 0)
            {
                Program.SpeedPatch = false;
                Program.LauncherDlg.Text = "Launcher";
            }
            else
            {
                Program.SpeedPatch = true;
                Program.LauncherDlg.Text = "Launcher (속도 패치)";
            }
        }
        public static void LoadOldData()
        {
            Load_SetRider();
            Load_SetRiderItem();
            Load_SetMyRoom();
            Load_SetGameOption();
        }
        public static void Load_SetRider()
        {
            string Load_Nickname = FileName.SetRider_LoadFile + FileName.SetRider_Nickname + FileName.Extension;
            if (File.Exists(Load_Nickname))
            {
                string textValue = System.IO.File.ReadAllText(Load_Nickname);
                ProfileService.ProfileConfig.Rider.Nickname = textValue;
            }
            //-------------------------------------------------------------------------
            string Load_RiderIntro = FileName.SetRider_LoadFile + FileName.SetRider_RiderIntro + FileName.Extension;
            if (File.Exists(Load_RiderIntro))
            {
                string textValue = System.IO.File.ReadAllText(Load_RiderIntro);
                ProfileService.ProfileConfig.Rider.RiderIntro = textValue;
            }
            //-------------------------------------------------------------------------
            string Load_Card = FileName.SetRider_LoadFile + FileName.SetRider_Card + FileName.Extension;
            if (File.Exists(Load_Card))
            {
                string textValue = System.IO.File.ReadAllText(Load_Card);
                ProfileService.ProfileConfig.Rider.Card = textValue;
            }
            //-------------------------------------------------------------------------
            string Load_Emblem1 = FileName.SetRider_LoadFile + FileName.SetRider_Emblem1 + FileName.Extension;
            if (File.Exists(Load_Emblem1))
            {
                string textValue = System.IO.File.ReadAllText(Load_Emblem1);
                ProfileService.ProfileConfig.Rider.Emblem1 = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Emblem2 = FileName.SetRider_LoadFile + FileName.SetRider_Emblem2 + FileName.Extension;
            if (File.Exists(Load_Emblem2))
            {
                string textValue = System.IO.File.ReadAllText(Load_Emblem2);
                ProfileService.ProfileConfig.Rider.Emblem2 = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Lucci = FileName.SetRider_LoadFile + FileName.SetRider_Lucci + FileName.Extension;
            if (File.Exists(Load_Lucci))
            {
                string textValue = System.IO.File.ReadAllText(Load_Lucci);
                ProfileService.ProfileConfig.Rider.Lucci = uint.Parse(textValue);
                if (ProfileService.ProfileConfig.Rider.Lucci > SessionGroup.LucciMax)
                {
                    ProfileService.ProfileConfig.Rider.Lucci = SessionGroup.LucciMax;
                    using (StreamWriter streamWriter = new StreamWriter(Load_Lucci, false))
                    {
                        streamWriter.Write(SessionGroup.LucciMax);
                    }
                }
            }
            //-------------------------------------------------------------------------
            string Load_RP = FileName.SetRider_LoadFile + FileName.SetRider_RP + FileName.Extension;
            if (File.Exists(Load_RP))
            {
                string textValue = System.IO.File.ReadAllText(Load_RP);
                ProfileService.ProfileConfig.Rider.RP = uint.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Koin = FileName.SetRider_LoadFile + FileName.SetRider_Koin + FileName.Extension;
            if (File.Exists(Load_Koin))
            {
                string textValue = System.IO.File.ReadAllText(Load_Koin);
                ProfileService.ProfileConfig.Rider.Koin = uint.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Premium = FileName.SetRider_LoadFile + FileName.SetRider_Premium + FileName.Extension;
            if (File.Exists(Load_Premium))
            {
                string textValue = System.IO.File.ReadAllText(Load_Premium);
                ProfileService.ProfileConfig.Rider.Premium = int.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_SlotChanger = FileName.SetRider_LoadFile + FileName.SetRider_SlotChanger + FileName.Extension;
            if (File.Exists(Load_SlotChanger))
            {
                string textValue = System.IO.File.ReadAllText(Load_SlotChanger);
                ProfileService.ProfileConfig.Rider.SlotChanger = ushort.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_ClubMark_LOGO = FileName.SetRider_LoadFile + FileName.SetRider_ClubMark_LOGO + FileName.Extension;
            if (File.Exists(Load_ClubMark_LOGO))
            {
                string textValue = System.IO.File.ReadAllText(Load_ClubMark_LOGO);
                ProfileService.ProfileConfig.Rider.ClubMark_LOGO = int.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_ClubMark_LINE = FileName.SetRider_LoadFile + FileName.SetRider_ClubMark_LINE + FileName.Extension;
            if (File.Exists(Load_ClubMark_LINE))
            {
                string textValue = System.IO.File.ReadAllText(Load_ClubMark_LINE);
                ProfileService.ProfileConfig.Rider.ClubMark_LINE = int.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_ClubName = FileName.SetRider_LoadFile + FileName.SetRider_ClubName + FileName.Extension;
            if (File.Exists(Load_ClubName))
            {
                string textValue = System.IO.File.ReadAllText(Load_ClubName);
                ProfileService.ProfileConfig.Rider.ClubName = textValue;
            }
        }
        public static void Load_SetRiderItem()
        {
            string Load_Character = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Character + FileName.Extension;
            if (File.Exists(Load_Character))
            {
                string textValue = System.IO.File.ReadAllText(Load_Character);
                ProfileService.ProfileConfig.RiderItem.Set_Character = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Paint = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Paint + FileName.Extension;
            if (File.Exists(Load_Paint))
            {
                string textValue = System.IO.File.ReadAllText(Load_Paint);
                ProfileService.ProfileConfig.RiderItem.Set_Paint = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Kart = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Kart + FileName.Extension;
            if (File.Exists(Load_Kart))
            {
                string textValue = System.IO.File.ReadAllText(Load_Kart);
                ProfileService.ProfileConfig.RiderItem.Set_Kart = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Plate = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Plate + FileName.Extension;
            if (File.Exists(Load_Plate))
            {
                string textValue = System.IO.File.ReadAllText(Load_Plate);
                ProfileService.ProfileConfig.RiderItem.Set_Plate = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Goggle = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Goggle + FileName.Extension;
            if (File.Exists(Load_Goggle))
            {
                string textValue = System.IO.File.ReadAllText(Load_Goggle);
                ProfileService.ProfileConfig.RiderItem.Set_Goggle = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Balloon = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Balloon + FileName.Extension;
            if (File.Exists(Load_Balloon))
            {
                string textValue = System.IO.File.ReadAllText(Load_Balloon);
                ProfileService.ProfileConfig.RiderItem.Set_Balloon = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_HeadBand = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_HeadBand + FileName.Extension;
            if (File.Exists(Load_HeadBand))
            {
                string textValue = System.IO.File.ReadAllText(Load_HeadBand);
                ProfileService.ProfileConfig.RiderItem.Set_HeadBand = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_HeadPhone = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_HeadPhone + FileName.Extension;
            if (File.Exists(Load_HeadPhone))
            {
                string textValue = System.IO.File.ReadAllText(Load_HeadPhone);
                ProfileService.ProfileConfig.RiderItem.Set_HeadPhone = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_HandGearL = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_HandGearL + FileName.Extension;
            if (File.Exists(Load_HandGearL))
            {
                string textValue = System.IO.File.ReadAllText(Load_HandGearL);
                ProfileService.ProfileConfig.RiderItem.Set_HandGearL = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Uniform = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Uniform + FileName.Extension;
            if (File.Exists(Load_Uniform))
            {
                string textValue = System.IO.File.ReadAllText(Load_Uniform);
                ProfileService.ProfileConfig.RiderItem.Set_Uniform = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Decal = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Decal + FileName.Extension;
            if (File.Exists(Load_Decal))
            {
                string textValue = System.IO.File.ReadAllText(Load_Decal);
                ProfileService.ProfileConfig.RiderItem.Set_Decal = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Pet = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Pet + FileName.Extension;
            if (File.Exists(Load_Pet))
            {
                string textValue = System.IO.File.ReadAllText(Load_Pet);
                ProfileService.ProfileConfig.RiderItem.Set_Pet = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_FlyingPet = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_FlyingPet + FileName.Extension;
            if (File.Exists(Load_FlyingPet))
            {
                string textValue = System.IO.File.ReadAllText(Load_FlyingPet);
                ProfileService.ProfileConfig.RiderItem.Set_FlyingPet = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Aura = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Aura + FileName.Extension;
            if (File.Exists(Load_Aura))
            {
                string textValue = System.IO.File.ReadAllText(Load_Aura);
                ProfileService.ProfileConfig.RiderItem.Set_Aura = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_SkidMark = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_SkidMark + FileName.Extension;
            if (File.Exists(Load_SkidMark))
            {
                string textValue = System.IO.File.ReadAllText(Load_SkidMark);
                ProfileService.ProfileConfig.RiderItem.Set_SkidMark = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_RidColor = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_RidColor + FileName.Extension;
            if (File.Exists(Load_RidColor))
            {
                string textValue = System.IO.File.ReadAllText(Load_RidColor);
                ProfileService.ProfileConfig.RiderItem.Set_RidColor = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_BonusCard = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_BonusCard + FileName.Extension;
            if (File.Exists(Load_BonusCard))
            {
                string textValue = System.IO.File.ReadAllText(Load_BonusCard);
                ProfileService.ProfileConfig.RiderItem.Set_BonusCard = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Tachometer = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Tachometer + FileName.Extension;
            if (File.Exists(Load_Tachometer))
            {
                string textValue = System.IO.File.ReadAllText(Load_Tachometer);
                ProfileService.ProfileConfig.RiderItem.Set_Tachometer = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Dye = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Dye + FileName.Extension;
            if (File.Exists(Load_Dye))
            {
                string textValue = System.IO.File.ReadAllText(Load_Dye);
                ProfileService.ProfileConfig.RiderItem.Set_Dye = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_KartSN = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_KartSN + FileName.Extension;
            if (File.Exists(Load_KartSN))
            {
                string textValue = System.IO.File.ReadAllText(Load_KartSN);
                ProfileService.ProfileConfig.RiderItem.Set_KartSN = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_slotBg = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_slotBg + FileName.Extension;
            if (File.Exists(Load_slotBg))
            {
                string textValue = System.IO.File.ReadAllText(Load_slotBg);
                ProfileService.ProfileConfig.RiderItem.Set_slotBg = byte.Parse(textValue);
            }
        }
        public static void Load_SetMyRoom()
        {
            string Load_MyRoom = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoom + FileName.Extension;
            if (File.Exists(Load_MyRoom))
            {
                string textValue = System.IO.File.ReadAllText(Load_MyRoom);
                ProfileService.ProfileConfig.MyRoom.MyRoom = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_MyRoomBGM = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoomBGM + FileName.Extension;
            if (File.Exists(Load_MyRoomBGM))
            {
                string textValue = System.IO.File.ReadAllText(Load_MyRoomBGM);
                ProfileService.ProfileConfig.MyRoom.MyRoomBGM = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_UseRoomPwd = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_UseRoomPwd + FileName.Extension;
            if (File.Exists(Load_UseRoomPwd))
            {
                string textValue = System.IO.File.ReadAllText(Load_UseRoomPwd);
                ProfileService.ProfileConfig.MyRoom.UseRoomPwd = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_UseItemPwd = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_UseItemPwd + FileName.Extension;
            if (File.Exists(Load_UseItemPwd))
            {
                string textValue = System.IO.File.ReadAllText(Load_UseItemPwd);
                ProfileService.ProfileConfig.MyRoom.UseItemPwd = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_TalkLock = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_TalkLock + FileName.Extension;
            if (File.Exists(Load_TalkLock))
            {
                string textValue = System.IO.File.ReadAllText(Load_TalkLock);
                ProfileService.ProfileConfig.MyRoom.TalkLock = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_RoomPwd = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_RoomPwd + FileName.Extension;
            if (File.Exists(Load_RoomPwd))
            {
                string textValue = System.IO.File.ReadAllText(Load_RoomPwd);
                ProfileService.ProfileConfig.MyRoom.RoomPwd = textValue;
            }
            //-------------------------------------------------------------------------
            string Load_ItemPwd = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_ItemPwd + FileName.Extension;
            if (File.Exists(Load_ItemPwd))
            {
                string textValue = System.IO.File.ReadAllText(Load_ItemPwd);
                ProfileService.ProfileConfig.MyRoom.ItemPwd = textValue;
            }
            //-------------------------------------------------------------------------
            string Load_MyRoomKart1 = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoomKart1 + FileName.Extension;
            if (File.Exists(Load_MyRoomKart1))
            {
                string textValue = System.IO.File.ReadAllText(Load_MyRoomKart1);
                ProfileService.ProfileConfig.MyRoom.MyRoomKart1 = short.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_MyRoomKart2 = FileName.SetMyRoom_LoadFile + FileName.SetMyRoom_MyRoomKart2 + FileName.Extension;
            if (File.Exists(Load_MyRoomKart2))
            {
                string textValue = System.IO.File.ReadAllText(Load_MyRoomKart2);
                ProfileService.ProfileConfig.MyRoom.MyRoomKart2 = short.Parse(textValue);
            }
        }
        public static void Load_SetGameOption()
        {
            string Load_Set_BGM = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Set_BGM + FileName.Extension;
            if (File.Exists(Load_Set_BGM))
            {
                string textValue = System.IO.File.ReadAllText(Load_Set_BGM);
                ProfileService.ProfileConfig.GameOption.Set_BGM = float.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Set_Sound = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Set_Sound + FileName.Extension;
            if (File.Exists(Load_Set_Sound))
            {
                string textValue = System.IO.File.ReadAllText(Load_Set_Sound);
                ProfileService.ProfileConfig.GameOption.Set_Sound = float.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Main_BGM = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Main_BGM + FileName.Extension;
            if (File.Exists(Load_Main_BGM))
            {
                string textValue = System.IO.File.ReadAllText(Load_Main_BGM);
                ProfileService.ProfileConfig.GameOption.Main_BGM = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Sound_effect = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Sound_effect + FileName.Extension;
            if (File.Exists(Load_Sound_effect))
            {
                string textValue = System.IO.File.ReadAllText(Load_Sound_effect);
                ProfileService.ProfileConfig.GameOption.Sound_effect = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Full_screen = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Full_screen + FileName.Extension;
            if (File.Exists(Load_Full_screen))
            {
                string textValue = System.IO.File.ReadAllText(Load_Full_screen);
                ProfileService.ProfileConfig.GameOption.Full_screen = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk1 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk1 + FileName.Extension;
            if (File.Exists(Load_Unk1))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk1);
                ProfileService.ProfileConfig.GameOption.ShowMirror = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk2 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk2 + FileName.Extension;
            if (File.Exists(Load_Unk2))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk2);
                ProfileService.ProfileConfig.GameOption.ShowOtherPlayerNames = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk3 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk3 + FileName.Extension;
            if (File.Exists(Load_Unk3))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk3);
                ProfileService.ProfileConfig.GameOption.ShowOutlines = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk4 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk4 + FileName.Extension;
            if (File.Exists(Load_Unk4))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk4);
                ProfileService.ProfileConfig.GameOption.ShowShadows = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk5 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk5 + FileName.Extension;
            if (File.Exists(Load_Unk5))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk5);
                ProfileService.ProfileConfig.GameOption.HighLevelEffect = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk6 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk6 + FileName.Extension;
            if (File.Exists(Load_Unk6))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk6);
                ProfileService.ProfileConfig.GameOption.MotionBlurEffect = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk7 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk7 + FileName.Extension;
            if (File.Exists(Load_Unk7))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk7);
                ProfileService.ProfileConfig.GameOption.MotionDistortionEffect = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk8 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk8 + FileName.Extension;
            if (File.Exists(Load_Unk8))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk8);
                ProfileService.ProfileConfig.GameOption.HighEndOptimization = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk9 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk9 + FileName.Extension;
            if (File.Exists(Load_Unk9))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk9);
                ProfileService.ProfileConfig.GameOption.AutoReady = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk10 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk10 + FileName.Extension;
            if (File.Exists(Load_Unk10))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk10);
                ProfileService.ProfileConfig.GameOption.PropDescription = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk11 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk11 + FileName.Extension;
            if (File.Exists(Load_Unk11))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk11);
                ProfileService.ProfileConfig.GameOption.VideoQuality = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_BGM_Check = FileName.SetGameOption_LoadFile + FileName.SetGameOption_BGM_Check + FileName.Extension;
            if (File.Exists(Load_BGM_Check))
            {
                string textValue = System.IO.File.ReadAllText(Load_BGM_Check);
                ProfileService.ProfileConfig.GameOption.BGM_Check = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Sound_Check = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Sound_Check + FileName.Extension;
            if (File.Exists(Load_Sound_Check))
            {
                string textValue = System.IO.File.ReadAllText(Load_Sound_Check);
                ProfileService.ProfileConfig.GameOption.Sound_Check = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk12 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk12 + FileName.Extension;
            if (File.Exists(Load_Unk12))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk12);
                ProfileService.ProfileConfig.GameOption.ShowHitInfo = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk13 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk13 + FileName.Extension;
            if (File.Exists(Load_Unk13))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk13);
                ProfileService.ProfileConfig.GameOption.AutoBoost = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_GameType = FileName.SetGameOption_LoadFile + FileName.SetGameOption_GameType + FileName.Extension;
            if (File.Exists(Load_GameType))
            {
                string textValue = System.IO.File.ReadAllText(Load_GameType);
                ProfileService.ProfileConfig.GameOption.GameType = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_SetGhost = FileName.SetGameOption_LoadFile + FileName.SetGameOption_SetGhost + FileName.Extension;
            if (File.Exists(Load_SetGhost))
            {
                string textValue = System.IO.File.ReadAllText(Load_SetGhost);
                ProfileService.ProfileConfig.GameOption.SetGhost = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_SpeedType = FileName.SetGameOption_LoadFile + FileName.SetGameOption_SpeedType + FileName.Extension;
            if (File.Exists(Load_SpeedType))
            {
                string textValue = System.IO.File.ReadAllText(Load_SpeedType);
                ProfileService.ProfileConfig.GameOption.SpeedType = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk14 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk14 + FileName.Extension;
            if (File.Exists(Load_Unk14))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk14);
                ProfileService.ProfileConfig.GameOption.RoomChat = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk15 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk15 + FileName.Extension;
            if (File.Exists(Load_Unk15))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk15);
                ProfileService.ProfileConfig.GameOption.DrivingChat = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk16 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk16 + FileName.Extension;
            if (File.Exists(Load_Unk16))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk16);
                ProfileService.ProfileConfig.GameOption.ShowAllPlayerHitInfo = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk17 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk17 + FileName.Extension;
            if (File.Exists(Load_Unk17))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk17);
                ProfileService.ProfileConfig.GameOption.ShowTeamColor = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Set_screen = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Set_screen + FileName.Extension;
            if (File.Exists(Load_Set_screen))
            {
                string textValue = System.IO.File.ReadAllText(Load_Set_screen);
                ProfileService.ProfileConfig.GameOption.Set_screen = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Unk18 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk18 + FileName.Extension;
            if (File.Exists(Load_Unk18))
            {
                string textValue = System.IO.File.ReadAllText(Load_Unk18);
                ProfileService.ProfileConfig.GameOption.HideCompetitiveRank = byte.Parse(textValue);
            }
            //-------------------------------------------------------------------------
            string Load_Version = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Version + FileName.Extension;
            if (File.Exists(Load_Version))
            {
                string textValue = System.IO.File.ReadAllText(Load_Version);
                ProfileService.ProfileConfig.GameOption.Version = ushort.Parse(textValue);
            }
        }
        private static void DeleteOldFiles()
        {
            var dir_path = AppDomain.CurrentDomain.BaseDirectory + @"Profile\Launcher";
            if (Directory.Exists(dir_path))
            {
                Directory.Delete(dir_path, true);
            }
        }
    }
}
