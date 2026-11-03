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
    /// <summary>处理 <see cref="PacketName.PqFinishTimeAttack"/> 数据包。</summary>
    internal sealed class PqFinishTimeAttack : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqFinishTimeAttack;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int type = inPacket.ReadInt();
            inPacket.ReadInt();
            byte RewardType = inPacket.ReadByte();
            inPacket.ReadInt();
            inPacket.ReadInt();
            inPacket.ReadInt(); //使用加速器次数
            inPacket.ReadInt(); //碰撞次数
            var finishConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            finishConfig.Rider.Time = inPacket.ReadUInt();
            var finishReward = TimeReward.FinishReward(RewardType);
            finishConfig.Rider.RP += finishReward.RP;
            finishConfig.Rider.Lucci += finishReward.Lucci;
            ProfileService.Save(session.Client.Nickname, finishConfig);
            var timeSpan = TrackRankData.GetTimeSpan(finishConfig.Rider.Time);
            Log.Info($"FinishTimeAttack: {RewardType} / {RandomTrack.GetTrackName(finishConfig.Rider.Track)} / {timeSpan.min}:{timeSpan.sec}:{timeSpan.mil}");
            using (OutPacket outPacket = new OutPacket("PrFinishTimeAttack"))
            {
                outPacket.WriteInt(type);
                if (finishConfig.Rider.AttackType == 0 && RewardType == 1)
                {
                    byte Level = TimeAttack.TrainingMission(session.Client.Nickname, finishConfig.Rider.Track);
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
                session.Client.Send(outPacket);
            }
            TrackRankData.AddTrackRank(finishConfig.Rider.Track, finishConfig.Rider.SpeedType, finishConfig.Rider.GameType, new TrackRank
            {
                UserNO = ClientManager.GetUserNO(session.Client.Nickname),
                Nickname = session.Client.Nickname,
                Kart = ProfileService.GetProfileConfig(session.Client.Nickname).RiderItem.Set_Kart,
                Time = finishConfig.Rider.Time
            });
            return true;
        }
    }
}
