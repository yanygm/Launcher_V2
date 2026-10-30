using System;
using System.Collections.Generic;
using KartRider.Logging;

namespace KartRider
{
    /// <summary>
    /// 开局道具（"比赛开始时获得[X]，[Y]…中的一种"）。
    ///
    /// 数据来源：ClientItemRules（启动时解析客户端 item.kml 得到），
    /// 不再读写 Profile/V2StartItem.json —— 客户端是唯一事实来源。
    /// 客户端没有该车词条时用内置兜底池，避免退回"全表随机"。
    /// </summary>
    public static class V2StartItem
    {
        private static readonly List<short> Fallback = new List<short> { 6, 8, 114, 13 };
        private static readonly HashSet<int> WarnedKarts = new HashSet<int>();

        public static short Pick(string nickname, int kartId)
        {
            List<short> candidates = ResolveCandidates(kartId);
            if (candidates.Count == 0)
            {
                return Fallback[Random.Shared.Next(Fallback.Count)];
            }

            if (kartId != 0 && !(ClientItemRules.TryGet(kartId, out ClientItemRules.KartRule rule) && rule.StartItems.Count > 0))
            {
                bool first;
                lock (WarnedKarts)
                {
                    first = WarnedKarts.Add(kartId);
                }
                if (first)
                {
                    Log.Debug($"[V2StartItem] 卡丁车 {kartId} 客户端无开局词条，使用兜底池 {string.Join(",", candidates)}");
                }
            }

            short chosen = candidates[Random.Shared.Next(candidates.Count)];

            // 开局道具也算"获得"：占用稀有道具名额（天使/锁/闪电，每局每人最多 2 个）
            if (ItemRule.IsRare(chosen))
            {
                int roomId = RoomManager.TryGetRoomId(nickname);
                GameRoom room = roomId == 0 ? null : RoomManager.GetRoom(roomId);
                Player player = roomId == 0 ? null : RoomManager.GetPlayer(roomId, nickname);
                if (room != null && player != null && !room.TryConsumeRare(player.ID, ItemRule.AllowLimit(chosen) > 0 ? ItemRule.AllowLimit(chosen) : 2))
                {
                    List<short> alternatives = candidates.FindAll(c => !ItemRule.IsRare(c));
                    chosen = alternatives.Count > 0
                        ? alternatives[Random.Shared.Next(alternatives.Count)]
                        : ItemRule.Booster;
                }
            }

            Log.Debug($"[V2StartItem] {nickname} kart={kartId} 开局道具 {chosen}（候选 {string.Join(",", candidates)}）");
            return chosen;
        }

        /// <summary>候选池：客户端词条优先，其次内置兜底。</summary>
        public static List<short> ResolveCandidates(int kartId)
        {
            if (kartId != 0 && ClientItemRules.TryGet(kartId, out ClientItemRules.KartRule rule) && rule.StartItems.Count > 0)
            {
                return new List<short>(rule.StartItems);
            }
            return new List<short>(Fallback);
        }

        public static List<short> CurrentCandidates(int kartId)
        {
            return ResolveCandidates(kartId);
        }

        public static void LogEffectiveConfig()
        {
            Log.Info($"[V2StartItem] 开局道具取自客户端词条；未命中时兜底 {string.Join(",", Fallback)}");
        }
    }
}
