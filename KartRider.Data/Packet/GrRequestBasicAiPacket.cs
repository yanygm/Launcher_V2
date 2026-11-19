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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using static KartRider.MultyPlayer;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.GrRequestBasicAiPacket"/> 数据包。</summary>
    internal sealed class GrRequestBasicAiPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrRequestBasicAiPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
            }
            // 官服格式（抓包 `官服建立组队竞速房踢出AI添加AI抓包`）：[int 槽位][byte 等级]
            // int 是要操作的槽位号，byte 不是 ID。之前把 byte 当 ID → 踢 AI 变成加 AI、加 AI 变成越界。
            int target = iPacket.Available >= 4 ? iPacket.ReadInt() : 0;
            byte level = iPacket.Available >= 1 ? iPacket.ReadByte() : (byte)0;
            object detail = RoomManager.TryGetIdDetail(roomId, target);
            if (detail is Ai ai)
            {
                // 踢 AI：只做移除，靠随后的 GrSlotDataPacket 全量广播让客户端刷新。
                // （自定义的 GrSlotDataBasicAi 布局会让客户端解析崩，先不自行构造。）
                room.RemoveMember(ai.SlotId, "");
                // 广播「该 AI 槽变空」的 GrSlotDataBasicAi —— 官服抓包实测 58 字节，包体 54：
                // u32 1 | u8 1 | u32 槽位 | 0 | 1 | 0 | 1 | u8 0 | -1 | 5×u32(其余 AI 槽位) | -1
                // 没有名称字段！之前自己塞 12 个 0 导致客户端解析越界崩溃。
                Log.Info($"[Room] 踢出 AI 槽位 {ai.SlotId}（房间 {roomId}），已广播 58 字节空 AI 槽");
            }
            else if (detail is Player)
            {
                Log.Warn($"[Room] GrRequestBasicAiPacket 目标槽位 {target} 是玩家（房间 {roomId}），忽略");
            }
            else
            {
                AddAi(Parent, roomId, target);
            }
            using (OutPacket oPacket = new OutPacket("GrReplyBasicAiPacket"))
            {
                // 官服回包只有 1 字节（总 5 字节），多发 4 个 0 会被客户端判非法
                oPacket.WriteByte(1);
                BroadCast(roomId, oPacket);
            }
            return true;
        }
    }
}
