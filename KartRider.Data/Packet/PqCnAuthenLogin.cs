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
    /// <summary>处理 <see cref="PacketName.PqCnAuthenLogin"/> 数据包。</summary>
    internal sealed class PqCnAuthenLogin : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqCnAuthenLogin;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            inPacket.ReadInt();
            inPacket.ReadInt();
            string dataPacket = Base64Helper.Decode(inPacket.ReadString(true));
            DataPacket packet = JsonHelper.Deserialize<DataPacket>(dataPacket);
            if (packet == null) return true;
            // 原实现是 &&（且），空昵称只要版本匹配就会放行，随后 Length 空引用；
            // 这里改为 ||，任一条件不满足即拒绝。
            if (string.IsNullOrEmpty(packet.Nickname)
                || (ProfileService.SettingConfig.ClientVersion != 0
                    && packet.ClientVersion != ProfileService.SettingConfig.ClientVersion))
            {
                Log.Warn($"[Login] 拒绝登录：昵称='{packet.Nickname}' 版本={packet.ClientVersion}");
                return true;
            }
            if (ClientManager.HasClientWithNickname(packet.Nickname))
            {
                var existingConfig = ProfileService.GetProfileConfig(packet.Nickname);
                if (existingConfig.Rider.BanType == 0)
                {
                    existingConfig.Rider.BanType = 1;
                    ProfileService.Save(packet.Nickname, existingConfig);
                }
            }
            using (OutPacket outPacket = new OutPacket("PrCnAuthenLogin"))
            {
                outPacket.WriteInt(1);
                outPacket.WriteString("pnlcdfngkdjfdhdermnkicqknmqrnjnkrlpdirerjrqkcllhpckngophnrrfclgiojmopomonkjilgmheoldpmmcdokgdqljqcnkrplffhflqdnchherghnhoihgfnon");
                outPacket.WriteByte(0);
                outPacket.WriteString("https://www.tiancity.com/agreement");
                session.Client.Send(outPacket);
            }
            IPEndPoint clientEndPoint = session.Client.Socket.RemoteEndPoint as IPEndPoint;
            if (clientEndPoint == null) return true;
            string clientId = ClientManager.GetClientId(clientEndPoint);
            session.Client.Nickname = packet.Nickname;
            EnsureProfileLoaded(packet.Nickname);
            Bingo.LoadProgress(packet.Nickname);
            uint UserNO = ClientManager.GetUserNO(packet.Nickname);
            var loginConfig = ProfileService.GetProfileConfig(packet.Nickname);
            loginConfig.Rider.ClientId = clientId;
            if (packet.Nickname.Length > 1 && packet.Nickname.StartsWith("ob", StringComparison.OrdinalIgnoreCase))
            {
                if (loginConfig.Rider.pmap == 0)
                {
                    loginConfig.Rider.pmap = 718;
                }
            }
            ProfileService.Save(packet.Nickname, loginConfig);
            return true;
        }

        /// <summary>
        /// 玩家连线并上报昵称后加载档案；后续数据包直接使用 ProfileService 缓存。
        /// </summary>
        private static void EnsureProfileLoaded(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }
            if (!FileName.FileNames.ContainsKey(nickname))
            {
                FileName.Load(nickname);
            }
            ProfileService.Load(nickname);
        }
    }
}
