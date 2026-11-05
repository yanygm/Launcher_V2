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

                // 按名次档位（人数+名次表）加权抽道具；已达稀有上限时排除天使/锁/闪电
                int playerCount = room.GetCount();
                int rank = ItemRule.ComputeRank(room, id, playerRank, playerCount);
                bool rareCapped = room.RareCount(id) >= ItemRule.RareCapPerGame;
                // 统一由服务端抽签（type=2 时 skill2 是“被切换的道具”，不能直接回给客户端）
                short rolled = ItemRule.Roll(room.GameType, playerCount, rank, rareCapped);
                short skill = rolled;
                bool tuneConverted = false;

                // ① 12 代车改装（kart12 tuning）的「获得即转换」：如 加速器→警灯、香蕉→水地雷。
                //    按“刚获得的那件道具”判定，并且**排在最前面**：
                //    改装对“所有获得途径”生效，不能等 kart 词条把源道具改掉之后再查（否则源就没了）。
                if (type == 1 || type == 2)
                {
                    var rider = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.RiderItem;
                    List<TuneEffect> tuneEffects = KartTuneTable.GetPlayerEffects(Parent.Client.Nickname, rider?.Set_Kart ?? 0, rider?.Set_KartSN ?? 0);
                    if (KartTuneTable.TryRoll(tuneEffects, rolled, TuneEffectKind.Gain, type == 1, out TuneEffect tuneHit, out short tuneTo))
                    {
                        skill = tuneTo;
                        tuneConverted = true;
                    }
                }

                // ② kart 自己的转换表（transformByKart）：只认 type==1，且用“抽签原值”查表。
                //    改装已经命中时不再覆盖，改装（玩家的选择）优先。
                if (type == 1 && !tuneConverted)
                {
                    int itemKart = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.RiderItem?.Set_Kart ?? 0;
                    if (itemKart != 0
                        && ClientItemRules.TryGetTransform(itemKart, rolled, out ClientItemRules.Rule transformRule, out int transformProb)
                        && Random.Shared.Next(100) < transformProb)
                    {
                        Log.Debug($"[Transform] {Parent.Client.Nickname} 车 {itemKart}：{rolled} → {transformRule.To}（{transformProb}%）");
                        skill = transformRule.To;
                    }
                }

                // ③ 词条改写（纯文本规则：获得[X]时转换为[Y] / 将[X]变更为[Y]）：放在最后
                skill = GetItemSkill(Parent.Client.Nickname, skill);

                if (ItemRule.IsRare(skill) && !room.TryConsumeRare(id, ItemRule.AllowLimit(skill) > 0 ? ItemRule.AllowLimit(skill) : 2))
                {
                    // 词条转换后变成稀有道具但名额已满：退回加速器
                    skill = ItemRule.Booster;
                }

                // 夺旗赛客户端只实现了 itemProb_flag 里的道具；车辆改装/词条可能把道具转成
                // 香蕉、云、水炸弹等普通道具，直接下发会让客户端吃道具时闪退，这里强制改回加速器。
                if (GameModes.IsFlagMode(room.GameType) && !ItemRule.IsFlagItem(skill))
                {
                    Log.Info($"[ItemRule] 夺旗赛过滤非法道具 {skill} → {ItemRule.Booster}（{Parent.Client.Nickname}）");
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

                    // 12 代车改装：使用 X 时获得 Y（如 用磁铁获得加速器，组 521）
                    var riderOnUse = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.RiderItem;
                    List<TuneEffect> tuneOnUse = KartTuneTable.GetPlayerEffects(Parent.Client.Nickname, riderOnUse?.Set_Kart ?? 0, riderOnUse?.Set_KartSN ?? 0);
                    if (KartTuneTable.TryRoll(tuneOnUse, skill, TuneEffectKind.FiringGain, false, out TuneEffect onUseHit, out short onUseTo))
                    {
                        AddItemSkill(roomId, id, Parent, onUseTo);
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

                // 12 代车改装：被 X 击中时获得 Y（如 受飞碟攻击获盾牌，组 514）
                var riderFired = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.RiderItem;
                List<TuneEffect> tuneFired = KartTuneTable.GetPlayerEffects(Parent.Client.Nickname, riderFired?.Set_Kart ?? 0, riderFired?.Set_KartSN ?? 0);
                if (KartTuneTable.TryRoll(tuneFired, skill, TuneEffectKind.FiredGain, false, out TuneEffect firedHit, out short firedTo))
                {
                    AttackedSkill(roomId, id, Parent, type, uni, firedTo);
                }

                Log.Debug($"GameSlotPacket, Attacked. Skill = {skill}");
                return;
            }
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

    /// <summary>
    /// 12 代车改装的「获得即转换」：对**所有获得途径**统一套一次。
    /// （开箱/更换由主分支处理，词条奖励、被击奖励、其它改装奖励都走这里）
    /// </summary>
    private static short ApplyTuneGainOnAcquire(string nickname, short item, string source)
    {
        if (item <= 0)
        {
            return item;
        }
        var rider = ProfileService.GetProfileConfig(nickname)?.RiderItem;
        List<TuneEffect> tuneEffects = KartTuneTable.GetPlayerEffects(nickname, rider?.Set_Kart ?? 0, rider?.Set_KartSN ?? 0);
        if (KartTuneTable.TryRoll(tuneEffects, item, TuneEffectKind.Gain, false, out TuneEffect tuneHit, out short tuneTo))
        {
            return tuneTo;
        }
        return item;
    }

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

        // 12 代车改装：改装对「所有获得途径」生效，额外发放的道具也要走一遍「获得即转换」
        skill = ApplyTuneGainOnAcquire(Parent.Client.Nickname, skill, "额外获得");

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

        // 12 代车改装：改装对「所有获得途径」生效，被攻击获得的道具也走一遍「获得即转换」
        skill = ApplyTuneGainOnAcquire(Parent.Client.Nickname, skill, "被击中获得");

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
}