using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using KartRider.IO.Packet;
using Profile;

namespace KartRider;

public class Coupon
{
    public string CouponNO { get; set; } // L25
    public int stockId { get; set; }
    public int Count { get; set; }
}

public class Gift
{
    public int stockId { get; set; }
    public DateTime Sent { get; set; }
    public DateTime Receive { get; set; }
    public string Message { get; set; }
    public int ID { get; set; }
    public string Giver { get; set; }
    public bool Received { get; set; } = false;
}

public class RewardBox
{
    public long ID { get; set; }
    public byte Type { get; set; }
    public int stockId { get; set; }
}

public static class CouponList
{
    public static void QueryCoupon(SessionGroup Parent, string Coupon, bool Use = false)
    {
        var CouponList = new List<Coupon>();
        if (File.Exists(FileName.Coupon))
        {
            try
            {
                CouponList = JsonHelper.DeserializeNoBom<List<Coupon>>(FileName.Coupon) ?? new List<Coupon>();
            }
            catch
            {
                CouponList = new List<Coupon>();
            }
        }
        Coupon value = CouponList.FirstOrDefault(x => x.CouponNO == Coupon);
        if (value == null)
        {
            using (OutPacket outPacket = new OutPacket("SpRpQueryCoupon"))
            {
                outPacket.WriteInt(17); // 17-错误;19-已使用
                outPacket.WriteByte(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteByte(0);
                Parent.Client.Send(outPacket);
            }
        }
        else if (value.Count <= 0)
        {
            using (OutPacket outPacket = new OutPacket("SpRpQueryCoupon"))
            {
                outPacket.WriteInt(19); // 17-错误;19-已使用
                outPacket.WriteByte(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteByte(0);
                Parent.Client.Send(outPacket);
            }
        }
        else
        {
            if (Use)
            {
                // 扣减优惠券数量并发送成功响应
                using (OutPacket outPacket = new OutPacket("SpRpUseCoupon"))
                {
                    outPacket.WriteInt(0);
                    Parent.Client.Send(outPacket);
                }
                value.Count--;
                File.WriteAllText(FileName.Coupon, JsonHelper.Serialize(CouponList));
                return;
            }

            using (OutPacket outPacket = new OutPacket("SpRpQueryCoupon"))
            {
                outPacket.WriteInt(0); // 成功
                outPacket.WriteByte(0);
                outPacket.WriteInt(value.stockId);
                outPacket.WriteInt(0);
                outPacket.WriteByte(0);
                Parent.Client.Send(outPacket);
            }
        }
    }

    public static void DuplicatedItem(SessionGroup Parent, int stockId)
    {
        using (OutPacket outPacket = new OutPacket("SpRpDuplicatedItemPacket"))
        {
            outPacket.WriteInt(0); // 0-验证成功;1-验证失败
            outPacket.WriteUShort(0); // itemCatId
            outPacket.WriteUShort(0); // itemId
            outPacket.WriteByte(0);
            outPacket.WriteUShort(0); // itemCount
            Parent.Client.Send(outPacket);
        }
    }

    public static void GiveGift(SessionGroup Parent, string UserName, int stockId, string Message, string Password)
    {
        using (OutPacket outPacket = new OutPacket("PrCnChkPassword"))
        {
            outPacket.WriteByte(1); // 0-密码错误;1-密码正确
            Parent.Client.Send(outPacket);
        }

        if (!FileName.FileNames.ContainsKey(UserName))
        {
            FileName.Load(UserName);
        }
        var filename = FileName.FileNames[UserName];
        var GiftList = new List<Gift>();
        if (File.Exists(filename.GiveGift_LoadFile))
        {
            try
            {
                GiftList = JsonHelper.DeserializeNoBom<List<Gift>>(filename.GiveGift_LoadFile) ?? new List<Gift>();
            }
            catch
            {
                GiftList = new List<Gift>();
            }
        }

        // ID 为列表最大 ID + 1，不存在则为 1
        int newId = GiftList.Count > 0 ? GiftList.Max(g => g.ID) + 1 : 1;

        var gift = new Gift
        {
            ID = newId,
            stockId = stockId,
            Sent = DateTime.Now,
            Receive = DateTime.Now,
            Message = Message,
            Giver = Parent.Client.Nickname
        };
        GiftList.Add(gift);

        File.WriteAllText(filename.GiveGift_LoadFile, JsonHelper.Serialize(GiftList));
    }

    public static void GetGiftList(SessionGroup Parent, bool Received = false)
    {
        if (!FileName.FileNames.ContainsKey(Parent.Client.Nickname))
        {
            FileName.Load(Parent.Client.Nickname);
        }
        var filename = FileName.FileNames[Parent.Client.Nickname];
        var GiftList = new List<Gift>();
        if (File.Exists(filename.GiveGift_LoadFile))
        {
            try
            {
                GiftList = JsonHelper.DeserializeNoBom<List<Gift>>(filename.GiveGift_LoadFile) ?? new List<Gift>();
            }
            catch
            {
                GiftList = new List<Gift>();
            }
        }

        if (Received == false)
        {
            var SentList = GiftList.Where(g => !g.Received).ToList();

            if (SentList.Count <= 0)
            {
                using (OutPacket outPacket = new OutPacket("SpRpGetGiftListIncomingPacket"))
                {
                    outPacket.WriteInt(1);
                    outPacket.WriteInt(1);
                    outPacket.WriteInt(0);
                    Parent.Client.Send(outPacket);
                }
                return;
            }

            int range = 10;//分批次数
            int times = SentList.Count / range + (SentList.Count % range > 0 ? 1 : 0);
            for (int i = 0; i < times; i++)
            {
                var tempList = SentList.GetRange(i * range, (i + 1) * range > SentList.Count ? (SentList.Count - i * range) : range);
                using (OutPacket outPacket = new OutPacket("SpRpGetGiftListIncomingPacket"))
                {
                    outPacket.WriteInt(times);
                    outPacket.WriteInt(i + 1);
                    outPacket.WriteInt(tempList.Count);
                    foreach (var Gift in tempList)
                    {
                        outPacket.WriteInt(Gift.stockId);
                        outPacket.WriteDateTime(Gift.Sent);
                        outPacket.WriteDateTime(Gift.Receive);
                        outPacket.WriteString(Gift.Message);
                        outPacket.WriteInt(Gift.ID);
                        outPacket.WriteString(Gift.Giver);
                        outPacket.WriteBool(Gift.Received);
                        outPacket.WriteHexString("FF FF FF 7F");
                    }
                    Parent.Client.Send(outPacket);
                }
            }
        }
        else
        {
            var ReceiveList = GiftList.Where(g => g.Received).Reverse().Take(10).ToList();

            using (OutPacket outPacket = new OutPacket("SpRpGetGiftListReceivedPacket"))
            {
                outPacket.WriteInt(ReceiveList.Count);
                foreach (var Gift in ReceiveList)
                {
                    outPacket.WriteInt(Gift.stockId);
                    outPacket.WriteDateTime(Gift.Sent);
                    outPacket.WriteDateTime(Gift.Receive);
                    outPacket.WriteString(Gift.Message);
                    outPacket.WriteInt(Gift.ID);
                    outPacket.WriteString(Gift.Giver);
                    outPacket.WriteBool(Gift.Received);
                    outPacket.WriteHexString("FF FF FF 7F");
                }
                Parent.Client.Send(outPacket);
            }
        }
    }

    public static void ReceiveGift(SessionGroup Parent, int giftId)
    {
        if (!FileName.FileNames.ContainsKey(Parent.Client.Nickname))
        {
            FileName.Load(Parent.Client.Nickname);
        }
        var filename = FileName.FileNames[Parent.Client.Nickname];
        var GiftList = new List<Gift>();
        if (File.Exists(filename.GiveGift_LoadFile))
        {
            try
            {
                GiftList = JsonHelper.DeserializeNoBom<List<Gift>>(filename.GiveGift_LoadFile) ?? new List<Gift>();
            }
            catch
            {
                GiftList = new List<Gift>();
            }
        }

        var gift = GiftList.FirstOrDefault(g => g.ID == giftId && !g.Received);
        if (gift != null)
        {
            using (OutPacket outPacket = new OutPacket("SpRepReceiveGiftPacket"))
            {
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
            }
            gift.Received = true;
            gift.Receive = DateTime.Now;
            File.WriteAllText(filename.GiveGift_LoadFile, JsonHelper.Serialize(GiftList));
        }
        else
        {
            using (OutPacket outPacket = new OutPacket("SpRepReceiveGiftPacket"))
            {
                outPacket.WriteInt(1);
                Parent.Client.Send(outPacket);
            }
        }
    }

    public static void Delete(SessionGroup Parent, int giftId, bool Receive = false)
    {
        if (!FileName.FileNames.ContainsKey(Parent.Client.Nickname))
        {
            FileName.Load(Parent.Client.Nickname);
        }
        var filename = FileName.FileNames[Parent.Client.Nickname];
        var GiftList = new List<Gift>();
        if (File.Exists(filename.GiveGift_LoadFile))
        {
            try
            {
                GiftList = JsonHelper.DeserializeNoBom<List<Gift>>(filename.GiveGift_LoadFile) ?? new List<Gift>();
            }
            catch
            {
                GiftList = new List<Gift>();
            }
        }

        string packetName = Receive ? "SpRepDeleteReceiveLogPacket" : "SpRepDeleteGiftPacket";

        var gift = GiftList.FirstOrDefault(g => g.ID == giftId);
        if (gift != null)
        {
            using (OutPacket outPacket = new OutPacket(packetName))
            {
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
            }
            GiftList.Remove(gift);
            File.WriteAllText(filename.GiveGift_LoadFile, JsonHelper.Serialize(GiftList));
        }
        else
        {
            using (OutPacket outPacket = new OutPacket(packetName))
            {
                outPacket.WriteInt(1);
                Parent.Client.Send(outPacket);
            }
        }
    }

    public static void GetRewardBox(SessionGroup Parent)
    {
        if (!FileName.FileNames.ContainsKey(Parent.Client.Nickname))
        {
            FileName.Load(Parent.Client.Nickname);
        }
        var filename = FileName.FileNames[Parent.Client.Nickname];
        var RewardBoxList = new List<RewardBox>();
        if (File.Exists(filename.RewardBox_LoadFile))
        {
            try
            {
                RewardBoxList = JsonHelper.DeserializeNoBom<List<RewardBox>>(filename.RewardBox_LoadFile) ?? new List<RewardBox>();
            }
            catch
            {
                RewardBoxList = new List<RewardBox>();
            }
        }

        if (RewardBoxList.Count <= 0)
        {
            using (OutPacket outPacket = new OutPacket("SpRpEnterRewardBoxStage"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteByte(1);
                Parent.Client.Send(outPacket);
            }
            return;
        }

        int range = 8;//分批次数
        int times = RewardBoxList.Count / range + (RewardBoxList.Count % range > 0 ? 1 : 0);
        for (int i = 0; i < times; i++)
        {
            var tempList = RewardBoxList.GetRange(i * range, (i + 1) * range > RewardBoxList.Count ? (RewardBoxList.Count - i * range) : range);
            using (OutPacket outPacket = new OutPacket("SpRpEnterRewardBoxStage"))
            {
                outPacket.WriteInt(tempList.Count);
                foreach (var RewardBox in tempList)
                {
                    outPacket.WriteLong(RewardBox.ID);
                    outPacket.WriteByte(RewardBox.Type);
                    outPacket.WriteInt(RewardBox.stockId);
                    outPacket.WriteDateTime(DateTime.Now);
                    outPacket.WriteDateTime(DateTime.Now.AddDays(7));
                }
                outPacket.WriteInt(RewardBoxList.Count);
                outPacket.WriteByte((byte)(i + 1));
                Parent.Client.Send(outPacket);
            }
        }
    }

    public static void ReceiveReward(SessionGroup Parent, long RewardBoxId, int stockId)
    {
        if (!FileName.FileNames.ContainsKey(Parent.Client.Nickname))
        {
            FileName.Load(Parent.Client.Nickname);
        }
        var filename = FileName.FileNames[Parent.Client.Nickname];
        var RewardBoxList = new List<RewardBox>();
        if (File.Exists(filename.RewardBox_LoadFile))
        {
            try
            {
                RewardBoxList = JsonHelper.DeserializeNoBom<List<RewardBox>>(filename.RewardBox_LoadFile) ?? new List<RewardBox>();
            }
            catch
            {
                RewardBoxList = new List<RewardBox>();
            }
        }
        var RewardBox = RewardBoxList.FirstOrDefault(r => r.ID == RewardBoxId && r.stockId == stockId);
        if (RewardBox != null)
        {
            using (OutPacket outPacket = new OutPacket("SpRpReceiveRewardItemPacket"))
            {
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
            }
            RewardBoxList.Remove(RewardBox);
            File.WriteAllText(filename.RewardBox_LoadFile, JsonHelper.Serialize(RewardBoxList));
        }
        else
        {
            using (OutPacket outPacket = new OutPacket("SpRpReceiveRewardItemPacket"))
            {
                outPacket.WriteInt(1);
                Parent.Client.Send(outPacket);
            }
        }
    }
}