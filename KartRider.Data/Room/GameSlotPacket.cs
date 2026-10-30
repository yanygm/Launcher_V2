using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ExcData;
using KartRider.IO.Packet;
using KartRider.Logging;
using Profile;

namespace KartRider;

public class SlotData
{
    private static readonly Random _random = new Random();

    public static void GameSlotPacket(SessionGroup Parent, InPacket iPacket)
    {
        int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            return;
        }

        Player player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);
        int id = iPacket.ReadInt();
        uint item = iPacket.ReadUInt();
        byte type = iPacket.ReadByte();

        if (id == player.ID)
        {
            if (type <= 2) // 道具获取
            {
                byte[] segment = iPacket.ReadBytes(5);
                uint ts1 = iPacket.ReadUInt();
                uint ts2 = iPacket.ReadUInt();
                float x = iPacket.ReadFloat();
                float y = iPacket.ReadFloat();
                float z = iPacket.ReadFloat();
                short playerRank = iPacket.ReadShort();
                byte unk1 = iPacket.ReadByte();
                byte[] data2 = iPacket.ReadBytes(4);
                byte unk2 = iPacket.ReadByte();
                short skill2 = iPacket.ReadShort();
                byte[] data3 = iPacket.ReadBytes(21);
                uint id2 = iPacket.ReadUInt();
                uint ticks = iPacket.ReadUInt();

                // 记录该玩家坐标/圈数/累计里程（每个玩家在自己的连接上报，room.Tracks 聚合全房间）
                room.UpdateTrack(id, x, y, ts1);
                // 按名次档位（人数+名次表）加权抽道具；已达稀有上限时排除天使/锁/闪电
                int playerCount = room.GetCount();
                int rank = ItemRule.ComputeRank(room, id, playerRank, playerCount);
                bool rareCapped = room.RareCount(id) >= ItemRule.RareCapPerGame;
                // 统一由服务端抽签（type=2 时 skill2 是“被切换的道具”，不能直接回给客户端）
                short skill = ItemRule.Roll(room.GameType, playerCount, rank, rareCapped);

                skill = GetItemSkill(Parent.Client.Nickname, skill);

                // 词条“获得[道具]时转换为[道具]”：按 transformByKart 概率改写抽到的道具
                int itemKart = type == 1 ? (ProfileService.GetProfileConfig(Parent.Client.Nickname)?.RiderItem?.Set_Kart ?? 0) : 0;
                if (itemKart != 0
                    && ClientItemRules.TryGetTransform(itemKart, skill, out ClientItemRules.Rule transformRule, out int transformProb)
                    && Random.Shared.Next(100) < transformProb)
                {
                    Log.Debug($"[Transform] {Parent.Client.Nickname} 车 {itemKart}：{skill} → {transformRule.To}（{transformProb}%）");
                    skill = transformRule.To;
                }
                if (ItemRule.IsRare(skill) && !room.TryConsumeRare(id, ItemRule.AllowLimit(skill) > 0 ? ItemRule.AllowLimit(skill) : 2))
                {
                    // 词条转换后变成稀有道具但名额已满：退回加速器
                    skill = ItemRule.Booster;
                }
                using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
                {
                    oPacket.WriteInt(id);
                    oPacket.WriteUInt(item);
                    oPacket.WriteByte(type);
                    oPacket.WriteBytes(segment);
                    oPacket.WriteUInt(ts1);
                    oPacket.WriteUInt(ts2);
                    oPacket.WriteFloat(x);
                    oPacket.WriteFloat(y);
                    oPacket.WriteFloat(z);
                    oPacket.WriteShort(skill);
                    oPacket.WriteByte(1);
                    oPacket.WriteBytes(data2);
                    oPacket.WriteByte(2);
                    oPacket.WriteShort(skill);
                    oPacket.WriteBytes(data3);
                    oPacket.WriteUInt(id2);
                    oPacket.WriteUInt(ticks);
                    MultyPlayer.BroadCast(roomId, oPacket);
                }
                return;
            }
            else if (type is 5 or 7 or 8 or 17)
            {
                using (OutPacket oPacket = new OutPacket())
                {
                    oPacket.WriteBytes(iPacket.ToArray());
                    MultyPlayer.BroadCast(roomId, oPacket);
                }
                return;
            }
            else if (type is 9 or 12) // 9道具获得通知 12道具投掷/命中动画
            {
                using (OutPacket oPacket = new OutPacket())
                {
                    oPacket.WriteBytes(iPacket.ToArray());
                    MultyPlayer.BroadCast(roomId, oPacket, Parent.Client.Nickname);
                }

                return;
            }
            else if (type == 10) // 道具使用
            {
                byte uni = iPacket.ReadByte();
                byte success = iPacket.ReadByte();
                byte unk = iPacket.ReadByte();
                var skill = iPacket.ReadShort();
                if (success == 1 || success == 2)
                {
                    // 0) 客户端词条（"使用[X]时获得[Y]"）：直接来自 item.kml
                    int useKartId = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.RiderItem?.Set_Kart ?? 0;
                    short clientBonus = 0;
                    bool handledByClient = useKartId != 0
                                           && ClientItemRules.TryOnUse(useKartId, skill, out clientBonus);
                    if (handledByClient)
                    {
                        AddItemSkill(roomId, id, Parent, clientBonus);
                    }

                    Log.Debug($"GameSlotPacket, Mapping. Skill = {skill}");
                }
                using (OutPacket oPacket = new OutPacket())
                {
                    oPacket.WriteBytes(iPacket.ToArray());
                    MultyPlayer.BroadCast(roomId, oPacket, Parent.Client.Nickname);
                }
                return;
            }
            else if (type == 11) // 被道具攻击
            {
                var uni = iPacket.ReadByte();
                var skill = iPacket.ReadShort();

                // 0) 客户端词条（"被[X]攻击时获得[Y]"）：直接来自 item.kml
                int hitKartId = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.RiderItem?.Set_Kart ?? 0;
                short clientBonus = 0;
                bool handledByClient = hitKartId != 0
                                       && ClientItemRules.TryOnAttacked(hitKartId, skill, out clientBonus);
                if (handledByClient)
                {
                    AttackedSkill(roomId, id, Parent, type, uni, clientBonus);
                }

                Log.Debug($"GameSlotPacket, Attacked. Skill = {skill}");
                return;
            }
        }
    }

    /// <summary>
    /// 抽一个道具：档位由「房间人数 + 名次」查表决定（见 ItemRule），不再使用里程差分档。
    /// rankAttr 仅为兼容旧调用保留，传 null 时按 1位 处理。
    /// </summary>
    public static short RandomItemSkill(string Nickname, byte gameType, string rankAttr = null)
    {
        if (gameType != 2 && gameType != 4)
        {
            return 0;
        }

        RankGroup group = string.IsNullOrEmpty(rankAttr) ? RankGroup.Top : ParseLegacyGroup(rankAttr);
        short skill = ItemRule.RollGroup(gameType, group, excludeRare: false);
        return GetItemSkill(Nickname, skill);
    }

    private static RankGroup ParseLegacyGroup(string rankAttr)
    {
        switch (rankAttr)
        {
            case "highrank": return RankGroup.High;
            case "midrank": return RankGroup.Mid;
            case "lowrank": return RankGroup.Low;
            default: return RankGroup.Top;
        }
    }

    public static short GetItemSkill(string Nickname, short skill, bool applySkillMap = true)
    {
        if (!applySkillMap)
        {
            return skill;
        }

        // 0) 客户端词条（"将[X]变更为[Y]"）：直接来自 item.kml，优先于手写配置
        int clientKartId = ProfileService.GetProfileConfig(Nickname)?.RiderItem?.Set_Kart ?? 0;
        if (clientKartId != 0 && ClientItemRules.TryConvert(clientKartId, skill, out short clientMapped))
        {
            Log.Debug($"[KartRules] {Nickname} 卡丁车 {clientKartId}：道具 {skill} → {clientMapped}");
            return clientMapped;
        }

        return skill;
    }

    /// <summary>每名玩家上一次的道具栏数量。</summary>

    /// <summary>下一次快照是我们自己追加造成的，不参与判定（防止概率道具再被当成“获得”）。</summary>

    /// <summary>每名玩家上一次的道具 id 集合，用于找出“新增道具”。</summary>

    /// <summary>
    /// 词条"获得道具时一定概率获得 X"：只在开箱（道具数增加且不超过 2）时掷一次，
    /// 命中后额外发放目标道具。用 count &lt;= 2 防止自己追加出来的第 3 个再次触发。
    /// </summary>

    public static void AddItemSkill(int roomId, int id, SessionGroup Parent, short skill, byte probability = 100)
    {
        // 概率判断：不触发时直接返回，不发送数据包
        if (probability < 100 && _random.Next(100) >= probability)
        {
            Log.Debug($"[AddItemSkill] {Parent.Client.Nickname} 技能 {skill} 未触发（{probability}%）");
            return;
        }

        // 额外获得的道具已经是词条的目标，不能再被别的词条二次改写（否则会"给 A 却拿到 B"）
        skill = GetItemSkill(Parent.Client.Nickname, skill, applySkillMap: false);

        // 稀有道具名额（天使/锁/闪电）每局合计最多 2 个
        if (ItemRule.IsRare(skill))
        {
            GameRoom room = RoomManager.GetRoom(roomId);
            if (room == null || !room.TryConsumeRare(id, ItemRule.AllowLimit(skill) > 0 ? ItemRule.AllowLimit(skill) : 2))
            {
                Log.Debug($"[ItemRule] {Parent.Client.Nickname} 稀有道具已达上限，放弃额外获得 {skill}");
                return;
            }
        }

        using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
        {
            oPacket.WriteInt(id);
            oPacket.WriteUInt(uint.MaxValue);
            oPacket.WriteByte(10);
            oPacket.WriteHexString("001000");
            oPacket.WriteShort(skill);
            oPacket.WriteByte(1);
            oPacket.WriteBytes(new byte[3]);
            oPacket.WriteByte(2);
            oPacket.WriteShort(skill);
            oPacket.WriteBytes(new byte[5]);
            Parent.Client.Send(oPacket);
            BroadCast(roomId, id, Parent.Client.Nickname, skill);
        }
    }

    public static void AttackedSkill(int roomId, int id, SessionGroup Parent, byte type, byte uni, short skill, byte probability = 100)
    {
        // 概率判断：不触发时直接返回，不发送数据包
        if (probability < 100 && _random.Next(100) >= probability)
        {
            Log.Debug($"[AttackedSkill] {Parent.Client.Nickname} 技能 {skill} 未触发（{probability}%）");
            return;
        }

        // 额外获得的道具已经是词条的目标，不能再被别的词条二次改写（否则会"给 A 却拿到 B"）
        skill = GetItemSkill(Parent.Client.Nickname, skill, applySkillMap: false);

        // 稀有道具名额（天使/锁/闪电）每局合计最多 2 个
        if (ItemRule.IsRare(skill))
        {
            GameRoom room = RoomManager.GetRoom(roomId);
            if (room == null || !room.TryConsumeRare(id, ItemRule.AllowLimit(skill) > 0 ? ItemRule.AllowLimit(skill) : 2))
            {
                Log.Debug($"[ItemRule] {Parent.Client.Nickname} 稀有道具已达上限，放弃被攻击获得 {skill}");
                return;
            }
        }

        using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
        {
            oPacket.WriteInt(id);
            oPacket.WriteUInt();
            oPacket.WriteByte(type);
            oPacket.WriteByte(uni);
            oPacket.WriteShort(skill);
            oPacket.WriteByte(1);
            oPacket.WriteShort();
            oPacket.WriteByte(2);
            oPacket.WriteShort(skill);
            oPacket.WriteBytes(new byte[5]);
            Parent.Client.Send(oPacket);
            BroadCast(roomId, id, Parent.Client.Nickname, skill);
        }
    }

    public static void BroadCast(int roomId, int id, string Nickname, short skill, uint ticks = 0)
    {
        using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
        {
            oPacket.WriteInt(id);
            oPacket.WriteUInt(uint.MaxValue);
            oPacket.WriteByte(1);
            oPacket.WriteByte(0);
            oPacket.WriteHexString("00 00 00 F0");
            oPacket.WriteUInt(ticks == 0 ? MultyPlayer.ConvertTick() : ticks);
            oPacket.WriteBytes(new byte[16]);
            oPacket.WriteShort(skill);
            oPacket.WriteByte(1);
            oPacket.WriteHexString("FF FF 00 00");
            oPacket.WriteByte(2);
            oPacket.WriteShort(skill);
            oPacket.WriteBytes(new byte[13]);
            oPacket.WriteHexString("00 00 00 F0 01 00 00 00");
            oPacket.WriteInt(id);
            oPacket.WriteUInt(ticks == 0 ? MultyPlayer.ConvertTick() : ticks);
            MultyPlayer.BroadCast(roomId, oPacket, Nickname);
        }
    }

    /// <param name="roomId">房间 id</param>
    /// <param name="victimId">被攻击玩家 id（客户端会话 id，同 AddItemSkill 的 id 参数；动画起点坐标来源）</param>
    /// <param name="attackerId">攻击者 ObID（Player.ID，8~15；仅用于定位攻击者昵称）</param>
    /// <param name="itemId">攻击道具 id（导弹 7、水炸弹 9、水苍蝇 4 等）</param>
    public static void SimulateAttack(int roomId, int victimId, int attackerId, short itemId)
    {
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            Console.WriteLine("[SimulateAttack] 房间 {0} 不存在，模拟攻击失败", roomId);
            return;
        }

        // 攻击者在房间内时，按其技能配置转换道具（能力卡可能改变道具效果）
        string attackerName = null;
        foreach (RoomMember member in room.ObIDs)
        {
            if (member is Player p && p.ID == attackerId)
            {
                attackerName = p.Nickname;
                break;
            }
        }
        if (attackerName != null)
        {
            itemId = GetItemSkill(attackerName, itemId);
        }

        // 动画位置：优先取受害者的最后已知坐标（无跟踪数据时用赛道起点附近默认值）
        float ax = 450f, ay = 600f, az = 120f;
        if (room.Tracks.TryGetValue(victimId, out TrackPos vt))
        {
            ax = vt.X;
            ay = vt.Y;
        }

        uint tick = MultyPlayer.ConvertTick();

        // ===== 第一个包：投掷动画（攻击前提醒）=====
        // 结构对照日志 L11（payload=102，lenfield=0x52=off20 之后字节数）：
        //   id=0 | item=0 | type=0C | uni=00 | 00 00 | 00 00 00 00 | 0x52 |
        //   固定标识(8) | 事件序号(4) | 01 | 00 00 00 | skill(2) | 时间戳(4) | 帧号(4)=05 |
        //   位置xyz(12) | 3x3旋转矩阵(36) | 00×8
        using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
        {
            oPacket.WriteInt(victimId);                         // id = 0
            oPacket.WriteUInt(0);                               // item = 0
            oPacket.WriteByte(12);                              // type = 12 道具动画
            oPacket.WriteByte(0);                               // uni = 0（投掷）
            oPacket.WriteShort(0);                              // 00 00
            oPacket.WriteInt(0);                                // 00 00 00 00
            oPacket.WriteInt(0x52);                             // 长度字段 = 82
            oPacket.WriteHexString("8E 03 29 11 AD 04 4C 1D");  // 固定标识（真实包全部一致）
            oPacket.WriteHexString("51 00 00 10");              // 事件序号（真实包按事件递增）
            oPacket.WriteByte(1);                               // 01 = 投掷动画
            oPacket.WriteBytes(new byte[3]);                    // 00 00 00
            oPacket.WriteShort(itemId);                         // 攻击道具 skill（如 7=导弹）
            oPacket.WriteUInt(tick);                            // 时间戳
            oPacket.WriteInt(attackerId);                       // 攻击者ID
            oPacket.WriteFloat(ax);                             // 位置 x
            oPacket.WriteFloat(ay);                             // 位置 y
            oPacket.WriteFloat(az);                             // 位置 z
            // 3x3 单位旋转矩阵（行优先），真实包为道具姿态矩阵
            oPacket.WriteFloat(1f); oPacket.WriteFloat(0f); oPacket.WriteFloat(0f);
            oPacket.WriteFloat(0f); oPacket.WriteFloat(1f); oPacket.WriteFloat(0f);
            oPacket.WriteFloat(0f); oPacket.WriteFloat(0f); oPacket.WriteFloat(1f);
            oPacket.WriteBytes(new byte[8]);                    // 尾部填充（真实包 8 字节全零）
            MultyPlayer.BroadCast(roomId, oPacket);             // 广播全房间播放投掷动画（攻击前提醒）
        }

        // ===== 第二个包：被攻击（type=11，触发被攻击判定）=====
        // 结构对照日志 L17（payload=23，紧随 type=12 投掷动画之后，才是真正的"被攻击"）：
        //   id(4)=0 | item(4)=0 | type(1)=0B | uni(1)=00 | skill(2)=攻击道具 | 00×11
        using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
        {
            oPacket.WriteInt(victimId);                         // id = 0
            oPacket.WriteUInt(0);                               // item = 0
            oPacket.WriteByte(11);                              // type = 11 被攻击
            oPacket.WriteByte(0);                               // uni = 0
            oPacket.WriteShort(itemId);                         // 攻击道具 skill（如 7=导弹）
            oPacket.WriteBytes(new byte[11]);                   // 00×11（真实包全零）
            MultyPlayer.BroadCast(roomId, oPacket);             // 广播被攻击包，客户端触发被攻击
        }
        Console.WriteLine("[SimulateAttack] 房间 {0}: 玩家 {1} 投掷道具 {2}（type=12 提醒 + type=11 被攻击双包，位置 ({3:0.0}, {4:0.0}, {5:0.0})）",
            roomId, attackerName ?? attackerId.ToString(), itemId, ax, ay, az);
    }
}