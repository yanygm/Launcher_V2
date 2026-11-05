using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;
using Profile;
using System;

namespace KartRider.Packet
{
    /// <summary>
    /// 处理 <see cref="PacketName.GameFlagRequestAddScorePacket"/>（夺旗赛，模式 7）。
    ///
    /// 客户端 → 服务端：byte team（夺旗方阵营，1=红 / 2=蓝）
    /// 服务端 → 客户端：GameFlagSetScorePacket(uint 红队夺旗数, uint 蓝队夺旗数)
    ///
    /// 抓包（夺旗赛.log）：
    ///   [C]GameFlagRequestAddScorePacket: ... 02
    ///   [S]GameFlagSetScorePacket:       ... 03 00 00 00 01 00 00 00
    /// 即请求携带阵营字节，服务端累计后向全房间广播双方比分。
    /// 正赛 180 秒打平会先下发 GameControl(state=6) 进入加时（比分清零，先夺旗者胜）。
    /// </summary>
    internal sealed class GameFlagRequestAddScorePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GameFlagRequestAddScorePacket;

        protected override bool Handle(SessionGroup session, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(session.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
            }
            if (!GameModes.IsTeamFlagMode(room.GameType))
            {
                Log.Debug($"[Flag] 忽略得分请求：房间 {roomId} 模式 {room.GameType} 不是团队夺旗赛");
                return true;
            }
            if (!room.Started || room.EndTicks != 0)
            {
                return true;
            }

            Player player = RoomManager.GetPlayer(roomId, session.Client.Nickname);
            if (player == null)
            {
                return true;
            }

            byte team = iPacket.Available > 0 ? iPacket.ReadByte() : player.Team;
            if (team != 1 && team != 2)
            {
                team = player.Team;
            }
            if (team != 1 && team != 2)
            {
                Log.Debug($"[Flag] 忽略得分请求：{session.Client.Nickname} 没有有效阵营");
                return true;
            }

            bool finished = false;
            int red;
            int blue;
            lock (room.Sync)
            {
                if (team == 1)
                {
                    room.RedFlagScore++;
                }
                else
                {
                    room.BlueFlagScore++;
                }
                room.AddModeScore(player.ID, 1);
                red = room.RedFlagScore;
                blue = room.BlueFlagScore;

                // 加时中先得分的一方直接获胜
                finished = room.FlagOvertime && red != blue;
            }

            MultyPlayer.BroadcastFlagScore(room);
            Log.Info($"[Flag] 房间 {roomId} {session.Client.Nickname} 为 {(team == 1 ? "红队" : "蓝队")} 夺旗，比分 {red}:{blue}{(room.FlagOvertime ? "（加时）" : "")}");

            if (finished)
            {
                MultyPlayer.EndModeRace(room, "加时夺旗");
            }
            return true;
        }
    }
}
