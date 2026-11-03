using ExcData;
using KartRider;
using KartRider.Common.Network;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider.Logging;
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
using System.Windows.Forms.Design;
using System.Xml;
using System.Xml.Linq;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PqUpdateGameOption"/> 数据包。</summary>
    internal sealed class PqUpdateGameOption : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqUpdateGameOption;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var gameOptionConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            gameOptionConfig.GameOption.Set_BGM = inPacket.ReadFloat();
            gameOptionConfig.GameOption.Set_Sound = inPacket.ReadFloat();
            gameOptionConfig.GameOption.Main_BGM = inPacket.ReadByte();
            gameOptionConfig.GameOption.Sound_effect = inPacket.ReadByte();
            gameOptionConfig.GameOption.Full_screen = inPacket.ReadByte();
            gameOptionConfig.GameOption.ShowMirror = inPacket.ReadByte();
            gameOptionConfig.GameOption.ShowOtherPlayerNames = inPacket.ReadByte();
            gameOptionConfig.GameOption.ShowOutlines = inPacket.ReadByte();
            gameOptionConfig.GameOption.ShowShadows = inPacket.ReadByte();
            gameOptionConfig.GameOption.HighLevelEffect = inPacket.ReadByte();
            gameOptionConfig.GameOption.MotionBlurEffect = inPacket.ReadByte();
            gameOptionConfig.GameOption.MotionDistortionEffect = inPacket.ReadByte();
            gameOptionConfig.GameOption.HighEndOptimization = inPacket.ReadByte();//오토 레디
            gameOptionConfig.GameOption.AutoReady = inPacket.ReadByte();//아이템 설명
            gameOptionConfig.GameOption.PropDescription = inPacket.ReadByte();//녹화 품질
            gameOptionConfig.GameOption.VideoQuality = inPacket.ReadByte();
            gameOptionConfig.GameOption.BGM_Check = inPacket.ReadByte();//배경음
            gameOptionConfig.GameOption.Sound_Check = inPacket.ReadByte();//효과음
            gameOptionConfig.GameOption.ShowHitInfo = inPacket.ReadByte();
            gameOptionConfig.GameOption.AutoBoost = inPacket.ReadByte();
            gameOptionConfig.GameOption.GameType = inPacket.ReadByte();//팀전 개인전 여부
            gameOptionConfig.GameOption.SetGhost = inPacket.ReadByte();//고스트 사용여부
            gameOptionConfig.GameOption.SpeedType = inPacket.ReadByte();//채널 속도
            gameOptionConfig.GameOption.RoomChat = inPacket.ReadByte();//查看房间内聊天内容
            gameOptionConfig.GameOption.DrivingChat = inPacket.ReadByte();//查看行驶中聊天内容
            gameOptionConfig.GameOption.ShowAllPlayerHitInfo = inPacket.ReadByte();
            gameOptionConfig.GameOption.ShowTeamColor = inPacket.ReadByte();
            gameOptionConfig.GameOption.Set_screen = inPacket.ReadByte();
            gameOptionConfig.GameOption.HideCompetitiveRank = inPacket.ReadByte();//隐藏自己的等级段位
            gameOptionConfig.GameOption.QuickMsg[0] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.QuickMsg[1] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.QuickMsg[2] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.QuickMsg[3] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.QuickMsg[4] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.QuickMsg[5] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.QuickMsg[6] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.QuickMsg[7] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.QuickMsg[8] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.QuickMsg[9] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.TeamQuickMsg[0] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.TeamQuickMsg[1] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.TeamQuickMsg[2] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.TeamQuickMsg[3] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.TeamQuickMsg[4] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.TeamQuickMsg[5] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.TeamQuickMsg[6] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.TeamQuickMsg[7] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.TeamQuickMsg[8] = inPacket.ReadString(false);
            gameOptionConfig.GameOption.TeamQuickMsg[9] = inPacket.ReadString(false);
            ProfileService.Save(session.Client.Nickname, gameOptionConfig);
            return true;
        }
    }
}
