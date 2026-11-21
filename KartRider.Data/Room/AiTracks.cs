#pragma warning disable
using System;
using System.Collections.Generic;

namespace KartRider
{
    /// <summary>
    /// 「支持 AI」的赛道白名单（track hash）。判定依据：客户端
    /// <c>track/common/aiPath/&lt;trackId&gt;_&lt;n&gt;.kap</c> 是否存在——没有 .kap 的图，AI 画得出来但不会跑。
    /// 按官服行为：建 AI 房时服务端会固定一张本表里的图（竞速/道具分池），客户端据此显示「+ 添加 AI」按钮。
    /// **本文件由 _work/gen_ai_tracks_cs.py 自动生成，换客户端后重跑脚本刷新。**
    /// </summary>
    public static class AiTracks
    {
        /// <summary>竞速图（103 张）</summary>
        public static readonly HashSet<uint> Speed = new HashSet<uint>
        {
            0x23780334, // abyss_R01 深渊之都 地平线
            0x237A0335, // abyss_R02 深渊之都 海边密道
            0x23800338, // abyss_R05 深渊之都 海底竞技场
            0x213C0305, // beach_R01 滨海大道 风景
            0x21420308, // beach_R04 滨海大道 赛道9
            0x22E60322, // brodi_R01 未来工厂 神秘研究所
            0x2B1E038E, // castle_R01 大城堡 秘密地下室
            0x222C0315, // china_R01 龙行华夏 龙之遗迹
            0x222E0316, // china_R02 龙行华夏 街头狂飙
            0x22300317, // china_R03 龙行华夏 龙之遗迹2
            0x22320318, // china_R04 龙行华夏 龙之运河
            0x22340319, // china_R05 龙行华夏 黄山赛道
            0x2236031A, // china_R06 龙行华夏 海韵
            0x2238031B, // china_R07 龙行华夏 日光城
            0x222E0315, // china_R10 龙行华夏 山明水秀之都
            0x22320317, // china_R12 龙行华夏 天门山奇观之旅
            0x2B860399, // desert_R01 沙漠 旋转工地
            0x2B88039A, // desert_R02 沙漠 古老水渠
            0x2B8C039C, // desert_R04 沙漠 恐龙遗迹
            0x34BC040C, // factory_R03 工厂 改装车间
            0x2312032D, // fairy_R01 童话世界 彩虹之门
            0x2314032E, // fairy_R02 童话世界 沉睡的森林巨人
            0x2316032F, // fairy_R03 童话世界 魔女城
            0x2C6003A6, // forest_R02 森林 发夹
            0x2C6403A8, // forest_R04 森林 崎岖下山道
            0x2C6603A9, // forest_R05 森林 九曲木桥
            0x2C6803AA, // forest_R06 森林 牧羊场
            0x2C6A03AB, // forest_R07 森林 瀑布险境
            0x14C6024D, // god_R02 神之国度 诸神世界
            0x14C8024E, // god_R03 神之国度 次元之门
            0x1B8002B8, // gold_R01 黄金文明 黄金坐标
            0x1B8202B9, // gold_R02 黄金文明 秘密机关的威胁
            0x1424023A, // ice_I01 冰河 冰峰裂谷
            0x145A0243, // ice_R01 冰河 滑雪场
            0x145C0244, // ice_R02 冰河 破碎雪原
            0x145E0245, // ice_R03 冰河 机场跳跃
            0x40020476, // jurassic_R01 侏罗纪 恐龙岛大冒险
            0x40060478, // jurassic_R03 侏罗纪 恐龙决斗场
            0x40080479, // jurassic_R04 侏罗纪 恐龙坟墓
            0x233E0324, // korea_R01 
            0x23400325, // korea_R02 
            0x23420326, // korea_R03 
            0x1AD602AB, // mabi_R01 洛奇 艾明马恰
            0x22E20321, // maple_R01 冒险岛 射手村公园
            0x22E40322, // maple_R02 冒险岛 明珠港坡路
            0x22E60323, // maple_R03 冒险岛 拉克兰噩梦时间塔
            0x3CD6044A, // mechanic_R01 机械 遗忘孤城
            0x1BAE02BB, // mine_R01 矿山 曲折滑坡
            0x1BB002BC, // mine_R02 矿山 盘旋电车轨道
            0x1BB202BD, // mine_R03 矿山 冶炼厂
            0x1BB402BE, // mine_R04 矿山 斜坡捷径
            0x3FB60474, // moonhill_R01 月光之城 浮空要塞
            0x3FB80475, // moonhill_R02 月光之城 秘密通道
            0x3FBE0478, // moonhill_R05 
            0x1BDE02C1, // nemo_R01 像素世界 圣诞秘密空间
            0x1BE002C2, // nemo_R02 像素世界 玩具工厂
            0x1BE202C3, // nemo_R03 像素世界 钢铁熔炉
            0x36680417, // northeu_R01 太空 蜿蜒跑道
            0x366A0418, // northeu_R02 太空 军用机场
            0x366C0419, // northeu_R03 太空 惊恐高架
            0x2498033E, // nymph_R01 精灵 海底神殿
            0x37240425, // olympos_R01 奥林匹斯 神殿
            0x37260426, // olympos_R02 奥林匹斯 宙斯城
            0x2BF80397, // pirate_R01 海盗 绝壁海湾
            0x2BFA0398, // pirate_R02 海盗 宝藏迷宫
            0x2BFC0399, // pirate_R03 海盗 峭壁瞭望塔
            0x23B0032C, // steam_R01 1920 古老火车站
            0x24DE0341, // sword_R01 刀剑 云中峡谷
            0x24E00342, // sword_R02 刀剑 野外修炼馆
            0x1C4802C4, // tomb_R01 墓地 骷髅手指
            0x1C4A02C5, // tomb_R02 墓地 幽暗峡谷
            0x1C4C02C6, // tomb_R03 墓地 骷髅探险
            0x1C5002C8, // tomb_R05 墓地 亡者之爪
            0x1C5202C9, // tomb_R06 墓地 青月幽灵村
            0x349603EE, // village_I02 城镇 手指
            0x34CA03F6, // village_R01 城镇 高速公路
            0x34CC03F7, // village_R02 城镇 手指2
            0x34CE03F8, // village_R03 城镇 公路
            0x34D003F9, // village_R04 城镇 万里长城赛道
            0x34D203FA, // village_R05 城镇 极限竞技场
            0x34D403FB, // village_R06 城镇 高速公路2
            0x34D603FC, // village_R07 城镇 命运之桥
            0x34D803FD, // village_R08 城镇 申城环道
            0x34DA03FE, // village_R09 城镇 灵岩环道
            0x34CC03F6, // village_R10 城镇 破碎手指
            0x34CE03F7, // village_R11 
            0x34D003F8, // village_R12 城镇 南山
            0x34D403FA, // village_R14 城镇 高架
            0x156A0257, // wkc_R01 WKC 狮城狂飙
            0x156C0258, // wkc_R02 WKC 沙场拉力
            0x156E0259, // wkc_R03 WKC 申城环道
            0x1570025A, // wkc_R04 WKC 灵岩环道
            0x1572025B, // wkc_R05 WKC 英特拉格斯
            0x1574025C, // wkc_R06 WKC 力宝环道
            0x1576025D, // wkc_R07 WKC 铃鹿赛道
            0x156E0258, // wkc_R11 WKC 丹娜美拉码头
            0x15700259, // wkc_R12 WKC 美洲赛道
            0x2488033A, // world_R01 环游世界 纽约狂飙
            0x248A033B, // world_R02 环游世界 里约滑坡
            0x248E033D, // world_R04 环游世界 环球赛道
            0x248A033A, // world_R10 环游世界 迪拜富人区
            0x16FC027C, // xyy_R01 
            0x16FE027D, // xyy_R02 
        };

