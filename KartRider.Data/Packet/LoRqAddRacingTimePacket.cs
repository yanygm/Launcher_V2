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
    /// <summary>处理 <see cref="PacketName.LoRqAddRacingTimePacket"/> 数据包。</summary>
    internal sealed class LoRqAddRacingTimePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.LoRqAddRacingTimePacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            if (inPacket.Length < 24)
            {
                // 短包直接忽略：原实现用 inPacket.Length - 8 定位，长度不足时索引变负并越界
                Log.Debug("[Race] LoRqAddRacingTimePacket 过短，忽略");
                return true;
            }
            uint Track = inPacket.ReadUInt();
            byte SpeedType = inPacket.ReadByte();
            byte GameType = inPacket.ReadByte();
            inPacket.ReadBytes(8);
            ushort Kart = inPacket.ReadUShort();
            //inPacket.ReadBytes(417);
            inPacket.Position = inPacket.Length - 8;
            short Booster = inPacket.ReadShort();
            inPacket.ReadShort();
            short Crash = inPacket.ReadShort();
            inPacket.ReadShort();
            var racingTime = ProfileService.GetProfileConfig(session.Client.Nickname);
            using (OutPacket outPacket = new OutPacket("LoRpAddRacingTimePacket"))
            {
                //outPacket.WriteHexString("FF FF FF FF 00 00 00 00 00 00 00 00 00 00");
                outPacket.WriteUInt(racingTime.Rider.Time);
                outPacket.WriteInt(0);
                outPacket.WriteShort(0);
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            var manager = new CompetitiveDataManager();
            Dictionary<uint, TrackData> TrackDictionary = TimeAttack.GetTrackDictionary();
            CompleteTrackScoreCalculator calculator = new CompleteTrackScoreCalculator();
            var scores = calculator.CalculateTrackScoreDetails(Track, racingTime.Rider.Time, Booster, Crash, TrackDictionary);
            if (scores != null)
            {
                var data = new CompetitiveData { Track = Track, Kart = Kart, Time = racingTime.Rider.Time, Booster = Booster, BoosterPoint = scores.BoostScore, Crash = Crash, CrashPoint = scores.CrashScore, Point = scores.TotalScore };
                manager.SaveData(session.Client.Nickname, data);
            }
            using (OutPacket outPacket = new OutPacket("PrGetCompetitiveSlotInfo"))
            {
                var competitiveData = manager.LoadAllData(session.Client.Nickname);
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
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
