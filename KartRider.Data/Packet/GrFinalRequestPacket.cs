using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;

namespace KartRider.Packet
{
    /// <summary>
    /// 处理 <see cref="PacketName.GrFinalRequestPacket"/>（挑战者模式，模式 17）。
    ///
    /// 抓包（挑战者模式.log）中该包只有包头、没有正文，出现在 GameResultPacket /
    /// GameControl(state=4) 之后，表示客户端已完成结算界面、准备回到房间。
    /// 服务端保留房间状态即可，下一局由 GrRequestStartPacket 重新开局。
    /// </summary>
    internal sealed class GrFinalRequestPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrFinalRequestPacket;

        protected override bool Handle(SessionGroup session, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(session.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            Log.Info($"[Mode] {session.Client.Nickname} 完成结算（房间 {roomId}，模式 {(room == null ? "-" : GameModes.Name(room.GameType))}）");
            // 这个包是「结算画面看完了」的唯一证据：客户端进了结算画面才会发。
            // 挑战赛三局制靠它把 7 秒止血看门狗换成 25 秒长等待（见 MultyPlayer.OnCampaignFinalRequest）。
            MultyPlayer.OnCampaignFinalRequest(roomId);
            return true;
        }
    }
}
