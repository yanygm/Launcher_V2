using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;
using Profile;
using System;

namespace KartRider.Packet
{
    /// <summary>
    /// 处理 <see cref="PacketName.GameDeathMatchRequestAddScorePacket"/>（死斗，模式 57/58）。
    ///
    /// 客户端 → 服务端：byte team（得分方阵营）
    /// 服务端 → 客户端：GameDeathMatchSetScorePacket(int 序号, int 阵营, int 积分)
    ///
    /// 抓包（死斗.log）：
    ///   [C]GameDeathMatchRequestAddScorePacket: ... 01
    ///   [S]GameDeathMatchSetScorePacket:       ... 00 00 00 00 01 00 00 00 01 00 00 00
    /// 三个 int 依次是：本局得分事件序号、得分阵营、该阵营当前积分；
    /// 客户端据此刷新死斗积分板。正赛 150 秒后由服务端下发 GameControl(state=3) 结算。
    /// </summary>
    internal sealed class GameDeathMatchRequestAddScorePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GameDeathMatchRequestAddScorePacket;

        protected override bool Handle(SessionGroup session, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(session.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
            }
            if (!GameModes.IsDeathMatch(room.GameType))
            {
                Log.Debug($"[DeathMatch] 忽略得分请求：房间 {roomId} 模式 {room.GameType} 不是死斗");
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

            byte rawTeam = iPacket.Available > 0 ? iPacket.ReadByte() : player.Team;
            bool teamMode = room.GameType == GameModes.DeathMatchTeam;
            byte team = rawTeam;
            if (team != 1 && team != 2)
            {
                team = player.Team;
            }
            if (!teamMode)
            {
                // 个人死斗：积分记在个人头上，第二个字段回玩家 ID，客户端按玩家排行
                team = (byte)Math.Clamp(player.ID, 0, 255);
            }
            else if (team != 1 && team != 2)
            {
                Log.Debug($"[DeathMatch] 忽略得分请求：{session.Client.Nickname} 没有有效阵营");
                return true;
            }

            int sequence;
            int score;
            lock (room.Sync)
            {
                sequence = room.DeathMatchSequence++;
                int personal = room.AddModeScore(player.ID, 1);
                if (teamMode)
                {
                    if (team == 1)
                    {
                        room.DeathMatchRedScore++;
                    }
                    else
                    {
                        room.DeathMatchBlueScore++;
                    }
                    score = team == 1 ? room.DeathMatchRedScore : room.DeathMatchBlueScore;
                }
                else
                {
                    score = personal;
                }
            }

            using (OutPacket oPacket = new OutPacket("GameDeathMatchSetScorePacket"))
            {
                oPacket.WriteInt(sequence);
                oPacket.WriteInt(team);
                oPacket.WriteInt(score);
                MultyPlayer.BroadCast(roomId, oPacket);
            }
            Log.Info($"[DeathMatch] 房间 {roomId} {session.Client.Nickname} 得分（阵营 {team}，个人 {room.ModeScore(player.ID)}，比分 {room.DeathMatchRedScore}:{room.DeathMatchBlueScore}）");
            return true;
        }
    }
}
