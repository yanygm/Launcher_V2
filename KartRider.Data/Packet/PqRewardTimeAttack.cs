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
    /// <summary>处理 <see cref="PacketName.PqRewardTimeAttack"/> 数据包。</summary>
    internal sealed class PqRewardTimeAttack : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqRewardTimeAttack;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            byte RewardType = inPacket.ReadByte();
            int RP = inPacket.ReadInt();
            int Lucci = inPacket.ReadInt();
            int TimeAttack_StartTicks = inPacket.ReadInt();
            uint Track = inPacket.ReadUInt();
            var rewardConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            rewardConfig.Rider.Track = Track;
            Log.Info($"RewardTimeAttack : ResultType: {RewardType}, RP: {RP}, Lucci: {Lucci}, Track: {RandomTrack.GetTrackName(Track)}");
            var finishReward = TimeReward.FinishReward(RewardType);
            rewardConfig.Rider.RP += finishReward.RP;
            rewardConfig.Rider.Lucci += finishReward.Lucci;
            ProfileService.Save(session.Client.Nickname, rewardConfig);
            return true;
        }
    }
}
