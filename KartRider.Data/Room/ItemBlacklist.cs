using System.Collections.Generic;

namespace KartRider
{
    /// <summary>
    /// 客户端 item.kml 里存在、但没有任何获取途径的“内部命名/占位”条目
    /// （韩文 더미=dummy、run_*、test/NPC、%s 占位符等）。
    /// 由扫描工具从 item.kml 生成，禁止下发/展示。
    /// </summary>
    public static class ItemBlacklist
    {
        private static readonly HashSet<(ushort Cat, ushort Id)> Blocked = new HashSet<(ushort, ushort)>
        {
            (1, 147), (1, 149), (1, 159), (1, 175), (1, 176), (1, 231), (1, 245), (1, 246),
            (1, 247), (1, 265), (1, 333), (1, 334), (1, 335), (1, 336), (1, 337), (1, 338),
            (1, 339), (1, 350), (1, 391), (1, 392), (1, 396), (1, 397), (1, 440), (1, 441),
            (1, 442), (1, 443), (1, 444), (1, 467), (1, 486), (1, 487), (1, 488), (1, 489),
            (11, 201), (16, 281), (24, 601), (24, 602), (24, 603), (24, 1131), (24, 1132), (24, 1133),
            (24, 1134), (24, 1212), (24, 1213), (24, 1214), (24, 1215), (56, 2), (63, 1), (63, 2),
            (64, 1), (64, 2), (65, 1), (65, 2), (66, 1), (66, 2),
        };

        public static bool IsBlocked(ushort itemCatId, ushort itemId)
        {
            return Blocked.Contains((itemCatId, itemId));
        }

        public static int Count => Blocked.Count;
    }
}
