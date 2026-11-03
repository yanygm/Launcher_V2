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
    /// <summary>处理 <see cref="PacketName.GrRequestClosePacket"/> 数据包。</summary>
    internal sealed class GrRequestClosePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrRequestClosePacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                return true;
            }
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
            }
            uint unk1 = iPacket.ReadUInt();
            byte type = iPacket.ReadByte();
            uint slotId1 = iPacket.ReadUInt();
            uint unk2 = iPacket.ReadUInt();
            uint slotId2 = iPacket.ReadUInt();
            using (OutPacket oPacket = new OutPacket("GrReplyClosePacket"))
            {
                oPacket.WriteUInt(ClientManager.GetUserNO(Parent.Client.Nickname));
                if (room.GameType == 3 || room.GameType == 4)
                {
                    if (unk1 < 8 && slotId1 < 8 && unk2 < 8 && slotId2 < 8 && type == 1 && !room.CloseSlotIds.Contains((byte)slotId1) && !room.CloseSlotIds.Contains((byte)slotId2))
                    {
                        if (room.AddClose((byte)slotId1, (int)unk1) && room.AddClose((byte)slotId2, (int)unk2))
                        {
                            oPacket.WriteByte(1);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk2);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(1);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                        else
                        {
                            oPacket.WriteByte(0);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk2);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                    }
                    else if (unk1 < 8 && slotId1 < 8 && unk2 < 8 && slotId2 < 8 && type == 0 && room.CloseSlotIds.Contains((byte)slotId1) && room.CloseSlotIds.Contains((byte)slotId2))
                    {
                        if (room.RemoveClose((byte)slotId1, (int)unk1) && room.RemoveClose((byte)slotId2, (int)unk2))
                        {
                            oPacket.WriteByte(1);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk2);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                        else
                        {
                            oPacket.WriteByte(0);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk2);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                    }
                    else
                    {
                        oPacket.WriteByte(0);
                        oPacket.WriteUInt(unk1);
                        oPacket.WriteUInt(unk2);
                        int closeCount = room.CloseSlotIds.Count;
                        oPacket.WriteInt(0);
                        oPacket.WriteInt(closeCount);
                        foreach (byte slotId in room.CloseSlotIds)
                        {
                            oPacket.WriteByte(slotId);
                        }
                    }
                    BroadCast(roomId, oPacket);
                }
                else
                {
                    if (unk1 < 8 && slotId1 < 8 && type == 1 && !room.CloseSlotIds.Contains((byte)slotId1))
                    {
                        if (room.AddClose((byte)slotId1, (int)unk1))
                        {
                            oPacket.WriteByte(1);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk1);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(1);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                        else
                        {
                            oPacket.WriteByte(0);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk1);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                    }
                    else if (unk1 < 8 && slotId1 < 8 && type == 0 && room.CloseSlotIds.Contains((byte)slotId1))
                    {
                        if (room.RemoveClose((byte)slotId1, (int)unk1))
                        {
                            oPacket.WriteByte(1);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk1);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                        else
                        {
                            oPacket.WriteByte(0);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk1);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                    }
                    else
                    {
                        oPacket.WriteByte(0);
                        oPacket.WriteUInt(unk1);
                        oPacket.WriteUInt(unk1);
                        int closeCount = room.CloseSlotIds.Count;
                        oPacket.WriteInt(0);
                        oPacket.WriteInt(closeCount);
                        foreach (byte slotId in room.CloseSlotIds)
                        {
                            oPacket.WriteByte(slotId);
                        }
                    }
                    BroadCast(roomId, oPacket);
                }
            }
            return true;
        }
    }
}