        /// <summary>道具图（136 张）</summary>
        public static readonly HashSet<uint> Item = new HashSet<uint>
        {
            0x2342032B, // abyss_I01 深渊之都 海陆隧道
            0x2344032C, // abyss_I02 深渊之都 海洋之主
            0x2346032D, // abyss_I03 深渊之都 深海之路
            0x210602FC, // beach_I01 滨海大道 水上机场
            0x210802FD, // beach_I02 滨海大道 游轮
            0x22B00319, // brodi_I01 未来工厂 能量生产线
            0x22B2031A, // brodi_I02 未来工厂 能量之源泉
            0x2AEA0386, // castle_I02 大城堡 路易的玩具屋
            0x2AEC0387, // castle_I03 大城堡 梦幻浴池
            0x2AEE0388, // castle_I04 大城堡 隐秘房间
            0x2AF00389, // castle_I05 大城堡 路易的书房
            0x21F6030C, // china_I01 龙行华夏 龙之宫殿
            0x21F8030D, // china_I02 龙行华夏 龙之都市
            0x21FA030E, // china_I03 龙行华夏 奥林匹克公园
            0x21FE0310, // china_I05 龙行华夏 龙之冰灯祭
            0x22000311, // china_I06 龙行华夏 龙之无尽路
            0x21F8030C, // china_I10 龙行华夏 灯光璀璨之都
            0x21FA030D, // china_I11 龙行华夏 海纳百川之岛城
            0x21FC030E, // china_I12 龙行华夏 英雄圣祠
            0x2B500390, // desert_I01 沙漠 地狱沙丘
            0x2B520391, // desert_I02 沙漠 金字塔
            0x2B540392, // desert_I03 沙漠 绿洲
            0x2B560393, // desert_I04 沙漠 市场
            0x2B580394, // desert_I05 沙漠 水库
            0x2B5A0395, // desert_I06 沙漠 狮身人面像的秘密
            0x2B5C0396, // desert_I07 沙漠 秘密水渠
            0x2B5E0397, // desert_I08 沙漠 战场II
            0x2B520390, // desert_I10 沙漠 古代君主之墓
            0x2B540391, // desert_I11 沙漠 流沙峡谷
            0x34820401, // factory_I01 工厂 机械战场
            0x34840402, // factory_I02 工厂 巨型齿轮
            0x34880404, // factory_I04 工厂 神秘之门
            0x348A0405, // factory_I05 工厂 加速器车间
            0x348E0407, // factory_I07 工厂  危险储藏室
            0x22DC0324, // fairy_I01 童话世界 皮蛋和豆树
            0x22DE0325, // fairy_I02 童话世界 青蛙之家
            0x22E00326, // fairy_I03 童话世界 旋转木马
            0x22E20327, // fairy_I04 童话世界 绿野迷踪
            0x22E40328, // fairy_I05 童话世界 刺藤怪物
            0x3DD00459, // fengshen_I03 封神传说 蜘蛛巢穴
            0x2C28039C, // forest_I01 森林 木桶
            0x2C2A039D, // forest_I02 森林 蘑菇洞穴
            0x2C2C039E, // forest_I03 森林 山谷
            0x2C2E039F, // forest_I04 森林 奇石
            0x2C3003A0, // forest_I05 森林 瀑布
            0x2C3203A1, // forest_I06 森林 游击场
            0x2C3403A2, // forest_I07 森林 熊猫乐园
            0x2C3803A4, // forest_I09 森林 藤蔓天桥
            0x14900244, // god_I02 神之国度 光之道
            0x14920245, // god_I03 神之国度 挑战之殿
            0x1B4A02AF, // gold_I01 黄金文明 古老神殿
            0x1B4C02B0, // gold_I02 黄金文明 恐怖斜坡
            0x1B4E02B1, // gold_I03 黄金文明 愤怒黄金神
            0x1B5002B2, // gold_I04 黄金文明 黄金熔炉
            0x1B5202B3, // gold_I05 黄金文明 神秘的黄金神鹰
            0x1B5402B4, // gold_I06 黄金文明 阿兹特克
            0x1426023B, // ice_I02 冰河 冰鲨墓地
            0x1428023C, // ice_I03 冰河 冰雪企鹅岛
            0x142A023D, // ice_I04 冰河 冰水晶洞穴
            0x142C023E, // ice_I05 冰河 极速
            0x142E023F, // ice_I06 冰河 雪球险道
            0x14300240, // ice_I07 冰河 冬季训练场
            0x3FCC046D, // jurassic_I01 侏罗纪 进击的恐龙村
            0x3FCE046E, // jurassic_I02 侏罗纪 险峻的火山
            0x2308031B, // korea_I01 
            0x230A031C, // korea_I02 
            0x230C031D, // korea_I03 
            0x230E031E, // korea_I04 
            0x2310031F, // korea_I05 
            0x1AA002A2, // mabi_I01 洛奇 迪尔科内尔
            0x1AA202A3, // mabi_I02 洛奇 敦巴伦
            0x22AC0318, // maple_I01 冒险岛 射手村小道
            0x22AE0319, // maple_I02 冒险岛 明珠港码头
            0x22B0031A, // maple_I03 冒险岛 梦之都拉克兰
            0x3CA00441, // mechanic_I01 机械 烽火战场
            0x3CA20442, // mechanic_I02 
            0x1B5A02AF, // mine_C04 矿山 斜坡捷径（道具）
            0x1B7802B2, // mine_I01 矿山 宝石开采场
            0x1B7A02B3, // mine_I02 矿山 岩浆洞穴
            0x1B7C02B4, // mine_I03 矿山 瀑布
            0x1B8002B6, // mine_I05 矿山 狂奔的矿车
            0x3F80046B, // moonhill_I01 月光之城 直角夜道
            0x3F82046C, // moonhill_I02 月光之城 都市风情
            0x3F84046D, // moonhill_I03 月光之城 狂飙隧道
            0x3F86046E, // moonhill_I04 月光之城 小镇铁路
            0x3F88046F, // moonhill_I05 月光之城 逃脱地铁
            0x3F8A0470, // moonhill_I06 月光之城 华丽剧场街
            0x1B8602B3, // nemo_C02 像素世界 玩具工厂（道具）
            0x1BA802B8, // nemo_I01 像素世界 像素村镇
            0x1BAA02B9, // nemo_I02 像素世界 麦迪娅隐匿所
            0x1BAC02BA, // nemo_I03 像素世界 咕咕鸟巢
            0x3632040E, // northeu_I01 太空 高速轨道
            0x3634040F, // northeu_I02 太空 危险捷径
            0x36360410, // northeu_I03 太空 回旋走廊
            0x36380411, // northeu_I04 太空 宇宙船舱
            0x363A0412, // northeu_I05 太空 极限跳跃
            0x363C0413, // northeu_I06 太空 喷射滑坡
            0x363E0414, // northeu_I07 太空 基地
            0x36400415, // northeu_I08 太空 跳跃峡谷
            0x36420416, // northeu_I09 太空 窒息隧道
            0x24620335, // nymph_I01 精灵 精灵之都
            0x24640336, // nymph_I02 精灵 木桶捷径
            0x24660337, // nymph_I03 精灵 月光庭院
            0x24680338, // nymph_I04 精灵 范塔利尼火山
            0x246A0339, // nymph_I05 精灵 月光泉
            0x36EE041C, // olympos_I01 奥林匹斯 抉择之门
            0x36F2041E, // olympos_I03 奥林匹斯 女神圣殿
            0x2BC2038E, // pirate_I01 海盗 藏宝营地
            0x2BC4038F, // pirate_I02 海盗 惊魂走廊
            0x2BC60390, // pirate_I03 海盗 眩晕码头
            0x2BC80391, // pirate_I04 海盗 暴风山寨
            0x2BCA0392, // pirate_I05 海盗 海盗船争夺战
            0x24A80338, // sword_I01 刀剑 月下门
            0x24AA0339, // sword_I02 刀剑 龙之路
            0x1C1202BB, // tomb_I01 墓地 幽灵峡谷
            0x1C1402BC, // tomb_I02 墓地 恐怖之路
            0x1C1602BD, // tomb_I03 墓地 神秘修道院
            0x1C1802BE, // tomb_I04 墓地 幽灵鬼域
            0x1C1C02C0, // tomb_I06 墓地 骷髅祭坛
            0x1C2002C2, // tomb_I08 墓地 蝙蝠城堡潜入行动
            0x1C2202C3, // tomb_I09 墓地 魔王的瞭望塔
            0x1C1402BB, // tomb_I10 墓地 幽灵吊灯
            0x347003E7, // village_C01 城镇 手指（道具）
            0x349403ED, // village_I01 城镇 钟楼
            0x349803EF, // village_I03 城镇 环道
            0x349A03F0, // village_I04 城镇 运河
            0x349C03F1, // village_I05 城镇 水路
            0x34A003F3, // village_I07 城镇 水坝上的对决
            0x34A203F4, // village_I08 城镇 破碎钟楼
            0x349603ED, // village_I10 
            0x349E03F1, // village_I14 城镇 风车村铁路
            0x2430032C, // world_C02 环游世界 里约滑坡（道具）
            0x24520331, // world_I01 环游世界 巴黎风情
            0x24560333, // world_I03 环游世界 希腊假日
            0x245A0335, // world_I05 环游世界 巴西体育场
            0x16C60273, // xyy_I01 
        };

        private static readonly Random Rng = new Random();

        /// <summary>按模式池随机抽一张；池为空返回 0。</summary>
        public static uint PickRandom(bool itemMode)
        {
            var pool = itemMode ? Item : Speed;
            if (pool.Count == 0) return 0;
            int idx = Rng.Next(pool.Count);
            foreach (uint h in pool)
            {
                if (idx-- == 0) return h;
            }
            return 0;
        }
    }
}
