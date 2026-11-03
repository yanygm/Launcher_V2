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
    /// <summary>处理 <see cref="PacketName.PqStartTimeAttack"/> 数据包。</summary>
    internal sealed class PqStartTimeAttack : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqStartTimeAttack;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var attackConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            var StartTimeAttack_Unk1 = inPacket.ReadInt();
            var StartTimeAttack_Unk2 = inPacket.ReadInt();
            var StartTimeAttack_Track = inPacket.ReadUInt();
            attackConfig.Rider.SpeedType = inPacket.ReadByte();
            attackConfig.Rider.GameType = inPacket.ReadByte();
            var Kart_id = inPacket.ReadUShort();
            var FlyingPet_id = inPacket.ReadUShort();
            var StartTimeAttack_StartType = inPacket.ReadByte();
            var StartTimeAttack_Unk3 = inPacket.ReadInt();
            var StartTimeAttack_Unk4 = inPacket.ReadInt();
            var StartTimeAttack_Unk5 = inPacket.ReadByte();
            attackConfig.Rider.AttackType = inPacket.ReadByte();
            var StartTimeAttack_TimaAttackMpdeType = inPacket.ReadByte();
            var StartTimeAttack_TimaAttackMpde = inPacket.ReadInt();
            var StartTimeAttack_RandomTrackGameType = inPacket.ReadByte();
            if (StartTimeAttack_TimaAttackMpdeType == 1)
            {
                if (attackConfig.Rider.Lucci >= 1000)
                {
                    attackConfig.Rider.Lucci -= 1000;
                }
                else
                {
                    attackConfig.Rider.Lucci = 0;
                }
            }
            Log.Info($"StartTimeAttack: {attackConfig.Rider.SpeedType} / {attackConfig.Rider.GameType} / {Kart_id} / {FlyingPet_id} / {RandomTrack.GetTrackName(StartTimeAttack_Track)} / {StartTimeAttack_StartType} / {attackConfig.Rider.AttackType} / {StartTimeAttack_TimaAttackMpdeType}");
            attackConfig.Rider.Track = RandomTrack.GetRandomTrack(session, session.Client.Nickname, StartTimeAttack_RandomTrackGameType, StartTimeAttack_Track);
            ProfileService.Save(session.Client.Nickname, attackConfig);
            byte StartType = 3;
            StartGameData.Start_KartSpac(session, session.Client.Nickname, StartType, StartTimeAttack_StartType, StartTimeAttack_Unk1, attackConfig.Rider.Track, attackConfig.Rider.SpeedType, Kart_id, FlyingPet_id);
            return true;
        }
    }
}
