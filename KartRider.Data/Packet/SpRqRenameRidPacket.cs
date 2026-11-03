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
    /// <summary>处理 <see cref="PacketName.SpRqRenameRidPacket"/> 数据包。</summary>
    internal sealed class SpRqRenameRidPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.SpRqRenameRidPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var oldNickname = session.Client.Nickname;
            var newNickname = inPacket.ReadString(false);

            if (!ClientSession.IsSafeNickname(newNickname))
            {
                // 拒绝含路径分隔符/穿越字符的昵称（原实现直接拼路径做 Directory.Move，可越出安装目录）
                Log.Warn($"[Rename] 非法昵称: {newNickname}");
                using (OutPacket outPacket = new OutPacket("SpRpRenameRidPacket"))
                {
                    outPacket.WriteInt(1);
                    session.Client.Send(outPacket);
                }
                return true;
            }

            if (!FileName.FileNames.ContainsKey(oldNickname))
            {
                // 旧昵称未加载，拒绝改名
                using (OutPacket outPacket = new OutPacket("SpRpRenameRidPacket"))
                {
                    outPacket.WriteInt(1);
                    session.Client.Send(outPacket);
                }
                return true;
            }
            var filename = FileName.FileNames[oldNickname];
            string newfile = Path.GetFullPath(Path.Combine(FileName.ProfileDir, newNickname));
            if (!ClientSession.IsUnderProfileDir(newfile))
            {
                Log.Warn($"[Rename] 目标路径越界: {newfile}");
                using (OutPacket outPacket = new OutPacket("SpRpRenameRidPacket"))
                {
                    outPacket.WriteInt(1);
                    session.Client.Send(outPacket);
                }
                return true;
            }

            if (Directory.Exists(filename.NicknameDir))
            {
                if (Directory.Exists(newfile))
                {
                    using (OutPacket outPacket = new OutPacket("SpRpRenameRidPacket"))
                    {
                        outPacket.WriteInt(1);
                        session.Client.Send(outPacket);
                    }
                }
                else
                {
                    Directory.Move(filename.NicknameDir, newfile);

                    // 更新所有存储Nickname的地方为新昵称
                    // 1. 更新 SessionGroup.Nickname
                    session.Client.Nickname = newNickname;

                    // 2. 更新 FileName.FileNames 的 key
                    FileName.FileNames.TryRemove(oldNickname, out _);
                    FileName.Load(newNickname);

                    // 3. 更新 ClientManager.NicknameToUserNO
                    ClientManager.UpdateNickname(oldNickname, newNickname);

                    // 3.1 迁移Bingo缓存（面板、连线、次数一起跟到新昵称）
                    Bingo.MigrateNickname(oldNickname, newNickname);

                    // 4. 同步房间与UDP映射（防止改名后房间开局数据错乱、房间残留幽灵成员）
                    RoomManager.OnNicknameChanged(oldNickname, newNickname);
                    UdpServer.OnNicknameChanged(oldNickname, newNickname);

                    // 5. 删除可能被意外重建的旧昵称目录
                    string oldDir = Path.GetFullPath(Path.Combine(FileName.ProfileDir, oldNickname));
                    if (Directory.Exists(oldDir))
                    {
                        Directory.Delete(oldDir, true);
                    }

                    if (ProfileService.SettingConfig.Name == oldNickname)
                    {
                        ProfileService.SettingConfig.Name = newNickname;
                        ProfileService.SaveSettings();
                    }

                    using (OutPacket outPacket = new OutPacket("SpRpRenameRidPacket"))
                    {
                        outPacket.WriteInt(0);
                        session.Client.Send(outPacket);
                    }
                }
            }
            return true;
        }
    }
}
