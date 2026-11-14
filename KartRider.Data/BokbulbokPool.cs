using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using KartRider.Logging;

namespace KartRider;

/// <summary>
/// 客户端 RP 赛（福不福）池子：`kart\zeta_\{区服}\content\bokbulbok\bokbulbok.xml`
///
/// 结构：
/// <code>
/// &lt;bokbulbok&gt;
///   &lt;bokbulbokInfo type='speed' enable='true'&gt;
///     &lt;gradeItemList&gt;
///       &lt;item grade='1' itemCatId='3' itemId='1564' prob='10' /&gt;   ← 好运车（迅）
///       &lt;item grade='2' itemCatId='3' itemId='1401' prob='10' /&gt;   ← 霉运车（V1）
///     &lt;/gradeItemList&gt;
///   &lt;/bokbulbokInfo&gt;
///   &lt;bokbulbokInfo type='item' ...&gt; ... &lt;/bokbulbokInfo&gt;
/// &lt;/bokbulbok&gt;
/// </code>
///
/// 客户端**只认这份表里的车**：抽到的车不在表里，抽车动画会退化成默认（霉运）画面。
/// 服务端没有任何字段能把这个“好运/霉运”等级发给客户端（官服的 GrGetBokbulBokSlotChanger
/// 是空载荷的纯触发包），所以这里把表读进来，用于：
///   ① 后台给池子里的车标 ⚠（客户端不认识）
///   ② 首次启动时用这张表当默认车辆池
/// </summary>
public static class BokbulbokPool
{
    private static readonly object Sync = new();

    public static List<ushort> SpeedGood { get; } = new();
    public static List<ushort> SpeedBad { get; } = new();
    public static List<ushort> ItemGood { get; } = new();
    public static List<ushort> ItemBad { get; } = new();

    public static bool Loaded { get; private set; }

    /// <summary>
    /// 内置兜底（2026-11-14 版客户端 bokbulbok.xml 的实际内容）。
    /// 动态读取成功时会先清空再按文件重建；读不到就用这份，保证后台的 ⚠ 标记仍然准确。
    /// </summary>
    static BokbulbokPool()
    {
        SpeedGood.AddRange(new ushort[] { 1564, 1574, 1587 });
        SpeedBad.AddRange(new ushort[] { 1401, 1430, 1440, 1459, 1466 });
        ItemGood.AddRange(new ushort[] { 1565, 1575, 1588, 1600 });
        ItemBad.AddRange(new ushort[] { 1455, 1467, 1487, 1496, 1510 });
    }

    /// <summary>解析一份 bokbulbok.xml（基础包/补丁包可能各带一份，按 id 去重合并）。</summary>
    public static void Load(byte[] data, string source = null)
    {
        if (data == null || data.Length == 0)
        {
            return;
        }
        try
        {
            XDocument doc;
            using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
            {
                doc = XDocument.Load(ms);
            }
            lock (Sync)
            {
                // 读到真表就以表为准（先清掉内置兜底）
                SpeedGood.Clear();
                SpeedBad.Clear();
                ItemGood.Clear();
                ItemBad.Clear();
                foreach (XElement info in doc.Descendants("bokbulbokInfo"))
                {
                    string type = (info.Attribute("type")?.Value ?? "").Trim().ToLowerInvariant();
                    bool isSpeed = type == "speed";
                    bool isItem = type == "item";
                    if (!isSpeed && !isItem)
                    {
                        continue;
                    }
                    foreach (XElement item in info.Descendants("item"))
                    {
                        // 只关心车辆：itemCatId=3
                        if (!ushort.TryParse(item.Attribute("itemCatId")?.Value, out ushort cat) || cat != 3)
                        {
                            continue;
                        }
                        if (!ushort.TryParse(item.Attribute("itemId")?.Value, out ushort kartId) || kartId == 0)
                        {
                            continue;
                        }
                        if (!int.TryParse(item.Attribute("grade")?.Value, out int grade))
                        {
                            continue;
                        }
                        var good = isSpeed ? SpeedGood : ItemGood;
                        var bad = isSpeed ? SpeedBad : ItemBad;
                        var target = grade == 1 ? good : grade == 2 ? bad : null;
                        if (target == null || target.Contains(kartId))
                        {
                            continue;
                        }
                        target.Add(kartId);
                    }
                }
                Loaded = true;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[Bokbul] 解析失败{(source == null ? "" : " ← " + source)}：{ex.Message}");
        }
    }

    /// <summary>客户端认不认这辆车：1 = 好运（grade1），2 = 霉运（grade2），0 = 不在表里。</summary>
    public static int Grade(int kartId)
    {
        lock (Sync)
        {
            if (SpeedGood.Contains((ushort)kartId) || ItemGood.Contains((ushort)kartId))
            {
                return 1;
            }
            if (SpeedBad.Contains((ushort)kartId) || ItemBad.Contains((ushort)kartId))
            {
                return 2;
            }
        }
        return 0;
    }

    public static void LogSummary()
    {
        lock (Sync)
        {
            Log.Info($"[Bokbul] 客户端 RP 池：竞速 好运 {SpeedGood.Count} / 霉运 {SpeedBad.Count}，" +
                     $"道具 好运 {ItemGood.Count} / 霉运 {ItemBad.Count}" +
                     $"（竞速好运 {string.Join(",", SpeedGood)}；竞速霉运 {string.Join(",", SpeedBad)}）");
        }
    }
}
