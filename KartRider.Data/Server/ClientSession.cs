using ExcData;
using KartRider;
using KartRider.Common.Network;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider_PacketName;
using Profile;
using RHOParser;
using RiderData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace KartRider
{
    public class ClientSession : Session
    {
        public SessionGroup Parent
        {
            get;
            set;
        }

        public ClientSession(SessionGroup parent, System.Net.Sockets.Socket socket) : base(socket)
        {
            this.Parent = parent;
        }

        public static uint Time = 0;

        public override void OnDisconnect()
        {
            this.Parent.Client.Disconnect();
        }

        public override void OnPacket(InPacket iPacket)
        {
            int ALLnum;
            lock (this.Parent.m_lock)
            {
                iPacket.Position = 0;
                uint hash = iPacket.ReadUInt();
                if (hash != 1950550337)//PqServerSideUdpBindCheck
                {
                    Console.WriteLine((PacketName)hash + "：" + BitConverter.ToString(iPacket.ToArray()).Replace("-", ""));
                }
                if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCnAuthenLogin", 0))
                {
                    using (OutPacket outPacket = new OutPacket("PrCnAuthenLogin"))
                    {
                        outPacket.WriteInt(1);
                        outPacket.WriteString("pnlcdfngkdjfdhdermnkicqknmqrnjnkrlpdirerjrqkcllhpckngophnrrfclgiojmopomonkjilgmheoldpmmcdokgdqljqcnkrplffhflqdnchherghnhoihgfnon");
                        outPacket.WriteByte(0);
                        outPacket.WriteString("https://www.tiancity.com/agreement");
                        this.Parent.Client.Send(outPacket);
                    }
                    return;
                }
                if (hash == Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("PcReportRaidOccur"), 0) ? false : hash != 1340475309)//PqGameReportMyBadUdp
                {
                    if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRiderTalkPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqEnterMagicHatPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("LoPingRequestPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqGetRiderQuestUX2ndData", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqAddTimeEventInitPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqCountdownBoxPeriodPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqServerSideUdpBindCheck", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqVipGradeCheck", 0))
                    {
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqUpdateRiderSchoolDataPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("RmRiderTalkPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqNeedTimerGiftEvent", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("GameBoosterAddPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("LoRqCheckReplayItemPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqGetRecommandChatServerInfo", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("LoCheckLoginEvent", 0))
                    {
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqBlockWordLogPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqWriteActionLogPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqAddTimeEventTimerPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqTimeShopOpenTimePacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqItemPresetSlotDataList", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("VipPlaytimeCheck", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("LoRqEventRewardPacket", 0))
                    {
                        //PqGetRecommandChatServerInfo = 라이더 챗
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqAddRacingTimePacket", 0))
                    {
                        uint Track = iPacket.ReadUInt();
                        iPacket.ReadBytes(10);
                        short Kart = iPacket.ReadShort();
                        iPacket.ReadBytes(416);
                        short Boooster = iPacket.ReadShort();
                        iPacket.ReadShort();
                        short Crash = iPacket.ReadShort();
                        iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("LoRpAddRacingTimePacket"))
                        {
                            //outPacket.WriteHexString("FF FF FF FF 00 00 00 00 00 00 00 00 00 00");
                            outPacket.WriteUInt(Time);
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        var manager = new CompetitiveDataManager();
                        CompleteTrackScoreCalculator calculator = new CompleteTrackScoreCalculator();
                        var scores = calculator.CalculateTrackScoreDetails(Track, Time, Boooster, Crash, FavoriteItem.TrackDictionary);
                        if (scores != null)
                        {
                            var data = new CompetitiveData { Track = Track, Kart = Kart, Time = Time, Boooster = Boooster, BooosterPoint = scores.BoostScore, Crash = Crash, CrashPoint = scores.CrashScore, Point = scores.TotalScore };
                            manager.SaveData(data);
                        }
                        using (OutPacket outPacket = new OutPacket("PrGetCompetitiveSlotInfo"))
                        {
                            var competitiveData = manager.LoadAllData();
                            outPacket.WriteInt(competitiveData.Count);
                            foreach (var competitive in competitiveData)
                            {
                                outPacket.WriteUInt(competitive.Track);
                                outPacket.WriteUInt(competitive.Track);
                                outPacket.WriteShort(competitive.Kart);
                                outPacket.WriteUInt(competitive.Time);
                                outPacket.WriteHexString("FF FF FF FF");
                                outPacket.WriteShort(competitive.Boooster);
                                outPacket.WriteUInt(competitive.BooosterPoint);
                                outPacket.WriteShort(competitive.Crash);
                                outPacket.WriteUInt(competitive.CrashPoint);
                                outPacket.WriteUInt(competitive.Point);
                                outPacket.WriteInt(0);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqUploadFilePacket", 0))
                    {
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqStartSinglePacket", 0))
                    {
                        int TimeAttackStartTicks = iPacket.ReadInt();
                        this.Parent.TimeAttackStartTicks = TimeAttackStartTicks;
                        this.Parent.PlaneCheck1 = (byte)this.Parent.TimeAttackStartTicks;
                        uint key = CryptoConstants.GetKey(CryptoConstants.GetKey((uint)this.Parent.TimeAttackStartTicks)) % 5 + 6;
                        ALLnum = (int)key;
                        this.Parent.SendPlaneCount = (int)key;
                        this.Parent.TotalSendPlaneCount = 0;
                        Console.WriteLine("PlaneCheckMax: {0}", this.Parent.SendPlaneCount);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameReportPacket", 0))
                    {
                        byte[] DateTime1 = iPacket.ReadBytes(18);
                        int GetItem = iPacket.ReadInt();
                        int UseItem = iPacket.ReadInt();
                        int UseBooster = iPacket.ReadEncodedInt();
                        int[][] hashArray1 = new int[20][];
                        for (int k = 0; k < 20; k++)
                        {
                            hashArray1[k] = new int[3];
                            hashArray1[k][0] = iPacket.ReadEncodedInt();
                            hashArray1[k][1] = iPacket.ReadEncodedInt();
                            hashArray1[k][2] = iPacket.ReadEncodedInt();
                        }
                        int hash1 = iPacket.ReadEncodedInt();
                        int hash2 = iPacket.ReadEncodedInt();
                        int hash3 = iPacket.ReadEncodedInt();
                        float single1 = iPacket.ReadEncodedFloat();
                        float single2 = iPacket.ReadEncodedFloat();
                        float single3 = iPacket.ReadEncodedFloat();
                        int PlaneCheck = iPacket.ReadInt();
                        byte[] hashArray2 = iPacket.ReadBytes(20);
                        int hash4 = iPacket.ReadInt();
                        byte[] hashArray3 = iPacket.ReadBytes(16);
                        this.Parent.TotalSendPlaneCount += PlaneCheck;
                        Console.WriteLine("PlaneCheck: {0}, Total: {1}, Max: {2}, Dist: {3}", PlaneCheck, this.Parent.TotalSendPlaneCount, this.Parent.SendPlaneCount, single3);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqRenameRidPacket", 0))
                    {
                        ProfileService.ProfileConfig.Rider.Nickname = iPacket.ReadString(false);
                        using (OutPacket outPacket = new OutPacket("SpRpRenameRidPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        ProfileService.Save();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetRider", 0))
                    {
                        NewRider.LoadItemData();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqGetRiderItemPacket", 0))
                    {
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqSetRiderItemOnPacket", 0))
                    {
                        ProfileService.ProfileConfig.RiderItem.Set_Character = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Paint = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Kart = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Plate = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Goggle = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Balloon = iPacket.ReadShort();
                        iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_HeadBand = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_HeadPhone = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_HandGearL = iPacket.ReadShort();
                        iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Uniform = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Decal = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Pet = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_FlyingPet = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Aura = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_SkidMark = iPacket.ReadShort();
                        iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_RidColor = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_BonusCard = iPacket.ReadShort();
                        iPacket.ReadShort();
                        short Set_KartPlant1 = iPacket.ReadShort();
                        short Set_KartPlant2 = iPacket.ReadShort();
                        short Set_KartPlant3 = iPacket.ReadShort();
                        short Set_KartPlant4 = iPacket.ReadShort();
                        iPacket.ReadShort();
                        iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Tachometer = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_Dye = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_KartSN = iPacket.ReadShort();
                        iPacket.ReadByte();
                        short Set_KartCoating = iPacket.ReadShort();
                        short Set_KartTailLamp = iPacket.ReadShort();
                        ProfileService.ProfileConfig.RiderItem.Set_slotBg = iPacket.ReadShort();
                        short Set_KartCoating12 = iPacket.ReadShort();
                        short Set_KartTailLamp12 = iPacket.ReadShort();
                        short Set_KartBoosterEffect12 = iPacket.ReadShort();
                        iPacket.ReadShort();
                        Console.WriteLine($"Set_KartPlant1: {Set_KartPlant1}");
                        Console.WriteLine($"Set_KartPlant2: {Set_KartPlant2}");
                        Console.WriteLine($"Set_KartPlant3: {Set_KartPlant3}");
                        Console.WriteLine($"Set_KartPlant4: {Set_KartPlant4}");
                        Console.WriteLine($"Set_KartCoating: {Set_KartCoating}");
                        Console.WriteLine($"Set_KartTailLamp: {Set_KartTailLamp}");
                        Console.WriteLine($"Set_KartCoating12: {Set_KartCoating12}");
                        Console.WriteLine($"Set_KartTailLamp12: {Set_KartTailLamp12}");
                        Console.WriteLine($"Set_KartBoosterEffect12: {Set_KartBoosterEffect12}");
                        ProfileService.Save();
                        StartGameData.FlyingPet_id = ProfileService.ProfileConfig.RiderItem.Set_FlyingPet;
                        StartGameData.Kart_id = ProfileService.ProfileConfig.RiderItem.Set_Kart;
                        ExcSpec.Use_PartsSpec(ProfileService.ProfileConfig.RiderItem.Set_Kart, ProfileService.ProfileConfig.RiderItem.Set_KartSN);
                        ExcSpec.Use_ExcSpec(ProfileService.ProfileConfig.RiderItem.Set_Kart, ProfileService.ProfileConfig.RiderItem.Set_KartSN);
                        ExcSpec.Use_PlantSpec(ProfileService.ProfileConfig.RiderItem.Set_Kart, ProfileService.ProfileConfig.RiderItem.Set_KartSN);
                        ExcSpec.Use_KartLevelSpec(ProfileService.ProfileConfig.RiderItem.Set_Kart, ProfileService.ProfileConfig.RiderItem.Set_KartSN);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetRiderInfo", 0))
                    {
                        iPacket.ReadInt();
                        iPacket.ReadInt();
                        string Nickname = iPacket.ReadString(false);
                        if (Nickname == ProfileService.ProfileConfig.Rider.Nickname)
                        {
                            //GameSupport.PrGetRiderInfo();
                            using (OutPacket outPacket = new OutPacket("PrGetRiderInfo"))
                            {
                                outPacket.WriteByte(1);
                                outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.UserNO);
                                outPacket.WriteString(ProfileService.ProfileConfig.Rider.UserID);
                                outPacket.WriteString(ProfileService.ProfileConfig.Rider.Nickname);
                                outPacket.WriteUShort((ushort)RouterListener.DataTime()[0]);
                                outPacket.WriteUShort((ushort)RouterListener.DataTime()[1]);
                                GameSupport.GetRider(outPacket);
                                outPacket.WriteShort(0);
                                outPacket.WriteString(ProfileService.ProfileConfig.Rider.Card);
                                outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.RP);
                                outPacket.WriteInt(0);
                                outPacket.WriteByte(6);//Licenses
                                outPacket.WriteUShort((ushort)RouterListener.DataTime()[0]);
                                outPacket.WriteUShort((ushort)RouterListener.DataTime()[1]);
                                outPacket.WriteBytes(new byte[17]);
                                outPacket.WriteShort(ProfileService.ProfileConfig.Rider.Emblem1);
                                outPacket.WriteShort(ProfileService.ProfileConfig.Rider.Emblem2);
                                outPacket.WriteShort(0);
                                outPacket.WriteString(ProfileService.ProfileConfig.Rider.RiderIntro);
                                outPacket.WriteInt(ProfileService.ProfileConfig.Rider.Premium);
                                outPacket.WriteByte(1);
                                if (ProfileService.ProfileConfig.Rider.Premium == 0)
                                    outPacket.WriteInt(0);
                                else if (ProfileService.ProfileConfig.Rider.Premium == 1)
                                    outPacket.WriteInt(10000);
                                else if (ProfileService.ProfileConfig.Rider.Premium == 2)
                                    outPacket.WriteInt(30000);
                                else if (ProfileService.ProfileConfig.Rider.Premium == 3)
                                    outPacket.WriteInt(60000);
                                else if (ProfileService.ProfileConfig.Rider.Premium == 4)
                                    outPacket.WriteInt(120000);
                                else if (ProfileService.ProfileConfig.Rider.Premium == 5)
                                    outPacket.WriteInt(200000);
                                else
                                    outPacket.WriteInt(0);
                                if (ProfileService.ProfileConfig.Rider.ClubMark_LOGO == 0)
                                {
                                    outPacket.WriteInt(0);
                                    outPacket.WriteInt(0);
                                    outPacket.WriteInt(0);
                                    outPacket.WriteString("");
                                }
                                else
                                {
                                    outPacket.WriteInt(ProfileService.ProfileConfig.Rider.ClubCode);
                                    outPacket.WriteInt(ProfileService.ProfileConfig.Rider.ClubMark_LOGO);
                                    outPacket.WriteInt(ProfileService.ProfileConfig.Rider.ClubMark_LINE);
                                    outPacket.WriteString(ProfileService.ProfileConfig.Rider.ClubName);
                                }
                                outPacket.WriteInt(0);
                                outPacket.WriteByte(ProfileService.ProfileConfig.Rider.Ranker);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteByte(0);
                                outPacket.WriteByte(0);
                                outPacket.WriteByte(0);
                                RouterListener.MySession.Client.Send(outPacket);
                            }
                        }
                        else
                        {
                            using (OutPacket outPacket = new OutPacket("PrGetRiderInfo"))
                            {
                                outPacket.WriteByte(0);
                                this.Parent.Client.Send(outPacket);
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUpdateRiderIntro", 0))
                    {
                        ProfileService.ProfileConfig.Rider.RiderIntro = iPacket.ReadString(false);
                        ProfileService.Save();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUpdateRiderSchoolLevelPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrUpdateRiderSchoolLevelPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSetPlaytimeEventTick", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrSetPlaytimeEventTick"))
                        {
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[0]);
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[1]);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUpdateGameOption", 0))
                    {
                        ProfileService.ProfileConfig.GameOption.Set_BGM = iPacket.ReadFloat();
                        ProfileService.ProfileConfig.GameOption.Set_Sound = iPacket.ReadFloat();
                        ProfileService.ProfileConfig.GameOption.Main_BGM = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.Sound_effect = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.Full_screen = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.ShowMirror = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.ShowOtherPlayerNames = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.ShowOutlines = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.ShowShadows = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.HighLevelEffect = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.MotionBlurEffect = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.MotionDistortionEffect = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.HighEndOptimization = iPacket.ReadByte();//오토 레디
                        ProfileService.ProfileConfig.GameOption.AutoReady = iPacket.ReadByte();//아이템 설명
                        ProfileService.ProfileConfig.GameOption.PropDescription = iPacket.ReadByte();//녹화 품질
                        ProfileService.ProfileConfig.GameOption.VideoQuality = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.BGM_Check = iPacket.ReadByte();//배경음
                        ProfileService.ProfileConfig.GameOption.Sound_Check = iPacket.ReadByte();//효과음
                        ProfileService.ProfileConfig.GameOption.ShowHitInfo = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.AutoBoost = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.GameType = iPacket.ReadByte();//팀전 개인전 여부
                        ProfileService.ProfileConfig.GameOption.SetGhost = iPacket.ReadByte();//고스트 사용여부
                        // 速度由客户端控制，游戏内调整速度不生效
                        //ProfileService.ProfileConfig.GameOption.SpeedType = iPacket.ReadByte();//채널 속도
                        iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.RoomChat = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.DrivingChat = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.ShowAllPlayerHitInfo = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.ShowTeamColor = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.Set_screen = iPacket.ReadByte();
                        ProfileService.ProfileConfig.GameOption.HideCompetitiveRank = iPacket.ReadByte();
                        ProfileService.Save();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetGameOption", 0))
                    {
                        GameSupport.PrGetGameOption();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqVipInfo", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrVipInfo"))
                        {
                            outPacket.WriteInt(1);
                            for (int i = 0; i < 10; i++)
                            {
                                outPacket.WriteInt(0);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqLoginVipInfo", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrLoginVipInfo"))
                        {
                            outPacket.WriteInt(ProfileService.ProfileConfig.Rider.Premium);
                            outPacket.WriteByte(1);
                            outPacket.WriteInt(0);
                            outPacket.WriteHexString("FFFFFFFF");
                            this.Parent.Client.Send(outPacket);
                        }
                        using (OutPacket outPacket = new OutPacket("LoRpEventRewardPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        using (OutPacket outPacket = new OutPacket("PcSlaveNotice"))
                        {
                            outPacket.WriteString("单机版完全免费，GitHub：https://github.com/yanygm/Launcher_V2");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChReRqEnterMyRoomPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("ChRqEnterRandomMyRoomPacket", 0))
                    {
                        if (hash == 1733888222)//ChReRqEnterMyRoomPacket
                        {
                            GameType.EnterMyRoomType = 0;
                        }
                        else if (hash == 2423851656)//ChRqEnterRandomMyRoomPacket
                        {
                            GameType.EnterMyRoomType = 5;
                        }
                        GameSupport.ChRpEnterMyRoomPacket();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChRqEnterMyRoomPacket", 0))
                    {
                        string Nickname = iPacket.ReadString(false);
                        if (Nickname == ProfileService.ProfileConfig.Rider.Nickname)
                        {
                            GameType.EnterMyRoomType = 0;
                        }
                        else
                        {
                            GameType.EnterMyRoomType = 3;
                        }
                        GameSupport.ChRpEnterMyRoomPacket();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RmFirstRequestPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("RmSlotDataPacket"))
                        {
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.UserNO);
                            outPacket.WriteEndPoint(IPAddress.Parse(RouterListener.client.Address.ToString()), (ushort)RouterListener.client.Port);
                            outPacket.WriteInt();
                            outPacket.WriteShort();
                            outPacket.WriteString(ProfileService.ProfileConfig.Rider.Nickname);
                            GameSupport.GetRider(outPacket);
                            outPacket.WriteShort(0);
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.RP);
                            outPacket.WriteBytes(new byte[44]);
                            for (int i = 0; i < 7; i++)
                            {
                                outPacket.WriteBytes(new byte[133]);
                                //outPacket.WriteHexString("FF");
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RmNotiMyRoomInfoPacket", 0))
                    {
                        ProfileService.ProfileConfig.MyRoom.MyRoom = iPacket.ReadShort();
                        ProfileService.ProfileConfig.MyRoom.MyRoomBGM = iPacket.ReadByte();
                        ProfileService.ProfileConfig.MyRoom.UseRoomPwd = iPacket.ReadByte();
                        iPacket.ReadByte();
                        ProfileService.ProfileConfig.MyRoom.UseItemPwd = iPacket.ReadByte();
                        ProfileService.ProfileConfig.MyRoom.TalkLock = iPacket.ReadByte();
                        ProfileService.ProfileConfig.MyRoom.RoomPwd = iPacket.ReadString(false);
                        iPacket.ReadString(false);
                        ProfileService.ProfileConfig.MyRoom.ItemPwd = iPacket.ReadString(false);
                        ProfileService.ProfileConfig.MyRoom.MyRoomKart1 = iPacket.ReadShort();
                        ProfileService.ProfileConfig.MyRoom.MyRoomKart2 = iPacket.ReadShort();
                        ProfileService.Save();
                        GameSupport.RmNotiMyRoomInfoPacket();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChRqSecedeMyRoomPacket", 0))
                    {
                        //마이룸 나갈때
                        using (OutPacket outPacket = new OutPacket("ChRpSecedeMyRoomPacket"))
                        {
                            outPacket.WriteByte(1);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqStartScenario", 0))
                    {
                        GameType.ScenarioType = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrStartScenario"))
                        {
                            outPacket.WriteInt(GameType.ScenarioType);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCompleteScenarioSingle", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrCompleteScenarioSingle"))
                        {
                            outPacket.WriteInt(GameType.ScenarioType);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartSpec", 0))
                    {
                        StartGameData.StartTimeAttack_SpeedType = iPacket.ReadByte();
                        StartGameData.Kart_id = iPacket.ReadShort();
                        StartGameData.FlyingPet_id = iPacket.ReadShort();
                        GameType.StartType = 1;
                        SpeedType.SpeedTypeData();
                        StartGameData.Start_KartSpac();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChapterInfoPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrChapterInfoPacket"))
                        {
                            outPacket.WriteInt(KartExcData.scenario.Count);
                            foreach (int id in KartExcData.scenario)
                            {
                                outPacket.WriteInt(id | 0x1000000);
                                outPacket.WriteInt((int)(Math.Pow(2, 30) - 1));
                                outPacket.WriteInt(0);
                                outPacket.WriteByte(0);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChallengerInfoPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrChallengerInfoPacket"))
                        {
                            int stage = 40;
                            outPacket.WriteInt(stage);
                            for (int i = 0; i < stage; i++)
                            {
                                outPacket.WriteShort(55);
                            }
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(1);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqStartChallenger", 0))
                    {
                        int stage_id = iPacket.ReadInt();
                        int GameType = iPacket.ReadInt();
                        short Kart = iPacket.ReadShort();
                        byte Unk1 = iPacket.ReadByte();
                        byte Unk2 = iPacket.ReadByte();
                        byte Unk3 = iPacket.ReadByte();
                        using (OutPacket outPacket = new OutPacket("PrStartChallenger"))
                        {
                            outPacket.WriteInt(stage_id);
                            outPacket.WriteInt(GameType);
                            outPacket.WriteByte(0);
                            outPacket.WriteByte(1);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqchallengerKartSpec", 0))
                    {
                        StartGameData.StartTimeAttack_SpeedType = iPacket.ReadByte();
                        StartGameData.Kart_id = iPacket.ReadShort();
                        StartGameData.FlyingPet_id = iPacket.ReadShort();
                        GameType.StartType = 2;
                        SpeedType.SpeedTypeData();
                        StartGameData.Start_KartSpac();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCompleteChallenger", 0))
                    {
                        int stage = 40;
                        byte StageType = iPacket.ReadByte();
                        iPacket.ReadInt();
                        int EndType = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrCompleteChallenger"))
                        {
                            outPacket.WriteByte(StageType);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(EndType);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(stage);
                            for (int i = 0; i < stage; i++)
                            {
                                outPacket.WriteShort(55);
                            }
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqExChangePacket", 0))
                    {
                        GameSupport.OnDisconnect();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartLevelPointClear", 0))
                    {
                        short Kart = iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("PrKartLevelPointClear"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteShort(Kart);
                            outPacket.WriteShort(SN);
                            outPacket.WriteShort(5);
                            outPacket.WriteShort(35);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Koin);
                            this.Parent.Client.Send(outPacket);
                        }
                        KartExcData.AddLevelList(Kart, SN, 5, 35, 0, 0, 0, 0, 0);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqDisassembleXPartsItem", 0))
                    {
                        iPacket.ReadByte();
                        iPacket.ReadShort();
                        short Kart = iPacket.ReadShort();
                        iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        iPacket.ReadBytes(6);
                        byte[] data = iPacket.ReadBytes(28);
                        Console.WriteLine("DisassembleXPartsItem: " + Kart + " " + SN);
                        using (OutPacket outPacket = new OutPacket("PrDisassembleXPartsItem"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(3);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(Kart);
                            outPacket.WriteShort(1);
                            outPacket.WriteShort(0);
                            outPacket.WriteByte(1);//Grade
                            outPacket.WriteByte(0);//X-1 V1-2
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteBytes(data);
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Lucci);
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Koin);
                            this.Parent.Client.Send(outPacket);
                        }
                        KartExcData.AddPartsList(Kart, SN, 63, 0, 0, 0);
                        KartExcData.AddPartsList(Kart, SN, 64, 0, 0, 0);
                        KartExcData.AddPartsList(Kart, SN, 65, 0, 0, 0);
                        KartExcData.AddPartsList(Kart, SN, 66, 0, 0, 0);
                        KartExcData.AddPartsList(Kart, SN, 68, 0, 0, 0);
                        KartExcData.AddPartsList(Kart, SN, 69, 0, 0, 0);
                        KartExcData.AddPlantList(Kart, SN, 43, 0);
                        KartExcData.AddPlantList(Kart, SN, 44, 0);
                        KartExcData.AddPlantList(Kart, SN, 45, 0);
                        KartExcData.AddPlantList(Kart, SN, 46, 0);
                        ExcSpec.Use_PartsSpec(Kart, SN);
                        ExcSpec.Use_PlantSpec(Kart, SN);
                        //GameSupport.OnDisconnect();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartLevelUpProbText", 0))
                    {
                        short Kart = iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        iPacket.ReadShort();
                        iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("PrKartLevelUpProbText"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartLevelUp", 0))
                    {
                        short Kart = iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        short Kart2 = iPacket.ReadShort();
                        short SN2 = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("PrKartLevelUp"))
                        {
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[0]);
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[1]);
                            outPacket.WriteInt(1);
                            outPacket.WriteShort(Kart);
                            outPacket.WriteShort(SN);
                            outPacket.WriteShort(5);
                            outPacket.WriteShort(35);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(0);//Kart2
                            outPacket.WriteShort(0);//SN2
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Koin);
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Lucci);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        KartExcData.AddLevelList(Kart, SN, 5, 35, 0, 0, 0, 0, 0);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartLevelPointUpdate", 0))
                    {
                        short Kart = iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        short v1 = iPacket.ReadShort();
                        short v2 = iPacket.ReadShort();
                        short v3 = iPacket.ReadShort();
                        short v4 = iPacket.ReadShort();
                        short pointleft = 0;
                        short Effect = 0;
                        using (OutPacket outPacket = new OutPacket("PrKartLevelPointUpdate"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteShort(Kart);
                            outPacket.WriteShort(SN);
                            outPacket.WriteShort(5);
                            outPacket.WriteShort(pointleft);
                            outPacket.WriteShort(v1);
                            outPacket.WriteShort(v2);
                            outPacket.WriteShort(v3);
                            outPacket.WriteShort(v4);
                            outPacket.WriteShort(Effect);
                            this.Parent.Client.Send(outPacket);
                        }
                        ExcSpec.Use_KartLevelSpec(Kart, SN);
                        var existingLevelList = KartExcData.LevelList.FirstOrDefault(list => list[0] == Kart && list[1] == SN);
                        if (existingLevelList == null)
                        {
                            pointleft = (short)(35 - v1 - v2 - v3 - v4);
                            KartExcData.AddLevelList(Kart, SN, 5, pointleft, v1, v2, v3, v4, 0);
                        }
                        else
                        {
                            pointleft = (short)(existingLevelList[3] - v1 - v2 - v3 - v4);
                            short v1New = (short)(existingLevelList[4] + v1);
                            short v2New = (short)(existingLevelList[5] + v2);
                            short v3New = (short)(existingLevelList[6] + v3);
                            short v4New = (short)(existingLevelList[7] + v4);
                            short effect = existingLevelList[8];
                            KartExcData.AddLevelList(Kart, SN, 5, pointleft, v1New, v2New, v3New, v4New, effect);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqGetMaxGiftIdPacket", 0))
                    {
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartLevelSpecialSlotUpdate", 0))
                    {
                        short Kart = iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        short Effect = iPacket.ReadShort();

                        var existingLevelList = KartExcData.LevelList.FirstOrDefault(list => list[0] == Kart && list[1] == SN);

                        using (OutPacket outPacket = new OutPacket("PrKartLevelSpecialSlotUpdate"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteShort(Kart);
                            outPacket.WriteShort(SN);
                            if (existingLevelList != null)
                            {
                                outPacket.WriteShort(existingLevelList[2]);
                                outPacket.WriteShort(existingLevelList[3]);
                                outPacket.WriteShort(existingLevelList[4]);
                                outPacket.WriteShort(existingLevelList[5]);
                                outPacket.WriteShort(existingLevelList[6]);
                                outPacket.WriteShort(existingLevelList[7]);
                                outPacket.WriteShort(Effect);
                                KartExcData.AddLevelList(Kart, SN, existingLevelList[2], existingLevelList[3], existingLevelList[4], existingLevelList[5], existingLevelList[6], existingLevelList[7], Effect);
                            }
                            else
                            {
                                outPacket.WriteShort(5);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(10);
                                outPacket.WriteShort(10);
                                outPacket.WriteShort(10);
                                outPacket.WriteShort(5);
                                outPacket.WriteShort(Effect);
                                KartExcData.AddLevelList(Kart, SN, 5, 0, 10, 10, 10, 5, Effect);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUseSocketItem", 0))
                    {
                        short Item = iPacket.ReadShort();
                        short Item_Id = iPacket.ReadShort();
                        short Kart = iPacket.ReadShort();
                        iPacket.ReadShort();
                        short KartSN = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("PrUseSocketItem"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(Item);
                            outPacket.WriteShort(Item_Id);
                            outPacket.WriteShort(Kart);
                            outPacket.WriteShort(KartSN);
                            outPacket.WriteShort(KartSN);
                            outPacket.WriteShort(2);
                            outPacket.WriteHexString("00 00 00 00 FF FF 00 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        using (OutPacket outPacket = new OutPacket("PcSlaveNotice"))
                        {
                            outPacket.WriteString("使用粒子激活器R直接获得启变佳！");
                            this.Parent.Client.Send(outPacket);
                        }
                        KartExcData.AddTuneList(Kart, KartSN, 0, 0, 0, -1, 0, -1, 0);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUseTuneItem", 0))
                    {
                        short Item = iPacket.ReadShort();
                        short Item_Id = iPacket.ReadShort();
                        short Kart = iPacket.ReadShort();
                        iPacket.ReadShort();
                        short KartSN = iPacket.ReadShort();
                        Random random = new Random();
                        var existingList = KartExcData.TuneList.FirstOrDefault(list => list[0] == Kart && list[1] == KartSN);
                        if (existingList != null)
                        {
                            if (Item == 5)
                            {
                                using (OutPacket outPacket = new OutPacket("PrUseTuneItem"))
                                {
                                    outPacket.WriteInt(0);
                                    outPacket.WriteShort(Item);
                                    outPacket.WriteShort(Item_Id);
                                    outPacket.WriteShort(Kart);
                                    outPacket.WriteShort(KartSN);
                                    outPacket.WriteShort(0);
                                    outPacket.WriteShort(603);
                                    outPacket.WriteShort(703);
                                    outPacket.WriteShort(903);
                                    outPacket.WriteShort(existingList[5]);
                                    outPacket.WriteShort(existingList[6]);
                                    outPacket.WriteShort(existingList[7]);
                                    outPacket.WriteShort(existingList[8]);
                                    this.Parent.Client.Send(outPacket);
                                }
                                existingList[2] = 603;
                                existingList[3] = 703;
                                existingList[4] = 903;
                                KartExcData.SaveTuneList(KartExcData.TuneList);
                                ExcSpec.Use_ExcSpec(Kart, KartSN);
                            }
                            else
                            {
                                List<short> tuneList1 = new List<short> { existingList[2], existingList[3], existingList[4] };
                                List<short> tuneList2 = new List<short>();
                                using (OutPacket outPacket = new OutPacket("PrUseTuneItem"))
                                {
                                    outPacket.WriteInt(0);
                                    outPacket.WriteShort(Item);
                                    outPacket.WriteShort(Item_Id);
                                    outPacket.WriteShort(Kart);
                                    outPacket.WriteShort(KartSN);
                                    outPacket.WriteShort(0);
                                    if (existingList[2] != 0)
                                    {
                                        outPacket.WriteShort(existingList[2]);
                                    }
                                    else
                                    {
                                        while (tuneList1.Count < 4)
                                        {
                                            short number = short.Parse(random.Next(1, 10).ToString() + "03");
                                            if (!tuneList1.Contains(number))
                                            {
                                                outPacket.WriteShort(number);
                                                existingList[2] = number;
                                                tuneList1.Add(number);
                                                tuneList2.Add(number);
                                            }
                                        }
                                        tuneList1.RemoveAt(3);
                                    }
                                    if (existingList[3] != 0)
                                    {
                                        outPacket.WriteShort(existingList[3]);
                                    }
                                    else
                                    {
                                        while (tuneList1.Count < 4)
                                        {
                                            short number = short.Parse(random.Next(1, 10).ToString() + "03");
                                            if (!tuneList1.Contains(number))
                                            {
                                                if (!tuneList2.Contains(number))
                                                {
                                                    outPacket.WriteShort(number);
                                                    existingList[3] = number;
                                                    tuneList1.Add(number);
                                                    tuneList2.Add(number);
                                                }
                                            }
                                        }
                                        tuneList1.RemoveAt(3);
                                    }
                                    if (existingList[4] != 0)
                                    {
                                        outPacket.WriteShort(existingList[4]);
                                    }
                                    else
                                    {
                                        while (tuneList1.Count < 4)
                                        {
                                            short number = short.Parse(random.Next(1, 10).ToString() + "03");
                                            if (!tuneList1.Contains(number))
                                            {
                                                if (!tuneList2.Contains(number))
                                                {
                                                    outPacket.WriteShort(number);
                                                    existingList[4] = number;
                                                    tuneList1.Add(number);
                                                    tuneList2.Add(number);
                                                }
                                            }
                                        }
                                        tuneList1.RemoveAt(3);
                                    }
                                    outPacket.WriteShort(existingList[5]);
                                    outPacket.WriteShort(existingList[6]);
                                    outPacket.WriteShort(existingList[7]);
                                    outPacket.WriteShort(existingList[8]);
                                    this.Parent.Client.Send(outPacket);
                                }
                            }
                            KartExcData.SaveTuneList(KartExcData.TuneList);
                            ExcSpec.Use_ExcSpec(Kart, KartSN);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUseProtectSpannerItem", 0))
                    {
                        short Protect = iPacket.ReadShort();
                        short v2 = iPacket.ReadShort();
                        short Item_Id = iPacket.ReadShort();
                        short Kart = iPacket.ReadShort();
                        short KartSN = iPacket.ReadShort();
                        iPacket.ReadShort();
                        short slot = iPacket.ReadShort();
                        var existingList = KartExcData.TuneList.FirstOrDefault(list => list[0] == Kart && list[1] == KartSN);
                        if (existingList != null)
                        {
                            using (OutPacket outPacket = new OutPacket("PrUseProtectSpannerItem"))
                            {
                                outPacket.WriteInt(0);
                                outPacket.WriteShort(Protect);
                                outPacket.WriteShort(v2);
                                outPacket.WriteShort(Item_Id);
                                outPacket.WriteShort(Kart);
                                outPacket.WriteShort(KartSN);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(existingList[2]);
                                outPacket.WriteShort(existingList[3]);
                                outPacket.WriteShort(existingList[4]);
                                if (Protect == 49)
                                {
                                    outPacket.WriteShort(slot);
                                    outPacket.WriteShort(4);
                                    outPacket.WriteShort(existingList[7]);
                                    outPacket.WriteShort(existingList[8]);
                                    existingList[5] = slot;
                                    existingList[6] = 4;
                                }
                                else if (Protect == 53)
                                {
                                    outPacket.WriteShort(existingList[5]);
                                    outPacket.WriteShort(existingList[6]);
                                    outPacket.WriteShort(slot);
                                    outPacket.WriteShort(3);
                                    existingList[7] = slot;
                                    existingList[8] = 3;
                                }
                                else
                                {
                                    outPacket.WriteShort(existingList[5]);
                                    outPacket.WriteShort(existingList[6]);
                                    outPacket.WriteShort(existingList[7]);
                                    outPacket.WriteShort(existingList[8]);
                                }
                                this.Parent.Client.Send(outPacket);
                                KartExcData.SaveTuneList(KartExcData.TuneList);
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUseResetSocketItem", 0))
                    {
                        short Item = iPacket.ReadShort();
                        short Item_Id = iPacket.ReadShort();
                        short Kart = iPacket.ReadShort();
                        iPacket.ReadShort();
                        short KartSN = iPacket.ReadShort();
                        var existingList = KartExcData.TuneList.FirstOrDefault(list => list[0] == Kart && list[1] == KartSN);
                        if (existingList != null)
                        {
                            using (OutPacket outPacket = new OutPacket("PrUseResetSocketItem"))
                            {
                                outPacket.WriteInt(0);
                                outPacket.WriteShort(Item);
                                outPacket.WriteShort(Item_Id);
                                outPacket.WriteShort(Kart);
                                outPacket.WriteShort(KartSN);
                                outPacket.WriteShort(34);
                                outPacket.WriteShort(76);
                                outPacket.WriteShort(1);
                                outPacket.WriteShort(1);
                                outPacket.WriteShort(existingList[2]);
                                outPacket.WriteShort(existingList[3]);
                                outPacket.WriteShort(existingList[4]);
                                outPacket.WriteShort(existingList[5]);
                                outPacket.WriteShort(existingList[6]);
                                outPacket.WriteShort(existingList[7]);
                                outPacket.WriteShort(existingList[8]);
                                this.Parent.Client.Send(outPacket);
                            }
                            List<short> secure = new List<short>();
                            if (existingList[6] == 0)
                            {
                                existingList[5] = -1;
                            }
                            else
                            {
                                existingList[6] = (short)((int)existingList[6] - 1);
                            }
                            if (existingList[8] == 0)
                            {
                                existingList[7] = -1;
                            }
                            else
                            {
                                existingList[8] = (short)((int)existingList[8] - 1);
                            }
                            Console.WriteLine("TuneProtect: " + existingList[5] + ", " + existingList[7]);
                            Console.WriteLine("TuneProtectCount: " + existingList[6] + ", " + existingList[8]);
                            for (int i = 2; i <= 4; i++)
                            {
                                if (existingList[5] != -1 && (int)(existingList[5]) + 2 == i)
                                {
                                }
                                else if (existingList[7] != -1 && (int)(existingList[7]) + 2 == i)
                                {
                                }
                                else
                                {
                                    existingList[i] = 0;
                                }
                            }
                            Console.WriteLine("TuneList: " + existingList[2] + ", " + existingList[3] + ", " + existingList[4]);
                            KartExcData.SaveTuneList(KartExcData.TuneList);
                            ExcSpec.Use_ExcSpec(Kart, KartSN);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqEquipTuningExPacket", 0))
                    {
                        short Item = iPacket.ReadShort();
                        short Item_Id = iPacket.ReadShort();
                        short Kart = iPacket.ReadShort();
                        short Kart_Id = iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        KartExcData.AddPlantList(Kart_Id, SN, Item, Item_Id);
                        ExcSpec.Use_PlantSpec(Kart_Id, SN);
                        using (OutPacket outPacket = new OutPacket("PrEquipTuningPacket"))
                        {
                            outPacket.WriteByte(1);
                            outPacket.WriteShort(SN);
                            outPacket.WriteShort(SN);
                            outPacket.WriteShort(Kart_Id);
                            outPacket.WriteShort(Item);
                            outPacket.WriteShort(Item_Id);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUnequipXPartsItem", 0))
                    {
                        short Kart = iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        short item = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("PrUnequipXPartsItem"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(Kart);
                            outPacket.WriteShort(SN);
                            outPacket.WriteShort(item);
                            this.Parent.Client.Send(outPacket);
                        }
                        KartExcData.AddPartsList(Kart, SN, item, 0, 0, 0);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqEquipXPartsItem", 0))
                    {
                        short Kart = iPacket.ReadShort();
                        short KartSN = iPacket.ReadShort();
                        short Item_Cat_Id = iPacket.ReadShort();
                        short Item_Id = iPacket.ReadShort();
                        short Quantity = iPacket.ReadShort();
                        short Unk1 = iPacket.ReadShort();
                        byte Grade = iPacket.ReadByte();
                        byte Unk2 = iPacket.ReadByte();
                        short PartsValue = iPacket.ReadShort();
                        short Unk3 = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("PrEquipXPartsItem"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(Kart);
                            outPacket.WriteShort(KartSN);
                            outPacket.WriteShort(Item_Cat_Id);
                            outPacket.WriteShort(Item_Id);
                            outPacket.WriteShort(Quantity);
                            outPacket.WriteShort(Unk1);
                            outPacket.WriteByte(Grade);
                            outPacket.WriteByte(Unk2);
                            outPacket.WriteShort(PartsValue);
                            outPacket.WriteShort(Unk3);
                            this.Parent.Client.Send(outPacket);
                        }
                        KartExcData.AddPartsList(Kart, KartSN, Item_Cat_Id, Item_Id, Grade, PartsValue);
                        Console.WriteLine("ClientSession : Kart: {0}, KartSN: {1}, Item: {2}:{3}, Quantity: {4}, Grade: {5}, PartsValue: {6}", Kart, KartSN, Item_Cat_Id, Item_Id, Quantity, Grade, PartsValue);
                        ExcSpec.Use_PartsSpec(Kart, KartSN);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetTrainingMission", 0))
                    {
                        int type = iPacket.ReadInt();
                        uint track = iPacket.ReadUInt();
                        byte Level = FavoriteItem.GetTrackLevel(track);
                        //PrGetTrainingMission 00 08 B7 51 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
                        using (OutPacket outPacket = new OutPacket("PrGetTrainingMission"))
                        {
                            outPacket.WriteInt(type);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(0); //完成赛道
                            outPacket.WriteByte(0); //使用加速器道具（完成时累积）
                            outPacket.WriteByte(0); //撞击次数%s次以内
                            outPacket.WriteByte(0); //达成赛道纪录
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetDuelMissionBulk", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetDuelMissionBulk"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[0]);
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[1]);
                            outPacket.WriteInt(FavoriteItem.MissionList.Count);
                            foreach (string Track in FavoriteItem.MissionList)
                            {
                                byte Level = FavoriteItem.GetTrackLevel(Adler32Helper.GenerateAdler32_UNICODE(Track, 0));
                                outPacket.WriteByte(Level);
                                outPacket.WriteInt(0);
                            }
                            //outPacket.WriteHexString("0F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqAdjustDuelMissionDifficulty", 0))
                    {
                        Console.WriteLine("PqAdjustDuelMissionDifficulty: {0}", iPacket);
                        int type = iPacket.ReadInt();
                        int unk = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrAdjustDuelMissionDifficulty"))
                        {
                            outPacket.WriteInt(type);
                            outPacket.WriteInt(unk);
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqBlueMarble", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrBlueMarble"))
                        {
                            outPacket.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqGetTrackRankPacket", 0))
                    {
                        uint track = iPacket.ReadUInt();
                        byte SpeedType = iPacket.ReadByte();
                        byte GameType = iPacket.ReadByte();
                        using (OutPacket outPacket = new OutPacket("LoRpGetTrackRankPacket"))
                        {
                            outPacket.WriteUInt(track);
                            outPacket.WriteByte(SpeedType);
                            outPacket.WriteByte(GameType);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqFavoriteTrackUpdate", 0))
                    {
                        iPacket.ReadByte();
                        int tracks = iPacket.ReadInt(); //赛道数量
                        for (int i = 0; i < tracks; i++)
                        {
                            short theme = iPacket.ReadShort(); //主题代码
                            int track = iPacket.ReadInt(); //赛道代码
                            byte Add_Del = iPacket.ReadByte(); //1添加，2删除
                            if (Add_Del == 1)
                            {
                                FavoriteItem.Favorite_Track_Add(theme, track);
                            }
                            else if (Add_Del == 2)
                            {
                                FavoriteItem.Favorite_Track_Del(theme, track);
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqDecLucciPacket", 0))
                    {
                        iPacket.ReadByte();
                        uint Lucci = iPacket.ReadUInt();
                        ProfileService.ProfileConfig.Rider.Lucci -= Lucci;
                        ProfileService.Save();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqStartTimeAttack", 0))
                    {
                        StartGameData.StartTimeAttack_Unk1 = iPacket.ReadInt();
                        StartGameData.StartTimeAttack_Unk2 = iPacket.ReadInt();
                        StartGameData.StartTimeAttack_Track = iPacket.ReadUInt();
                        StartGameData.StartTimeAttack_SpeedType = iPacket.ReadByte();
                        StartGameData.StartTimeAttack_GameType = iPacket.ReadByte();
                        StartGameData.Kart_id = iPacket.ReadShort();
                        StartGameData.FlyingPet_id = iPacket.ReadShort();
                        StartGameData.StartTimeAttack_StartType = iPacket.ReadByte();
                        StartGameData.StartTimeAttack_Unk3 = iPacket.ReadInt();
                        StartGameData.StartTimeAttack_Unk4 = iPacket.ReadInt();
                        StartGameData.StartTimeAttack_Unk5 = iPacket.ReadByte();
                        StartGameData.StartTimeAttack_RankingTimaAttackType = iPacket.ReadByte();
                        StartGameData.StartTimeAttack_TimaAttackMpdeType = iPacket.ReadByte();
                        StartGameData.StartTimeAttack_TimaAttackMpde = iPacket.ReadInt();
                        StartGameData.StartTimeAttack_RandomTrackGameType = iPacket.ReadByte();
                        if (StartGameData.StartTimeAttack_TimaAttackMpdeType == 1)
                        {
                            ProfileService.ProfileConfig.Rider.Lucci -= 1000;
                            ProfileService.Save();
                        }
                        Console.WriteLine("StartTimeAttack: {0} / {1} / {2} / {3} / {4} / {5} / {6} / {7}", StartGameData.StartTimeAttack_SpeedType, StartGameData.StartTimeAttack_GameType, StartGameData.Kart_id, StartGameData.FlyingPet_id, RandomTrack.GetTrackName(StartGameData.StartTimeAttack_Track), StartGameData.StartTimeAttack_StartType, StartGameData.StartTimeAttack_RankingTimaAttackType, StartGameData.StartTimeAttack_TimaAttackMpdeType);
                        GameType.StartType = 3;
                        RandomTrack.SetGameType();
                        StartGameData.Start_KartSpac();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqFinishTimeAttack", 0))
                    {
                        int type = iPacket.ReadInt();
                        iPacket.ReadInt();
                        GameType.RewardType = iPacket.ReadByte();
                        iPacket.ReadInt();
                        iPacket.ReadInt();
                        iPacket.ReadInt();
                        iPacket.ReadInt();
                        Time = iPacket.ReadUInt();
                        GameType.min = Time / 60000;
                        uint sec = Time - GameType.min * 60000;
                        GameType.sec = sec / 1000;
                        GameType.mil = Time % 1000;
                        if (GameType.RewardType == 0)
                        {
                            GameType.TimeAttack_RP = 10;
                            GameType.TimeAttack_Lucci = 20;
                        }
                        else if (GameType.RewardType == 1)
                        {
                            GameType.TimeAttack_RP = 20;
                            GameType.TimeAttack_Lucci = 50;
                        }
                        ProfileService.ProfileConfig.Rider.RP += GameType.TimeAttack_RP;
                        ProfileService.ProfileConfig.Rider.Lucci += GameType.TimeAttack_Lucci;
                        Console.WriteLine("FinishTimeAttack: {0} / {1} / {2} / {3} / {4}:{5}:{6}", GameType.RewardType, GameType.TimeAttack_RP, GameType.TimeAttack_Lucci, RandomTrack.GetTrackName(StartGameData.StartTimeAttack_Track), GameType.min, GameType.sec, GameType.mil);
                        using (OutPacket outPacket = new OutPacket("PrFinishTimeAttack"))
                        {
                            outPacket.WriteInt(type);
                            if (StartGameData.StartTimeAttack_RankingTimaAttackType == 0 && GameType.RewardType == 1)
                            {
                                byte Level = FavoriteItem.TrainingMission(StartGameData.StartTimeAttack_Track);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteByte(0); //完成赛道
                                outPacket.WriteByte(0); //使用加速器道具（完成时累积）
                                outPacket.WriteByte(0); //撞击次数%s次以内
                                outPacket.WriteByte(0); //达成赛道纪录
                                outPacket.WriteInt(0);
                                outPacket.WriteByte(Level);
                                outPacket.WriteInt(0);
                            }
                            else
                            {
                                outPacket.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00");
                            }
                            outPacket.WriteUInt(GameType.TimeAttack_RP);//RP
                            outPacket.WriteUInt(GameType.TimeAttack_Lucci);//LUCCI
                            this.Parent.Client.Send(outPacket);
                        }
                        ProfileService.Save();
                        // Save_RecordTimeAttack
                        {
                            string trackName = StartGameData.StartTimeAttack_Track.ToString();
                            if (KartExcData.track.ContainsKey(StartGameData.StartTimeAttack_Track))
                            {
                                trackName = KartExcData.track[StartGameData.StartTimeAttack_Track];
                            }
                            using (StreamWriter streamWriter = new StreamWriter("TimeAttack.log", true))
                            {
                                streamWriter.WriteLine("[{0}] SpeedType:{1}, Infinite:{2}, GameType:{3}, Kart:{4}, FlyingPet:{5}, ResultType:{6}, RP:{7}, Lucci:{8}, Track:{9}, Record:{10}:{11}:{12}", DateTime.Now, SpeedType.speedNames.FirstOrDefault(pair => pair.Value == ProfileService.ProfileConfig.GameOption.SpeedType).Key, StartGameData.StartTimeAttack_SpeedType == 4 ? 1 : 0, StartGameData.StartTimeAttack_GameType, StartGameData.Kart_id, StartGameData.FlyingPet_id, GameType.RewardType, GameType.TimeAttack_RP, GameType.TimeAttack_Lucci, trackName, GameType.min, GameType.sec, GameType.mil);
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRewardTimeAttack", 0))
                    {
                        byte RewardType = iPacket.ReadByte();
                        int RP = iPacket.ReadInt();
                        int Lucci = iPacket.ReadInt();
                        int TimeAttack_StartTicks = iPacket.ReadInt();
                        uint Track = iPacket.ReadUInt();
                        Console.WriteLine("RewardTimeAttack : ResultType: {0}, RP: {1}, Lucci: {2}, Track: {3}", RewardType, RP, Lucci, RandomTrack.GetTrackName(Track));
                        if (RewardType == 0)
                        {
                            ProfileService.ProfileConfig.Rider.RP += 10;
                            ProfileService.ProfileConfig.Rider.Lucci += 20;
                        }
                        else if (RewardType == 1)
                        {
                            ProfileService.ProfileConfig.Rider.RP += 20;
                            ProfileService.ProfileConfig.Rider.Lucci += 50;
                        }
                        ProfileService.Save();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqUseItemPacket", 0))
                    {
                        short ItemType = iPacket.ReadShort();
                        short Type = iPacket.ReadShort();
                        ProfileService.ProfileConfig.Rider.SlotChanger = iPacket.ReadUShort();
                        if (Type == 1)
                        {
                            ProfileService.Save();
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqQuestUX2ndPacket", 0))
                    {
                        GameSupport.PrQuestUX2ndPacket();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGameOutRunUX2ndClearPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGameOutRunUX2ndClearPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetTrainingMissionReward", 0))
                    {
                        Console.WriteLine("PqGetTrainingMissionReward: {0}", iPacket);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRequestKartInfoPacket", 0))
                    {
                        int count = 0;
                        using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                        {
                            outPacket.WriteByte(1);
                            outPacket.WriteInt(count);
                            for (int i = 0; i < count; i++)
                            {
                                outPacket.WriteShort(3);
                                outPacket.WriteShort(1494);
                                outPacket.WriteShort(1);
                                outPacket.WriteShort(1);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(-1);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                            }
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqBingoGachaSelectSetPacket", 0))
                    {
                        Bingo.BingoItem = iPacket.ReadByte();//选择的Bingo道具
                        using (OutPacket outPacket = new OutPacket("SpRpBingoGachaSelectSetPacket"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqBingoGachaInfoPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpBingoGachaInfoPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(Bingo.BingoItem);//选择的Bingo道具
                            outPacket.WriteBytes(new byte[3]);
                            outPacket.WriteShort(0);
                            outPacket.WriteInt(Bingo.BingoNumsList.Count);//Bingo格子数量
                            foreach (var num in Bingo.BingoNumsList)
                            {
                                outPacket.WriteByte(num);//Bingo格子数字
                                outPacket.WriteByte(Bingo.BingoNums[num]);//数字是否获得
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqBingoGachaPacket", 0))
                    {
                        int BingoType = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("SpRpBingoGachaPacket"))
                        {
                            outPacket.WriteInt(BingoType);
                            outPacket.WriteInt(Bingo.BingoNumsList.Count);//Bingo格子数量
                            foreach (var num in Bingo.BingoNumsList)
                            {
                                outPacket.WriteByte(num);//Bingo格子数字
                                outPacket.WriteByte(Bingo.BingoNums[num]);//数字是否获得
                            }
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(Bingo.BingoItemsList.Count);//Bingo道具数量
                            foreach (var item in Bingo.BingoItemsList)
                            {
                                outPacket.WriteInt(item);//Bingo道具
                                outPacket.WriteByte(Bingo.BingoItems[item]);//道具是否获得
                            }
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(Bingo.BingoCount);
                            outPacket.WriteByte(0);
                            outPacket.WriteByte(Bingo.BingoNum);//上次获取的数字
                            outPacket.WriteByte(0);
                            outPacket.WriteByte(0);
                            outPacket.WriteByte(0);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqLotteryMileagePrizePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrLotteryMileagePrizePacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        Bingo.BingoItem = 0;
                        Bingo.BingoNum = 0;
                        Bingo.BingoCount = 0;
                        Bingo.BingoNums = new Dictionary<byte, byte>();
                        Bingo.BingoNumsList = new List<byte>();
                        Bingo.BingoItems = new Dictionary<int, byte>();
                        Bingo.BingoItemsList = new List<int>();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCheckMyClubStatePacket", 0))
                    {
                        GameSupport.PrCheckMyClubStatePacket();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCheckMyLeaveDatePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrCheckMyLeaveDatePacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetUserWaitingJoinClubPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetUserWaitingJoinClubPacket"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCheckCreateClubConditionPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrCheckCreateClubConditionPacket"))
                        {
                            outPacket.WriteInt(3);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqClubChannelSwitch", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrInitClubPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqInitClubInfoPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrInitClubInfoPacket"))
                        {
                            outPacket.WriteHexString("50 F7 EA 07 00 00 00 00 00 00 FA DC 98 00 00 00 00 00 02 00 00 00 50 F7 EA 07 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 0F 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSearchClubListPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrSearchClubListPacket"))
                        {
                            outPacket.WriteHexString("0C 00 00 00 7F 3D 00 00 04 00 00 00 1C 75 C7 8F 1D 52 4B 60 05 F4 01 00 00 FF FF FF FF 00 00 00 00 40 4B 4C 00 07 00 00 00 00 00 00 00 07 00 00 00 0B 4E E8 96 56 00 52 00 61 00 69 00 6E 00 64 00 00 00 E7 AA 50 46 0F 00 00 00 A1 6C C0 4E 48 4E 79 72 7F 95 2C 00 31 5C 2F 66 65 55 FD 90 1A 4F B9 70 2E 00 2E 00 2E 00 00 05 C4 D6 6E 01 5D 37 00 00 08 00 00 00 D0 67 9D 5B 1C 64 EB 77 C5 60 E5 5D 5C 4F A4 5B 05 2C 01 00 00 FF FF FF FF 00 00 00 00 C0 C6 2D 00 1C 01 00 00 00 00 00 00 06 00 00 00 13 4E 1A 4E 37 52 5A 80 50 4E E8 90 1B 00 00 00 A3 AA B7 2A 4C 00 00 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 3D 00 3D 00 3D 00 3D 00 D1 8D D1 8D 94 54 01 4E 66 8F 20 00 D8 98 FB 79 3D 00 3D 00 3D 00 3D 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 A4 7F 3A 00 37 00 39 00 30 00 30 00 33 00 33 00 36 00 31 00 37 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 7D 59 0B 67 CB 53 00 4E 77 8D D8 98 00 04 8F 1C 4E 00 04 00 00 00 07 00 00 00 59 00 78 00 36 4E 54 00 65 00 61 00 6D 00 05 2C 01 00 00 FF FF FF FF 00 00 00 00 C0 C6 2D 00 92 01 00 00 00 00 00 00 05 00 00 00 59 00 78 00 36 4E 1F 96 7F 95 24 00 00 00 33 AA 99 2A 46 00 00 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 28 4E E0 65 50 96 66 8F 1F 96 28 4E 20 00 66 8F 1F 96 03 80 38 68 36 65 BA 4E 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 A5 63 85 5F 2F 00 03 80 38 68 71 00 A4 7F 3B 00 34 00 39 00 39 00 33 00 33 00 33 00 33 00 31 00 31 00 20 00 00 04 DA 93 27 00 DF 21 00 00 04 00 00 00 28 4E 18 7F 2F 54 28 4E 05 F4 01 00 00 FF FF FF FF 00 00 00 00 40 4B 4C 00 70 01 00 00 00 00 00 00 04 00 00 00 CC 51 75 70 36 4E 63 00 14 00 00 00 47 AA EF 2E 2F 00 00 00 2E 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 3C 00 20 00 20 00 20 00 22 6B 20 00 11 7B 20 00 C5 60 20 00 82 59 20 00 E7 65 20 00 2D 00 20 00 27 84 20 00 8F 75 20 00 13 9B 20 00 F2 5D 20 00 91 65 20 00 20 00 3E 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 00 05 50 6B 20 00 64 3E 00 00 04 00 00 00 52 00 69 00 63 00 68 00 05 2C 01 00 00 FF FF FF FF 00 00 00 00 C0 C6 2D 00 8B 01 00 00 00 00 00 00 07 00 00 00 52 00 69 00 63 00 68 00 CE 8F A8 60 65 67 60 00 00 00 F4 AA CF 4E 5E 00 00 00 20 00 20 00 20 00 20 00 20 00 52 00 69 00 63 00 68 00 94 4E A7 7E 66 8F 1F 96 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 22 6B CE 8F A8 60 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 3B 4E A9 73 E0 65 50 96 20 00 20 00 20 00 20 00 20 00 20 00 66 8F 1F 96 03 80 38 68 A5 63 85 5F A4 7F 3A 00 35 00 32 00 37 00 31 00 32 00 36 00 39 00 38 00 37 00 00 04 3C 45 14 00 7A 49 00 00 05 00 00 00 08 54 A6 7E 36 65 CF 85 B6 5B 05 2C 01 00 00 FF FF FF FF 00 00 00 00 C0 C6 2D 00 75 00 00 00 00 00 00 00 06 00 00 00 48 00 79 00 2C 7B 00 4E 73 59 4C 80 23 00 00 00 5D AB AB 36 05 00 00 00 5E 97 F7 8B FF 52 65 51 2E 00 00 04 D4 1E 11 00 8F 55 00 00 06 00 00 00 28 4E 5D 4E 29 59 FD 63 08 67 28 4E 05 2C 01 00 00 FF FF FF FF 00 00 00 00 C0 C6 2D 00 8F 01 00 00 00 00 00 00 06 00 00 00 3D 63 1F 66 B8 82 50 6C E5 82 36 4E 30 00 00 00 A4 AB 44 52 07 00 00 00 66 8F 1F 96 82 66 F6 65 0D 4E 36 65 BA 4E 00 04 90 6D 10 00 AD 0B 00 00 06 00 00 00 28 4E A2 7E D7 65 66 8F 1F 96 28 4E 05 2C 01 00 00 FF FF FF FF 00 00 00 00 C0 C6 2D 00 75 00 00 00 00 00 00 00 06 00 00 00 A2 7E D7 65 85 8D A7 7E 66 5B 38 97 18 00 00 00 33 AA 4C 4A 25 00 00 00 65 51 1F 96 81 89 42 6C 3A 00 66 8F 93 5E E5 62 09 67 A2 7E D7 65 68 51 FB 7C 66 8F 86 8F 2C 00 3A 7F 00 4E 0D 4E EF 53 2C 00 26 7B 08 54 81 89 42 6C 84 76 A0 52 A4 7F 31 00 32 00 39 00 30 00 34 00 33 00 33 00 37 00 32 00 00 04 EC 39 10 00 75 01 00 00 06 00 00 00 28 4E 0D 54 1F 66 66 8F 1F 96 28 4E 05 F4 01 00 00 FF FF FF FF 00 00 00 00 40 4B 4C 00 95 01 00 00 00 00 00 00 05 00 00 00 0D 54 1F 66 01 80 05 5E E5 54 33 01 00 00 33 AA 6B 2B 54 00 00 00 0D 54 1F 66 F1 4F 50 4E E8 90 2F 66 CC 53 94 4E A7 7E CC 53 EE 4F F1 4F 50 4E E8 90 20 00 DE 7A 1F 90 3B 4E 53 62 53 00 32 00 20 00 53 90 77 51 3A 4E 4E 4F 1F 90 CC 53 81 79 8C 54 26 5E 01 95 20 00 16 59 A4 4E A4 7F A4 7F F7 53 3A 4E 33 00 31 00 36 00 36 00 34 00 31 00 32 00 30 00 30 00 20 00 22 6B CE 8F FF 7E 72 82 A9 73 B6 5B A0 52 65 51 20 00 81 89 42 6C 39 65 0D 54 20 00 D1 53 B0 73 28 75 85 8F A9 52 D1 8D 66 8F 20 00 05 6E 06 74 20 00 0C 5E 1B 67 27 59 B6 5B 92 4E F8 76 D1 76 63 77 3E 4E A5 62 00 05 06 82 09 00 31 15 00 00 06 00 00 00 28 4E 64 8D DA 8B 4B 4E C3 5F 28 4E 05 2C 01 00 00 FF FF FF FF 00 00 00 00 C0 C6 2D 00 71 01 00 00 00 00 00 00 06 00 00 00 64 8D DA 8B 28 4E 6B 84 A6 82 28 4E 1C 00 00 00 36 AA 63 24 4C 00 00 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 00 4E 47 72 64 8D DA 8B 4B 4E C3 5F 20 00 B3 7E B0 65 A4 7F 31 00 30 00 35 00 36 00 39 00 31 00 39 00 38 00 36 00 31 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 2A 67 39 65 0D 54 00 4E 8B 5F 0D 4E 88 4E 1A 90 C7 8F 20 00 20 00 20 00 3C 68 0F 5F 20 00 20 00 20 00 64 8D DA 8B 28 4E 78 00 78 00 28 4E 00 04 C5 1A 05 00 F7 32 00 00 08 00 00 00 50 00 6C 00 61 00 79 00 43 00 6C 00 75 00 62 00 05 2C 01 00 00 FF FF FF FF 00 00 00 00 C0 C6 2D 00 75 00 00 00 00 00 00 00 06 00 00 00 50 00 6C 00 61 00 79 00 1C 4E CE 98 A0 00 00 00 81 AA 71 25 49 00 00 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 50 00 6C 00 61 00 79 00 20 00 2D 00 20 00 43 00 6C 00 75 00 62 00 20 00 2D 00 20 00 46 00 6F 00 72 00 65 00 76 00 65 00 72 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00 00 04 9A E7 03 00 6B 20 00 00 03 00 00 00 28 4E 6A 8C 28 4E 05 F4 01 00 00 FF FF FF FF 00 00 00 00 40 4B 4C 00 03 00 00 00 00 00 00 00 02 00 00 00 E4 4E 77 83 3F 00 00 00 43 AA E0 01 25 00 00 00 53 90 77 51 66 8F 1F 96 20 00 20 00 20 00 00 97 81 89 39 65 0D 54 20 00 20 00 20 00 A5 63 85 5F A4 7F 20 00 31 00 20 00 31 00 20 00 35 00 20 00 39 00 20 00 30 00 20 00 35 00 20 00 33 00 20 00 35 00 20 00 32 00 20 00 36 00 00 05 CF 9C 03 00 01");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetClubListCountPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetClubListCountPacket"))
                        {
                            outPacket.WriteHexString("7F F7 00 00 01 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetClubWaitingCrewCountPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetClubWaitingCrewCountPacket"))
                        {
                            outPacket.WriteHexString("32 00 00 00 32 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqLotteryPacket", 0))
                    {
                        short Lottery_Item = iPacket.ReadShort();
                        byte Unk = iPacket.ReadByte();
                        int Type = iPacket.ReadInt();
                        if (Lottery_Item == 1175 || Lottery_Item == 1176 || Lottery_Item == 1177)
                        {
                            Bingo.SpRpLotteryPacket();
                        }
                        else
                        {
                            GameSupport.SpRpLotteryPacket();
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqEventBuyCount", 0))
                    {
                        EventBuyCount.BuyCount = iPacket.ReadInt();
                        EventBuyCount.ShopItem = new int[EventBuyCount.BuyCount];
                        for (int i = 0; i < EventBuyCount.BuyCount; i++)
                        {
                            EventBuyCount.ShopItem[i] = iPacket.ReadInt();
                        }
                        EventBuyCount.PrEventBuyCount();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetRiderTaskContext", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetRiderTaskContext"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqVersusModeRankOnePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrVersusModeRankOnePacket"))
                        {
                            outPacket.WriteHexString("00 FF FF FF FF FF FF FF FF");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRiderSchoolDataPacket", 0))
                    {
                        //RiderSchool.PrRiderSchoolData();
                        using (OutPacket outPacket = new OutPacket("PrRiderSchoolDataPacket"))
                        {
                            outPacket.WriteByte(6);//라이센스 등급
                            outPacket.WriteByte(42);//마지막 클리어
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[0]);
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[1]);
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(0);
                            RouterListener.MySession.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRiderSchoolProPacket", 0))
                    {
                        //RiderSchool.PrRiderSchoolPro();
                        int stepcount = 6;
                        int remainder = RouterListener.DataTime()[2] % stepcount;
                        if (remainder == 0) remainder = stepcount;
                        byte step;
                        switch (remainder)
                        {
                            case 1:
                                step = 31;
                                break;
                            case 2:
                                step = 33;
                                break;
                            case 3:
                                step = 35;
                                break;
                            case 4:
                                step = 37;
                                break;
                            case 5:
                                step = 39;
                                break;
                            case 6:
                                step = 41;
                                break;
                            default:
                                step = 31;
                                break;
                        }
                        using (OutPacket oPacket = new OutPacket("PrRiderSchoolProPacket"))
                        {
                            oPacket.WriteByte(1);//엠블럼 체크
                            oPacket.WriteByte(step);
                            oPacket.WriteByte(6);
                            oPacket.WriteByte((byte)((int)step + 1));
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(0);
                            RouterListener.MySession.Client.Send(oPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqStartRiderSchool", 0))
                    {
                        RiderSchool.PrStartRiderSchool();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRiderSchoolExpiredCheck", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrRiderSchoolExpiredCheck"))
                        {
                            outPacket.WriteBytes(new byte[10]);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRankerInfoPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrRankerInfoPacket"))
                        {
                            outPacket.WriteByte(0);
                            outPacket.WriteByte(ProfileService.ProfileConfig.Rider.Ranker);
                            outPacket.WriteHexString("00 00 C8 42 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRequestExtradata", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrRequestExtradata"))
                        {
                            outPacket.WriteShort(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChRequestChStaticRequestPacket", 0))
                    {
                        GameSupport.ChRequestChStaticReplyPacket();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqDynamicCommand", 0))
                    {
                        GameSupport.PrDynamicCommand();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqPubCommandPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrPubCommandPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteHexString("60 B2 6B 2B");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqWebEventCompleteCheckPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrWebEventCompleteCheckPacket"))
                        {
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqKoinBalance", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpKoinBalance"))
                        {
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Koin);
                            outPacket.WriteUInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqFavoriteTrackMapGet", 0))
                    {
                        FavoriteItem.Favorite_Track();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetFavoriteChannel", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetFavoriteChannel"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartPassInitPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrKartPassInitPacket"))
                        {
                            //outPacket.WriteInt(3);
                            //outPacket.WriteInt(0);
                            outPacket.WriteHexString("0000000001000000000000000000000000");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqGetCashInventoryPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpGetCashInventoryPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqRemainCashPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpRemainCashPacket"))
                        {
                            outPacket.WriteUInt(0);
                            outPacket.WriteUInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqRemainTcCashPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpRemainTcCashPacket"))
                        {
                            outPacket.WriteUInt(99);
                            outPacket.WriteUInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpReqNormalShopBuyItemPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("SpReqItemPresetShopBuyItemPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRepBuyItemPacket"))
                        {
                            outPacket.WriteHexString("01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetCurrentRid", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetCurrentRid"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetMyCouponList", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetMyCouponList"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqDisassembleFeeInfo", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrDisassembleFeeInfo"))
                        {
                            outPacket.WriteHexString("0000000008000000030000E803030100F401020000E803020100F401010000E803010100F401000000E803000100F401");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRequestExchangeInitPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrRequestExchangeInitPacket"))
                        {
                            outPacket.WriteHexString("01 03 00 00 00 F4 01 00 00 01 00 00 00 02 00 00 00 03 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRequestPeriodExchangeInitPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrRequestPeriodExchangeInitPacket"))
                        {
                            outPacket.WriteBytes(new byte[22]);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqEnterRewardBoxStage", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpEnterRewardBoxStage"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(1);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqReceiveRewardItemPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpReceiveRewardItemPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqExitRewardBoxStage", 0))
                    {
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqGetGiftListIncomingPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpGetGiftListIncomingPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqGetGiftListReceivedPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpGetGiftListReceivedPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetCompetitiveRankInfo", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetCompetitiveRankInfo"))
                        {
                            outPacket.WriteHexString("0100000000FF000000000000000000000000");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetCompetitiveSlotInfo", 0))
                    {
                        var manager = new CompetitiveDataManager();
                        var competitiveData = manager.LoadAllData();
                        using (OutPacket outPacket = new OutPacket("PrGetCompetitiveSlotInfo"))
                        {
                            outPacket.WriteInt(competitiveData.Count);
                            foreach (var competitive in competitiveData)
                            {
                                outPacket.WriteUInt(competitive.Track);
                                outPacket.WriteUInt(competitive.Track);
                                outPacket.WriteShort(competitive.Kart);
                                outPacket.WriteUInt(competitive.Time);
                                outPacket.WriteHexString("FF FF FF FF");
                                outPacket.WriteShort(competitive.Boooster);
                                outPacket.WriteUInt(competitive.BooosterPoint);
                                outPacket.WriteShort(competitive.Crash);
                                outPacket.WriteUInt(competitive.CrashPoint);
                                outPacket.WriteUInt(competitive.Point);
                                outPacket.WriteInt(0);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetCompetitiveCount", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetCompetitiveCount"))
                        {
                            //outPacket.WriteHexString("B3 02 52 1B 00 00 B4 02 54 1B 00 00 B9 02 82 1B 00 00");
                            foreach (var track in FavoriteItem.Competitive)
                            {
                                outPacket.WriteUInt(Adler32Helper.GenerateAdler32_UNICODE(track, 0));
                                outPacket.WriteShort(0);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSearchCompetitiveRankPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrSearchCompetitiveRankPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetCompetitivePreRankPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetCompetitivePreRankPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RmRequestEmblemsPacket", 0))
                    {
                        Emblem.RmOwnerEmblemPacket();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RmRqUpdateMainEmblemPacket", 0))
                    {
                        ProfileService.ProfileConfig.Rider.Emblem1 = iPacket.ReadShort();
                        ProfileService.ProfileConfig.Rider.Emblem2 = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("RmRpUpdateMainEmblemPacket"))
                        {
                            outPacket.WriteByte(1);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        ProfileService.Save();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSyncDictionaryInfoPacket", 0))
                    {
                        int Dictionary = iPacket.ReadInt();
                        int Count = KartExcData.Dictionary.Count;
                        using (OutPacket outPacket = new OutPacket("PrSyncDictionaryInfoPacket"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(Count);
                            foreach (var item in KartExcData.Dictionary)
                            {
                                outPacket.WriteShort(item[0]);
                                outPacket.WriteShort(item[1]);
                            }
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetDictionaryRewardInfoPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetDictionaryRewardInfoPacket"))
                        {
                            outPacket.WriteShort(56);
                            outPacket.WriteInt(1);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqNewCareerListPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrNewCareerListPacket"))
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                outPacket.WriteInt(0);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqDeleteItemPacket", 0))
                    {
                        iPacket.ReadInt();
                        iPacket.ReadInt();
                        iPacket.ReadInt();
                        iPacket.ReadShort();
                        short ItemType = iPacket.ReadShort();
                        short ItemID = iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("LoRpDeleteItemPacket"))
                        {
                            this.Parent.Client.Send(outPacket);
                        }
                        if (ItemType == 3)
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(GetKart.NewKart_LoadFile);
                            XmlElement elementToRemove = doc.SelectSingleNode("//Kart[@id='" + ItemID + "' and @sn='" + SN + "']") as XmlElement;
                            if (elementToRemove != null)
                            {
                                elementToRemove.ParentNode.RemoveChild(elementToRemove);
                            }
                            doc.Save(GetKart.NewKart_LoadFile);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqQueryCoupon", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpQueryCoupon"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqShopCashPage", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrShopCashPage"))
                        {
                            outPacket.WriteString("https://github.com/yanygm/Launcher_V2/releases");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqShopURLPage", 0))
                    {
                        int URLPageType = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrShopURLPage"))
                        {
                            outPacket.WriteInt(URLPageType);
                            outPacket.WriteString("https://github.com/yanygm/Launcher_V2/releases");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqBingoSync", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrBingoSync"))
                        {
                            outPacket.WriteByte(0);
                            outPacket.WriteUShort(0);
                            outPacket.WriteUShort(0);
                            for (int i = 0; i < 15; i++)
                            {
                                outPacket.WriteByte(0);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqEnterKartPassPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrEnterKartPassPacket"))
                        {
                            outPacket.WriteHexString("00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartPassPlayTimePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrKartPassPlayTimePacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartPassRewardPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrKartPassRewardPacket"))
                        {
                            outPacket.WriteHexString("00 00 00 00 00 00 00 00 01 00 00 00 01 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqEnterSeasonPassPacket", 0))
                    {
                        byte SeasonPassType = iPacket.ReadByte();
                        using (OutPacket outPacket = new OutPacket("PrEnterSeasonPassPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(SeasonPassType);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSeasonPassRewardPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrSeasonPassRewardPacket"))
                        {
                            outPacket.WriteHexString("00 00 00 00 00 00 00 00 01 00 00 00 01 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCheckPassword", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrCheckPassword"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUnLockedItem", 0))
                    {
                        int useType = iPacket.ReadInt();
                        int stringType = iPacket.ReadInt();
                        for (int i = 0; i < useType; i++)
                        {
                            iPacket.ReadString(false);
                        }
                        byte Type = iPacket.ReadByte();
                        using (OutPacket outPacket = new OutPacket("PrUnLockedItem"))
                        {
                            outPacket.WriteByte(Type);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqFavoriteItemGet", 0)) //즐겨 찾기 목록
                    {
                        FavoriteItem.Favorite_Item();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqFavoriteItemUpdate", 0))
                    {
                        iPacket.ReadByte();
                        int j = iPacket.ReadShort();
                        iPacket.ReadShort();
                        for (int i = 0; i < j; i++)
                        {
                            short item = iPacket.ReadShort();
                            short id = iPacket.ReadShort();
                            short sn = iPacket.ReadShort();
                            byte Add_Del = iPacket.ReadByte();
                            if (Add_Del == 1)
                            {
                                FavoriteItem.Favorite_Item_Add(item, id, sn);
                            }
                            else if (Add_Del == 2)
                            {
                                FavoriteItem.Favorite_Item_Del(item, id, sn);
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqLockedItemGet", 0))//아이템 보호
                    {
                        using (OutPacket outPacket = new OutPacket("PrLockedItemGet"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqLockedItemUpdate", 0))
                    {
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSimGameSimpleInfoAndRankPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrSimGameSimpleInfoAndRankPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSimGameEnterPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrSimGameEnterPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqExchangeXPartsItem", 0))
                    {
                        short KartType = iPacket.ReadShort();
                        iPacket.ReadShort();
                        byte Grade = iPacket.ReadByte();
                        Random random = new Random();
                        short[] numbers = { 63, 64, 65, 66 };
                        int randomIndex = random.Next(0, numbers.Length);
                        short randomNumber = numbers[randomIndex];
                        using (OutPacket outPacket = new OutPacket("PrExchangeXPartsItem"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(ProfileService.ProfileConfig.Rider.SlotChanger);
                            outPacket.WriteShort(randomNumber);
                            outPacket.WriteShort(KartType);
                            outPacket.WriteShort(1);
                            outPacket.WriteShort(0);
                            outPacket.WriteByte(Grade);
                            outPacket.WriteByte(1);
                            outPacket.WriteShort(1200);
                            outPacket.WriteShort(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqTimeShopPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpTimeShopPacket"))
                        {
                            outPacket.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF FF FF FF FF FF 47 00 00 00 00 00 47 00 00 00 00 00 00 00 02 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSecretShopEnterPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrSecretShopEnterPacket"))
                        {
                            outPacket.WriteHexString("00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqEnterUpgradeGearPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrEnterUpgradeGearPacket"))
                        {
                            outPacket.WriteHexString("05 00 00 00 03 00 00 00 05 00 00 00 00 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqBlockCatchEnterPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrBlockCatchEnterPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(3);
                            outPacket.WriteInt(3);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(5);
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(7);
                            outPacket.WriteInt(2);
                            outPacket.WriteInt(600);
                            outPacket.WriteInt(300);
                            outPacket.WriteInt(200);
                            outPacket.WriteInt(100);
                            outPacket.WriteInt(-100);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RqEnterFishingStagePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("RpEnterFishingStagePacket"))
                        {
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqPcCafeShowcaseCoupon", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrPcCafeShowcaseCoupon"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetRiderCareerSummary", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetRiderCareerSummary"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("checkSecondAuthenPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("checkSecondAuthenPacket"))
                        {
                            outPacket.WriteInt(2);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqTestServerAddItemPacket", 0))
                    {
                        TestServer.Type = iPacket.ReadShort();
                        TestServer.ItemID = iPacket.ReadShort();
                        TestServer.Amount = iPacket.ReadShort();
                        TestServer.TestServerAddItem();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqServerTime", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrServerTime"))
                        {
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[0]);
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[1]);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqLogin", 0))
                    {
                        GameDataReset.DataReset();
                        using (OutPacket outPacket = new OutPacket("PrLogin"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[0]);
                            outPacket.WriteUShort((ushort)RouterListener.DataTime()[1]);
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.UserNO);
                            outPacket.WriteString(ProfileService.ProfileConfig.Rider.UserID);
                            outPacket.WriteByte(2);
                            outPacket.WriteByte(1);
                            outPacket.WriteByte(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(0);
                            outPacket.WriteHexString("FF FF 5F 54");
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.pmap);
                            for (int i = 0; i < 11; i++)
                            {
                                outPacket.WriteInt(0);
                            }
                            outPacket.WriteByte(0);
                            outPacket.WriteEndPoint(IPAddress.Parse(RouterListener.sIP), 39311);
                            outPacket.WriteEndPoint(IPAddress.Parse(RouterListener.sIP), 39312);
                            outPacket.WriteInt(0);
                            outPacket.WriteString("");
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(1);
                            outPacket.WriteString("content");
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(1);
                            outPacket.WriteString("cc");
                            outPacket.WriteString(SessionGroup.Service);
                            outPacket.WriteInt(6);
                            outPacket.WriteString("content");
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(2);
                            outPacket.WriteString("name");
                            outPacket.WriteString("dynamicPpl");
                            outPacket.WriteString("enable");
                            outPacket.WriteString("false");
                            outPacket.WriteInt(1);
                            outPacket.WriteString("region");
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(1);
                            outPacket.WriteString("szId");
                            outPacket.WriteString(SessionGroup.usLocale.ToString());
                            outPacket.WriteInt(0);
                            outPacket.WriteString("content");
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(3);
                            outPacket.WriteString("name");
                            outPacket.WriteString("grandprix");
                            outPacket.WriteString("enable");
                            outPacket.WriteString("true");
                            outPacket.WriteString("visible");
                            outPacket.WriteString("true");
                            outPacket.WriteInt(0);
                            outPacket.WriteString("content");
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(3);
                            outPacket.WriteString("name");
                            outPacket.WriteString("endingBanner");
                            outPacket.WriteString("enable");
                            outPacket.WriteString("false");
                            outPacket.WriteString("value");
                            outPacket.WriteString("https://github.com/yanygm/Launcher_V2/releases");
                            outPacket.WriteInt(0);
                            outPacket.WriteString("content");
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(3);
                            outPacket.WriteString("name");
                            outPacket.WriteString("themeXyy");
                            outPacket.WriteString("enable");
                            outPacket.WriteString("true");
                            outPacket.WriteString("visible");
                            outPacket.WriteString("true");
                            outPacket.WriteInt(0);
                            outPacket.WriteString("content");
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(3);
                            outPacket.WriteString("name");
                            outPacket.WriteString("themeKorea");
                            outPacket.WriteString("enable");
                            outPacket.WriteString("true");
                            outPacket.WriteString("visible");
                            outPacket.WriteString("true");
                            outPacket.WriteInt(0);
                            outPacket.WriteString("content");
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(5);
                            outPacket.WriteString("name");
                            outPacket.WriteString("timeAttack");
                            outPacket.WriteString("enable");
                            outPacket.WriteString("true");
                            outPacket.WriteString("visible");
                            outPacket.WriteString("true");
                            outPacket.WriteString("value");
                            outPacket.WriteString("village_R01");
                            outPacket.WriteString("maxReplayFileCount");
                            outPacket.WriteString("250");
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(0);
                            outPacket.WriteByte(ProfileService.ProfileConfig.GameOption.Set_screen);
                            outPacket.WriteByte(ProfileService.ProfileConfig.Rider.IdentificationType);
                            RouterListener.MySession.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSyncJackpotPointCS", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrSyncJackpotPointCS"))
                        {
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqEnterShopPacket", 0))
                    {
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetlotteryMileageInfoPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetlotteryMileageInfoPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqTcCashEventUserInfoPacket", 0))
                    {
                        int unk1 = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrTcCashEventUserInfoPacket"))
                        {
                            outPacket.WriteInt(unk1);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqServerSideUdpBindCheck", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("PqMissionAttendUserStatePacket", 0))
                    {
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqBoomhillExchangeInfo", 0))
                    {
                        short Type = iPacket.ReadShort();
                        iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrBoomhillExchangeInfo"))
                        {
                            outPacket.WriteInt();
                            outPacket.WriteShort(Type);
                            outPacket.WriteInt();
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqBoomhillExchangeNeedNotice", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrBoomhillExchangeNeedNotice"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqMixItemExchangeCount", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrMixItemExchangeCount"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqMixItem", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrMixItem"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RqBoomhillExchangeKoin", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("RpBoomhillExchangeKoin"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqUseInitialCardPacket", 0))
                    {
                        ProfileService.ProfileConfig.Rider.Card = iPacket.ReadString();
                        ProfileService.Save();
                        using (OutPacket outPacket = new OutPacket("SpRpUseInitialCardPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKart12TuningLevelUp", 0))
                    {
                        short kart = iPacket.ReadShort();
                        short sn = iPacket.ReadShort();
                        uint[] money = new uint[] { 0, 10, 12, 15, 20, 30 };
                        var existingParts = KartExcData.Level12List.FirstOrDefault(list => list[0] == kart && list[1] == sn);
                        if (existingParts != null)
                        {
                            using (OutPacket outPacket = new OutPacket("PrKart12TuningLevelUp"))
                            {
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(1);
                                outPacket.WriteShort(3);
                                outPacket.WriteShort(kart);
                                outPacket.WriteShort(sn);
                                short Level = (short)((int)existingParts[2] + 1);
                                existingParts[2] = Level;
                                outPacket.WriteShort(Level);//1-1,2-3,3-6,4-10,5-15
                                short Point = (short)((int)existingParts[9] + (int)Level);
                                existingParts[9] = Point;
                                outPacket.WriteShort(Point);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(4);
                                outPacket.WriteShort(4);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteUInt(money[(int)Level]);
                                outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Lucci);
                                this.Parent.Client.Send(outPacket);
                                KartExcData.AddLevel12List(kart, sn, Level, -1, -1, -1, Point);
                            }
                        }
                        else
                        {
                            using (OutPacket outPacket = new OutPacket("PrKart12TuningLevelUp"))
                            {
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(1);
                                outPacket.WriteShort(3);
                                outPacket.WriteShort(kart);
                                outPacket.WriteShort(sn);
                                outPacket.WriteShort(5);//1-1,2-3,3-6,4-10,5-15
                                outPacket.WriteShort(15);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(4);
                                outPacket.WriteShort(4);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteUInt(10);
                                outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Lucci);
                                this.Parent.Client.Send(outPacket);
                                KartExcData.AddLevel12List(kart, sn, 5, 0, 0, 0, 15);
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKart12UnlockTuningSkill", 0))
                    {
                        short kart = iPacket.ReadShort();
                        short sn = iPacket.ReadShort();
                        short field = iPacket.ReadShort();
                        short SI = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("PrKart12UnlockTuningSkill"))
                        {
                            outPacket.WriteShort(kart);
                            outPacket.WriteShort(sn);
                            outPacket.WriteShort(field);
                            outPacket.WriteShort(SI);
                            if (field == 2)
                            {
                                outPacket.WriteShort(2);
                                outPacket.WriteShort(3);
                            }
                            else
                            {
                                outPacket.WriteShort(4);
                                outPacket.WriteShort(4);
                            }
                            outPacket.WriteShort(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKart12RestictTuningSkill", 0))
                    {
                        short kart = iPacket.ReadShort();
                        short sn = iPacket.ReadShort();
                        short field = iPacket.ReadShort();
                        var existingParts = KartExcData.Level12List.FirstOrDefault(list => list[0] == kart && list[1] == sn);
                        if (existingParts != null)
                        {
                            using (OutPacket outPacket = new OutPacket("PrKart12RestictTuningSkill"))
                            {
                                outPacket.WriteShort(kart);
                                outPacket.WriteShort(sn);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(field);
                                this.Parent.Client.Send(outPacket);
                            }
                            short point = existingParts[9];
                            short skill = 0;
                            short skilllevel = 0;
                            switch (field)
                            {
                                case 1:
                                    skill = existingParts[3];
                                    skilllevel = existingParts[4];
                                    break;
                                case 2:
                                    skill = existingParts[5];
                                    skilllevel = existingParts[6];
                                    break;
                                case 3:
                                    skill = existingParts[7];
                                    skilllevel = existingParts[8];
                                    break;
                            }
                            KartExcData.AddLevel12List(kart, sn, -1, field, skill, 0, (short)(point + skilllevel));
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKart12ChangeTuningSkill", 0))
                    {
                        short kart = iPacket.ReadShort();
                        short sn = iPacket.ReadShort();
                        short Skill = iPacket.ReadShort();
                        short field = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("PrKart12ChangeTuningSkill"))
                        {
                            outPacket.WriteShort(kart);
                            outPacket.WriteShort(sn);
                            outPacket.WriteShort(field);
                            outPacket.WriteShort(Skill);
                            outPacket.WriteShort(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        KartExcData.AddLevel12List(kart, sn, -1, field, Skill, -1, -1);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKart12TuningPointUpdate", 0))
                    {
                        short kart = iPacket.ReadShort();
                        short sn = iPacket.ReadShort();
                        short field = iPacket.ReadShort();
                        byte AddDel = iPacket.ReadByte();
                        var existingParts = KartExcData.Level12List.FirstOrDefault(list => list[0] == kart && list[1] == sn);
                        if (existingParts != null)
                        {
                            short point = existingParts[9];
                            short skilllevel = 0;
                            switch (field)
                            {
                                case 1:
                                    skilllevel = existingParts[4];
                                    break;
                                case 2:
                                    skilllevel = existingParts[6];
                                    break;
                                case 3:
                                    skilllevel = existingParts[8];
                                    break;
                            }
                            if (AddDel == 1)
                            {
                                using (OutPacket outPacket = new OutPacket("PrKart12TuningPointUpdate"))
                                {
                                    outPacket.WriteShort(kart);
                                    outPacket.WriteShort(sn);
                                    outPacket.WriteShort(field);
                                    outPacket.WriteShort((short)((int)skilllevel + 1));
                                    outPacket.WriteByte(1);
                                    outPacket.WriteShort((short)((int)point - 1));
                                    outPacket.WriteByte(1);
                                    this.Parent.Client.Send(outPacket);
                                }
                                KartExcData.AddLevel12List(kart, sn, -1, field, -1, (short)((int)skilllevel + 1), (short)((int)point - 1));
                            }
                            else if (AddDel == 0)
                            {
                                using (OutPacket outPacket = new OutPacket("PrKart12TuningPointUpdate"))
                                {
                                    outPacket.WriteShort(kart);
                                    outPacket.WriteShort(sn);
                                    outPacket.WriteShort(field);
                                    outPacket.WriteShort((short)((int)skilllevel - 1));
                                    outPacket.WriteByte(0);
                                    outPacket.WriteShort((short)((int)point + 1));
                                    outPacket.WriteByte(0);
                                    this.Parent.Client.Send(outPacket);
                                }
                                KartExcData.AddLevel12List(kart, sn, -1, field, -1, (short)((int)skilllevel - 1), (short)((int)point + 1));
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKart12PartsLevelUp", 0))
                    {
                        short kart = iPacket.ReadShort();
                        short sn = iPacket.ReadShort();
                        iPacket.ReadShort();
                        byte parts = iPacket.ReadByte();
                        byte old = iPacket.ReadByte();
                        iPacket.ReadByte();
                        byte leve = iPacket.ReadByte();
                        using (OutPacket outPacket = new OutPacket("PrKart12PartsLevelUp"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Lucci);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        short Item_Cat_Id = 0;
                        switch (parts)
                        {
                            case 0:
                                Item_Cat_Id = 72;
                                break;
                            case 1:
                                Item_Cat_Id = 73;
                                break;
                            case 2:
                                Item_Cat_Id = 74;
                                break;
                            case 3:
                                Item_Cat_Id = 75;
                                break;
                        }
                        if (kart != 0 && sn != 0 && Item_Cat_Id != 0)
                        {
                            KartExcData.AddPartsList(kart, sn, Item_Cat_Id, (short)(old + 1), V2Spec.GetGrade((byte)(old + 1)), V2Spec.Get12Parts((short)(old + 1)));
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKart12PartsLevelReset", 0))
                    {
                        short kart = iPacket.ReadShort();
                        short sn = iPacket.ReadShort();
                        iPacket.ReadShort();
                        byte parts = iPacket.ReadByte();
                        using (OutPacket outPacket = new OutPacket("PrKart12PartsLevelReset"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Lucci);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        short Item_Cat_Id = 0;
                        switch (parts)
                        {
                            case 0:
                                Item_Cat_Id = 72;
                                break;
                            case 1:
                                Item_Cat_Id = 73;
                                break;
                            case 2:
                                Item_Cat_Id = 74;
                                break;
                            case 3:
                                Item_Cat_Id = 75;
                                break;
                        }
                        if (kart != 0 && sn != 0 && Item_Cat_Id != 0)
                        {
                            var existingParts = KartExcData.Parts12List.FirstOrDefault(list => list[0] == kart && list[1] == sn);
                            if (existingParts != null)
                            {
                                short i = 0;
                                switch (Item_Cat_Id)
                                {
                                    case 72:
                                        i = 3;
                                        break;
                                    case 73:
                                        i = 6;
                                        break;
                                    case 74:
                                        i = 9;
                                        break;
                                    case 75:
                                        i = 12;
                                        break;
                                }
                                short Default = existingParts[i];
                                KartExcData.AddPartsList(kart, sn, Item_Cat_Id, Default, V2Spec.GetGrade((byte)Default), V2Spec.Get12Parts(Default));
                            }
                            KartExcData.AddPartsList(kart, sn, Item_Cat_Id, 1, 4, 201);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartExceedTypeChange", 0))
                    {
                        short kart1 = iPacket.ReadShort();
                        short sn1 = iPacket.ReadShort();
                        short kart2 = iPacket.ReadShort();
                        short sn2 = iPacket.ReadShort();
                        short Spanner = iPacket.ReadShort();
                        uint Lucci = iPacket.ReadUInt();

                        int[] numbers = { 2, 3, 4, 6 };
                        Random random = new Random();
                        int index = random.Next(numbers.Length);
                        int ExceedType = numbers[index];
                        //int ExceedType = Random.Shared.Next(2, 5);
                        using (OutPacket outPacket = new OutPacket("PrKartExceedTypeChange"))
                        {
                            outPacket.WriteShort(0);//kart2
                            outPacket.WriteShort(0);//sn2
                            outPacket.WriteShort(Spanner);
                            outPacket.WriteUInt(ProfileService.ProfileConfig.Rider.Lucci);
                            outPacket.WriteInt(ExceedType);
                            this.Parent.Client.Send(outPacket);
                        }
                        var existing12List = KartExcData.Parts12List.FirstOrDefault(list => list[0] == kart1 && list[1] == sn1);
                        if (existing12List == null)
                        {
                            var newList = new List<short> { kart1, sn1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (short)ExceedType };
                            KartExcData.Parts12List.Add(newList);
                            KartExcData.SaveParts12List(KartExcData.Parts12List);
                        }
                        else
                        {
                            existing12List[17] = (short)ExceedType;
                            KartExcData.SaveParts12List(KartExcData.Parts12List);
                        }
                        Console.WriteLine("ExceedType: " + ExceedType);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqMissionAttendNRUserStatePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrMissionAttendNRUserStatePacket"))
                        {
                            outPacket.WriteByte(4);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqMissionAttendUserStatePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrMissionAttendUserStatePacket"))
                        {
                            outPacket.WriteByte(4);
                            outPacket.WriteByte(1);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetlotteryMileageMultiInfoPacket", 0))
                    {
                        int Count = iPacket.ReadInt();
                        uint[] Data = new uint[Count];
                        for (int i = 0; i < Count; i++)
                        {
                            Data[i] = iPacket.ReadUInt();
                        }
                        using (OutPacket outPacket = new OutPacket("PrGetlotteryMileageMultiInfoPacket"))
                        {
                            outPacket.WriteInt(Count);
                            for (int i = 0; i < Count; i++)
                            {
                                outPacket.WriteUInt(Data[i]);
                                outPacket.WriteShort(1);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqQuestUX2ndForShutDownPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrQuestUX2ndForShutDownPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else
                    {
                        MultyPlayer.Clientsession(hash, iPacket);
                        return;
                    }
                }
            }
        }
    }
}
