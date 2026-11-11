using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using KartRider.Common.Data;

namespace Profile
{
    public class Setting
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string Name { get; set; } = "Yany";

        /// <summary>
        /// 服务器IP
        /// </summary>
        public string ServerIP { get; set; } = "127.0.0.1";

        /// <summary>
        /// 服务器端口
        /// </summary>
        public ushort ServerPort { get; set; } = 39311;

        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; } = "国服";

        /// <summary>
        /// 速度类型
        /// </summary>
        public byte SpeedType { get; set; } = 7;

        /// <summary>双人组队：是否允许双人组队频道（gameType 67/68）建出多队伍房间。</summary>
        public bool DuoTeamChannel { get; set; } = true;

        /// <summary>双人组队：每队人数（1~4，默认 2）。</summary>
        public byte DuoTeamSize { get; set; } = 2;

        /// <summary>控制台输出开关（默认开）。关掉后控制台不再打印日志，但窗口仍由 Console 控制。</summary>
        public bool LogEnabled { get; set; } = true;

        /// <summary>
        /// Ai速度类型
        /// </summary>
        public string AiSpeedType { get; set; } = "简单";

        /// <summary>
        /// 客户端版本
        /// </summary>
        public ushort ClientVersion { get; set; }

        public ushort LocaleID { get; set; }

        public ushort nClientLoc { get; set; }

        public bool Console { get; set; } = false;

        public bool NgsOn { get; set; } = false;

        public bool AutoUpdate { get; set; } = false;

        public bool EnableMod { get; set; } = false;

        public bool SoloRank { get; set; } = true;

        /// <summary>
        /// 是否读入「重复文件」：同一虚拟路径在基础包与补丁包里各有一份时（日志里的
        /// "[Data] 索引里有 N 条虚拟路径在多个分包中重复出现"），true = 每份都读入，
        /// 条目按主键（赛道 ID / 车辆·飞宠 ID / 道具 (类别,ID)）合并去重；
        /// false = 每个虚拟路径只读先装载的那一份。
        /// </summary>
        public bool ReadDuplicateLpkFiles { get; set; } = false;

        public string Proxy { get; set; } = "https://gh-proxy.com/";

        public string PatchUrl { get; set; } = "";

        /// <summary>日志级别：Trace/Debug/Info/Warn/Error/None（正式版默认 Info，排查时再改 Debug）</summary>
        public string LogLevel { get; set; } = "Info";

        /// <summary>是否记录每个包的十六进制（默认开：控制台与 Logs\packets.log 都能看到对局包）</summary>
        public bool PacketTrace { get; set; } = true;

        /// <summary>包跟踪只输出包名包含该子串的包（空=全部，例如 "GameSlot"）</summary>
        public string PacketTraceFilter { get; set; } = "";

        /// <summary>包跟踪采样：每 N 个包输出 1 个（默认 2，兼顾可读性与开销）</summary>
        public int PacketTraceSample { get; set; } = 2;

        /// <summary>包跟踪是否同时打印到控制台（PacketTrace 打开时默认打印）</summary>
        public bool PacketTraceConsole { get; set; } = true;

        /// <summary>包跟踪是否输出 UDP 对局包（默认关：只留 TCP 的 C/S，避免刷屏）</summary>
        public bool PacketTraceShowUdp { get; set; } = false;

        /// <summary>包环形缓冲（默认开：每个包只做一次定长拷贝，可用 /packets 回看最近 30 秒）</summary>
        public bool PacketRing { get; set; } = true;

        /// <summary>本地管理接口端口（0 表示关闭）</summary>
        public ushort AdminPort { get; set; } = 39314;

        /// <summary>管理接口令牌；为空时只允许本机回环访问</summary>
        public string AdminToken { get; set; } = "";
    }

    public class ProfileConfig
    {
        public ProfileConfig()
        {
            this.ServerSetting = new ServerSetting();
            this.Rider = new RiderData();
            this.RiderItem = new RiderItemData();
            this.MyRoom = new MyRoomData();
            this.GameOption = new GameOptionData();
        }

        public ServerSetting ServerSetting { get; set; }

        public RiderData Rider { get; set; }

        public RiderItemData RiderItem { get; set; }

        public MyRoomData MyRoom { get; set; }

        public GameOptionData GameOption { get; set; }
    }

    public class ServerSetting
    {

    }

    public class RiderData
    {
        public ushort BanType { get; set; } = 0; // 1:ID已登录 2:账号封停 3:非加盟网吧 4:ID错误 5:禁止IP登录 6:ID不正确

        public int ClubCode { get; set; } = 10000;

        public int ClubMark_LOGO { get; set; } = 0; //343 베로

        public int ClubMark_LINE { get; set; } = 0;

        public string ClubName { get; set; } = "TCCstar";

        public string ClubIntro { get; set; } = "跑跑卡丁车交流群：84338611\n单机启动器下载地址：https://yanygm.github.io/Launcher_V2/";

        public string RiderIntro { get; set; } = "";

        public string Card { get; set; } = "";

        public short Emblem1 { get; set; } = 0;

        public short Emblem2 { get; set; } = 0;

        public uint Lucci { get; set; } = 1000000;

        public uint RP { get; set; } = 2000000000;

        public uint Koin { get; set; } = 1000000;

        public uint Cash { get; set; } = 1000000;

        public uint TcCash { get; set; } = 1000000;

        public int Premium { get; set; } = 5; //100

        /// <summary>驾照：当前等级（0 基：1=新手、2=初级、3=L3、4=L2、5=L1、6=Pro）。</summary>
        public byte LicenseLevel { get; set; } = 0;

        /// <summary>驾照：累计已通过的关卡数（0~42），升级时**不清零**，只递增。</summary>
        public byte LicenseStep { get; set; } = 0;

        /// <summary>
        /// 车手学院：三个培训中心各自的已通过关卡数（依次为中心 1~3，每中心 4 关）。
        /// 三大中心可以各自从第 1 小关开始，进度必须按中心分别记录。
        /// </summary>
        public List<byte> TrainingCenterStages { get; set; } = new List<byte> { 0, 0, 0 };

        /// <summary>PRO 徽章（PRO 驾照前置任务，完成计时成绩后 PqRewardProEmblemPacket 领取）。</summary>
        public bool ProEmblemEarned { get; set; } = false;

        /// <summary>PRO 计时赛（关卡 31/33/35/37/39/41）已通过的数量 0~6，客户端 PRO 面板靠它决定能不能进。</summary>
        public byte ProSpeedCleared { get; set; } = 0;

        /// <summary>PRO 对决（关卡 32/34/36/38/40/42）已通过的数量 0~6。</summary>
        public byte ProVersusCleared { get; set; } = 0;

        public byte Ranker { get; set; } = 0;

        public ushort SlotChanger { get; set; } = (ushort)short.MaxValue;

        public uint pmap { get; set; } = 0; //3130 //2520 //ObMaster:718 //Ob:590 //比赛账号:1798,2098

        /// <summary>当前所在频道（由 PqChannelMovein 记录，-1=未知）。用于房间列表按频道隔离。</summary>
        public int Channel { get; set; } = -1;

        public byte IdentificationType { get; set; } = 1;

        public int ScenarioType { get; set; } = 0;

        public byte SpeedType { get; set; } = 0;

        public byte GameType { get; set; } = 0;

        public byte AttackType { get; set; } = 0;

        public uint Time { get; set; } = 0;

        public uint Track { get; set; } = 0;

        public string ClientId { get; set; } = "";

        public int P2pPort { get; set; } = 0;

        public int UdpPort { get; set; } = 0;
    }

    public class RiderItemData
    {
        public ushort Set_Character { get; set; } = 3;

        public ushort Set_Paint { get; set; } = 1;

        public ushort Set_Kart { get; set; } = 0;

        public ushort Set_Plate { get; set; } = 0;

        public ushort Set_Goggle { get; set; } = 0;

        public ushort Set_Balloon { get; set; } = 0;

        public ushort Set_Unknown1 { get; set; } = 0;

        public ushort Set_HeadBand { get; set; } = 0;

        public ushort Set_HeadPhone { get; set; } = 0;

        public ushort Set_HandGearL { get; set; } = 0;

        public ushort Set_Unknown2 { get; set; } = 0;

        public ushort Set_Uniform { get; set; } = 0;

        public ushort Set_Decal { get; set; } = 0;

        public ushort Set_Pet { get; set; } = 0;

        public ushort Set_FlyingPet { get; set; } = 0;

        public ushort Set_Aura { get; set; } = 0;

        public ushort Set_SkidMark { get; set; } = 0;

        public ushort Set_SpecialKit { get; set; } = 0;

        public ushort Set_RidColor { get; set; } = 0;

        public ushort Set_BonusCard { get; set; } = 0;

        public ushort Set_BossModeCard { get; set; } = 0;

        public ushort Set_KartPlant1 { get; set; } = 0;

        public ushort Set_KartPlant2 { get; set; } = 0;

        public ushort Set_KartPlant3 { get; set; } = 0;

        public ushort Set_KartPlant4 { get; set; } = 0;

        public ushort Set_Unknown3 { get; set; } = 0;

        public ushort Set_FishingPole { get; set; } = 0;

        public ushort Set_Tachometer { get; set; } = 0;

        public ushort Set_Dye { get; set; } = 1;

        public ushort Set_KartSN { get; set; } = 0;

        public byte Set_Unknown4 { get; set; } = 0;

        public ushort Set_KartCoating { get; set; } = 0;

        public ushort Set_KartTailLamp { get; set; } = 0;

        public ushort Set_slotBg { get; set; } = 0;

        public ushort Set_KartCoating12 { get; set; } = 0;

        public ushort Set_KartTailLamp12 { get; set; } = 0;

        public ushort Set_KartBoosterEffect12 { get; set; } = 0;

        public ushort Set_Unknown5 { get; set; } = 0;
    }

    public class MyRoomData
    {
        public short MyRoom { get; set; } = 0;

        public byte MyRoomBGM { get; set; } = 0;

        public byte UseRoomPwd { get; set; } = 0;

        public byte UseItemPwd { get; set; } = 0;

        public byte TalkLock { get; set; } = 1;

        public string RoomPwd { get; set; } = "";

        public string ItemPwd { get; set; } = "";

        public short MyRoomKart1 { get; set; } = 0;

        public short MyRoomKart2 { get; set; } = 0;
    }

    public class GameOptionData
    {
        public float Set_BGM { get; set; } = 1f;

        public float Set_Sound { get; set; } = 1f;

        public byte Main_BGM { get; set; } = 0;

        /// <summary>
        /// 音效
        /// </summary>
        public byte Sound_effect { get; set; } = 1;

        /// <summary>
        /// 全屏
        /// </summary>
        public byte Full_screen { get; set; } = 1;

        /// <summary>
        /// 显示后视镜
        /// </summary>
        public byte ShowMirror { get; set; } = 1;

        /// <summary>
        /// 显示其他车手昵称
        /// </summary>
        public byte ShowOtherPlayerNames { get; set; } = 1;

        /// <summary>
        /// 显示轮廓线
        /// </summary>
        public byte ShowOutlines { get; set; } = 1;

        /// <summary>
        /// 显示影子
        /// </summary>
        public byte ShowShadows { get; set; } = 1;

        /// <summary>
        /// 高级道具效果
        /// </summary>
        public byte HighLevelEffect { get; set; } = 0;

        /// <summary>
        /// 加速模糊
        /// </summary>
        public byte MotionBlurEffect { get; set; } = 0;

        /// <summary>
        /// 加速曲折
        /// </summary>
        public byte MotionDistortionEffect { get; set; } = 0;

        /// <summary>
        /// 高端配置游戏最佳化
        /// </summary>
        public byte HighEndOptimization { get; set; } = 1;

        /// <summary>
        /// 自动准备
        /// </summary>
        public byte AutoReady { get; set; } = 1;

        /// <summary>
        /// 道具说明及超负荷提示
        /// </summary>
        public byte PropDescription { get; set; } = 1;

        /// <summary>
        /// 录像品质
        /// </summary>
        public byte VideoQuality { get; set; } = 14; //녹화 품질

        /// <summary>
        /// BGM开关
        /// </summary>
        public byte BGM_Check { get; set; } = 1;

        /// <summary>
        /// 声音开关
        /// </summary>
        public byte Sound_Check { get; set; } = 1;

        /// <summary>
        /// 显示被击中信息
        /// </summary>
        public byte ShowHitInfo { get; set; } = 1;

        /// <summary>
        /// 自动增压加速
        /// </summary>
        public byte AutoBoost { get; set; } = 1;

        /// <summary>
        /// 游戏类型
        /// </summary>
        public byte GameType { get; set; } = 0;

        /// <summary>
        /// 幽灵设置
        /// </summary>
        public byte SetGhost { get; set; } = 1;

        /// <summary>
        /// 速度设置
        /// </summary>
        public byte SpeedType { get; set; } = 7;

        /// <summary>
        /// 查看房间内聊天内容
        /// </summary>
        public byte RoomChat { get; set; } = 1;

        /// <summary>
        /// 查看行驶中聊天内容
        /// </summary>
        public byte DrivingChat { get; set; } = 1;

        /// <summary>
        /// 显示全体被击中消息
        /// </summary>
        public byte ShowAllPlayerHitInfo { get; set; } = 1;

        /// <summary>
        /// 显示队伍颜色图标
        /// </summary>
        public byte ShowTeamColor { get; set; } = 1;

        /// <summary>
        /// 分辨率
        /// </summary>
        public byte Set_screen { get; set; } = 0;

        /// <summary>
        /// 隐藏等级赛段位
        /// </summary>
        public byte HideCompetitiveRank { get; set; } = 0;

        /// <summary>
        /// 全体快速回复
        /// </summary>
        public Dictionary<int, string> QuickMsg { get; set; } = new Dictionary<int, string>();

        /// <summary>
        /// 队伍快速回复
        /// </summary>
        public Dictionary<int, string> TeamQuickMsg { get; set; } = new Dictionary<int, string>();
    }
}
