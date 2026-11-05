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
    /// <summary>处理 <see cref="PacketName.PqLogin"/> 数据包。</summary>
    internal sealed class PqLogin : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqLogin;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            GameDataReset.DataReset(session.Client.Nickname);

            // 管理后台封禁检查：IP 封禁只影响本次登录（解封后立即恢复），昵称封禁持久化到档案
            ushort banType = AdminWeb.GetLoginBanType(session.Client.Nickname, session.Client.GetRemoteAddress(), out bool bannedNow);

            bool PcMsgPassport = false;

            using (OutPacket outPacket = new OutPacket("PrLogin"))
            {
                outPacket.WriteInt(banType);
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteUInt(ClientManager.GetUserNO(session.Client.Nickname));
                outPacket.WriteString(session.Client.Nickname); // UserID
                outPacket.WriteByte(2); // 2
                outPacket.WriteByte(1);
                outPacket.WriteByte(0);
                outPacket.WriteInt(0);
                outPacket.WriteByte(0);
                outPacket.WriteHexString("FF FF 5F 54");
                outPacket.WriteUInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.pmap);
                for (int i = 0; i < 11; i++)
                {
                    outPacket.WriteInt(0);
                }
                outPacket.WriteByte(0);
                outPacket.WriteEndPoint(IPAddress.Any, ProfileService.SettingConfig.ServerPort);
                outPacket.WriteEndPoint(IPAddress.Any, (ushort)(ProfileService.SettingConfig.ServerPort + 1));
                outPacket.WriteByte(0);
                outPacket.WriteByte(0);
                outPacket.WriteByte((byte)(PcMsgPassport ? 1 : 0));
                outPacket.WriteByte(0);
                outPacket.WriteString("");
                outPacket.WriteInt(0);
                outPacket.WriteByte(1);
                outPacket.WriteString("content");
                outPacket.WriteInt(0);
                outPacket.WriteInt(1);
                outPacket.WriteString("cc");
                outPacket.WriteString(KartRhoFile.regionCode);
                outPacket.WriteInt(6);
                outPacket.WriteString("content");
                outPacket.WriteInt(0);
                outPacket.WriteInt(2);
                outPacket.WriteString("name");
                outPacket.WriteString("dynamicPpl");
                outPacket.WriteString("enable");
                outPacket.WriteString("false");
                outPacket.WriteInt(1);
                outPacket.WriteString("region");
                outPacket.WriteInt(0);
                outPacket.WriteInt(1);
                outPacket.WriteString("szId");
                outPacket.WriteString(ProfileService.SettingConfig.LocaleID.ToString());
                outPacket.WriteInt(0);
                outPacket.WriteString("content");
                outPacket.WriteInt(0);
                outPacket.WriteInt(3);
                outPacket.WriteString("name");
                outPacket.WriteString("grandprix");
                outPacket.WriteString("enable");
                outPacket.WriteString("true");
                outPacket.WriteString("visible");
                outPacket.WriteString("true");
                outPacket.WriteInt(0);
                outPacket.WriteString("content");
                outPacket.WriteInt(0);
                outPacket.WriteInt(3);
                outPacket.WriteString("name");
                outPacket.WriteString("endingBanner");
                outPacket.WriteString("enable");
                outPacket.WriteString("false");
                outPacket.WriteString("value");
                outPacket.WriteString("https://yanygm.github.io/Launcher_V2/");
                outPacket.WriteInt(0);
                outPacket.WriteString("content");
                outPacket.WriteInt(0);
                outPacket.WriteInt(3);
                outPacket.WriteString("name");
                outPacket.WriteString("themeXyy");
                outPacket.WriteString("enable");
                outPacket.WriteString("true");
                outPacket.WriteString("visible");
                outPacket.WriteString("true");
                outPacket.WriteInt(0);
                outPacket.WriteString("content");
                outPacket.WriteInt(0);
                outPacket.WriteInt(3);
                outPacket.WriteString("name");
                outPacket.WriteString("themeKorea");
                outPacket.WriteString("enable");
                outPacket.WriteString("true");
                outPacket.WriteString("visible");
                outPacket.WriteString("true");
                outPacket.WriteInt(0);
                outPacket.WriteString("content");
                outPacket.WriteInt(0);
                outPacket.WriteInt(5);
                outPacket.WriteString("name");
                outPacket.WriteString("timeAttack");
                outPacket.WriteString("enable");
                outPacket.WriteString("true");
                outPacket.WriteString("visible");
                outPacket.WriteString("true");
                outPacket.WriteString("value");
                outPacket.WriteString("village_R01");
                outPacket.WriteString("maxReplayFileCount");
                outPacket.WriteString("250");
                outPacket.WriteInt(0);
                outPacket.WriteByte(0);
                outPacket.WriteByte(ProfileService.GetProfileConfig(session.Client.Nickname).GameOption.Set_screen);
                outPacket.WriteByte(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.IdentificationType);
                session.Client.Send(outPacket);
            }
            if (PcMsgPassport)
            {
                using (OutPacket outPacket = new OutPacket("PcMsgPassport"))
                {
                    outPacket.WriteString(session.Client.Nickname);
                    outPacket.WriteString("2000142541");
                    outPacket.WriteString("meemllpigrppochjepdmgclfekpocdikifemgnkddeierhkekiefcidhrreienedmrcemjcjpqmjpkolfjfpggiqiefelqleondcpennpmpfenhpfomdpfqdpdpggdjm", true);
                    session.Client.Send(outPacket);
                }
            }
            if (bannedNow)
            {
                Log.Debug($"[Ban] 拒绝登录并断开：{session.Client.Nickname}@{session.Client.GetRemoteAddress()}");
                session.Client.Disconnect();
                return true;
            }
            if (ProfileService.GetProfileConfig(session.Client.Nickname).Rider.BanType == 1)
            {
                var banConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
                banConfig.Rider.BanType = 0;
                ProfileService.Save(session.Client.Nickname, banConfig);
            }
            return true;
        }
    }
}
