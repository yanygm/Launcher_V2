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
        public ushort ServerPort { get; set; } = 39312;

        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; } = "国服";

        /// <summary>
        /// 速度类型
        /// </summary>
        public byte SpeedType { get; set; } = 7;

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

        public bool NgsOn { get; set; } = false;

        public bool PatchUpdate { get; set; } = false;

        public string Proxy { get; set; } = "https://gh-proxy.com/";

        public List<PINFile.IPEndPoint> ServerList { get; set; }
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
        public byte PreventItem_Use { get; set; } = 0;

        public byte SpeedPatch_Use { get; set; } = 0;
    }

    public class RiderData
    {
        public int ClubCode { get; set; } = 10000;

        public int ClubMark_LOGO { get; set; } = 2; //343 베로

        public int ClubMark_LINE { get; set; } = 0;

        public string ClubName { get; set; } = "TCCstar";

        public string ClubIntro { get; set; } = "跑跑卡丁车交流群：84338611\n单机启动器下载地址：https://github.com/yanygm/Launcher_V2/releases";

        public string RiderIntro { get; set; } = "";

        public string Card { get; set; } = "";

        public short Emblem1 { get; set; } = 0;

        public short Emblem2 { get; set; } = 0;

        public uint Lucci { get; set; } = 1000000;

        public uint RP { get; set; } = 2000000000;

        public uint Koin { get; set; } = 10000;

        public uint Cash { get; set; } = 10000;

        public uint TcCash { get; set; } = 10000;

        public int Premium { get; set; } = 5; //100

        public byte Ranker { get; set; } = 0;

        public ushort SlotChanger { get; set; } = (ushort)short.MaxValue;

        public uint pmap { get; set; } = 0; //3130 //1068 //2520

        public byte IdentificationType { get; set; } = 1;

        public byte EnterMyRoomType { get; set; } = 0;

        public int ScenarioType { get; set; } = 0;

        public byte AttackType { get; set; } = 0;

        public uint Track { get; set; } = 0;

        public string Client { get; set; } = "";
    }

    public class RiderItemData
    {
        public short Set_Character { get; set; } = 3;

        public short Set_Paint { get; set; } = 1;

        public short Set_Kart { get; set; } = 0;

        public short Set_Plate { get; set; } = 0;

        public short Set_Goggle { get; set; } = 0;

        public short Set_Balloon { get; set; } = 0;

        public short Set_Unknown1 { get; set; } = 0;

        public short Set_HeadBand { get; set; } = 0;

        public short Set_HeadPhone { get; set; } = 0;

        public short Set_HandGearL { get; set; } = 0;

        public short Set_Unknown2 { get; set; } = 0;

        public short Set_Uniform { get; set; } = 0;

        public short Set_Decal { get; set; } = 0;

        public short Set_Pet { get; set; } = 0;

        public short Set_FlyingPet { get; set; } = 0;

        public short Set_Aura { get; set; } = 0;

        public short Set_SkidMark { get; set; } = 0;

        public short Set_SpecialKit { get; set; } = 0;

        public short Set_RidColor { get; set; } = 0;

        public short Set_BonusCard { get; set; } = 0;

        public short Set_BossModeCard { get; set; } = 0;

        public short Set_KartPlant1 { get; set; } = 0;

        public short Set_KartPlant2 { get; set; } = 0;

        public short Set_KartPlant3 { get; set; } = 0;

        public short Set_KartPlant4 { get; set; } = 0;

        public short Set_Unknown3 { get; set; } = 0;

        public short Set_FishingPole { get; set; } = 0;

        public short Set_Tachometer { get; set; } = 0;

        public short Set_Dye { get; set; } = 1;

        public short Set_KartSN { get; set; } = 0;

        public byte Set_Unknown4 { get; set; } = 0;

        public short Set_KartCoating { get; set; } = 0;

        public short Set_KartTailLamp { get; set; } = 0;

        public short Set_slotBg { get; set; } = 0;

        public short Set_KartCoating12 { get; set; } = 0;

        public short Set_KartTailLamp12 { get; set; } = 0;

        public short Set_KartBoosterEffect12 { get; set; } = 0;

        public short Set_Unknown5 { get; set; } = 0;
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
    }
}

