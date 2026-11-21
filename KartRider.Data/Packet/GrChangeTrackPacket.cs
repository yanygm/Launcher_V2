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
    /// <summary>处理 <see cref="PacketName.GrChangeTrackPacket"/> 数据包。</summary>
    internal sealed class GrChangeTrackPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrChangeTrackPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
            }
            uint newTrack = iPacket.ReadUInt();
            string requiredTrackType = GameModes.TrackGameType(room.GameType);
            if (requiredTrackType != null
                && RandomTrack.TrackList.ContainsKey(newTrack)
                && !RandomTrack.IsTrackOfGameType(newTrack, requiredTrackType))
            {
                // 夺旗/死斗只能选对应类型的地图，选普通图会让客户端进图闪退
                Log.Warn($"[Room] 房间 {roomId} 模式 {GameModes.Name(room.GameType)} 拒绝地图 {RandomTrack.GetTrackName(newTrack)}（需要 {requiredTrackType} 类型）");
                GrSlotDataPacket(roomId);
                return true;
            }
            room.track = newTrack;
            // AI 房（建房时自动固定了一张 basicAi 图）要跟着用户的选择走，否则槽位包和开局
            // 都会继续用建房时那张固定图（表现：AI 房里换不了地图）。
            //   · 选了「随机图池」（0=全部随机，1~40=人气/热门/新图/反向/专业/疯狂等随机码）
            //     → 清掉固定图，开局交给 RandomTrack：它按 room.track 的随机码抽池，
            //       并且 ai=true 时会再筛一遍 basicAi（随机池 = AI 图）
            //   · 选了具体地图（真实 hash）→ 就用它（AiFixedTrack 保留，避免开局再随机）
            if (newTrack <= 40)
            {
                room.AiFixedTrack = false;
                room.trackTemp = 0;
            }
            else
            {
                room.AiFixedTrack = true;
                room.trackTemp = newTrack;
                if (room.GetAiCount() > 0 && RandomTrack.TrackList.TryGetValue(newTrack, out var chosen) && !chosen.basicAi)
                {
                    Log.Warn($"[Room] 房间 {roomId} 换到没有 AI 导航数据的图 {RandomTrack.GetTrackName(newTrack)}，AI 可能不会跑");
                }
            }
            // IDA: GrChangeTrackPacket = int + int
            if (iPacket.Available >= 4) iPacket.ReadInt();
            Log.Info($"[Room] 房间 {roomId} 换图：{RandomTrack.GetTrackName(room.track)}（AI 房固定图={room.AiFixedTrack}）");
            GrSlotDataPacket(roomId);
            return true;
        }
    }
}
