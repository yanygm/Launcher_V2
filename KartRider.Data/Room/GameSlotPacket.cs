using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ExcData;
using KartRider.IO.Packet;
using Profile;

namespace KartRider;

public class SlotData
{
    // 个人道具赛概率配置（技能 id 取 item 的 idx 属性），Rho.cs 加载资源文件成功时会被覆盖
    public static XDocument itemProb_indi = BuildItemProb(
        @"<item name='香蕉皮' idx='8' toprank='25' highrank='0' midrank='0' lowrank='0'/>
          <item name='乌云' idx='114' toprank='20' highrank='0' midrank='0' lowrank='0'/>
          <item name='盾牌' idx='10' toprank='40' highrank='25' midrank='0' lowrank='0'/>
          <item name='电磁波' idx='12' toprank='15' highrank='0' midrank='0' lowrank='0'/>
          <item name='大魔王' idx='2' toprank='0' highrank='2' midrank='2' lowrank='2'/>
          <item name='追踪导弹' idx='33' toprank='0' highrank='0' midrank='5' lowrank='3'/>
          <item name='飞碟' idx='3' toprank='0' highrank='0' midrank='5' lowrank='6'/>
          <item name='路障' idx='113' toprank='0' highrank='0' midrank='5' lowrank='5'/>
          <item name='导弹' idx='7' toprank='0' highrank='23' midrank='20' lowrank='0'/>
          <item name='水炸弹' idx='9' toprank='0' highrank='20' midrank='11' lowrank='0'/>
          <item name='水苍蝇' idx='4' toprank='0' highrank='25' midrank='10' lowrank='0'/>
          <item name='闪电' idx='111' toprank='0' highrank='0' midrank='3' lowrank='1'/>
          <item name='加速器' idx='6' toprank='0' highrank='0' midrank='24' lowrank='51'/>
          <item name='磁铁' idx='5' toprank='0' highrank='5' midrank='15' lowrank='32'/>");

    // 组队道具赛概率配置（技能 id 取 item 的 idx 属性），Rho.cs 加载资源文件成功时会被覆盖
    public static XDocument itemProb_team = BuildItemProb(
        @"<item name='香蕉皮' idx='8' toprank='25' highrank='0' midrank='0' lowrank='0'/>
          <item name='乌云' idx='114' toprank='20' highrank='0' midrank='0' lowrank='0'/>
          <item name='盾牌' idx='10' toprank='40' highrank='25' midrank='0' lowrank='0'/>
          <item name='电磁波' idx='12' toprank='13' highrank='0' midrank='5' lowrank='0'/>
          <item name='大魔王' idx='2' toprank='0' highrank='2' midrank='2' lowrank='2'/>
          <item name='追踪导弹' idx='33' toprank='0' highrank='0' midrank='5' lowrank='3'/>
          <item name='飞碟' idx='3' toprank='0' highrank='0' midrank='5' lowrank='3'/>
          <item name='路障' idx='113' toprank='0' highrank='0' midrank='5' lowrank='5'/>
          <item name='导弹' idx='7' toprank='0' highrank='23' midrank='13' lowrank='0'/>
          <item name='水炸弹' idx='9' toprank='0' highrank='15' midrank='7' lowrank='0'/>
          <item name='水苍蝇' idx='4' toprank='0' highrank='25' midrank='10' lowrank='0'/>
          <item name='闪电' idx='111' toprank='0' highrank='0' midrank='3' lowrank='1'/>
          <item name='加速器' idx='6' toprank='0' highrank='0' midrank='20' lowrank='55'/>
          <item name='磁铁' idx='5' toprank='0' highrank='5' midrank='12' lowrank='27'/>
          <item name='透视镜' idx='109' toprank='12' highrank='0' midrank='0' lowrank='0'/>
          <item name='道具锁' idx='110' toprank='0' highrank='0' midrank='3' lowrank='2'/>
          <item name='天使' idx='11' toprank='0' highrank='2' midrank='5' lowrank='2'/>
          <item name='定时水炸弹' idx='13' toprank='0' highrank='3' midrank='5' lowrank='0'/>");

    // 构建道具概率配置 XDocument
    static XDocument BuildItemProb(string itemsXml)
    {
        return XDocument.Parse($"<items>{itemsXml}</items>");
    }

    private static readonly Random _random = new Random();

    public static void GameSlotPacket(SessionGroup Parent, InPacket iPacket)
    {
        var kartConfig = SpecialKartConfig.LoadConfigFromFile(FileName.SpecialKartConfig);
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
                // 按包内 playerRank=0 判定第一名，落后程度用里程差半圈分档发道具
                short skill = RandomItemSkill(Parent.Client.Nickname, room.GameType, GetRankAttrByDist(room, id, playerRank));
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
                    List<short> skills = V2Specs.GetSkills(Parent.Client.Nickname);
                    if (skills.Contains(14) && skill == 5)
                    {
                        AddItemSkill(roomId, id, Parent, 6);
                    }

                    // Ensure profile is loaded before accessing
                    var parentConfig2 = ProfileService.GetProfileConfig(Parent.Client.Nickname);
                    if (kartConfig.SkillMappings.TryGetValue(parentConfig2.RiderItem.Set_Kart, out var kartSkills2))
                    {
                        if (kartSkills2.TryGetValue(skill, out var skillConfig2))
                        {
                            // 传入概率参数，由 AddItemSkill 内部判断是否触发
                            AddItemSkill(roomId, id, Parent, skillConfig2.TargetItemId, skillConfig2.Probability);
                        }
                    }
                    Console.WriteLine("GameSlotPacket, Mapping. Skill = {0}", skill);
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
                List<short> skills = V2Specs.GetSkills(Parent.Client.Nickname);
                if (skills.Contains(13) && skill == 3)
                {
                    AttackedSkill(roomId, id, Parent, type, uni, 10);
                }

                // Ensure profile is loaded before accessing
                var parentConfig = ProfileService.GetProfileConfig(Parent.Client.Nickname);
                if (kartConfig.SkillAttacked.TryGetValue(parentConfig.RiderItem.Set_Kart, out var kartSkills))
                {
                    if (kartSkills.TryGetValue(skill, out var skillConfig))
                    {
                        // 传入概率参数，由 AttackedSkill 内部判断是否触发
                        AttackedSkill(roomId, id, Parent, type, uni, skillConfig.TargetItemId, skillConfig.Probability);
                    }
                }
                Console.WriteLine("GameSlotPacket, Attacked. Skill = {0}", skill);
                return;
            }
        }
    }

