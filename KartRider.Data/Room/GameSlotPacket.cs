using System;
using System.Collections.Generic;
using ExcData;
using KartRider.IO.Packet;
using Profile;

namespace KartRider;

public class SlotData
{
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
            if (type <= 2)
            {
                byte[] data1 = iPacket.ReadBytes(25);
                short skill1 = iPacket.ReadShort();
                byte unk1 = iPacket.ReadByte();
                byte[] data2 = iPacket.ReadBytes(4);
                byte unk2 = iPacket.ReadByte();
                short skill2 = iPacket.ReadShort();
                byte[] data3 = iPacket.ReadBytes(21);
                int id2 = iPacket.ReadInt();
                uint ticks = iPacket.ReadUInt();
                short skill = RandomItemSkill(Parent.Client.Nickname, room.GameType);
                using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
                {
                    oPacket.WriteInt(id);
                    oPacket.WriteUInt(item);
                    oPacket.WriteByte(type);
                    oPacket.WriteBytes(data1);
                    oPacket.WriteShort(skill);
                    oPacket.WriteByte(1);
                    oPacket.WriteBytes(data2);
                    oPacket.WriteByte(2);
                    oPacket.WriteShort(skill);
                    oPacket.WriteBytes(data3);
                    oPacket.WriteInt(id2);
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
            else if (type is 9 or 12)
            {
                using (OutPacket oPacket = new OutPacket())
                {
                    oPacket.WriteBytes(iPacket.ToArray());
                    MultyPlayer.BroadCast(roomId, oPacket, Parent.Client.Nickname);
                }
                return;
            }
            else if (type == 10)
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
            else if(type == 11)
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

    public static short RandomItemSkill(string Nickname, byte gameType)
    {
        if (gameType == 2)
        {
            Random random = new Random();
            int index = random.Next(MultyPlayer.itemProb_indi.Count);
            short skill = MultyPlayer.itemProb_indi[index];
            skill = GetItemSkill(Nickname, skill);
            return skill;
        }
        else if (gameType == 4)
        {
            Random random = new Random();
            int index = random.Next(MultyPlayer.itemProb_team.Count);
            short skill = MultyPlayer.itemProb_team[index];
            skill = GetItemSkill(Nickname, skill);
            return skill;
        }
        return 0;
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
}