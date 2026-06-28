using ExcData;
using KartRider;
using KartRider.Common.Network;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider_PacketName;
using Profile;
using RiderData;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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

        public override void OnDisconnect()
        {
            if (this.Parent?.Client != null)
            {
                this.Parent.Client.Disconnect();
            }
            ClientManager.RemoveClient(this.Parent.Client.Socket);
        }

        public override void OnPacket(InPacket iPacket)
        {
            int ALLnum;
            lock (this.Parent.m_lock)
            {
                iPacket.Position = 0;
                uint hash = iPacket.ReadUInt();

                if (PacketDispatcher.Dispatch(typeof(MsgrServer), (PacketName)hash, iPacket, iPacket.ToArray(), this.Parent, null))
                    return;

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"[{currentTime}][{this.Parent.Client.Nickname}] " + (PacketName)hash + ": " + BitConverter.ToString(iPacket.ToArray()).Replace("-", " "));
                if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCnAuthenLogin", 0))
                {
                    iPacket.ReadInt();
                    iPacket.ReadInt();
                    string dataPacket = Base64Helper.Decode(iPacket.ReadString(true));
                    DataPacket packet = JsonHelper.Deserialize<DataPacket>(dataPacket);
                    if (packet == null) return;
                    if (string.IsNullOrEmpty(packet.Nickname) && packet.ClientVersion != ProfileService.SettingConfig.ClientVersion)
                        return;
                    if (ClientManager.HasClientWithNickname(packet.Nickname))
                    {
                        var existingConfig = ProfileService.GetProfileConfig(packet.Nickname);
                        if (existingConfig.Rider.BanType == 0)
                        {
                            existingConfig.Rider.BanType = 1;
                            ProfileService.Save(packet.Nickname, existingConfig);
                        }
                    }
                    using (OutPacket outPacket = new OutPacket("PrCnAuthenLogin"))
                    {
                        outPacket.WriteInt(1);
                        outPacket.WriteString("pnlcdfngkdjfdhdermnkicqknmqrnjnkrlpdirerjrqkcllhpckngophnrrfclgiojmopomonkjilgmheoldpmmcdokgdqljqcnkrplffhflqdnchherghnhoihgfnon");
                        outPacket.WriteByte(0);
                        outPacket.WriteString("https://www.tiancity.com/agreement");
                        this.Parent.Client.Send(outPacket);
                    }
                    IPEndPoint clientEndPoint = Parent.Client.Socket.RemoteEndPoint as IPEndPoint;
                    if (clientEndPoint == null) return;
                    string clientId = ClientManager.GetClientId(clientEndPoint);
                    this.Parent.Client.Nickname = packet.Nickname;
                    FileName.Load(packet.Nickname);
                    uint UserNO = ClientManager.GetUserNO(packet.Nickname);
                    var loginConfig = ProfileService.GetProfileConfig(packet.Nickname);
                    loginConfig.Rider.ClientId = clientId;
                    if (packet.Nickname.Length > 1 && packet.Nickname.StartsWith("ob", StringComparison.OrdinalIgnoreCase))
                    {
                        if (loginConfig.Rider.pmap == 0)
                        {
                            loginConfig.Rider.pmap = 718;
                        }
                    }
                    ProfileService.Save(packet.Nickname, loginConfig);
                    return;
                }
                if (hash != Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("PcReportRaidOccur"), 0) && hash != Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("PqGameReportMyBadUdp"), 0))
                {
                    if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqAddRacingTimePacket", 0))
                    {
                        uint Track = iPacket.ReadUInt();
                        byte SpeedType = iPacket.ReadByte();
                        byte GameType = iPacket.ReadByte();
                        iPacket.ReadBytes(8);
                        ushort Kart = iPacket.ReadUShort();
                        //iPacket.ReadBytes(417);
                        iPacket.Position = iPacket.Length - 8;
                        short Booster = iPacket.ReadShort();
                        iPacket.ReadShort();
                        short Crash = iPacket.ReadShort();
                        iPacket.ReadShort();
                        var racingTime = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        using (OutPacket outPacket = new OutPacket("LoRpAddRacingTimePacket"))
                        {
                            //outPacket.WriteHexString("FF FF FF FF 00 00 00 00 00 00 00 00 00 00");
                            outPacket.WriteUInt(racingTime.Rider.Time);
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        var manager = new CompetitiveDataManager();
                        CompleteTrackScoreCalculator calculator = new CompleteTrackScoreCalculator();
                        var scores = calculator.CalculateTrackScoreDetails(Track, racingTime.Rider.Time, Booster, Crash, TimeAttack.TrackDictionary);
                        if (scores != null)
                        {
                            var data = new CompetitiveData { Track = Track, Kart = Kart, Time = racingTime.Rider.Time, Booster = Booster, BoosterPoint = scores.BoostScore, Crash = Crash, CrashPoint = scores.CrashScore, Point = scores.TotalScore };
                            manager.SaveData(this.Parent.Client.Nickname, data);
                        }
                        using (OutPacket outPacket = new OutPacket("PrGetCompetitiveSlotInfo"))
                        {
                            var competitiveData = manager.LoadAllData(this.Parent.Client.Nickname);
                            outPacket.WriteInt(competitiveData.Count);
                            foreach (var competitive in competitiveData)
                            {
                                outPacket.WriteUInt(competitive.Track);
                                outPacket.WriteUInt(competitive.Track);
                                outPacket.WriteUShort(competitive.Kart);
                                outPacket.WriteUInt(competitive.Time);
                                outPacket.WriteHexString("FF FF FF FF");
                                outPacket.WriteShort(competitive.Booster);
                                outPacket.WriteUInt(competitive.BoosterPoint);
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
                        var oldNickname = this.Parent.Client.Nickname;
                        var newNickname = iPacket.ReadString(false);

                        if (!FileName.FileNames.ContainsKey(oldNickname))
                        {
                            // 旧昵称未加载，拒绝改名
                            using (OutPacket outPacket = new OutPacket("SpRpRenameRidPacket"))
                            {
                                outPacket.WriteInt(1);
                                this.Parent.Client.Send(outPacket);
                            }
                            return;
                        }
                        var filename = FileName.FileNames[oldNickname];
                        string newfile = Path.GetFullPath(Path.Combine(FileName.ProfileDir, newNickname));

                        if (Directory.Exists(filename.NicknameDir))
                        {
                            if (Directory.Exists(newfile))
                            {
                                using (OutPacket outPacket = new OutPacket("SpRpRenameRidPacket"))
                                {
                                    outPacket.WriteInt(1);
                                    this.Parent.Client.Send(outPacket);
                                }
                            }
                            else
                            {
                                Directory.Move(filename.NicknameDir, newfile);

                                // 更新所有存储Nickname的地方为新昵称
                                // 1. 更新 SessionGroup.Nickname
                                this.Parent.Client.Nickname = newNickname;

                                // 2. 更新 FileName.FileNames 的 key
                                FileName.FileNames.Remove(oldNickname);
                                FileName.Load(newNickname);

                                // 3. 更新 ClientManager.NicknameToUserNO
                                ClientManager.UpdateNickname(oldNickname, newNickname);

                                // 4. 删除可能被意外重建的旧昵称目录
                                string oldDir = Path.GetFullPath(Path.Combine(FileName.ProfileDir, oldNickname));
                                if (Directory.Exists(oldDir))
                                {
                                    Directory.Delete(oldDir, true);
                                }

                                if (ProfileService.SettingConfig.Name == oldNickname)
                                {
                                    ProfileService.SettingConfig.Name = newNickname;
                                    ProfileService.SaveSettings();
                                }

                                using (OutPacket outPacket = new OutPacket("SpRpRenameRidPacket"))
                                {
                                    outPacket.WriteInt(0);
                                    this.Parent.Client.Send(outPacket);
                                }
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetRider", 0))
                    {
                        NewRider.LoadItemData(this.Parent, this.Parent.Client.Nickname);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqGetRiderItemPacket", 0))
                    {
                        //NewRider.LoadItemData();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqSetRiderItemOnPacket", 0))
                    {
                        var riderItemConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        riderItemConfig.RiderItem.Set_Character = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Paint = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Kart = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Plate = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Goggle = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Balloon = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Unknown1 = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_HeadBand = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_HeadPhone = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_HandGearL = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Unknown2 = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Uniform = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Decal = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Pet = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_FlyingPet = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Aura = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_SkidMark = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_SpecialKit = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_RidColor = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_BonusCard = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_BossModeCard = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_KartPlant1 = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_KartPlant2 = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_KartPlant3 = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_KartPlant4 = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Unknown3 = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_FishingPole = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Tachometer = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Dye = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_KartSN = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Unknown4 = iPacket.ReadByte();
                        riderItemConfig.RiderItem.Set_KartCoating = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_KartTailLamp = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_slotBg = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_KartCoating12 = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_KartTailLamp12 = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_KartBoosterEffect12 = iPacket.ReadUShort();
                        riderItemConfig.RiderItem.Set_Unknown5 = iPacket.ReadUShort();
                        ProfileService.Save(this.Parent.Client.Nickname, riderItemConfig);
                        int roomId = RoomManager.TryGetRoomId(this.Parent.Client.Nickname);
                        if (roomId != -1)
                        {
                            Player player = RoomManager.GetPlayer(roomId, this.Parent.Client.Nickname);
                            if (player != null)
                            {
                                using (OutPacket outPacket = new OutPacket("GrSlotItemOnPacket"))
                                {
                                    outPacket.WriteInt(player.ID);
                                    GameSupport.GetRider(this.Parent.Client.Nickname, outPacket);
                                    MultyPlayer.BroadCast(roomId, outPacket, this.Parent.Client.Nickname);
                                }
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetRiderInfo", 0))
                    {
                        uint UserID = iPacket.ReadUInt();
                        iPacket.ReadInt();
                        string nickname = ClientManager.GetNickname(UserID);
                        if (UserID == 0)
                        {
                            nickname = iPacket.ReadString(false);
                        }
                        if (nickname == this.Parent.Client.Nickname)
                        {
                            GameSupport.PrGetRiderInfo(nickname, this.Parent);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(nickname))
                            {
                                using (OutPacket outPacket = new OutPacket("PrGetRiderInfo"))
                                {
                                    outPacket.WriteByte(0);
                                    this.Parent.Client.Send(outPacket);
                                }
                            }
                            else
                            {
                                GameSupport.PrGetRiderInfo(nickname, this.Parent);
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUpdateRiderIntro", 0))
                    {
                        var introConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        introConfig.Rider.RiderIntro = iPacket.ReadString(false);
                        ProfileService.Save(this.Parent.Client.Nickname, introConfig);
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
                            outPacket.WriteByte(0);
                            // outPacket.WriteHexString("01 CC B3 6F 48");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqUpdateGameOption", 0))
                    {
                        var gameOptionConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        gameOptionConfig.GameOption.Set_BGM = iPacket.ReadFloat();
                        gameOptionConfig.GameOption.Set_Sound = iPacket.ReadFloat();
                        gameOptionConfig.GameOption.Main_BGM = iPacket.ReadByte();
                        gameOptionConfig.GameOption.Sound_effect = iPacket.ReadByte();
                        gameOptionConfig.GameOption.Full_screen = iPacket.ReadByte();
                        gameOptionConfig.GameOption.ShowMirror = iPacket.ReadByte();
                        gameOptionConfig.GameOption.ShowOtherPlayerNames = iPacket.ReadByte();
                        gameOptionConfig.GameOption.ShowOutlines = iPacket.ReadByte();
                        gameOptionConfig.GameOption.ShowShadows = iPacket.ReadByte();
                        gameOptionConfig.GameOption.HighLevelEffect = iPacket.ReadByte();
                        gameOptionConfig.GameOption.MotionBlurEffect = iPacket.ReadByte();
                        gameOptionConfig.GameOption.MotionDistortionEffect = iPacket.ReadByte();
                        gameOptionConfig.GameOption.HighEndOptimization = iPacket.ReadByte();//오토 레디
                        gameOptionConfig.GameOption.AutoReady = iPacket.ReadByte();//아이템 설명
                        gameOptionConfig.GameOption.PropDescription = iPacket.ReadByte();//녹화 품질
                        gameOptionConfig.GameOption.VideoQuality = iPacket.ReadByte();
                        gameOptionConfig.GameOption.BGM_Check = iPacket.ReadByte();//배경음
                        gameOptionConfig.GameOption.Sound_Check = iPacket.ReadByte();//효과음
                        gameOptionConfig.GameOption.ShowHitInfo = iPacket.ReadByte();
                        gameOptionConfig.GameOption.AutoBoost = iPacket.ReadByte();
                        gameOptionConfig.GameOption.GameType = iPacket.ReadByte();//팀전 개인전 여부
                        gameOptionConfig.GameOption.SetGhost = iPacket.ReadByte();//고스트 사용여부
                        gameOptionConfig.GameOption.SpeedType = iPacket.ReadByte();//채널 속도
                        gameOptionConfig.GameOption.RoomChat = iPacket.ReadByte();//查看房间内聊天内容
                        gameOptionConfig.GameOption.DrivingChat = iPacket.ReadByte();//查看行驶中聊天内容
                        gameOptionConfig.GameOption.ShowAllPlayerHitInfo = iPacket.ReadByte();
                        gameOptionConfig.GameOption.ShowTeamColor = iPacket.ReadByte();
                        gameOptionConfig.GameOption.Set_screen = iPacket.ReadByte();
                        gameOptionConfig.GameOption.HideCompetitiveRank = iPacket.ReadByte();//隐藏自己的等级段位
                        gameOptionConfig.GameOption.QuickMsg[0] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.QuickMsg[1] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.QuickMsg[2] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.QuickMsg[3] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.QuickMsg[4] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.QuickMsg[5] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.QuickMsg[6] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.QuickMsg[7] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.QuickMsg[8] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.QuickMsg[9] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.TeamQuickMsg[0] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.TeamQuickMsg[1] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.TeamQuickMsg[2] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.TeamQuickMsg[3] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.TeamQuickMsg[4] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.TeamQuickMsg[5] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.TeamQuickMsg[6] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.TeamQuickMsg[7] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.TeamQuickMsg[8] = iPacket.ReadString(false);
                        gameOptionConfig.GameOption.TeamQuickMsg[9] = iPacket.ReadString(false);
                        ProfileService.Save(this.Parent.Client.Nickname, gameOptionConfig);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetGameOption", 0))
                    {
                        GameSupport.PrGetGameOption(this.Parent, this.Parent.Client.Nickname);
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
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqEventRewardPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("LoRpEventRewardPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqVipGradeCheck", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrVipGradeCheck"))
                        {
                            outPacket.WriteShort(0);
                            outPacket.WriteByte(0);
                            outPacket.WriteByte(0);
                            outPacket.WriteDateTime(DateTime.Now);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqLoginVipInfo", 0))
                    {
                        var vipConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        using (OutPacket outPacket = new OutPacket("PrLoginVipInfo"))
                        {
                            outPacket.WriteShort((short)vipConfig.Rider.Premium);
                            if (vipConfig.Rider.Premium == 0)
                                outPacket.WriteInt(0);
                            else if (vipConfig.Rider.Premium == 1)
                                outPacket.WriteInt(10000);
                            else if (vipConfig.Rider.Premium == 2)
                                outPacket.WriteInt(30000);
                            else if (vipConfig.Rider.Premium == 3)
                                outPacket.WriteInt(60000);
                            else if (vipConfig.Rider.Premium == 4)
                                outPacket.WriteInt(120000);
                            else if (vipConfig.Rider.Premium == 5)
                                outPacket.WriteInt(200000);
                            else
                                outPacket.WriteInt(0);
                            outPacket.WriteShort((short)vipConfig.Rider.Premium);
                            outPacket.WriteByte();
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        using (OutPacket outPacket = new OutPacket("PcSlaveNotice"))
                        {
                            outPacket.WriteString("单机版完全免费, https://yanygm.github.io/Launcher_V2/");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChReRqEnterMyRoomPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("ChRqEnterRandomMyRoomPacket", 0))
                    {
                        var availableNicknames = ClientManager.NicknameToUserNO.Keys.Where(n => n != this.Parent.Client.Nickname).ToList();
                        if (availableNicknames.Count == 0)
                        {
                            MyRoomData.ChRpEnterMyRoomPacket(this.Parent, 5);
                            return;
                        }
                        Random random = new Random();
                        int randomIndex = random.Next(availableNicknames.Count);
                        string targetNickname = availableNicknames[randomIndex];
                        MyRoomData.ChRpEnterMyRoomPacket(this.Parent, this.Parent.Client.Nickname, targetNickname);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChRqEnterMyRoomPacket", 0))
                    {
                        string nickname = iPacket.ReadString(false);
                        MyRoomData.ChRpEnterMyRoomPacket(this.Parent, this.Parent.Client.Nickname, nickname);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RmFirstRequestPacket", 0))
                    {
                        MyRoomData.RmSlotDataPacket(this.Parent, this.Parent.Client.Nickname);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RmRequestItemsPacket", 0))
                    {
                        MyRoomData.RmRequestItemsPacket(this.Parent, this.Parent.Client.Nickname);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RmNotiMyRoomInfoPacket", 0))
                    {
                        var myRoomConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        myRoomConfig.MyRoom.MyRoom = iPacket.ReadShort();
                        myRoomConfig.MyRoom.MyRoomBGM = iPacket.ReadByte();
                        myRoomConfig.MyRoom.UseRoomPwd = iPacket.ReadByte();
                        iPacket.ReadByte();
                        myRoomConfig.MyRoom.UseItemPwd = iPacket.ReadByte();
                        myRoomConfig.MyRoom.TalkLock = iPacket.ReadByte();
                        myRoomConfig.MyRoom.RoomPwd = iPacket.ReadString(false);
                        iPacket.ReadString(false);
                        myRoomConfig.MyRoom.ItemPwd = iPacket.ReadString(false);
                        myRoomConfig.MyRoom.MyRoomKart1 = iPacket.ReadShort();
                        myRoomConfig.MyRoom.MyRoomKart2 = iPacket.ReadShort();
                        ProfileService.Save(this.Parent.Client.Nickname, myRoomConfig);
                        MyRoomData.RmNotiMyRoomInfoPacket(this.Parent, this.Parent.Client.Nickname);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChRqSecedeMyRoomPacket", 0))
                    {
                        // 마이룸 나갈 때
                        MyRoomData.ChRpSecedeMyRoomPacket(this.Parent, this.Parent.Client.Nickname);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RmRiderTalkPacket", 0))
                    {
                        string message = iPacket.ReadString(false);
                        MyRoomData.RmRiderTalkPacket(this.Parent.Client.Nickname, message);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChRqMyroomCheckPassEtcPacket", 0))
                    {
                        int id = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("ChRpMyroomCheckPassEtcPacket"))
                        {
                            outPacket.WriteInt(id);
                            if (id == 0)
                            {
                                outPacket.WriteInt(1);
                            }
                            else
                            {
                                outPacket.WriteInt(0); // 1 允许查看
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqStartScenario", 0))
                    {
                        var scenarioConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        scenarioConfig.Rider.ScenarioType = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrStartScenario"))
                        {
                            outPacket.WriteInt(scenarioConfig.Rider.ScenarioType);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        ProfileService.Save(this.Parent.Client.Nickname, scenarioConfig);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCompleteScenarioSingle", 0))
                    {
                        var completeConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        using (OutPacket outPacket = new OutPacket("PrCompleteScenarioSingle"))
                        {
                            outPacket.WriteInt(completeConfig.Rider.ScenarioType);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartSpec", 0))
                    {
                        var StartTimeAttack_SpeedType = iPacket.ReadByte();
                        var Kart_id = iPacket.ReadUShort();
                        var FlyingPet_id = iPacket.ReadUShort();
                        byte StartType = 1;
                        StartGameData.Start_KartSpac(this.Parent, this.Parent.Client.Nickname, StartType, 0, 0, 0, StartTimeAttack_SpeedType, Kart_id, FlyingPet_id);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChapterInfoPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrChapterInfoPacket"))
                        {
                            outPacket.WriteInt(GameSupport.scenario.Count);
                            foreach (int id in GameSupport.scenario)
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
                        var StartTimeAttack_SpeedType = iPacket.ReadByte();
                        var Kart_id = iPacket.ReadUShort();
                        var FlyingPet_id = iPacket.ReadUShort();
                        byte StartType = 2;
                        StartGameData.Start_KartSpac(this.Parent, this.Parent.Client.Nickname, StartType, 0, 0, 0, StartTimeAttack_SpeedType, Kart_id, FlyingPet_id);
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
                        GameSupport.OnDisconnect(this.Parent);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartLevelPointClear", 0))
                    {
                        short Kart = iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        var koinConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
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
                            outPacket.WriteUInt(koinConfig.Rider.Koin);
                            this.Parent.Client.Send(outPacket);
                        }
                        KartExcData.AddLevelList(this.Parent.Client.Nickname, Kart, SN, 5, 35, 0, 0, 0, 0, 0);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqDisassembleXPartsItem", 0))
                    {
                        iPacket.ReadByte();
                        iPacket.ReadShort();
                        ushort Kart = iPacket.ReadUShort();
                        iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        Console.WriteLine("DisassembleXPartsItem: " + Kart + " " + SN);
                        var currencyConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        using (OutPacket outPacket = new OutPacket("PrDisassembleXPartsItem"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(3);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteUShort(Kart);
                            outPacket.WriteShort(1);
                            outPacket.WriteShort(0);
                            outPacket.WriteByte(1);//Grade
                            outPacket.WriteByte(0);//X-1 V1-2
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteUInt(currencyConfig.Rider.Lucci);
                            outPacket.WriteUInt(currencyConfig.Rider.Koin);
                            outPacket.WriteBytes(new byte[20]);
                            this.Parent.Client.Send(outPacket);
                        }
                        NewRider.AddNewKart(this.Parent, this.Parent.Client.Nickname, Kart);
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
                            outPacket.WriteDateTime(DateTime.Now);
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
                            var koinConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                            outPacket.WriteUInt(koinConfig.Rider.Koin);
                            outPacket.WriteUInt(koinConfig.Rider.Lucci);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        KartExcData.AddLevelList(this.Parent.Client.Nickname, Kart, SN, 5, 35, 0, 0, 0, 0, 0);
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
                        if (!FileName.FileNames.ContainsKey(this.Parent.Client.Nickname))
                        {
                            FileName.Load(this.Parent.Client.Nickname);
                        }
                        var filename = FileName.FileNames[this.Parent.Client.Nickname];
                        var LevelList = new List<Level>();
                        if (File.Exists(filename.LevelData_LoadFile))
                        {
                            LevelList = JsonHelper.DeserializeNoBom<List<Level>>(filename.LevelData_LoadFile) ?? new List<Level>();
                        }
                        var existingLevelList = LevelList.FirstOrDefault(level => level.ID == Kart && level.SN == SN);
                        if (existingLevelList == null)
                        {
                            pointleft = (short)(35 - v1 - v2 - v3 - v4);
                            KartExcData.AddLevelList(this.Parent.Client.Nickname, Kart, SN, 5, pointleft, v1, v2, v3, v4, 0);
                        }
                        else
                        {
                            pointleft = (short)(existingLevelList.Points - v1 - v2 - v3 - v4);
                            short v1New = (short)(existingLevelList.Level1 + v1);
                            short v2New = (short)(existingLevelList.Level2 + v2);
                            short v3New = (short)(existingLevelList.Level3 + v3);
                            short v4New = (short)(existingLevelList.Level4 + v4);
                            short effect = existingLevelList.Effect;
                            KartExcData.AddLevelList(this.Parent.Client.Nickname, Kart, SN, 5, pointleft, v1New, v2New, v3New, v4New, effect);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqGetMaxGiftIdPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpGetMaxGiftIdPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKartLevelSpecialSlotUpdate", 0))
                    {
                        short Kart = iPacket.ReadShort();
                        short SN = iPacket.ReadShort();
                        short Effect = iPacket.ReadShort();

                        if (!FileName.FileNames.ContainsKey(this.Parent.Client.Nickname))
                        {
                            FileName.Load(this.Parent.Client.Nickname);
                        }
                        var filename = FileName.FileNames[this.Parent.Client.Nickname];
                        var LevelList = new List<Level>();
                        if (File.Exists(filename.LevelData_LoadFile))
                        {
                            LevelList = JsonHelper.DeserializeNoBom<List<Level>>(filename.LevelData_LoadFile) ?? new List<Level>();
                        }
                        var existingLevelList = LevelList.FirstOrDefault(level => level.ID == Kart && level.SN == SN);

                        using (OutPacket outPacket = new OutPacket("PrKartLevelSpecialSlotUpdate"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteShort(Kart);
                            outPacket.WriteShort(SN);
                            if (existingLevelList != null)
                            {
                                outPacket.WriteShort(existingLevelList.Grade);
                                outPacket.WriteShort(existingLevelList.Points);
                                outPacket.WriteShort(existingLevelList.Level1);
                                outPacket.WriteShort(existingLevelList.Level2);
                                outPacket.WriteShort(existingLevelList.Level3);
                                outPacket.WriteShort(existingLevelList.Level4);
                                outPacket.WriteShort(Effect);
                                KartExcData.AddLevelList(this.Parent.Client.Nickname, Kart, SN, existingLevelList.Grade, existingLevelList.Points, existingLevelList.Level1, existingLevelList.Level2, existingLevelList.Level3, existingLevelList.Level4, Effect);
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
                                KartExcData.AddLevelList(this.Parent.Client.Nickname, Kart, SN, 5, 0, 10, 10, 10, 5, Effect);
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
                        KartExcData.AddTuneList(this.Parent.Client.Nickname, Kart, KartSN, 0, 0, 0, -1, 0, -1, 0);
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
                        if (!FileName.FileNames.ContainsKey(this.Parent.Client.Nickname))
                        {
                            FileName.Load(this.Parent.Client.Nickname);
                        }
                        var filename = FileName.FileNames[this.Parent.Client.Nickname];
                        var TuneList = new List<Tune>();
                        if (File.Exists(filename.TuneData_LoadFile))
                        {
                            TuneList = JsonHelper.DeserializeNoBom<List<Tune>>(filename.TuneData_LoadFile) ?? new List<Tune>();
                        }
                        var existingList = TuneList.FirstOrDefault(tune => tune.ID == Kart && tune.SN == KartSN);
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
                                    outPacket.WriteShort(existingList.Slot1);
                                    outPacket.WriteShort(existingList.Count1);
                                    outPacket.WriteShort(existingList.Slot2);
                                    outPacket.WriteShort(existingList.Count2);
                                    this.Parent.Client.Send(outPacket);
                                }
                                existingList.Tune1 = 603;
                                existingList.Tune2 = 703;
                                existingList.Tune3 = 903;
                                KartExcData.AddTuneList(this.Parent.Client.Nickname, Kart, KartSN, existingList.Tune1, existingList.Tune2, existingList.Tune3, existingList.Slot1, existingList.Count1, existingList.Slot2, existingList.Count2);
                            }
                            else
                            {
                                List<short> tuneList1 = new List<short> { existingList.Tune1, existingList.Tune2, existingList.Tune3 };
                                List<short> tuneList2 = GameSupport.GetTuns(tuneList1, Item);
                                using (OutPacket outPacket = new OutPacket("PrUseTuneItem"))
                                {
                                    outPacket.WriteInt(0);
                                    outPacket.WriteShort(Item);
                                    outPacket.WriteShort(Item_Id);
                                    outPacket.WriteShort(Kart);
                                    outPacket.WriteShort(KartSN);
                                    outPacket.WriteShort(0);
                                    outPacket.WriteShort(tuneList2[0]);
                                    existingList.Tune1 = tuneList2[0];
                                    outPacket.WriteShort(tuneList2[1]);
                                    existingList.Tune2 = tuneList2[1];
                                    outPacket.WriteShort(tuneList2[2]);
                                    existingList.Tune3 = tuneList2[2];
                                    outPacket.WriteShort(existingList.Slot1);
                                    outPacket.WriteShort(existingList.Count1);
                                    outPacket.WriteShort(existingList.Slot2);
                                    outPacket.WriteShort(existingList.Count2);
                                    this.Parent.Client.Send(outPacket);
                                }
                            }
                            KartExcData.AddTuneList(this.Parent.Client.Nickname, Kart, KartSN, existingList.Tune1, existingList.Tune2, existingList.Tune3, existingList.Slot1, existingList.Count1, existingList.Slot2, existingList.Count2);
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

                        if (!FileName.FileNames.ContainsKey(this.Parent.Client.Nickname))
                        {
                            FileName.Load(this.Parent.Client.Nickname);
                        }
                        var filename = FileName.FileNames[this.Parent.Client.Nickname];
                        var TuneList = new List<Tune>();
                        if (File.Exists(filename.TuneData_LoadFile))
                        {
                            TuneList = JsonHelper.DeserializeNoBom<List<Tune>>(filename.TuneData_LoadFile) ?? new List<Tune>();
                        }
                        var existingList = TuneList.FirstOrDefault(tune => tune.ID == Kart && tune.SN == KartSN);
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
                                outPacket.WriteShort(existingList.Tune1);
                                outPacket.WriteShort(existingList.Tune2);
                                outPacket.WriteShort(existingList.Tune3);
                                if (Protect == 49)
                                {
                                    outPacket.WriteShort(slot);
                                    outPacket.WriteShort(4);
                                    outPacket.WriteShort(existingList.Slot2);
                                    outPacket.WriteShort(existingList.Count2);
                                    existingList.Slot1 = slot;
                                    existingList.Count1 = 4;
                                }
                                else if (Protect == 53)
                                {
                                    outPacket.WriteShort(existingList.Slot1);
                                    outPacket.WriteShort(existingList.Count1);
                                    outPacket.WriteShort(slot);
                                    outPacket.WriteShort(3);
                                    existingList.Slot2 = slot;
                                    existingList.Count2 = 3;
                                }
                                else
                                {
                                    outPacket.WriteShort(existingList.Slot1);
                                    outPacket.WriteShort(existingList.Count1);
                                    outPacket.WriteShort(existingList.Slot2);
                                    outPacket.WriteShort(existingList.Count2);
                                }
                                this.Parent.Client.Send(outPacket);
                                KartExcData.AddTuneList(this.Parent.Client.Nickname, Kart, KartSN, existingList.Tune1, existingList.Tune2, existingList.Tune3, existingList.Slot1, existingList.Count1, existingList.Slot2, existingList.Count2);
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

                        if (!FileName.FileNames.ContainsKey(this.Parent.Client.Nickname))
                        {
                            FileName.Load(this.Parent.Client.Nickname);
                        }
                        var filename = FileName.FileNames[this.Parent.Client.Nickname];
                        var TuneList = new List<Tune>();
                        if (File.Exists(filename.TuneData_LoadFile))
                        {
                            TuneList = JsonHelper.DeserializeNoBom<List<Tune>>(filename.TuneData_LoadFile) ?? new List<Tune>();
                        }
                        var existingList = TuneList.FirstOrDefault(tune => tune.ID == Kart && tune.SN == KartSN);
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
                                outPacket.WriteShort(existingList.Tune1);
                                outPacket.WriteShort(existingList.Tune2);
                                outPacket.WriteShort(existingList.Tune3);
                                outPacket.WriteShort(existingList.Slot1);
                                outPacket.WriteShort(existingList.Count1);
                                outPacket.WriteShort(existingList.Slot2);
                                outPacket.WriteShort(existingList.Count2);
                                this.Parent.Client.Send(outPacket);
                            }
                            List<short> secure = new List<short>();
                            if (existingList.Count1 == 0)
                            {
                                existingList.Slot1 = -1;
                            }
                            else
                            {
                                existingList.Count1 = (short)((int)existingList.Count1 - 1);
                            }
                            if (existingList.Count2 == 0)
                            {
                                existingList.Slot2 = -1;
                            }
                            else
                            {
                                existingList.Count2 = (short)((int)existingList.Count2 - 1);
                            }
                            Console.WriteLine("TuneProtect: " + existingList.Slot1 + ", " + existingList.Slot2);
                            Console.WriteLine("TuneProtectCount: " + existingList.Count1 + ", " + existingList.Count2);
                            for (int i = 2; i <= 4; i++)
                            {
                                if (existingList.Slot1 != -1 && (int)(existingList.Slot1) + 2 == i)
                                {
                                }
                                else if (existingList.Slot2 != -1 && (int)(existingList.Slot2) + 2 == i)
                                {
                                }
                                else
                                {
                                    if (i == 2) existingList.Tune1 = 0;
                                    else if (i == 3) existingList.Tune2 = 0;
                                    else if (i == 4) existingList.Tune3 = 0;
                                }
                            }
                            Console.WriteLine("TuneList: " + existingList.Tune1 + ", " + existingList.Tune2 + ", " + existingList.Tune3);
                            KartExcData.AddTuneList(this.Parent.Client.Nickname, Kart, KartSN, existingList.Tune1, existingList.Tune2, existingList.Tune3, existingList.Slot1, existingList.Count1, existingList.Slot2, existingList.Count2);
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
                        KartExcData.AddPlantList(this.Parent.Client.Nickname, Kart_Id, SN, Item, Item_Id);
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
                        KartExcData.AddPartsList(this.Parent.Client.Nickname, Kart, SN, item, 0, 0, 0);
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
                        KartExcData.AddPartsList(this.Parent.Client.Nickname, Kart, KartSN, Item_Cat_Id, Item_Id, Grade, PartsValue);
                        Console.WriteLine("ClientSession : Kart: {0}, KartSN: {1}, Item: {2}:{3}, Quantity: {4}, Grade: {5}, PartsValue: {6}", Kart, KartSN, Item_Cat_Id, Item_Id, Quantity, Grade, PartsValue);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetTrainingMission", 0))
                    {
                        int type = iPacket.ReadInt();
                        uint track = iPacket.ReadUInt();
                        //byte Level = TimeAttack.GetTrackLevel(Nickname, track);
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
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteInt(TimeAttack.MissionList.Count);
                            foreach (string Track in TimeAttack.MissionList)
                            {
                                byte Level = TimeAttack.GetTrackLevel(this.Parent.Client.Nickname, Adler32Helper.GenerateAdler32_UNICODE(Track, 0));
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
                        var trackConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        trackConfig.Rider.Track = track;
                        ProfileService.Save(this.Parent.Client.Nickname, trackConfig);
                        byte SpeedType = iPacket.ReadByte();
                        byte GameType = iPacket.ReadByte();
                        TrackRankData.LoRpGetTrackRankPacket(this.Parent, track, SpeedType, GameType);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqFavoriteTrackUpdate", 0))
                    {
                        iPacket.ReadByte();
                        int tracks = iPacket.ReadInt(); //赛道数量
                        for (int i = 0; i < tracks; i++)
                        {
                            short theme = iPacket.ReadShort(); //主题代码
                            uint track = iPacket.ReadUInt(); //赛道代码
                            byte Add_Del = iPacket.ReadByte(); //1添加，2删除
                            if (Add_Del == 1)
                            {
                                FavoriteItem.Favorite_Track_Add(this.Parent.Client.Nickname, theme, track);
                            }
                            else if (Add_Del == 2)
                            {
                                FavoriteItem.Favorite_Track_Del(this.Parent.Client.Nickname, theme, track);
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqDecLucciPacket", 0))
                    {
                        iPacket.ReadByte();
                        uint Lucci = iPacket.ReadUInt();
                        var lucciConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        lucciConfig.Rider.Lucci -= Lucci;
                        ProfileService.Save(this.Parent.Client.Nickname, lucciConfig);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqStartTimeAttack", 0))
                    {
                        var attackConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        var StartTimeAttack_Unk1 = iPacket.ReadInt();
                        var StartTimeAttack_Unk2 = iPacket.ReadInt();
                        var StartTimeAttack_Track = iPacket.ReadUInt();
                        attackConfig.Rider.SpeedType = iPacket.ReadByte();
                        attackConfig.Rider.GameType = iPacket.ReadByte();
                        var Kart_id = iPacket.ReadUShort();
                        var FlyingPet_id = iPacket.ReadUShort();
                        var StartTimeAttack_StartType = iPacket.ReadByte();
                        var StartTimeAttack_Unk3 = iPacket.ReadInt();
                        var StartTimeAttack_Unk4 = iPacket.ReadInt();
                        var StartTimeAttack_Unk5 = iPacket.ReadByte();
                        attackConfig.Rider.AttackType = iPacket.ReadByte();
                        var StartTimeAttack_TimaAttackMpdeType = iPacket.ReadByte();
                        var StartTimeAttack_TimaAttackMpde = iPacket.ReadInt();
                        var StartTimeAttack_RandomTrackGameType = iPacket.ReadByte();
                        if (StartTimeAttack_TimaAttackMpdeType == 1)
                        {
                            attackConfig.Rider.Lucci -= 1000;
                        }
                        Console.WriteLine("StartTimeAttack: {0} / {1} / {2} / {3} / {4} / {5} / {6} / {7}", attackConfig.Rider.SpeedType, attackConfig.Rider.GameType, Kart_id, FlyingPet_id, RandomTrack.GetTrackName(StartTimeAttack_Track), StartTimeAttack_StartType, attackConfig.Rider.AttackType, StartTimeAttack_TimaAttackMpdeType);
                        attackConfig.Rider.Track = RandomTrack.GetRandomTrack(this.Parent, this.Parent.Client.Nickname, StartTimeAttack_RandomTrackGameType, StartTimeAttack_Track);
                        ProfileService.Save(this.Parent.Client.Nickname, attackConfig);
                        byte StartType = 3;
                        StartGameData.Start_KartSpac(this.Parent, this.Parent.Client.Nickname, StartType, StartTimeAttack_StartType, StartTimeAttack_Unk1, attackConfig.Rider.Track, attackConfig.Rider.SpeedType, Kart_id, FlyingPet_id);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqFinishTimeAttack", 0))
                    {
                        int type = iPacket.ReadInt();
                        iPacket.ReadInt();
                        byte RewardType = iPacket.ReadByte();
                        iPacket.ReadInt();
                        iPacket.ReadInt();
                        iPacket.ReadInt(); //使用加速器次数
                        iPacket.ReadInt(); //碰撞次数
                        var finishConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        finishConfig.Rider.Time = iPacket.ReadUInt();
                        var finishReward = TimeReward.FinishReward(RewardType);
                        finishConfig.Rider.RP += finishReward.RP;
                        finishConfig.Rider.Lucci += finishReward.Lucci;
                        ProfileService.Save(this.Parent.Client.Nickname, finishConfig);
                        var timeSpan = TrackRankData.GetTimeSpan(finishConfig.Rider.Time);
                        Console.WriteLine("FinishTimeAttack: {0} / {1} / {2}:{3}:{4}", RewardType, RandomTrack.GetTrackName(finishConfig.Rider.Track), timeSpan.min, timeSpan.sec, timeSpan.mil);
                        using (OutPacket outPacket = new OutPacket("PrFinishTimeAttack"))
                        {
                            outPacket.WriteInt(type);
                            if (finishConfig.Rider.AttackType == 0 && RewardType == 1)
                            {
                                byte Level = TimeAttack.TrainingMission(this.Parent.Client.Nickname, finishConfig.Rider.Track);
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
                            if (RewardType == 0)
                            {
                                outPacket.WriteUInt(10);
                                outPacket.WriteUInt(20);
                            }
                            else if (RewardType == 1)
                            {
                                outPacket.WriteUInt(20);
                                outPacket.WriteUInt(50);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        TrackRankData.AddTrackRank(finishConfig.Rider.Track, finishConfig.Rider.SpeedType, finishConfig.Rider.GameType, new TrackRank
                        {
                            UserNO = ClientManager.GetUserNO(this.Parent.Client.Nickname),
                            Nickname = this.Parent.Client.Nickname,
                            Kart = ProfileService.GetProfileConfig(this.Parent.Client.Nickname).RiderItem.Set_Kart,
                            Time = finishConfig.Rider.Time
                        });
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRewardTimeAttack", 0))
                    {
                        byte RewardType = iPacket.ReadByte();
                        int RP = iPacket.ReadInt();
                        int Lucci = iPacket.ReadInt();
                        int TimeAttack_StartTicks = iPacket.ReadInt();
                        uint Track = iPacket.ReadUInt();
                        var rewardConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        rewardConfig.Rider.Track = Track;
                        Console.WriteLine("RewardTimeAttack : ResultType: {0}, RP: {1}, Lucci: {2}, Track: {3}", RewardType, RP, Lucci, RandomTrack.GetTrackName(Track));
                        var finishReward = TimeReward.FinishReward(RewardType);
                        rewardConfig.Rider.RP += finishReward.RP;
                        rewardConfig.Rider.Lucci += finishReward.Lucci;
                        ProfileService.Save(this.Parent.Client.Nickname, rewardConfig);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoRqUseItemPacket", 0))
                    {
                        short ItemType = iPacket.ReadShort();
                        short Type = iPacket.ReadShort();
                        ushort slotChanger = iPacket.ReadUShort();
                        if (Type == 1)
                        {

                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqQuestUX2ndPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrQuestUX2ndPacket"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(1);
                            GameSupport.PrQuestUX2ndPacket(outPacket);
                            this.Parent.Client.Send(outPacket);
                        }
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
                        GameSupport.PrCheckMyClubStatePacket(this.Parent, this.Parent.Client.Nickname);
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
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        var joinClubConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        joinClubConfig.Rider.ClubMark_LOGO = 2;
                        ProfileService.Save(this.Parent.Client.Nickname, joinClubConfig);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCheckCreateClubConditionPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrCheckCreateClubConditionPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCreateClubPacket", 0))
                    {
                        var createClubConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        createClubConfig.Rider.ClubName = iPacket.ReadString();
                        createClubConfig.Rider.ClubIntro = iPacket.ReadString();
                        createClubConfig.Rider.ClubMark_LOGO = iPacket.ReadInt();
                        ProfileService.Save(this.Parent.Client.Nickname, createClubConfig);
                        using (OutPacket outPacket = new OutPacket("PrNewCareerNoticePacket"))
                        {
                            this.Parent.Client.Send(outPacket);
                        }
                        using (OutPacket outPacket = new OutPacket("PrCreateClubPacket"))
                        {
                            outPacket.WriteInt(createClubConfig.Rider.ClubCode);
                            outPacket.WriteInt(createClubConfig.Rider.ClubMark_LINE);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqInitClubPacket", 0))
                    {
                        var clubSwitchConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        using (OutPacket outPacket = new OutPacket("PrInitClubPacket"))
                        {
                            if (clubSwitchConfig.Rider.ClubMark_LOGO == 0)
                            {
                                outPacket.WriteInt(0);
                            }
                            else
                            {
                                outPacket.WriteInt(1);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqClubChannelSwitch", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrChannelSwitch"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteShort(13);
                            outPacket.WriteShort(0);
                            outPacket.WriteEndPoint(new IPEndPoint(IPAddress.Any, 0));
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqInitClubInfoPacket", 0))
                    {
                        var initClubConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        using (OutPacket outPacket = new OutPacket("PrInitClubInfoPacket"))
                        {
                            outPacket.WriteInt(initClubConfig.Rider.ClubCode);
                            outPacket.WriteString(initClubConfig.Rider.ClubName);
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteByte(5);
                            outPacket.WriteByte(1);
                            outPacket.WriteUInt(ClientManager.GetUserNO(this.Parent.Client.Nickname));
                            outPacket.WriteString(this.Parent.Client.Nickname);
                            outPacket.WriteInt(500);//最大成员数
                            outPacket.WriteHexString("00000000E803000004FFFF0000");
                            outPacket.WriteString(initClubConfig.Rider.ClubIntro);
                            outPacket.WriteInt(initClubConfig.Rider.ClubMark_LOGO);
                            outPacket.WriteInt(initClubConfig.Rider.ClubMark_LINE);
                            outPacket.WriteInt(3000000);//周活跃度
                            outPacket.WriteInt(3000000);//俱乐部活跃度
                            outPacket.WriteHexString("C0C62D0000000000");
                            outPacket.WriteInt(500);//成员数
                            outPacket.WriteHexString("000000000000D43C1B01ED7E0B00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSearchClubListPacket", 0))
                    {
                        int ClubID = iPacket.ReadInt();
                        int ClubCount = 1;
                        var searchClubConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        using (OutPacket outPacket = new OutPacket("PrSearchClubListPacket"))
                        {
                            outPacket.WriteInt(ClubCount);
                            for (int i = 0; i < ClubCount; i++)
                            {
                                outPacket.WriteInt(searchClubConfig.Rider.ClubCode);
                                outPacket.WriteString(searchClubConfig.Rider.ClubName);
                                outPacket.WriteByte(5);
                                outPacket.WriteInt(500);//最大成员数
                                outPacket.WriteHexString("FFFFFFFF");
                                outPacket.WriteInt(3000000);//活跃度
                                outPacket.WriteInt(3000000);//最大活跃度
                                outPacket.WriteInt(searchClubConfig.Rider.ClubMark_LOGO);
                                outPacket.WriteInt(0);
                                outPacket.WriteString(this.Parent.Client.Nickname);
                                outPacket.WriteInt(500);//成员数
                                outPacket.WriteDateTime(DateTime.Now);
                                outPacket.WriteString(searchClubConfig.Rider.ClubIntro);
                                outPacket.WriteShort(0);
                                outPacket.WriteInt(3000000);//周活跃度
                            }
                            outPacket.WriteByte(1);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetClubListCountPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetClubListCountPacket"))
                        {
                            outPacket.WriteHexString("01 00 00 00 01 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetClubWaitingCrewCountPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetClubWaitingCrewCountPacket"))
                        {
                            outPacket.WriteHexString("01 00 00 00 01 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqClubInitFirstPacket", 0))
                    {
                        int ClubMemberCount = 1;
                        var initFirstConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        using (OutPacket outPacket = new OutPacket("PrClubInitFirstPacket"))
                        {
                            outPacket.WriteInt(initFirstConfig.Rider.ClubCode);
                            outPacket.WriteString(initFirstConfig.Rider.ClubName);
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteByte(5);
                            outPacket.WriteByte(1);
                            outPacket.WriteUInt(ClientManager.GetUserNO(this.Parent.Client.Nickname));
                            outPacket.WriteString(this.Parent.Client.Nickname);
                            outPacket.WriteInt(500);//最大成员数
                            outPacket.WriteHexString("00000000E803000004FFFF0000");
                            outPacket.WriteString(initFirstConfig.Rider.ClubIntro);
                            outPacket.WriteInt(initFirstConfig.Rider.ClubMark_LOGO);
                            outPacket.WriteInt(initFirstConfig.Rider.ClubMark_LINE);
                            outPacket.WriteInt(3000000);//周活跃度
                            outPacket.WriteInt(3000000);//俱乐部活跃度
                            outPacket.WriteHexString("C0C62D0000000000");
                            outPacket.WriteInt(500);//成员数
                            outPacket.WriteHexString("00000000A11100008B170000D20A000004100000");
                            outPacket.WriteInt(ClubMemberCount);
                            for (int i = 0; i < ClubMemberCount; i++)
                            {
                                outPacket.WriteInt(initFirstConfig.Rider.ClubCode);
                                outPacket.WriteUInt(ClientManager.GetUserNO(this.Parent.Client.Nickname));
                                outPacket.WriteString(this.Parent.Client.Nickname);
                                outPacket.WriteUInt(initFirstConfig.Rider.RP);
                                outPacket.WriteShort(5);//职位
                                outPacket.WriteUInt(3000000);//周活跃度
                                outPacket.WriteUInt(3000000);//累计活跃度
                                outPacket.WriteDateTime(DateTime.Now);
                                outPacket.WriteHexString("FFFF0000");
                                outPacket.WriteDateTime(DateTime.Now);
                                outPacket.WriteDateTime(DateTime.Now);
                                outPacket.WriteInt(0);
                            }
                            outPacket.WriteInt(initFirstConfig.Rider.ClubCode);
                            outPacket.WriteUInt(ClientManager.GetUserNO(this.Parent.Client.Nickname));
                            outPacket.WriteString(this.Parent.Client.Nickname);
                            outPacket.WriteUInt(initFirstConfig.Rider.RP);
                            outPacket.WriteShort(5);//职位
                            outPacket.WriteUInt(3000000);//周活跃度
                            outPacket.WriteUInt(3000000);//累计活跃度
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteHexString("FFFF0000");
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteHexString("D43C1B01ED7E0B00");
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteInt(0);
                            outPacket.WriteHexString("22394703A87F0B00880AFB03A67F0B0000000000");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetClubRealCrewListPacket", 0))
                    {
                        int ClubID = iPacket.ReadInt();
                        int ClubMemberCount = 1;
                        using (OutPacket outPacket = new OutPacket("PrGetClubRealCrewListPacket"))
                        {
                            outPacket.WriteInt(ClubMemberCount);
                            for (int i = 0; i < ClubMemberCount; i++)
                            {
                                outPacket.WriteInt(ClubID);
                                outPacket.WriteUInt(ClientManager.GetUserNO(this.Parent.Client.Nickname));
                                outPacket.WriteString(this.Parent.Client.Nickname);
                                outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.RP);
                                outPacket.WriteShort(5);//职位
                                outPacket.WriteUInt(3000000);//周活跃度
                                outPacket.WriteUInt(3000000);//累计活跃度
                                outPacket.WriteDateTime(DateTime.Now);
                                outPacket.WriteHexString("FFFF0000");
                                outPacket.WriteDateTime(DateTime.Now);
                                outPacket.WriteDateTime(DateTime.Now);
                                outPacket.WriteInt(0);
                            }
                            outPacket.WriteInt(300);
                            outPacket.WriteInt(ClubID);
                            outPacket.WriteUInt(ClientManager.GetUserNO(this.Parent.Client.Nickname));
                            outPacket.WriteString(this.Parent.Client.Nickname);
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.RP);
                            outPacket.WriteShort(5);//职位
                            outPacket.WriteUInt(3000000);//周活跃度
                            outPacket.WriteUInt(3000000);//累计活跃度
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteHexString("FFFF0000");
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteHexString("22394703A87F0B00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqCheckClubDirtyTimePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrCheckClubDirtyTimePacket"))
                        {
                            outPacket.WriteHexString("D43C1B01ED7E0B00");
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteInt(0);
                            outPacket.WriteHexString("22394703A87F0B00880AFB03A67F0B0000000000000000000000000000000000");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetClubRecordPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetClubRecordPacket"))
                        {
                            outPacket.WriteHexString("00000000000000000000000000000000");
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqBlockWordLogPacket", 0))
                    {
                        iPacket.ReadString();
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqLeaveClubPacket", 0))
                    {
                        int ClubID = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrLeaveClubPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(ClubID);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqBreakUpClubPacket", 0))
                    {
                        int ClubID = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrBreakUpClubPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(ClubID);
                            this.Parent.Client.Send(outPacket);
                        }
                        var profileConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        profileConfig.Rider.ClubMark_LOGO = 0;
                        ProfileService.Save(this.Parent.Client.Nickname, profileConfig);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChangeClubAutoJoinStatePacket", 0))
                    {
                        byte AutoJoin = iPacket.ReadByte();
                        using (OutPacket outPacket = new OutPacket("PrChangeClubAutoJoinStatePacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(AutoJoin);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqInitClubHousePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrInitClubHousePacket"))
                        {
                            outPacket.WriteString(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.ClubName);
                            outPacket.WriteByte(5);
                            outPacket.WriteByte(5);
                            outPacket.WriteInt(500);
                            outPacket.WriteInt(500);
                            outPacket.WriteInt(3000000);
                            outPacket.WriteInt(1000);
                            outPacket.WriteInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.ClubMark_LOGO);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqInitClubHQPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrInitClubHQPacket"))
                        {
                            outPacket.WriteHexString("00000000000000000000000000F4010000");
                            outPacket.WriteInt(10);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqInitClubBankPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrInitClubBankPacket"))
                        {
                            outPacket.WriteInt(1000);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(4);
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(3);
                            outPacket.WriteInt(5);
                            outPacket.WriteInt(10);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqClubSupportPacket", 0))
                    {
                        int LUCCI = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrClubSupportPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(LUCCI);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Lucci);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqInitRacingCenterPacket", 0))
                    {
                        byte[] levels = new byte[] { 2, 3, 4, 5, 0 };
                        using (OutPacket outPacket = new OutPacket("PrInitRacingCenterPacket"))
                        {
                            outPacket.WriteShort(0);
                            outPacket.WriteInt(5);
                            foreach (var i in levels)
                            {
                                outPacket.WriteByte(i);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                            }
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqInitRiderCenterPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrInitRiderCenterPacket"))
                        {
                            outPacket.WriteHexString("0232000000E09304000000000000000000");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChangeClubNamePacket", 0))
                    {
                        string oldClubName = iPacket.ReadString();
                        string newClubName = iPacket.ReadString();
                        using (OutPacket outPacket = new OutPacket("PrChangeClubNamePacket"))
                        {
                            outPacket.WriteString(newClubName);
                            outPacket.WriteInt(1000);
                            this.Parent.Client.Send(outPacket);
                            var changeNameConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                            changeNameConfig.Rider.ClubName = newClubName;
                            ProfileService.Save(this.Parent.Client.Nickname, changeNameConfig);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChangeClubMarkPacket", 0))
                    {
                        int oldClubMark = iPacket.ReadInt();
                        int newClubMark = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("PrChangeClubMarkPacket"))
                        {
                            outPacket.WriteInt(newClubMark);
                            outPacket.WriteInt(1000);
                            this.Parent.Client.Send(outPacket);
                            var changeMarkConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                            changeMarkConfig.Rider.ClubMark_LOGO = newClubMark;
                            ProfileService.Save(this.Parent.Client.Nickname, changeMarkConfig);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChangeClubIntroPacket", 0))
                    {
                        var changeIntroConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        changeIntroConfig.Rider.ClubIntro = iPacket.ReadString();
                        ProfileService.Save(this.Parent.Client.Nickname, changeIntroConfig);
                        using (OutPacket outPacket = new OutPacket("PrChangeClubIntroPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqLotteryPacket", 0))
                    {
                        short Lottery_Item = iPacket.ReadShort();
                        byte Unk = iPacket.ReadByte();
                        int Type = iPacket.ReadInt();
                        if (Bingo.BingoLotteryIDs.Contains(Lottery_Item))
                        {
                            Bingo.SpRpLotteryPacket(this.Parent);
                        }
                        else
                        {
                            GameSupport.SpRpLotteryPacket(this.Parent);
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
                        EventBuyCount.PrEventBuyCount(this.Parent);
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
                        RiderSchool.PrRiderSchoolData(this.Parent);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRiderSchoolProPacket", 0))
                    {
                        RiderSchool.PrRiderSchoolPro(this.Parent);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqStartRiderSchool", 0))
                    {
                        RiderSchool.PrStartRiderSchool(this.Parent, this.Parent.Client.Nickname);
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
                            outPacket.WriteByte(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Ranker);
                            outPacket.WriteHexString("00 00 C8 42 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRequestExtradata", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrRequestExtradata"))
                        {
                            outPacket.WriteHexString("00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChRequestChStaticRequestPacket", 0))
                    {
                        GameSupport.ChRequestChStaticReplyPacket(this.Parent);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqDynamicCommand", 0))
                    {
                        GameSupport.PrDynamicCommand(this.Parent);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqPubCommandPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrPubCommandPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteTime(DateTime.Now);
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
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Koin);
                            outPacket.WriteUInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqFavoriteTrackMapGet", 0))
                    {
                        FavoriteItem.Favorite_Track(this.Parent, this.Parent.Client.Nickname);
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
                            outPacket.WriteHexString("00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00");
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
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Cash);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqRemainTcCashPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("SpRpRemainTcCashPacket"))
                        {
                            outPacket.WriteUInt(99);
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.TcCash);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpReqNormalShopBuyItemPacket", 0) || hash == Adler32Helper.GenerateAdler32_ASCII("SpReqItemPresetShopBuyItemPacket", 0))
                    {
                        int stockId = iPacket.ReadInt();
                        int unk = iPacket.ReadInt();
                        byte mode = iPacket.ReadByte();//货币类型0:电池 1:金币 3:KOIN
                        using (OutPacket outPacket = new OutPacket("SpRepBuyItemPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Lucci);
                            outPacket.WriteHexString("00 00 00 00");
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Koin);
                            outPacket.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetCurrentRid", 0))
                    {
                        string UserName = iPacket.ReadString();
                        if (Directory.Exists(Path.GetFullPath(Path.Combine(FileName.ProfileDir, UserName))))
                        {
                            using (OutPacket outPacket = new OutPacket("PrGetCurrentRid"))
                            {
                                outPacket.WriteString(UserName);
                                this.Parent.Client.Send(outPacket);
                            }
                        }
                        else
                        {
                            using (OutPacket outPacket = new OutPacket("PrGetCurrentRid"))
                            {
                                outPacket.WriteInt(0);
                                this.Parent.Client.Send(outPacket);
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqDuplicatedItemPacket", 0))
                    {
                        string UserName = iPacket.ReadString();
                        int stockId = iPacket.ReadInt();
                        CouponList.DuplicatedItem(this.Parent, stockId);
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpReqNormalShopGiveGiftPacket", 0))
                    {
                        string UserName = iPacket.ReadString();
                        int stockId = iPacket.ReadInt();
                        iPacket.ReadInt();
                        string Message = iPacket.ReadString();
                        iPacket.ReadInt();
                        iPacket.ReadInt();
                        string Password = iPacket.ReadString();
                        CouponList.GiveGift(this.Parent, UserName, stockId, Message, Password);
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
                            outPacket.WriteBytes(new byte[32]);
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
                        CouponList.GetRewardBox(this.Parent);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqReceiveRewardItemPacket", 0))
                    {
                        long RewardBoxId = iPacket.ReadLong();
                        int stockId = iPacket.ReadInt();
                        CouponList.ReceiveReward(this.Parent, RewardBoxId, stockId);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqExitRewardBoxStage", 0))
                    {
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqGetGiftListIncomingPacket", 0))
                    {
                        CouponList.GetGiftList(this.Parent);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqGetGiftListReceivedPacket", 0))
                    {
                        CouponList.GetGiftList(this.Parent, true);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpReqReceiveGiftPacket", 0))
                    {
                        int giftId = iPacket.ReadInt();
                        CouponList.ReceiveGift(this.Parent, giftId);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpReqDeleteGiftPacket", 0))
                    {
                        iPacket.ReadInt();
                        iPacket.ReadInt();
                        string Password = iPacket.ReadString();
                        int giftId = iPacket.ReadInt();
                        CouponList.Delete(this.Parent, giftId);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpReqDeleteReceiveLogPacket", 0))
                    {
                        int giftId = iPacket.ReadInt();
                        CouponList.Delete(this.Parent, giftId, true);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetCompetitiveRankInfo", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetCompetitiveRankInfo"))
                        {
                            outPacket.WriteHexString("01 00 00 00 00 FF 00 00 00 00 00 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetCompetitiveSlotInfo", 0))
                    {
                        var manager = new CompetitiveDataManager();
                        var competitiveData = manager.LoadAllData(this.Parent.Client.Nickname);
                        using (OutPacket outPacket = new OutPacket("PrGetCompetitiveSlotInfo"))
                        {
                            outPacket.WriteInt(competitiveData.Count);
                            foreach (var competitive in competitiveData)
                            {
                                outPacket.WriteUInt(competitive.Track);
                                outPacket.WriteUInt(competitive.Track);
                                outPacket.WriteUShort(competitive.Kart);
                                outPacket.WriteUInt(competitive.Time);
                                outPacket.WriteHexString("FF FF FF FF");
                                outPacket.WriteShort(competitive.Booster);
                                outPacket.WriteUInt(competitive.BoosterPoint);
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
                            foreach (var track in TimeAttack.Competitive)
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
                        Emblem.RmOwnerEmblemPacket(this.Parent);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("RmRqUpdateMainEmblemPacket", 0))
                    {
                        var emblemConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        emblemConfig.Rider.Emblem1 = iPacket.ReadShort();
                        emblemConfig.Rider.Emblem2 = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("RmRpUpdateMainEmblemPacket"))
                        {
                            outPacket.WriteByte(1);
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        ProfileService.Save(this.Parent.Client.Nickname, emblemConfig);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSyncDictionaryInfoPacket", 0))
                    {
                        int Dictionary = iPacket.ReadInt();
                        int Count = GameSupport.Dictionary.Count;
                        using (OutPacket outPacket = new OutPacket("PrSyncDictionaryInfoPacket"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(Count);
                            foreach (var item in GameSupport.Dictionary)
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
                        ushort ItemType = iPacket.ReadUShort();
                        ushort ItemID = iPacket.ReadUShort();
                        ushort SN = iPacket.ReadUShort();
                        using (OutPacket outPacket = new OutPacket("LoRpDeleteItemPacket"))
                        {
                            this.Parent.Client.Send(outPacket);
                        }
                        if (ItemType == 3)
                        {
                            NewRider.DelNewKart(this.Parent.Client.Nickname, ItemID, SN);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqQueryCoupon", 0))
                    {
                        string Coupon = iPacket.ReadString();
                        CouponList.QueryCoupon(this.Parent, Coupon);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqUseCoupon", 0))
                    {
                        string Coupon = iPacket.ReadString();
                        CouponList.QueryCoupon(this.Parent, Coupon, true);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqShopCashPage", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrShopCashPage"))
                        {
                            outPacket.WriteString("https://yanygm.github.io/Launcher_V2/");
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
                            outPacket.WriteString("https://yanygm.github.io/Launcher_V2/");
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
                            //outPacket.WriteHexString("00 00 00 00 00 00 00 00 01 00 00 00 01 00 00 00");
                            for (int i = 0; i < 14; i++)
                            {
                                outPacket.WriteInt(0);
                            }
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
                        string password = iPacket.ReadString();
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
                        FavoriteItem.Favorite_Item(this.Parent, this.Parent.Client.Nickname);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqFavoriteItemUpdate", 0))
                    {
                        iPacket.ReadByte();
                        int j = iPacket.ReadInt();
                        for (int i = 0; i < j; i++)
                        {
                            ushort item = iPacket.ReadUShort();
                            ushort id = iPacket.ReadUShort();
                            ushort sn = iPacket.ReadUShort();
                            byte Add_Del = iPacket.ReadByte();
                            if (Add_Del == 1)
                            {
                                FavoriteItem.Favorite_Item_Add(this.Parent.Client.Nickname, item, id, sn);
                            }
                            else if (Add_Del == 2)
                            {
                                FavoriteItem.Favorite_Item_Del(this.Parent.Client.Nickname, item, id, sn);
                            }
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqLockedItemGet", 0))//아이템 보호
                    {
                        LockedItem.LockedItemGet(this.Parent, this.Parent.Client.Nickname);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqLockedItemUpdate", 0))
                    {
                        iPacket.ReadByte();
                        int j = iPacket.ReadInt();
                        for (int i = 0; i < j; i++)
                        {
                            ushort item = iPacket.ReadUShort();
                            ushort id = iPacket.ReadUShort();
                            ushort sn = iPacket.ReadUShort();
                            byte Add_Del = iPacket.ReadByte();
                            if (Add_Del == 1)
                            {
                                LockedItem.LockedItem_Add(this.Parent.Client.Nickname, item, id, sn);
                            }
                            else if (Add_Del == 2)
                            {
                                LockedItem.LockedItem_Del(this.Parent.Client.Nickname, item, id, sn);
                            }
                        }
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
                            outPacket.WriteInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.SlotChanger);
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
                        TestServer.TestServerAddItem(this.Parent);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqServerTime", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrServerTime"))
                        {
                            outPacket.WriteDateTime(DateTime.Now);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqLogin", 0))
                    {
                        GameDataReset.DataReset(this.Parent.Client.Nickname);

                        bool PcMsgPassport = false;

                        using (OutPacket outPacket = new OutPacket("PrLogin"))
                        {
                            outPacket.WriteInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.BanType);
                            outPacket.WriteDateTime(DateTime.Now);
                            outPacket.WriteUInt(ClientManager.GetUserNO(this.Parent.Client.Nickname));
                            outPacket.WriteString(this.Parent.Client.Nickname); // UserID
                            outPacket.WriteByte(2); // 2
                            outPacket.WriteByte(1);
                            outPacket.WriteByte(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(0);
                            outPacket.WriteHexString("FF FF 5F 54");
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.pmap);
                            for (int i = 0; i < 11; i++)
                            {
                                outPacket.WriteInt(0);
                            }
                            outPacket.WriteByte(0);
                            outPacket.WriteEndPoint(IPAddress.Any, ProfileService.SettingConfig.ServerPort);
                            outPacket.WriteEndPoint(IPAddress.Any, (ushort)(ProfileService.SettingConfig.ServerPort + 1));
                            outPacket.WriteByte(0);
                            outPacket.WriteByte(0);
                            outPacket.WriteByte((byte)(PcMsgPassport ? 1 : 0));
                            outPacket.WriteByte(0);
                            outPacket.WriteString("");
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(1);
                            outPacket.WriteString("content");
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(1);
                            outPacket.WriteString("cc");
                            outPacket.WriteString(KartRhoFile.regionCode);
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
                            outPacket.WriteString(ProfileService.SettingConfig.LocaleID.ToString());
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
                            outPacket.WriteString("https://yanygm.github.io/Launcher_V2/");
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
                            outPacket.WriteByte(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).GameOption.Set_screen);
                            outPacket.WriteByte(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.IdentificationType);
                            this.Parent.Client.Send(outPacket);
                        }
                        if (PcMsgPassport)
                        {
                            using (OutPacket outPacket = new OutPacket("PcMsgPassport"))
                            {
                                outPacket.WriteString(this.Parent.Client.Nickname);
                                outPacket.WriteString("2000142541");
                                outPacket.WriteString("meemllpigrppochjepdmgclfekpocdikifemgnkddeierhkekiefcidhrreienedmrcemjcjpqmjpkolfjfpggiqiefelqleondcpennpmpfenhpfomdpfqdpdpggdjm", true);
                                this.Parent.Client.Send(outPacket);
                            }
                        }
                        if (ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.BanType == 1)
                        {
                            var banConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                            banConfig.Rider.BanType = 0;
                            ProfileService.Save(this.Parent.Client.Nickname, banConfig);
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
                        uint unk1 = iPacket.ReadUInt();
                        byte unk2 = iPacket.ReadByte();
                        using (OutPacket outPacket = new OutPacket("PrTcCashEventUserInfoPacket"))
                        {
                            outPacket.WriteUInt(unk1);
                            outPacket.WriteByte(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqServerSideUdpBindCheck", 0))
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
                            outPacket.WriteInt();
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqBoomhillExchangeNeedNotice", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrBoomhillExchangeNeedNotice"))
                        {
                            outPacket.WriteHexString("00 00 00 00 01");
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
                        short Type = iPacket.ReadShort();
                        int Value = iPacket.ReadInt();
                        using (OutPacket outPacket = new OutPacket("RpBoomhillExchangeKoin"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(1);
                            outPacket.WriteShort(Type);
                            outPacket.WriteInt(Value);
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("SpRqUseInitialCardPacket", 0))
                    {
                        var cardConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        cardConfig.Rider.Card = iPacket.ReadString();
                        ProfileService.Save(this.Parent.Client.Nickname, cardConfig);
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
                        if (!FileName.FileNames.ContainsKey(this.Parent.Client.Nickname))
                        {
                            FileName.Load(this.Parent.Client.Nickname);
                        }
                        var filename = FileName.FileNames[this.Parent.Client.Nickname];
                        var Level12List = new List<Level12>();
                        if (File.Exists(filename.Level12Data_LoadFile))
                        {
                            Level12List = JsonHelper.DeserializeNoBom<List<Level12>>(filename.Level12Data_LoadFile) ?? new List<Level12>();
                        }
                        var existingParts = Level12List.FirstOrDefault(level12 => level12.ID == kart && level12.SN == sn);
                        if (existingParts != null)
                        {
                            using (OutPacket outPacket = new OutPacket("PrKart12TuningLevelUp"))
                            {
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(1);
                                outPacket.WriteShort(3);
                                outPacket.WriteShort(kart);
                                outPacket.WriteShort(sn);
                                short Level = (short)((int)existingParts.Grade + 1);
                                existingParts.Grade = Level;
                                outPacket.WriteShort(Level);//1-1,2-3,3-6,4-10,5-15
                                short Point = (short)((int)existingParts.SkillPoints + (int)Level);
                                existingParts.SkillPoints = Point;
                                outPacket.WriteShort(Point);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(4);
                                outPacket.WriteShort(4);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteInt(0);
                                outPacket.WriteUInt(money[(int)Level]);
                                outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Lucci);
                                this.Parent.Client.Send(outPacket);
                                KartExcData.AddLevel12List(this.Parent.Client.Nickname, kart, sn, Level, -1, -1, -1, Point);
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
                                outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Lucci);
                                this.Parent.Client.Send(outPacket);
                                KartExcData.AddLevel12List(this.Parent.Client.Nickname, kart, sn, 5, 0, 0, 0, 15);
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
                        if (!FileName.FileNames.ContainsKey(this.Parent.Client.Nickname))
                        {
                            FileName.Load(this.Parent.Client.Nickname);
                        }
                        var filename = FileName.FileNames[this.Parent.Client.Nickname];
                        var Level12List = new List<Level12>();
                        if (File.Exists(filename.Level12Data_LoadFile))
                        {
                            Level12List = JsonHelper.DeserializeNoBom<List<Level12>>(filename.Level12Data_LoadFile) ?? new List<Level12>();
                        }
                        var existingParts = Level12List.FirstOrDefault(level12 => level12.ID == kart && level12.SN == sn);
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
                            short point = existingParts.SkillPoints;
                            short skill = 0;
                            short skilllevel = 0;
                            switch (field)
                            {
                                case 1:
                                    skill = existingParts.Skill1;
                                    skilllevel = existingParts.SkillGrade1;
                                    break;
                                case 2:
                                    skill = existingParts.Skill2;
                                    skilllevel = existingParts.SkillGrade2;
                                    break;
                                case 3:
                                    skill = existingParts.Skill3;
                                    skilllevel = existingParts.SkillGrade3;
                                    break;
                            }
                            KartExcData.AddLevel12List(this.Parent.Client.Nickname, kart, sn, -1, field, skill, 0, (short)(point + skilllevel));
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
                        KartExcData.AddLevel12List(this.Parent.Client.Nickname, kart, sn, -1, field, Skill, -1, -1);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqKart12TuningPointUpdate", 0))
                    {
                        short kart = iPacket.ReadShort();
                        short sn = iPacket.ReadShort();
                        short field = iPacket.ReadShort();
                        byte AddDel = iPacket.ReadByte();
                        if (!FileName.FileNames.ContainsKey(this.Parent.Client.Nickname))
                        {
                            FileName.Load(this.Parent.Client.Nickname);
                        }
                        var filename = FileName.FileNames[this.Parent.Client.Nickname];
                        var Level12List = new List<Level12>();
                        if (File.Exists(filename.Level12Data_LoadFile))
                        {
                            Level12List = JsonHelper.DeserializeNoBom<List<Level12>>(filename.Level12Data_LoadFile) ?? new List<Level12>();
                        }
                        var existingParts = Level12List.FirstOrDefault(level12 => level12.ID == kart && level12.SN == sn);
                        if (existingParts != null)
                        {
                            short point = existingParts.SkillPoints;
                            short skilllevel = 0;
                            switch (field)
                            {
                                case 1:
                                    skilllevel = existingParts.SkillGrade1;
                                    break;
                                case 2:
                                    skilllevel = existingParts.SkillGrade2;
                                    break;
                                case 3:
                                    skilllevel = existingParts.SkillGrade3;
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
                                KartExcData.AddLevel12List(this.Parent.Client.Nickname, kart, sn, -1, field, -1, (short)((int)skilllevel + 1), (short)((int)point - 1));
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
                                KartExcData.AddLevel12List(this.Parent.Client.Nickname, kart, sn, -1, field, -1, (short)((int)skilllevel - 1), (short)((int)point + 1));
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
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Lucci);
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
                            KartExcData.AddPartsList(this.Parent.Client.Nickname, kart, sn, Item_Cat_Id, (short)(old + 1), V2Specs.GetGrade((byte)(old + 1)), V2Specs.Get12Parts((short)(old + 1)));
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
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Lucci);
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
                            KartExcData.AddPartsList(this.Parent.Client.Nickname, kart, sn, Item_Cat_Id, 1, 4, 201);
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

                        int ExceedType1 = 0;
                        if (!FileName.FileNames.ContainsKey(this.Parent.Client.Nickname))
                        {
                            FileName.Load(this.Parent.Client.Nickname);
                        }
                        var filename = FileName.FileNames[this.Parent.Client.Nickname];
                        var Parts12List = new List<Parts12>();
                        if (File.Exists(filename.Level12Data_LoadFile))
                        {
                            Parts12List = JsonHelper.DeserializeNoBom<List<Parts12>>(filename.Parts12Data_LoadFile) ?? new List<Parts12>();
                        }
                        var targetPart = Parts12List.FirstOrDefault(parts => parts.ID == kart1 && parts.SN == sn1);
                        if (targetPart != null)
                        {
                            ExceedType1 = targetPart.ExceedType;
                        }
                        Random random = new Random();
                        int index = random.Next(V2Specs.ExceedTypes.Length);
                        int ExceedType2 = V2Specs.ExceedTypes[index];
                        while (ExceedType2 == ExceedType1)
                        {
                            index = random.Next(V2Specs.ExceedTypes.Length);
                            ExceedType2 = V2Specs.ExceedTypes[index];
                        }
                        //int ExceedType = Random.Shared.Next(2, 5);
                        using (OutPacket outPacket = new OutPacket("PrKartExceedTypeChange"))
                        {
                            outPacket.WriteShort(0);//kart2
                            outPacket.WriteShort(0);//sn2
                            outPacket.WriteShort(Spanner);
                            outPacket.WriteUInt(ProfileService.GetProfileConfig(this.Parent.Client.Nickname).Rider.Lucci);
                            outPacket.WriteInt(ExceedType2);
                            this.Parent.Client.Send(outPacket);
                        }
                        KartExcData.AddPartsList(this.Parent.Client.Nickname, kart1, sn1, 0, (short)ExceedType2, 0, 0);
                        Console.WriteLine("ExceedType: " + ExceedType2);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqMissionAttendNRUserStatePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrMissionAttendNRUserStatePacket"))
                        {
                            outPacket.WriteHexString("64 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqMissionAttendUserStatePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrMissionAttendUserStatePacket"))
                        {
                            outPacket.WriteHexString("FC 00");
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
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqReturnMissionSetPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrReturnMissionSetPacket"))
                        {
                            outPacket.WriteHexString("00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqPromiseEventEnterPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrPromiseEventEnterPacket"))
                        {
                            outPacket.WriteHexString("00 00 00 00 00 00 00 FF FF 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqReturnMissionRequestDataPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrReturnMissionRequestDataPacket"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqHitPangPangEnterPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrHitPangPangEnterPacket"))
                        {
                            outPacket.WriteHexString("04 00 00 00 00 22 00 8C 02 06 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqHitPangPangInitPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrHitPangPangInitPacket"))
                        {
                            outPacket.WriteHexString("04000000002B000000000D070178000000002C240000013A07017800000000AB1F000000120701780000000061490000001307017800000000AE5300000013070178000000007F4700000010070178000000002972000000270701780000000071340000013D07017800000000F3480000020607012C0100000013750000011B07017800000000CF020000010E0701780000000045450000013F07017800000000F40B0000014807017800000000AD060000011B070178000000004D3E0000014807017800000000192A0000000007017800000000531A0000000007017800000000C4100000020607012C01000000A0570000000907017800000000EA19000001420701780000000083640000001707013C000000006E060000001007017800000000330E0000013C07017800000000803F000000000701780000000087130000002007017800000000872E000001480701780000000033620000020607012C01000000B9030000002007017800000000F24200000009070178000000002456000001410701780000000032530000013F0701780000000018430000001E070178000000001A6B0000002707017800000000376C0000000907017800000000FF040000001707013C000000008D3A0000020207012C010000009E770000001707013C0000000082720000000D07017800000000C366000001410701780000000085370000013D070178000000001B460000002007017800000000C06F0000013A0701780000000060380000012407017800000000355700005906000050C30000102700003333333F0000803F00000040");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqHitPangPangResultPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrHitPangPangResultPacket"))
                        {
                            outPacket.WriteHexString("04 00 00 00 FF FF FF FF D0 02 00 00 01");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqPlaytimePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrPlaytimePacket"))
                        {
                            outPacket.WriteByte(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqPersonalShopUserDataPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrPersonalShopUserDataPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqTimeShopOpenTimePacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrTimeShopOpenTimePacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqItemPresetSlotDataList", 0))
                    {
                        var itemPresetConfig = ItemPresetsService.GetItemPresetConfig(this.Parent.Client.Nickname);
                        using (OutPacket outPacket = new OutPacket("PrItemPresetSlotDataList"))
                        {
                            outPacket.WriteInt(itemPresetConfig.ItemPresets.Count);
                            foreach (var ItemPreset in itemPresetConfig.ItemPresets)
                            {
                                outPacket.WriteUShort(ItemPreset.ID);
                                outPacket.WriteUShort(ItemPreset.ID);
                                outPacket.WriteByte(ItemPreset.Badge);
                                if (ItemPreset.ID < 4)
                                {
                                    outPacket.WriteInt(0);
                                    outPacket.WriteHexString("44 B0 46 23");
                                }
                                else
                                {
                                    outPacket.WriteInt(27125 + ItemPreset.ID);
                                    outPacket.WriteHexString("44 B0 46 23");
                                }
                                outPacket.WriteByte(ItemPreset.Enable);
                                outPacket.WriteString(ItemPreset.Name);
                                outPacket.WriteUShort(ItemPreset.Character);
                                outPacket.WriteUShort(ItemPreset.Paint);
                                outPacket.WriteUShort(ItemPreset.Kart);
                                outPacket.WriteUShort(ItemPreset.Plate);
                                outPacket.WriteUShort(ItemPreset.Goggle);
                                outPacket.WriteUShort(ItemPreset.Balloon);
                                outPacket.WriteUShort(ItemPreset.Unknown1);
                                outPacket.WriteUShort(ItemPreset.HeadBand);
                                outPacket.WriteUShort(ItemPreset.HeadPhone);
                                outPacket.WriteUShort(ItemPreset.HandGearL);
                                outPacket.WriteUShort(ItemPreset.Unknown2);
                                outPacket.WriteUShort(ItemPreset.Uniform);
                                outPacket.WriteUShort(ItemPreset.Decal);
                                outPacket.WriteUShort(ItemPreset.Pet);
                                outPacket.WriteUShort(ItemPreset.FlyingPet);
                                outPacket.WriteUShort(ItemPreset.Aura);
                                outPacket.WriteUShort(ItemPreset.SkidMark);
                                outPacket.WriteUShort(ItemPreset.SpecialKit);
                                outPacket.WriteUShort(ItemPreset.RidColor);
                                outPacket.WriteUShort(ItemPreset.BonusCard);
                                outPacket.WriteUShort(ItemPreset.BossModeCard);
                                outPacket.WriteUShort(ItemPreset.KartPlant1);
                                outPacket.WriteUShort(ItemPreset.KartPlant2);
                                outPacket.WriteUShort(ItemPreset.KartPlant3);
                                outPacket.WriteUShort(ItemPreset.KartPlant4);
                                outPacket.WriteUShort(ItemPreset.Unknown3);
                                outPacket.WriteUShort(ItemPreset.FishingPole);
                                outPacket.WriteUShort(ItemPreset.Tachometer);
                                outPacket.WriteUShort(ItemPreset.Dye);
                                outPacket.WriteUShort(ItemPreset.KartSN);
                                outPacket.WriteByte(ItemPreset.Unknown4);
                                outPacket.WriteUShort(ItemPreset.KartCoating);
                                outPacket.WriteUShort(ItemPreset.KartTailLamp);
                                outPacket.WriteUShort(ItemPreset.slotBg);
                                outPacket.WriteUShort(ItemPreset.KartCoating12);
                                outPacket.WriteUShort(ItemPreset.KartTailLamp12);
                                outPacket.WriteUShort(ItemPreset.KartBoosterEffect12);
                                outPacket.WriteUShort(ItemPreset.Unknown5);
                            }
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqItemPresetUpdateSlotData", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrItemPresetUpdateSlotData"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        var itemPresetConfig = ItemPresetsService.GetItemPresetConfig(this.Parent.Client.Nickname);
                        iPacket.ReadInt();
                        short id1 = iPacket.ReadShort();
                        short id2 = iPacket.ReadShort();
                        var ItemPreset = itemPresetConfig.ItemPresets.FirstOrDefault(ItemPreset => ItemPreset.ID == id1 && ItemPreset.ID == id2);
                        if (ItemPreset != null)
                        {
                            ItemPreset.Badge = iPacket.ReadByte();
                            iPacket.ReadBytes(8);
                            ItemPreset.Enable = iPacket.ReadByte();
                            ItemPreset.Name = iPacket.ReadString();
                            ItemPreset.Character = iPacket.ReadUShort();
                            ItemPreset.Paint = iPacket.ReadUShort();
                            ItemPreset.Kart = iPacket.ReadUShort();
                            ItemPreset.Plate = iPacket.ReadUShort();
                            ItemPreset.Goggle = iPacket.ReadUShort();
                            ItemPreset.Balloon = iPacket.ReadUShort();
                            ItemPreset.Unknown1 = iPacket.ReadUShort();
                            ItemPreset.HeadBand = iPacket.ReadUShort();
                            ItemPreset.HeadPhone = iPacket.ReadUShort();
                            ItemPreset.HandGearL = iPacket.ReadUShort();
                            ItemPreset.Unknown2 = iPacket.ReadUShort();
                            ItemPreset.Uniform = iPacket.ReadUShort();
                            ItemPreset.Decal = iPacket.ReadUShort();
                            ItemPreset.Pet = iPacket.ReadUShort();
                            ItemPreset.FlyingPet = iPacket.ReadUShort();
                            ItemPreset.Aura = iPacket.ReadUShort();
                            ItemPreset.SkidMark = iPacket.ReadUShort();
                            ItemPreset.SpecialKit = iPacket.ReadUShort();
                            ItemPreset.RidColor = iPacket.ReadUShort();
                            ItemPreset.BonusCard = iPacket.ReadUShort();
                            ItemPreset.BossModeCard = iPacket.ReadUShort();
                            ItemPreset.KartPlant1 = iPacket.ReadUShort();
                            ItemPreset.KartPlant2 = iPacket.ReadUShort();
                            ItemPreset.KartPlant3 = iPacket.ReadUShort();
                            ItemPreset.KartPlant4 = iPacket.ReadUShort();
                            ItemPreset.Unknown3 = iPacket.ReadUShort();
                            ItemPreset.FishingPole = iPacket.ReadUShort();
                            ItemPreset.Tachometer = iPacket.ReadUShort();
                            ItemPreset.Dye = iPacket.ReadUShort();
                            ItemPreset.KartSN = iPacket.ReadUShort();
                            ItemPreset.Unknown4 = iPacket.ReadByte();
                            ItemPreset.KartCoating = iPacket.ReadUShort();
                            ItemPreset.KartTailLamp = iPacket.ReadUShort();
                            ItemPreset.slotBg = iPacket.ReadUShort();
                            ItemPreset.KartCoating12 = iPacket.ReadUShort();
                            ItemPreset.KartTailLamp12 = iPacket.ReadUShort();
                            ItemPreset.KartBoosterEffect12 = iPacket.ReadUShort();
                            ItemPreset.Unknown5 = iPacket.ReadUShort();
                            ItemPresetsService.Save(this.Parent.Client.Nickname, itemPresetConfig);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqItemPresetUseSlotData", 0))
                    {
                        short id = iPacket.ReadShort();
                        using (OutPacket outPacket = new OutPacket("PrItemPresetUseSlotData"))
                        {
                            outPacket.WriteInt(1);
                            this.Parent.Client.Send(outPacket);
                        }
                        var usePresetConfig = ItemPresetsService.GetItemPresetConfig(this.Parent.Client.Nickname);
                        foreach (var ItemPreset in usePresetConfig.ItemPresets)
                        {
                            if (ItemPreset.ID == id)
                            {
                                ItemPreset.Enable = 1;
                            }
                            else
                            {
                                ItemPreset.Enable = 0;
                            }
                        }
                        ItemPresetsService.Save(this.Parent.Client.Nickname, usePresetConfig);
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetMyMsgrInfoPacket", 0))
                    {
                        uint unk1 = iPacket.ReadUInt();
                        using (OutPacket outPacket = new OutPacket("PrGetMyMsgrInfoPacket"))
                        {
                            outPacket.WriteUInt(unk1);
                            outPacket.WriteByte(1);
                            outPacket.WriteBytes(new byte[15]);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetMsgrFriendList", 0))
                    {
                        uint unk1 = iPacket.ReadUInt();
                        using (OutPacket outPacket = new OutPacket("PrGetMsgrFriendList"))
                        {
                            GameSupport.GetMsgrFriendList(this.Parent, outPacket);
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(1);
                            outPacket.WriteBytes(new byte[8]);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRepeatGetMsgrFriendList", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrRepeatGetMsgrFriendList"))
                        {
                            GameSupport.GetMsgrFriendList(this.Parent, outPacket);
                            outPacket.WriteByte(1);
                            outPacket.WriteBytes(new byte[8]);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRefreshRecommendFriendList", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrRefreshRecommendFriendList"))
                        {
                            GameSupport.RefreshRecommendFriendList(this.Parent, outPacket);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRequestAddFriendPacket", 0))
                    {
                        uint UserID = iPacket.ReadUInt();
                        string UserName = ClientManager.GetNickname(UserID);
                        if (string.IsNullOrEmpty(UserName)) return;
                        var pConfig = ProfileService.GetProfileConfig(UserName);
                        IPEndPoint client = ClientManager.ClientToIPEndPoint(pConfig.Rider.ClientId);
                        using (OutPacket outPacket = new OutPacket("PrRequestAddFriendPacket"))
                        {
                            outPacket.WriteHexString("00 44 77 3D");
                            outPacket.WriteBytes(new byte[28]);
                            outPacket.WriteUInt(UserID);
                            outPacket.WriteString(UserName);
                            outPacket.WriteEndPoint(new IPEndPoint(client.Address, pConfig.Rider.P2pPort));
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqMsgrReceiveSendInfo", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrMsgrReceiveSendInfo"))
                        {
                            outPacket.WriteHexString("00 00 00 00 00 00 00 00 00");
                            this.Parent.Client.Send(outPacket);
                        }
                        using (OutPacket outPacket = new OutPacket("PrMsgrReceiveSendInfo"))
                        {
                            outPacket.WriteHexString("00 00 00 00 00 00 00 00 01");
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqExpeditionActiveMissionListPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrExpeditionActiveMissionListPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqExpeditionBuyMissionPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrExpeditionBuyMissionPacket"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqExpeditionMissionStartPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrExpeditionMissionStartPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteByte(2);
                            outPacket.WriteTime(DateTime.Now);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqGetRiderQuestUX2ndData", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrGetRiderQuestUX2ndData"))
                        {
                            outPacket.WriteInt(1);
                            outPacket.WriteInt(1);
                            GameSupport.PrQuestUX2ndPacket(outPacket);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqPlaytimeEventCompleteCheck", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrPlaytimeEventCompleteCheck"))
                        {
                            outPacket.WriteInt(0);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PcGameRequestRelay", 0))
                    {
                        int roomId = RoomManager.TryGetRoomId(this.Parent.Client.Nickname);
                        var room = RoomManager.GetRoom(roomId);
                        if (room == null)
                        {
                            return;
                        }

                        int val1 = iPacket.ReadInt();

                        // using (OutPacket outPacket = new OutPacket("GameRelayBroadcastingPacket"))
                        // {
                        //     outPacket.WriteInt(0);
                        //     outPacket.WriteInt(val1);
                        //     MultyPlayer.BroadCast(roomId, outPacket);
                        // }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChClientP2pAddrPacket", 0))
                    {
                        var ClientP2pAddr = iPacket.ReadEndPoint();
                        var p2pConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        p2pConfig.Rider.P2pPort = (ushort)ClientP2pAddr.Port;
                        ProfileService.Save(this.Parent.Client.Nickname, p2pConfig);
                        Console.WriteLine($"[{this.Parent.Client.Nickname}] {ClientP2pAddr.Address}:{ClientP2pAddr.Port}");
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChClientUdpAddrPacket", 0))
                    {
                        var ClientUdpAddr = iPacket.ReadEndPoint();
                        var udpConfig = ProfileService.GetProfileConfig(this.Parent.Client.Nickname);
                        udpConfig.Rider.UdpPort = (ushort)ClientUdpAddr.Port;
                        ProfileService.Save(this.Parent.Client.Nickname, udpConfig);
                        Console.WriteLine($"[{this.Parent.Client.Nickname}] {ClientUdpAddr.Address}:{ClientUdpAddr.Port}");
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqStartTrainingCenter", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrStartTrainingCenter"))
                        {
                            outPacket.WriteByte(1);
                            StartGameData.GetSchoolSpac(outPacket, this.Parent.Client.Nickname);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqMIssionAttendGetCurUserInfoPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrMIssionAttendGetCurUserInfoPacket"))
                        {
                            outPacket.WriteInt(0);
                            outPacket.WriteInt(0);
                            outPacket.WriteDateTime(DateTime.Now);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqQuestUX2ndForShutDownPacket", 0))
                    {
                        using (OutPacket outPacket = new OutPacket("PrQuestUX2ndForShutDownPacket"))
                        {
                            GameSupport.PrQuestUX2ndPacket(outPacket);
                            this.Parent.Client.Send(outPacket);
                        }
                        return;
                    }
                    else if (hash == Adler32Helper.GenerateAdler32_ASCII("LoPingRequestPacket", 0))
                    {
                        foreach (var client in ClientManager.GetClients())
                        {
                            using (OutPacket outPacket = new OutPacket("PrServerTime"))
                            {
                                outPacket.WriteDateTime(DateTime.Now);
                                client.Client.Send(outPacket);
                            }
                        }
                        return;
                    }
                    else
                    {
                        MultyPlayer.Clientsession(this.Parent, hash, iPacket);
                        return;
                    }
                }
            }
        }
    }
}
