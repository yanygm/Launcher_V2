using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;

namespace KartRider.Packet
{
    /// <summary>
    /// 处理 <see cref="PacketName.GameBoosterAddPacket"/>（死斗/组队加速槽）。
    ///
    /// 抓包（死斗.log）中该包只有包头、没有正文，官方服务端也没有回包：
    /// 加速槽由 GameTeamBoosterRequestAddGaugePacket / GameTeamBoosterSetGaugePacket 维护，
    /// 这里只做接收，避免后续包被当成未知包处理。
    /// </summary>
    internal sealed class GameBoosterAddPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GameBoosterAddPacket;

        protected override bool Handle(SessionGroup session, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(session.Client.Nickname);
            Log.Debug($"[Booster] {session.Client.Nickname} 触发团队加速（房间 {roomId}）");
            return true;
        }
    }
}
