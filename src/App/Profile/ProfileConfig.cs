using Launcher.App.Constant;
using Launcher.Library.Constant;

namespace Launcher.App.Profile
{
    public class ProfileConfig
    {
        public ProfileConfig()
        {
            ServerSetting = new ServerSetting();
            Rider = new RiderData();
            RiderItem = new RiderItemData();
            MyRoom = new MyRoomData();
            GameOption = new GameOptionData();
        }

        public ServerSetting ServerSetting { get; set; }
        public RiderData Rider { get; set; }
        public RiderItemData RiderItem { get; set; }
        public MyRoomData MyRoom { get; set; }
        public GameOptionData GameOption { get; set; }
    }

    public class ServerSetting
    {
        public byte PreventItem_Use { get; set; } = 0;
        public byte SpeedPatch_Use { get; set; } = 0;

        /// <summary>
        /// current country code
        /// </summary>
        public CountryCode CC { get; set; } = CountryCode.CN;

        /// <summary>
        /// console visibility
        /// </summary>
        public bool ConsoleVisibility { get; set; } = Constants.DBG;

        /// <summary>
        /// show packet detail or not
        /// </summary>
        public bool ShowPacketDetail { get; set; } = Constants.DBG;

        /// <summary>
        /// whether to check for updates on startup
        /// </summary>
        public bool AutoUpdate { get; set; } = false;
    }

    public class RiderData
    {
        public int ClubCode { get; set; } = 10000;
        public int ClubMark_LOGO { get; set; } = 2; // 343 Vero  343 베로
        public int ClubMark_LINE { get; set; } = 0;
        public string ClubName { get; set; } = "TCCstar";
        public string ClubIntro { get; set; } = $"跑跑卡丁车交流群: 84338611\n单机启动器下载地址: https://github.com/{Constants.Owner}/{Constants.Repo}/releases/latest";
        public string UserID { get; set; } = "KartPlayer";
        public uint UserNO { get; set; } = 1982596588;
        public string Nickname { get; set; } = "KartPlayer";
        public string RiderIntro { get; set; } = $"Kart Offline Player / 单机玩家\n单机启动器下载地址: https://github.com/{Constants.Owner}/{Constants.Repo}/releases/latest";
        public string Card { get; set; } = "";
        public short Emblem1 { get; set; } = 0;
        public short Emblem2 { get; set; } = 0;
        public uint Lucci { get; set; } = 1000000;
        public uint RP { get; set; } = 2000000000;
        public uint Koin { get; set; } = 10000;
        public uint Cash { get; set; } = 10000;
        public uint TcCash { get; set; } = 10000;
        public int Premium { get; set; } = 5; // 100
        public byte Ranker { get; set; } = 0;
        public ushort SlotChanger { get; set; } = 32766;
        public uint pmap { get; set; } = 0; // 3130 // 1068 // 2520

        /// <summary>
        /// 验证情况
        /// </summary>
        /// <value>1: 绿色玩家 0: 未验证 other: 强制置0</value>
        public byte IdentificationType { get; set; } = 1;

        public byte Team { get; set; } = 2;
    }
    public class RiderItemData
    {
        public short Set_Character { get; set; } = 3;
        public short Set_Paint { get; set; } = 1;
        public short Set_Kart { get; set; } = 0;
        public short Set_Plate { get; set; } = 0;
        public short Set_Goggle { get; set; } = 0;
        public short Set_Balloon { get; set; } = 0;
        public short Set_HeadBand { get; set; } = 0;
        public short Set_HeadPhone { get; set; } = 0;
        public short Set_HandGearL { get; set; } = 0;
        public short Set_Uniform { get; set; } = 0;
        public short Set_Decal { get; set; } = 0;
        public short Set_Pet { get; set; } = 0;
        public short Set_FlyingPet { get; set; } = 0;
        public short Set_Aura { get; set; } = 0;
        public short Set_SkidMark { get; set; } = 0;
        public short Set_RidColor { get; set; } = 0;
        public short Set_BonusCard { get; set; } = 0;
        public short Set_Tachometer { get; set; } = 0;
        public short Set_Dye { get; set; } = 1;
        public short Set_KartSN { get; set; } = 0;
        public short Set_slotBg { get; set; } = 0;
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
        /// <summary>
        /// 客户端当前版本 (PXXXX)
        /// </summary>
        public ushort Version { get; set; } = 0000;
        /// <summary>
        /// 背景音乐音量 (0f-1f)
        /// </summary>
        public float Set_BGM { get; set; } = 0.25f;
        /// <summary>
        /// 音效音量 (0f-1f)
        /// </summary>
        public float Set_Sound { get; set; } = 0.25f;
        /// <summary>
        /// 背景音乐 (主题默认为0)
        /// </summary>
        public byte Main_BGM { get; set; } = 0;
        /// <summary>
        /// 音效
        /// </summary>
        public byte Sound_effect { get; set; } = 1;
        /// <summary>
        /// 窗口模式 0: 全屏; 1: 窗口; 3: 无边框
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
        public byte HighEndOptimization { get; set; } = 0;
        /// <summary>
        /// 自动准备
        /// </summary>
        public byte AutoReady { get; set; } = 1;
        /// <summary>
        /// 道具说明及超负荷提示
        /// </summary>
        public byte PropDescription { get; set; } = 1;
        /// <summary>
        /// 录像品质 高: 14; 中: 3; 低: 2
        /// </summary>
        public byte VideoQuality { get; set; } = 14;
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
        /// 速度类型 速度类型详情见 ExcData.SpeedType
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
        /// 分辨率 0: 1600x900; 1: 1920x1080
        /// </summary>
        public byte Set_screen { get; set; } = 1;
        /// <summary>
        /// 隐藏等级赛段位
        /// </summary>
        public byte HideCompetitiveRank { get; set; } = 0;
    }
}
