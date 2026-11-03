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

namespace KartRider
{
    public class ClientSession : Session
    {
        public SessionGroup Parent
        {
            get;
            set;
        }

        public ClientSession(SessionGroup parent, System.Net.Sockets.Socket socket) : base(socket)
        {
            this.Parent = parent;
        }

        /// <summary>
        /// 昵称合法性校验：只允许普通字符，拒绝路径分隔符、通配符、控制字符与 ".."。
        /// 改名流程会用它拼目录名并做 Directory.Move，必须先拦住穿越字符。
        /// </summary>
        internal static bool IsSafeNickname(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 40)
            {
                return false;
            }
            foreach (char c in name)
            {
                if (c < 0x20 || c == '/' || c == '\\' || c == ':' || c == '*' || c == '?'
                    || c == '"' || c == '<' || c == '>' || c == '|')
                {
                    return false;
                }
            }
            return name != "." && name != ".." && !name.Contains("..", StringComparison.Ordinal);
        }

        /// <summary>解析后的路径必须仍在 Profile 目录之内。</summary>
        internal static bool IsUnderProfileDir(string fullPath)
        {
            try
            {
                string root = Path.GetFullPath(FileName.ProfileDir);
                if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                {
                    root += Path.DirectorySeparatorChar;
                }
                string target = Path.GetFullPath(fullPath);
                return target.StartsWith(root, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public override void OnDisconnect()
        {
            ClientManager.RemoveClient(this.Socket);
        }

        /// <summary>昵称变化时维护 昵称→会话 索引（登录、改名都会走到这里）。</summary>
        protected override void OnNicknameChanged(string oldNickname, string newNickname)
        {
            ClientManager.BindNickname(this.Parent, oldNickname, newNickname);
        }

        public override void OnPacket(InPacket iPacket)
        {
            // 单包处理异常不再让整条连接挂掉：记录后继续处理后续包
            try
            {
                OnPacketCore(iPacket);
            }
            catch (Exception ex)
            {
                // 异常日志必须能定位到具体包：包名（查不到用 uint 十进制）+ 长度 + 出错偏移 + 载荷前 64 字节 + 堆栈。
                // 否则只剩 "Index was out of range"，无法判断是哪个包、哪个字段读越界。
                int len = iPacket?.Length ?? 0;
                KartRider.Logging.Log.Warn(
                    $"[Packet] 处理异常 {Parent?.Client?.Nickname}@{Parent?.Client?.GetRemoteAddress()}: "
                    + $"packet={DescribePacket(iPacket)} len={len} pos={iPacket?.Position ?? 0} "
                    + $"data={PacketRing.GroupHex(iPacket?.GetRawBuffer(), 0, Math.Min(len, 64))} "
                    + $"{ex.GetType().Name} {ex.Message}",
                    ex);
            }
        }

        /// <summary>仅用于异常日志：把帧首 4 字节解析成包名，查不到枚举名时返回 uint 十进制值。</summary>
        private static string DescribePacket(InPacket packet)
        {
            if (packet == null || packet.Length < 4)
            {
                return "?";
            }
            uint opcode = BitConverter.ToUInt32(packet.GetRawBuffer(), 0);
            return Enum.GetName(typeof(PacketName), opcode) ?? opcode.ToString();
        }

        private void OnPacketCore(InPacket iPacket)
        {
            lock (this.Parent.m_lock)
            {
                iPacket.Position = 0;
                uint hash = iPacket.ReadUInt();
                var packetEmu = (PacketName)hash;

                // 先记录、后分发：聊天服(MsgrServer)的包原来在 Dispatch 命中后直接 return，会被漏记。
                // 用底层缓冲直接记录，避免每包 ToArray() 复制。
                if (Log.IsPacketTraceEnabled || Log.IsPacketRingEnabled)
                {
                    Log.Packet("C", this.Parent.Client.Nickname, iPacket.GetRawBuffer(), iPacket.Length);
                }

                // Mod 优先：保持原有行为，Mod 命中后不再进入内置处理。
                if (PacketDispatcher.Dispatch(typeof(MsgrServer), packetEmu, iPacket, iPacket.ToArray(), this.Parent, null))
                    return;

                // 开局等待期：任意包都算“客户端已就绪”（UDP 同步包要等进赛道才发，不可靠）
                RoomManager.MarkPlayerPacket(this.Parent.Client.Nickname);

                // 内置处理器与 Mod 使用同一套 IPacketHandler 机制，但放在 Mod 之后执行。
                // throwOnHandlerError: 保持原来的包级异常日志（OnPacket 的 catch）不变。
                if (PacketDispatcher.Dispatch(typeof(ClientSession), packetEmu, iPacket, iPacket.ToArray(), this.Parent, null, throwOnHandlerError: true, builtInOnly: true))
                    return;
            }
        }
    }
}
