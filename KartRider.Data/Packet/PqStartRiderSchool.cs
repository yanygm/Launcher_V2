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

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PqStartRiderSchool"/> 数据包。</summary>
    internal sealed class PqStartRiderSchool : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqStartRiderSchool;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // 记录客户端申请的关卡，便于核对进度上报（等级/关卡）格式
            Log.Info($"[RiderSchool] {session.Client.Nickname} 开始挑战: {ToHex(inPacket.ToArray(), inPacket.Position)}");
            RiderSchool.PrStartRiderSchool(session, session.Client.Nickname);
            return true;
        }

        private static string ToHex(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset >= data.Length)
            {
                return "";
            }
            return BitConverter.ToString(data, offset, Math.Min(data.Length - offset, 64)).Replace('-', ' ');
        }
    }
}
