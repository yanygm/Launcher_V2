using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;
using System;

namespace KartRider.Packet
{
    /// <summary>
    /// 处理 <see cref="PacketName.GrChangeCampaignPacket"/>（挑战赛，模式 17）。
    ///
    /// 官服抓包（挑战赛抓包.pcapng）里该包正文 6 字节：<c>[1B 固定 0][int32 战队 id][1B 难度]</c>。
    /// 客户端每次切换难度或切换 AI 战队都会发一次，并且紧跟两个 GrRequestSetSlotStatePacket（state 02 / 05），
    /// 由服务端重发槽位包把选择结果带回客户端——所以这里只负责存值，
    /// 真正的下发在 GrSlotDataPacket 的保留区（战队 id / 难度两个 int）。
    /// </summary>
    internal sealed class GrChangeCampaignPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrChangeCampaignPacket;

        protected override bool Handle(SessionGroup session, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(session.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Log.Info($"[挑战赛] 收到战队选择包但没有房间（{session.Client.Nickname}）");
                return true;
            }

            int campaignId = 0;
            byte difficulty = 0;
            try
            {
                iPacket.ReadByte();               // 固定 0，未用
                campaignId = iPacket.ReadInt();   // 战队 id（aiCampaign.xml 的 Campaign id）
                difficulty = iPacket.ReadByte();  // 0 简单 / 1 普通 / 2 困难
            }
            catch (Exception ex)
            {
                Log.Warn($"[挑战赛] 战队选择包解析失败：{ex.Message}");
                return true;
            }

            room.CampaignId = campaignId;
            room.CampaignDifficulty = difficulty;
            Log.Info($"[挑战赛] 房间 {roomId} 选择战队 id={campaignId}，难度={difficulty}");
            return true;
        }
    }
}