    public static short RandomItemSkill(string Nickname, byte gameType, string rankAttr = null)
    {
        XDocument doc;
        if (gameType == 2)
        {
            doc = itemProb_indi;
        }
        else if (gameType == 4)
        {
            doc = itemProb_team;
        }
        else
        {
            return 0;
        }

        if (doc == null)
        {
            return 0;
        }

        List<XElement> items = doc.Descendants("item").ToList();
        if (items.Count == 0)
        {
            return 0;
        }

        Random random = new Random();
        short skill;
        if (rankAttr != null)
        {
            // 按实时位置距离分档选取权重列
            skill = WeightedRandomItem(items, rankAttr, random);
        }
        else
        {
            // 无分档信息（如开局道具）：全部技能随机
            skill = GetItemIdx(items[random.Next(items.Count)]);
        }
        skill = GetItemSkill(Nickname, skill);
        return skill;
    }

    // 与第一名的里程差分档：
    // 无位置数据按名次分档。
    public static string GetRankAttrByDist(GameRoom room, int myId, short playerRank)
    {
        if (playerRank == 0)
        {
            return "toprank"; // 第一名直接用包内 playerRank=0
        }

        // 位置数据不足（<2 人）无法用里程差归一化：按包内名次直接映射档位
        if (room.Tracks.Count < 2)
        {
            int count = room.GetCount();
            if (count < 4)
            {
                // 人数过少（<4）：随机三档
                return Random.Shared.Next(3) switch
                {
                    0 => "highrank",
                    1 => "midrank",
                    _ => "lowrank"
                };
            }
            else if (count < 6)
            {
                if (playerRank >= count - 1)
                {
                    return "lowrank"; // 最后一位
                }
                if (playerRank == 1)
                {
                    return "highrank"; // 次名
                }
                return "midrank"; // 其余名次全部 midrank
            }
            else if (count < 8)
            {
                if (playerRank >= count - 2)
                {
                    return "lowrank"; // 最后二位
                }
                if (playerRank == 1)
                {
                    return "highrank"; // 次名
                }
                return "midrank"; // 其余名次全部 midrank
            }
            else
            {
                return playerRank switch
                {
                    >= 1 and <= 2 => "highrank",
                    >= 3 and <= 5 => "midrank",
                    _ => "lowrank" // 6、7 及其他
                };
            }
        }

        if (!room.Tracks.TryGetValue(myId, out TrackPos my))
        {
            return "lowrank"; // 无位置数据：按落后处理
        }

        float leaderDist = 0f;
        float lapLen = 0f;
        foreach (TrackPos t in room.Tracks.Values)
        {
            if (t.TotalDist > leaderDist)
            {
                leaderDist = t.TotalDist;
            }
            if (t.LapLength > lapLen)
            {
                lapLen = t.LapLength;
            }
        }

        float diff = leaderDist - my.TotalDist;
        if (diff >= 500f)
        {
            return "lowrank";
        }
        else if (diff >= 300f)
        {
            return "midrank";
        }
        else
        {
            return "highrank";
        }
    }

