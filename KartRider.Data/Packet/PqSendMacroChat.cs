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
    /// <summary>处理 <see cref="PacketName.PqSendMacroChat"/> 数据包。</summary>
    internal sealed class PqSendMacroChat : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqSendMacroChat;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int type = iPacket.ReadInt();
            byte id = iPacket.ReadByte();
            return HandleCore(Parent, type, id);
        }

        internal static bool HandleCore(SessionGroup Parent, int type, byte id)
        {
            var roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (roomId == -1)
            {
                return true;
            }

            Player player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);

            using (OutPacket outPacket = new OutPacket("PcSendMacroChat"))
            {
                outPacket.WriteUInt(ClientManager.GetUserNO(Parent.Client.Nickname));
                outPacket.WriteInt(type);
                outPacket.WriteByte(id);
                if (type == 0)
                {
                    outPacket.WriteString(ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.QuickMsg.GetValueOrDefault(id) ?? "");
                    BroadCast(roomId, outPacket, Parent.Client.Nickname);
                }
                else
                {
                    outPacket.WriteString(ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.TeamQuickMsg.GetValueOrDefault(id) ?? "");
                    BroadCast(roomId, outPacket, Parent.Client.Nickname, player.Team);
                }
                if (ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.QuickMsg.GetValueOrDefault(id) == "结束游戏" ||
                    ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.QuickMsg.GetValueOrDefault(id) == "結束遊戲" ||
                    ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.TeamQuickMsg.GetValueOrDefault(id) == "结束游戏" ||
                    ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.TeamQuickMsg.GetValueOrDefault(id) == "結束遊戲")
                {
                    if (room.GetPlayerCount(0) == 1)
                    {
                        StopGame(roomId, Parent);
                    }
                    else
                    {
                        bool remove = RoomManager.RemovePlayer(room.RoomId, player.SlotId, player.Nickname);
                        if (remove) MyRoomData.ChRpEnterMyRoomPacket(Parent, Parent.Client.Nickname);
                    }
                }
            }
            return true;
        }
    }
}
