using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using KartRider.Logging;

namespace KartRider;

/// <summary>
/// ModelMax XML数据（包含所有卡丁车的模型尺寸）
/// </summary>
public static class ModelMax
{
    public const string XmlContent = @"<?xml version='1.0' encoding='utf-8'?>
<ModelMax>
    <kart id='1470' name='R8-中国龙 V1' modelMaxX='0.83' modelMaxY='1.122' />
    <kart id='1476' name='红旗 V1' modelMaxX='0.773429' modelMaxY='1.095391' />
    <kart id='1485' name='黄金骑士 V1' modelMaxX='0.83' modelMaxY='1.122' />
    <kart id='1486' name='阿特密斯 V1' modelMaxX='0.75796' modelMaxY='1.073483' />
    <kart id='1490' name='天翊 V1' modelMaxX='0.83141' modelMaxY='1.44114' />
    <kart id='1492' name='SWORD V1' modelMaxX='0.831644' modelMaxY='1.220663' />
    <kart id='1495' name='黄金游侠 V1' modelMaxX='0.7556404' modelMaxY='1.158996' />
    <kart id='1498' name='猎犬 V1' modelMaxX='0.773429' modelMaxY='1.095391' />
    <kart id='1499' name='机械青龙 V1' modelMaxX='0.833404' modelMaxY='1.115986' />
    <kart id='1501' name='敞篷跑车 V1' modelMaxX='0.767086' modelMaxY='1.318437' />
    <kart id='1503' name='黑色甲虫 V1' modelMaxX='0.753635' modelMaxY='1.072198' />
    <kart id='1504' name='毒刺 V1' modelMaxX='0.83' modelMaxY='1.122' />
    <kart id='1508' name='克洛诺斯 V1' modelMaxX='0.766525' modelMaxY='1.036763' />
    <kart id='1515' name='怪物 V1' modelMaxX='0.79099' modelMaxY='1.10517' />
    <kart id='1523' name='仙界四不像 迅' modelMaxX='0.773709' modelMaxY='1.29294' />
    <kart id='1528' name='爆烈 迅' modelMaxX='0.8003274' modelMaxY='1.049711' />
    <kart id='1529' name='高级爆烈 迅' modelMaxX='0.8003274' modelMaxY='1.049711' />
    <kart id='1530' name='精英爆烈 迅' modelMaxX='0.7924033' modelMaxY='1.049711' />
    <kart id='1531' name='雷霆爆烈 迅' modelMaxX='0.7924033' modelMaxY='1.049711' />
    <kart id='1532' name='合金 迅' modelMaxX='0.8232' modelMaxY='1.041413' />
    <kart id='1533' name='高级合金 迅' modelMaxX='0.8232' modelMaxY='1.041413' />
    <kart id='1534' name='精英合金 迅' modelMaxX='0.8134' modelMaxY='1.030677' />
    <kart id='1535' name='钢铁合金 迅' modelMaxX='0.8134' modelMaxY='1.030677' />
    <kart id='1537' name='野马敞篷跑车 迅' modelMaxX='0.773879' modelMaxY='1.095391' />
    <kart id='1538' name='鲲鹏之势 迅' modelMaxX='0.831644' modelMaxY='1.123067' />
    <kart id='1539' name='马拉松 迅' modelMaxX='0.8439705' modelMaxY='0.963268' />
    <kart id='1540' name='高级马拉松 迅' modelMaxX='0.8439705' modelMaxY='0.963268' />
    <kart id='1541' name='精英马拉松 迅' modelMaxX='0.8355308' modelMaxY='0.9536353' />
    <kart id='1542' name='烈焰马拉松 迅' modelMaxX='0.8355308' modelMaxY='0.9536353' />
    <kart id='1544' name='尖锋 迅' modelMaxX='0.77347' modelMaxY='0.981104' />
    <kart id='1545' name='高级尖锋 迅' modelMaxX='0.77347' modelMaxY='0.981104' />
    <kart id='1546' name='精英尖锋 迅' modelMaxX='0.765735' modelMaxY='0.971293' />
    <kart id='1547' name='圣光天启尖锋 迅' modelMaxX='0.765735' modelMaxY='0.971293' />
    <kart id='1549' name='先驱者 迅' modelMaxX='0.765396' modelMaxY='1.03144' />
    <kart id='1550' name='离子 迅' modelMaxX='0.771933' modelMaxY='1.153993' />
    <kart id='1552' name='离子 迅 黑炎' modelMaxX='0.761359' modelMaxY='1.153993' />
    <kart id='1553' name='X10 暗黑 迅' modelMaxX='0.8232' modelMaxY='1.041413' />
    <kart id='1556' name='皮蛋计划 迅' modelMaxX='0.7633' modelMaxY='1.0304' />
    <kart id='1560' name='宝宝计划 迅' modelMaxX='0.7624' modelMaxY='0.9811' />
    <kart id='1562' name='魔界四不像 迅' modelMaxX='0.773709' modelMaxY='1.29294' />
    <kart id='1564' name='诛仙四剑 迅' modelMaxX='0.77347' modelMaxY='0.981104' />
    <kart id='1568' name='鹊桥飞光 迅' modelMaxX='0.7837' modelMaxY='1.2089' />
    <kart id='1572' name='小强计划 迅' modelMaxX='0.7734' modelMaxY='0.9811' />
    <kart id='1574' name='黑骑士 迅' modelMaxX='0.7339' modelMaxY='1.1108' />
    <kart id='1582' name='威龙 迅' modelMaxX='0.77175' modelMaxY='1.344' />
    <kart id='1583' name='赤炎威龙 迅' modelMaxX='0.7599165' modelMaxY='1.323392' />
    <kart id='1587' name='疾影 迅' modelMaxX='0.720975' modelMaxY='0.969122' />
    <kart id='1589' name='冰橇 迅' modelMaxX='0.744729' modelMaxY='1.053629' />
    <kart id='1595' name='白银威龙 迅' modelMaxX='0.7599165' modelMaxY='1.323392' />
    <kart id='1596' name='黄金威龙 迅' modelMaxX='0.7599165' modelMaxY='1.323392' />
    <kart id='1598' name='小乖计划 迅' modelMaxX='0.8005' modelMaxY='0.8335' />
    <kart id='1614' name='R8 迅' modelMaxX='0.744088' modelMaxY='1.150446' />
    <kart id='1616' name='等离子 迅' modelMaxX='0.780103' modelMaxY='1.139165' />
    <kart id='1617' name='高级等离子 迅' modelMaxX='0.780103' modelMaxY='1.139165' />
    <kart id='1618' name='精英等离子 迅' modelMaxX='0.773011' modelMaxY='1.128809' />
    <kart id='1621' name='黄金龙车 迅' modelMaxX='0.77347' modelMaxY='0.981104' />
    <kart id='1623' name='暗黑威龙 迅' modelMaxX='0.744729' modelMaxY='1.053629' />
    <kart id='1626' name='天蝎 迅' modelMaxX='0.75159' modelMaxY='1.03934' />
    <kart id='1628' name='蓝色威龙 迅' modelMaxX='0.7719' modelMaxY='1.1539' />
    <kart id='1629' name='绯影 迅' modelMaxX='0.720975' modelMaxY='1.124307' />
    <kart id='1632' name='粉红马拉松 迅' modelMaxX='0.80061' modelMaxY='0.93701' />
    <kart id='1636' name='光明骑士 迅' modelMaxX='0.7504' modelMaxY='0.9518' />
    <kart id='1640' name='阿斯特拉：钻石 迅' modelMaxX='0.796232' modelMaxY='1.143804' />
</ModelMax>";

    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    /// <summary>
    /// 用内置数据把本地 ModelMax.xml 补齐（启动时调用）：
    ///   · 只追加"程序内有、本地没有"的条目；
    ///   · 本地已有的条目（包括程序内已经不存在的）保持原样，不改值、不删除、不重排；
    ///   · 文本级插入，尽量保留用户手改的缩进/换行/注释。
    /// 返回本次新增的条目数。
    /// </summary>
    public static int SyncLocalFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return 0;
        }

        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        List<XElement> programKarts = ProgramKarts();
        if (!File.Exists(path))
        {
            File.WriteAllText(path, XmlContent, Utf8NoBom);
            ModelMaxTable.Invalidate();
            Log.Info($"[ModelMax] 已创建 {path}（内置 {programKarts.Count} 条）");
            return programKarts.Count;
        }

        string raw;
        try
        {
            // 兼容用户用记事本另存的 GBK/UTF-16 文件，避免中文车名被读成乱码
            raw = JsonHelper.ReadAllTextDetect(path);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Log.Error($"[ModelMax] 读取 {path} 失败，跳过本次同步", ex);
            return 0;
        }

        XDocument localXml;
        try
        {
            localXml = XDocument.Parse(raw, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            // 文件损坏时不覆盖用户数据，只告警（后台「配置」页可修复）
            Log.Error($"[ModelMax] 本地文件 {path} 不是合法 XML，跳过本次同步: {ex.Message}");
            return 0;
        }

        if (localXml.Root == null)
        {
            Log.Error($"[ModelMax] 本地文件 {path} 缺少根节点，跳过本次同步");
            return 0;
        }

        var localIds = new HashSet<string>(StringComparer.Ordinal);
        var localAllIds = new List<string>();
        foreach (XElement kart in localXml.Root.Elements("kart"))
        {
            string key = NormalizeId((string)kart.Attribute("id"));
            localIds.Add(key);
            localAllIds.Add(key);
        }

        var programIdSet = new HashSet<string>(StringComparer.Ordinal);
        var addedLines = new List<string>();
        foreach (XElement kart in programKarts)
        {
            string key = NormalizeId((string)kart.Attribute("id"));
            if (key.Length == 0 || !programIdSet.Add(key))
            {
                continue;
            }
            if (localIds.Contains(key))
            {
                continue;
            }
            addedLines.Add(kart.ToString(SaveOptions.DisableFormatting));
        }

        if (addedLines.Count == 0)
        {
            Log.Debug($"[ModelMax] 本地 {path} 已包含全部内置条目（{programKarts.Count} 条），无需更新");
            return 0;
        }

        string newText;
        bool textOk = TryInsertBeforeRootEnd(raw, localXml.Root, addedLines, out newText);
        if (!textOk && TryAddToDocument(localXml, addedLines, out XDocument merged))
        {
            newText = merged.ToString();
            textOk = true;
        }

        if (!textOk)
        {
            Log.Error($"[ModelMax] 无法在 {path} 中定位根节点结束标签，跳过本次同步");
            return 0;
        }

        try
        {
            WriteAtomic(path, newText, HasUtf8Bom(path));
        }
        catch (Exception ex)
        {
            Log.Error($"[ModelMax] 写入 {path} 失败，本次新增 {addedLines.Count} 条未生效", ex);
            return 0;
        }

        ModelMaxTable.Invalidate();
        int localOnly = localAllIds.Distinct(StringComparer.Ordinal).Count(id => !programIdSet.Contains(id));
        Log.Info($"[ModelMax] 本地文件已补齐 {addedLines.Count} 条内置数据：本地原有 {localAllIds.Count} 条保持不变"
                 + $"（其中程序内已没有的 {localOnly} 条），内置共 {programKarts.Count} 条（{path}）");
        return addedLines.Count;
    }

    private static List<XElement> ProgramKarts()
    {
        return XDocument.Parse(XmlContent).Root?.Elements("kart").ToList() ?? new List<XElement>();
    }

    /// <summary>id 归一化：能解析成整数的按整数比较（'01470' 与 '1470' 视为同一条），否则按去空白字符串比较。</summary>
    private static string NormalizeId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return string.Empty;
        }
        string text = id.Trim();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? "i:" + value.ToString(CultureInfo.InvariantCulture)
            : "s:" + text;
    }

    /// <summary>
    /// 在根节点结束标签前插入文本行，保留原文件的换行符、缩进与 BOM 之外的其它内容。
    /// 找不到结束标签（如自闭合根节点）时返回 false。
    /// </summary>
    private static bool TryInsertBeforeRootEnd(string raw, XElement root, List<string> lines, out string result)
    {
        result = raw;
        string closing = "</" + root.Name.LocalName + ">";
        int index = raw.LastIndexOf(closing, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return false;
        }

        string newLine = raw.Contains("\r\n") ? "\r\n" : "\n";
        string indent = DetectChildIndent(root);
        int lineStart = raw.LastIndexOf('\n', index);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        bool closingOnOwnLine = true;
        for (int i = lineStart; i < index; i++)
        {
            if (!char.IsWhiteSpace(raw[i]))
            {
                closingOnOwnLine = false;
                break;
            }
        }

        var buffer = new StringBuilder();
        if (closingOnOwnLine)
        {
            foreach (string line in lines)
            {
                buffer.Append(indent).Append(line).Append(newLine);
            }
            result = raw.Insert(lineStart, buffer.ToString());
        }
        else
        {
            foreach (string line in lines)
            {
                buffer.Append(newLine).Append(indent).Append(line);
            }
            buffer.Append(newLine);
            result = raw.Insert(index, buffer.ToString());
        }
        return true;
    }

    /// <summary>取根节点下已有子元素前面的缩进，用于新条目对齐；取不到时用 4 空格。</summary>
    private static string DetectChildIndent(XElement root)
    {
        foreach (XElement child in root.Elements())
        {
            if (child.PreviousNode is XText text)
            {
                string value = text.Value;
                int nl = value.LastIndexOf('\n');
                string prefix = nl >= 0 ? value.Substring(nl + 1) : value;
                if (prefix.Length > 0 && prefix.Trim().Length == 0)
                {
                    return prefix;
                }
            }
            break;
        }
        return "    ";
    }

    /// <summary>退路：文本插入失败时用 XDocument 合并（会重排缩进，但内容正确）。</summary>
    private static bool TryAddToDocument(XDocument localXml, List<string> lines, out XDocument merged)
    {
        merged = localXml;
        try
        {
            foreach (string line in lines)
            {
                localXml.Root.Add(XElement.Parse(line));
            }
            return true;
        }
        catch (XmlException ex)
        {
            Log.Error($"[ModelMax] 合并内置条目失败: {ex.Message}");
            return false;
        }
    }

    private static bool HasUtf8Bom(string path)
    {
        try
        {
            using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            int b0 = stream.ReadByte();
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            return b0 == 0xEF && b1 == 0xBB && b2 == 0xBF;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteAtomic(string path, string text, bool withBom)
    {
        string temp = path + ".tmp";
        File.WriteAllText(temp, text, withBom ? new UTF8Encoding(true) : Utf8NoBom);
        if (File.Exists(path))
        {
            try
            {
                File.Replace(temp, path, null);
                return;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // 某些文件系统/被占用场景不支持 Replace，退回覆盖写
            }
        }
        File.Copy(temp, path, true);
        File.Delete(temp);
    }
}