    // 按权重列加权随机选取道具，返回技能 id（item 的 idx 属性）
    private static short WeightedRandomItem(List<XElement> items, string rankAttr, Random random)
    {
        var weightedItems = items
            .Select(item => new { Item = item, Weight = ParseWeight(item, rankAttr) })
            .Where(x => x.Weight > 0)
            .ToList();

        // 该名次权重全为 0（无对应道具）：退化为全部技能随机
        if (weightedItems.Count == 0)
        {
            return GetItemIdx(items[random.Next(items.Count)]);
        }

        int totalWeight = weightedItems.Sum(x => x.Weight);
        int roll = random.Next(totalWeight);
        foreach (var entry in weightedItems)
        {
            roll -= entry.Weight;
            if (roll < 0)
            {
                return GetItemIdx(entry.Item);
            }
        }
        return GetItemIdx(weightedItems[weightedItems.Count - 1].Item);
    }

    private static int ParseWeight(XElement item, string rankAttr)
    {
        return int.TryParse(item.Attribute(rankAttr)?.Value, out int weight) ? weight : 0;
    }

    private static short GetItemIdx(XElement item)
    {
        return short.TryParse(item.Attribute("idx")?.Value, out short idx) ? idx : (short)0;
    }

    public static short GetItemSkill(string Nickname, short skill)
    {
        var kartConfig = SpecialKartConfig.LoadConfigFromFile(FileName.SpecialKartConfig);
        List<short> skills = V2Specs.GetSkills(Nickname);
        for (int i = 0; i < skills.Count; i++)
        {
            if (V2Specs.itemSkill.TryGetValue(skills[i], out var Level) &&
                Level.TryGetValue(skill, out var LevelSkill))
            {
                return LevelSkill;
            }
        }
        var slotConfig = ProfileService.GetProfileConfig(Nickname);
        if (kartConfig.SkillChange.TryGetValue(slotConfig.RiderItem.Set_Kart, out var changes) &&
            changes.TryGetValue(skill, out var skillConfig))
        {
            // 触发几率判断
            if (skillConfig.Probability >= 100 || _random.Next(100) < skillConfig.Probability)
            {
                Console.WriteLine("[SkillChange] 玩家 {0} 道具变更 {1} -> {2} (概率: {3}%)", Nickname, skill, skillConfig.TargetItemId, skillConfig.Probability);
                return skillConfig.TargetItemId;
            }
            else
            {
                Console.WriteLine("[SkillChange] 玩家 {0} 道具变更未触发 {1} (概率: {2}%)", Nickname, skill, skillConfig.Probability);
            }
        }
        return skill;
    }

    public static void AddItemSkill(int roomId, int id, SessionGroup Parent, short skill, byte probability = 100)
    {
        // 概率判断：不触发时直接返回，不发送数据包
        if (probability < 100 && _random.Next(100) >= probability)
        {
            Console.WriteLine("[AddItemSkill] 玩家 {0} 技能 {1} 未触发 (概率: {2}%)", Parent.Client.Nickname, skill, probability);
            return;
        }

        skill = GetItemSkill(Parent.Client.Nickname, skill);
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
            Console.WriteLine("[AttackedSkill] 玩家 {0} 技能 {1} 未触发 (概率: {2}%)", Parent.Client.Nickname, skill, probability);
            return;
        }

        skill = GetItemSkill(Parent.Client.Nickname, skill);
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