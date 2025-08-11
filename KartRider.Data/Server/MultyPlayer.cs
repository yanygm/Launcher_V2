using ExcData;
using KartLibrary.IO;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider_PacketName;
using Set_Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace KartRider
{
    public static class MultyPlayer
    {
        static string RoomName;
        static byte[] RoomUnkBytes;
        static long EndTicks = 0;
        static int channeldata2 = 0;
        //static uint track = Adler32Helper.GenerateAdler32_UNICODE("village_R01", 0);
        static uint track = 0;
        public static long BootTicksNow = 0;
        public static long StartTicks = 0;
        static uint FinishTime = 0;
        static string AiXmlFile = AppDomain.CurrentDomain.BaseDirectory + @"Profile\AI.xml";
        public static Dictionary<int, uint> AiTimeData = new Dictionary<int, uint>();
        public static Dictionary<int, uint> TimeData = new Dictionary<int, uint>();

        /// <summary>
        /// 特殊道具车：将指定道具变更为特殊道具
        /// </summary>
        public static Dictionary<short, Dictionary<short, short>> skillChange = new Dictionary<short, Dictionary<short, short>>
        {
            { 1565, new Dictionary<short, short> { {33, 137}, {3, 137} } },
            { 1563, new Dictionary<short, short> { {7, 136}, {114, 16} } },
            { 1561, new Dictionary<short, short> { {8, 37}, {6, 31} } },
            { 1551, new Dictionary<short, short> { {8, 25} } },
            { 1543, new Dictionary<short, short> { {6, 31} } },
            { 1548, new Dictionary<short, short> { {4, 132} } },
            { 1536, new Dictionary<short, short> { {8, 17}, {5, 103} } },
            { 1526, new Dictionary<short, short> { {9, 27} } },
            { 1522, new Dictionary<short, short> { {9, 34}, {6, 31} } },
            { 1511, new Dictionary<short, short> { {2, 38} } },
            { 1510, new Dictionary<short, short> { {7, 32} } },
            { 1509, new Dictionary<short, short> { {7, 32} } },
            { 1507, new Dictionary<short, short> { {6, 31} } },
            { 1506, new Dictionary<short, short> { {5, 103} } },
            { 1505, new Dictionary<short, short> { {8, 129}, {4, 120} } },
            { 1502, new Dictionary<short, short> { {7, 4} } },
            { 1500, new Dictionary<short, short> { {9, 34}, {113, 135}, {33, 135} } },
            { 1496, new Dictionary<short, short> { {7, 134}, {6, 31} } },
            { 1494, new Dictionary<short, short> { {4, 132}, {6, 31} } },
            { 1491, new Dictionary<short, short> { {8, 82}, {9, 27}, {13, 28} } },
            { 1489, new Dictionary<short, short> { {9, 111}, {6, 31} } },
            { 1487, new Dictionary<short, short> { {5, 103}, {10, 36} } },
            { 1484, new Dictionary<short, short> { {7, 32}, {6, 31} } },
            { 1482, new Dictionary<short, short> { {5, 6} } },
            { 1481, new Dictionary<short, short> { {7, 102}, {9, 34} } },
            { 1479, new Dictionary<short, short> { {7, 131} } }
        };

        /// <summary>
        /// 特殊道具车：使用指定道具后获得特殊道具
        /// </summary>
        public static Dictionary<short, Dictionary<short, short>> skillMappings = new Dictionary<short, Dictionary<short, short>>
        {
            { 1450, new Dictionary<short, short> { {7, 5}, {5, 24} } },
            { 1563, new Dictionary<short, short> { {136, 6} } },
            { 1548, new Dictionary<short, short> { {5, 6} } },
            { 1510, new Dictionary<short, short> { {32, 32} } },
            { 1507, new Dictionary<short, short> { {5, 31} } },
            { 1496, new Dictionary<short, short> { {5, 24} } },
            { 1489, new Dictionary<short, short> { {5, 10} } },
            { 1479, new Dictionary<short, short> { {131, 5} } }
        };

        /// <summary>
        /// 特殊道具车：被指定道具攻击后获得特殊道具
        /// </summary>
        public static Dictionary<short, Dictionary<short, short>> skillAttacked = new Dictionary<short, Dictionary<short, short>>
        {
            { 1561, new Dictionary<short, short> { {7, 111} } },
            { 1557, new Dictionary<short, short> { {7, 32}, {5, 103} } },
            { 1555, new Dictionary<short, short> { {4, 6}, {9, 6} } },
            { 1551, new Dictionary<short, short> { {7, 6} } },
            { 1524, new Dictionary<short, short> { {5, 103} } },
            { 1511, new Dictionary<short, short> { {7, 5} } },
            { 1510, new Dictionary<short, short> { {5, 10} } },
            { 1509, new Dictionary<short, short> { {5, 10} } },
            { 1506, new Dictionary<short, short> { {4, 6}, {9, 6} } },
            { 1502, new Dictionary<short, short> { {4, 9} } },
            { 1482, new Dictionary<short, short> { {4, 119}, {9, 119} } }
        };

        public static void milTime(uint time)
        {
            GameType.min = time / 60000;
            uint sec = time - GameType.min * 60000;
            GameType.sec = sec / 1000;
            GameType.mil = time % 1000;
        }

        public static long GetUpTime()
        {
            long Time = 0;
            try
            {
                Time = Environment.TickCount64;
                TimeSpan uptime = TimeSpan.FromMilliseconds(Time);
                Console.WriteLine($"系统已运行总毫秒数: {Time} ms");
                Console.WriteLine($"运行时间: {uptime.Days}天 {uptime.Hours}小时 {uptime.Minutes}分钟 {uptime.Seconds}秒");
                return Time;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取系统运行时间失败: {ex.Message}");
                return Time;
            }
            return Time;
        }

        public static Dictionary<int, int> GetAllRanks()
        {
            if (TimeData.Count == 0)
                return new Dictionary<int, int>();

            // 按值降序排序（值越大排名越靠前）
            var sortedItems = TimeData
                .OrderBy(item => item.Value)
                .ToList();

            var ranks = new Dictionary<int, int>();

            // 排名从0开始，逐个分配（相同值也会依次+1）
            for (int i = 0; i < sortedItems.Count; i++)
            {
                ranks[sortedItems[i].Key] = i; // 直接使用索引作为排名
            }

            return ranks;
        }

        static void Set_settleTrigger()
        {
            var onceTimer = new System.Timers.Timer();
            onceTimer.Interval = 10000;
            onceTimer.Elapsed += new System.Timers.ElapsedEventHandler((s, _event) => settleTrigger(s, _event));
            onceTimer.AutoReset = false;
            onceTimer.Start();
        }

        static void settleTrigger(object sender, System.Timers.ElapsedEventArgs e)
        {
            //SettleTicks = EndTicks + 3100;
            using (OutPacket outPacket = new OutPacket("GameNextStagePacket"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteInt();
                outPacket.WriteInt();
                RouterListener.MySession.Client.Send(outPacket);
            }
            using (OutPacket outPacket = new OutPacket("GameResultPacket"))
            {
                // 加载 XML 文件
                XDocument doc = XDocument.Load(AiXmlFile);
                IOrderedEnumerable<XElement> aiNodes = null;
                if (StartGameData.StartTimeAttack_RandomTrackGameType == 0)
                {
                    aiNodes = doc.Root
                    ?.Element("SpeedAI")  // 指定父节点（如ItemAI、SpeedAI等）
                    ?.Elements()        // 获取该父节点下的所有直接子元素
                    .Where(e => e.Name.LocalName.StartsWith("Ai")
                    && !e.Name.LocalName.Equals("SpeedSpec"))  // 筛选条件
                    .OrderBy(e => e.Name.LocalName);  // 按名称排序
                }
                else if (StartGameData.StartTimeAttack_RandomTrackGameType == 1)
                {
                    aiNodes = doc.Root
                    ?.Element("ItemAI")  // 指定父节点（如ItemAI、SpeedAI等）
                    ?.Elements()        // 获取该父节点下的所有直接子元素
                    .Where(e => e.Name.LocalName.StartsWith("Ai")
                    && !e.Name.LocalName.Equals("ItemSpec"))  // 筛选条件
                    .OrderBy(e => e.Name.LocalName);  // 按名称排序
                }
                outPacket.WriteByte();
                outPacket.WriteInt(1);
                outPacket.WriteInt();
                if (AiTimeData.Count < aiNodes.Count())
                {
                    // 如果 AiTimeData 中的 AI 数量少于 aiNodes，则填充缺失的 AI 时间数据
                    foreach (var node in aiNodes)
                    {
                        string nodeName = node.Name.LocalName;
                        int numberPart = int.Parse(nodeName.Substring(2)); // 从索引2开始截取（跳过"Ai"）
                        if (!AiTimeData.ContainsKey(numberPart))
                        {
                            AiTimeData[numberPart] = 4294967295;
                        }
                    }
                }
                TimeData = AiTimeData;
                if (FinishTime == 0)
                {
                    outPacket.WriteHexString("FFFFFFFF");
                    if (!TimeData.ContainsKey(0))
                    {
                        TimeData.Add(0, 4294967295);
                    }
                }
                else
                {
                    outPacket.WriteUInt(FinishTime);
                    if (!TimeData.ContainsKey(0))
                    {
                        TimeData.Add(0, FinishTime);
                    }
                }
                outPacket.WriteByte();
                outPacket.WriteShort(SetRiderItem.Set_Kart);
                var ranks = GetAllRanks();
                outPacket.WriteInt(ranks[0]);
                outPacket.WriteShort();
                outPacket.WriteByte();
                outPacket.WriteUInt(SetRider.RP += 10000);
                outPacket.WriteInt(10000); //Earned RP
                outPacket.WriteInt(10000); //Earned Lucci
                outPacket.WriteUInt(SetRider.Lucci += 10000);
                outPacket.WriteBytes(new byte[46]);
                outPacket.WriteInt(1);
                outPacket.WriteByte(0);
                outPacket.WriteShort(SetRiderItem.Set_Character);
                outPacket.WriteByte(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(12 - aiNodes.Count());
                outPacket.WriteBytes(new byte[40]);
                outPacket.WriteHexString("FF");
                outPacket.WriteHexString("00 00 00 00 00 00 00 E3 23 07 40 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
                outPacket.WriteInt(SetRider.ClubMark_LOGO);
                outPacket.WriteBytes(new byte[39]);
                outPacket.WriteInt(aiNodes.Count()); // AI count
                int index = 0;
                foreach (var node in aiNodes)
                {
                    // 提取 Ai 后的数值部分（例如："Ai2" → "2"）
                    string nodeName = node.Name.LocalName;
                    int numberPart = int.Parse(nodeName.Substring(2)); // 从索引2开始截取（跳过"Ai"）
                    outPacket.WriteInt(numberPart);

                    if (AiTimeData.ContainsKey(numberPart) && AiTimeData[numberPart] > 0)
                    {
                        outPacket.WriteUInt(AiTimeData[numberPart]);
                    }
                    else
                    {
                        outPacket.WriteHexString("FFFFFFFF");
                    }
                    outPacket.WriteByte();

                    // 获取 kart 属性值
                    short kart = ParseShort(node.Attribute("kart"));
                    outPacket.WriteShort(kart);
                    outPacket.WriteInt(ranks[numberPart]);
                    outPacket.WriteHexString("A0 60");
                    outPacket.WriteByte();
                    outPacket.WriteInt();
                    index++;
                }
                outPacket.WriteBytes(new byte[34]);
                outPacket.WriteHexString("FF FF FF FF 00 00 00 00 00");
                RouterListener.MySession.Client.Send(outPacket);
            }
            using (OutPacket outPacket = new OutPacket("GameControlPacket"))
            {
                outPacket.WriteInt(4);
                outPacket.WriteByte(0);
                outPacket.WriteLong(EndTicks + 5000);
                RouterListener.MySession.Client.Send(outPacket);
                Console.WriteLine("EndTicks = {0}", EndTicks + 5000);
            }
            //Console.WriteLine("GameSlotPacket, Settle. Ticks = {0}", SettleTicks);
        }

        static short ParseShort(XAttribute attribute)
        {
            if (attribute == null || !short.TryParse(attribute.Value, out short result))
            {
                return 0; // 默认值或错误处理
            }
            return result;
        }

        public static void Clientsession(uint hash, InPacket iPacket)
        {
            if (hash == Adler32Helper.GenerateAdler32_ASCII("GameSlotPacket", 0))
            {
                iPacket.ReadInt();
                uint item = iPacket.ReadUInt();
                byte type = iPacket.ReadByte();
                if (item == 4294967295 && iPacket.Length == 77)
                {
                    byte[] data1 = iPacket.ReadBytes(25);
                    short id1 = iPacket.ReadShort();
                    byte unk1 = iPacket.ReadByte();
                    byte[] data2 = iPacket.ReadBytes(4);
                    iPacket.ReadByte();
                    iPacket.ReadShort();
                    byte[] data3 = iPacket.ReadBytes(29);
                    Random random = new Random();
                    int index = random.Next(KartExcData.itemProb_indi.Count);
                    short skill = KartExcData.itemProb_indi[index];
                    skill = GameSupport.GetItemSkill(skill);
                    using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
                    {
                        oPacket.WriteInt();
                        oPacket.WriteUInt(item);
                        oPacket.WriteByte(type);
                        oPacket.WriteBytes(data1);
                        oPacket.WriteShort(skill);
                        oPacket.WriteByte(1);
                        oPacket.WriteBytes(data2);
                        oPacket.WriteByte(2);
                        oPacket.WriteShort(skill);
                        oPacket.WriteBytes(data3);
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                }
                if (type == 11)
                {
                    var uni = iPacket.ReadByte();
                    var skill = iPacket.ReadShort();
                    List<short> skills = V2Spec.GetSkills();
                    if (skills.Contains(13) && skill == 3)
                    {
                        GameSupport.AttackedSkill(type, uni, 10);
                    }
                    if (skillAttacked.TryGetValue(SetRiderItem.Set_Kart, out var kartSkills))
                    {
                        if (kartSkills.TryGetValue(skill, out var targetSkill))
                        {
                            GameSupport.AttackedSkill(type, uni, targetSkill);
                        }
                    }
                    Console.WriteLine("GameSlotPacket, Attacked. Skill = {0}", skill);
                }
                if (type == 18)
                {
                    var uni = iPacket.ReadByte();
                    iPacket.ReadShort();
                    iPacket.ReadByte();
                    var skill = iPacket.ReadShort();
                    List<short> skills = V2Spec.GetSkills();
                    if (skills.Contains(14) && skill == 5)
                    {
                        GameSupport.AddItemSkill(6);
                    }
                    if (skillMappings.TryGetValue(SetRiderItem.Set_Kart, out var kartSkills))
                    {
                        if (kartSkills.TryGetValue(skill, out var targetSkill))
                        {
                            GameSupport.AddItemSkill(targetSkill);
                        }
                    }
                    Console.WriteLine("GameSlotPacket, Mapping. Skill = {0}", skill);
                }
                // if (item == 0 && type == 12)
                // {
                //     iPacket.ReadBytes(7);
                //     var nextpacketlenth = iPacket.ReadInt();
                //     var nextpackethash = iPacket.ReadUInt();
                //     if (nextpackethash == Adler32Helper.GenerateAdler32_ASCII("GopCourse", 0))
                //     {
                //         iPacket.ReadBytes(nextpacketlenth - 4 - 4);
                //         ArrivalTicks = iPacket.ReadUInt();
                //     }
                //     Console.WriteLine("GameSlotPacket, Arrivaled. Ticks = {0}", ArrivalTicks);
                // }
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameControlPacket"))
            {
                var state = iPacket.ReadByte();
                //start
                if (state == 0)
                {
                    BootTicksNow = GetUpTime();
                    StartTicks = BootTicksNow + 10000;
                    //StartTicks += (StartTicks == 0) ? (BootTicksNow + 10000) : (BootTicksNow - BootTicksPrev);
                    //BootTicksPrev = BootTicksNow;
                    using (OutPacket oPacket = new OutPacket("GameAiMasterSlotNoticePacket"))
                    {
                        oPacket.WriteInt();
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                    {
                        oPacket.WriteInt(1);
                        oPacket.WriteByte(0);
                        oPacket.WriteLong(StartTicks);
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    AiTimeData = new Dictionary<int, uint>();
                    FinishTime = 0;
                    Console.WriteLine("StartTicks = {0}", StartTicks);
                }
                //finish
                else if (state == 2)
                {
                    iPacket.ReadInt();
                    FinishTime = iPacket.ReadUInt();
                    using (OutPacket oPacket = new OutPacket("GameRaceTimePacket"))
                    {
                        oPacket.WriteInt();
                        oPacket.WriteUInt(FinishTime);
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                    {
                        EndTicks = GetUpTime() + 15000;;
                        oPacket.WriteInt(3);
                        oPacket.WriteByte(0);
                        oPacket.WriteLong(GetUpTime() + 10000);
                    }
                    //Console.Write("GameControlPacket, Finish. Finish Time = {0}", FinishTime);
                    //Console.WriteLine(" , End - Start Ticks : {0}", EndTicks - StartTicks - 15000);
                    Set_settleTrigger();
                }
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChGetRoomListRequestPacket"))
            {
                using (OutPacket oPacket = new OutPacket("ChGetRoomListReplyPacket"))
                {
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    RouterListener.MySession.Client.Send(oPacket);
                }
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChannelSwitch", 0))
            {
                //Console.WriteLine("Channel Switch, avaliable = {0}", iPacket.Available);
                //Console.WriteLine(BitConverter.ToString(iPacket.ReadBytes(iPacket.Available)).Replace("-", " "));
                //iPacket.ReadInt();
                //iPacket.ReadBytes(14);
                byte[] DateTime1 = iPacket.ReadBytes(18);
                byte channel = iPacket.ReadByte();
                Console.WriteLine("Channel Switch, channel = {0}", channel);
                int channeldata1 = 0;
                channeldata1 = 1;
                channeldata2 = 4;
                //StartGameRacing.GameRacing_SpeedType = 4;
                if (channel == 72 || channel == 55)
                {
                    using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
                    {
                        oPacket.WriteInt(0);
                        //oPacket.WriteInt(channeldata1);
                        oPacket.WriteInt(4);
                        oPacket.WriteEndPoint(IPAddress.Parse("127.0.0.1"), (ushort)RouterListener.port);
                        //RouterListener.Listener.BeginAcceptSocket(new AsyncCallback(RouterListener.OnAcceptSocket), null);
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    //GameSupport.OnDisconnect();
                    StartGameData.StartTimeAttack_SpeedType = 7;
                    StartGameData.StartTimeAttack_RandomTrackGameType = 0;
                }
                else if (channel == 25)
                {
                    using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
                    {
                        oPacket.WriteInt(0);
                        oPacket.WriteInt(4);
                        oPacket.WriteEndPoint(IPAddress.Parse("127.0.0.1"), (ushort)RouterListener.port);
                        //RouterListener.Listener.BeginAcceptSocket(new AsyncCallback(RouterListener.OnAcceptSocket), null);
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    //GameSupport.OnDisconnect();
                    StartGameData.StartTimeAttack_SpeedType = 4;
                    StartGameData.StartTimeAttack_RandomTrackGameType = 0;
                }
                else if (channel == 70 || channel == 57)
                {
                    using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
                    {
                        oPacket.WriteInt(1);
                        oPacket.WriteInt(2);
                        oPacket.WriteEndPoint(IPAddress.Parse("127.0.0.1"), (ushort)RouterListener.port);
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    StartGameData.StartTimeAttack_SpeedType = 7;
                    StartGameData.StartTimeAttack_RandomTrackGameType = 1;
                }
                else
                {
                    using (OutPacket outPacket = new OutPacket("ChGetCurrentGpReplyPacket"))
                    {
                        outPacket.WriteInt(0);
                        outPacket.WriteInt(0);
                        outPacket.WriteInt(0);
                        outPacket.WriteInt(0);
                        outPacket.WriteInt(0);
                        outPacket.WriteByte(0);
                        RouterListener.MySession.Client.Send(outPacket);
                    }
                }
                //GameSupport.OnDisconnect();
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChannelMovein", 0))
            {
                using (OutPacket oPacket = new OutPacket("PrChannelMoveIn"))
                {
                    //oPacket.WriteHexString("01 3d a4 3d 49 8f 99 3d a4 3d 49 90 99");
                    oPacket.WriteByte(1);
                    oPacket.WriteEndPoint(IPAddress.Parse(RouterListener.sIP), 39311);
                    oPacket.WriteEndPoint(IPAddress.Parse(RouterListener.sIP), 39312);
                    RouterListener.MySession.Client.Send(oPacket);
                }
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqMissionAttendPacket", 0))
            {
                using (OutPacket oPacket = new OutPacket("PrMissionAttendPacket"))
                {
                    oPacket.WriteInt(3);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(15);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(-1);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(109);
                    RouterListener.MySession.Client.Send(oPacket);
                }
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChCreateRoomRequestPacket", 0))
            {
                Console.Write("Avaiable = {0}", iPacket.Available);
                RoomName = iPacket.ReadString();    //room name
                Console.WriteLine(" RoomName = {0}, len = {1}", RoomName, RoomName.Length);
                iPacket.ReadInt();
                var unk1 = iPacket.ReadByte(); //7c
                iPacket.ReadInt();
                var Playernum = iPacket.ReadInt();
                iPacket.ReadInt();
                iPacket.ReadInt();
                RoomUnkBytes = iPacket.ReadBytes(32);
                var unk2 = iPacket.ReadBytes(29);
                byte AiSwitch = iPacket.ReadByte();
                using (OutPacket oPacket = new OutPacket("ChCreateRoomReplyPacket"))
                {
                    oPacket.WriteShort(1);
                    oPacket.WriteByte((byte)Playernum);
                    oPacket.WriteByte(unk1);
                    RouterListener.MySession.Client.Send(oPacket);
                }
                if (Playernum > 0 && AiSwitch == 6)
                {
                    // 读取 XML 文件
                    XDocument doc = XDocument.Load(AiXmlFile);

                    string targetParentNode = "";
                    if (StartGameData.StartTimeAttack_RandomTrackGameType == 0)
                    {
                        targetParentNode = "SpeedAI";
                    }
                    else if (StartGameData.StartTimeAttack_RandomTrackGameType == 1)
                    {
                        targetParentNode = "ItemAI";
                    }

                    // 获取目标父节点
                    XElement targetParent = doc.Root?.Element(targetParentNode);

                    // 清空所有 Ai* 节点（保留 Spec）
                    RemoveAiNodes(targetParent);

                    // 新增 AI 节点数量
                    AddAiNodes(targetParent, Playernum - 1);

                    // 保存修改后的 XML 文件
                    doc.Save(AiXmlFile);
                }
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrFirstRequestPacket"))
            {
                GrSessionDataPacket();
                //Thread.Sleep(10);
                GrSlotDataPacket();
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrChangeTrackPacket"))
            {
                track = iPacket.ReadUInt();
                iPacket.ReadInt();
                RoomUnkBytes = iPacket.ReadBytes(32);
                Console.WriteLine("Gr Track Changed : {0}", RandomTrack.GetTrackName(track));
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestSetSlotStatePacket"))
            {
                int Data = iPacket.ReadInt();
                GrSlotDataPacket();
                //GrSlotStatePacket(Data);
                GrReplySetSlotStatePacket(Data);
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestClosePacket"))
            {
                using (OutPacket oPacket = new OutPacket("GrReplyClosePacket"))
                {
                    //oPacket.WriteHexString("ff 76 05 5d 01");
                    oPacket.WriteUInt(SetRider.UserNO);
                    oPacket.WriteByte(1);
                    oPacket.WriteInt(7);
                    oPacket.WriteInt(7);
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(0);
                    RouterListener.MySession.Client.Send(oPacket);
                }
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestStartPacket"))
            {
                using (OutPacket oPacket = new OutPacket("GrReplyStartPacket"))
                {
                    oPacket.WriteInt(0);
                    RouterListener.MySession.Client.Send(oPacket);
                }
                using (OutPacket oPacket = new OutPacket("GrCommandStartPacket"))
                {
                    StartGameData.StartTimeAttack_Track = track;
                    RandomTrack.SetGameType();

                    oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("GrSessionDataPacket")));
                    GrSessionDataPacket(oPacket);

                    oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("GrSlotDataPacket")));
                    GrSlotDataPacket(oPacket);

                    //kart data
                    StartGameData.GetKartSpac(oPacket);

                    //AI data
                    XmlDocument doc = new XmlDocument();
                    doc.Load(AiXmlFile);
                    int listCount = 0;
                    XmlNodeList lis = null;
                    if (StartGameData.StartTimeAttack_RandomTrackGameType == 0)
                    {
                        lis = doc.SelectNodes(
                        "//SpeedAI/*[starts-with(name(), 'Ai') " +
                        "and contains(translate(name(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'ai') " +
                        "and not(contains(name(), 'spec'))]"
                        );
                    }
                    else if (StartGameData.StartTimeAttack_RandomTrackGameType == 1)
                    {
                        lis = doc.SelectNodes(
                        "//ItemAI/*[starts-with(name(), 'Ai') " +
                        "and contains(translate(name(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'ai') " +
                        "and not(contains(name(), 'spec'))]"
                        );
                    }
                    //XmlNodeList lis = doc.SelectNodes("//*[starts-with(name(), 'Ai') and contains(translate(name(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'ai') and not(contains(name(), 'data'))]");
                    if (lis.Count > 0)
                    {
                        listCount = lis.Count;
                    }
                    oPacket.WriteInt(listCount); //AI count
                    XmlNodeList Data = null;
                    if (StartGameData.StartTimeAttack_RandomTrackGameType == 0)
                    {
                        Data = doc.GetElementsByTagName("SpeedSpec");
                    }
                    else if (StartGameData.StartTimeAttack_RandomTrackGameType == 1)
                    {
                        Data = doc.GetElementsByTagName("ItemSpec");
                    }
                    if (Data.Count > 0)
                    {
                        for (int i = 0; i < listCount; i++)
                        {
                            oPacket.WriteEncFloat(float.Parse(Data[0].Attributes[0].Value));
                            oPacket.WriteEncFloat(float.Parse(Data[0].Attributes[1].Value));
                            oPacket.WriteEncFloat(float.Parse(Data[0].Attributes[2].Value));
                            oPacket.WriteEncFloat(float.Parse(Data[0].Attributes[3].Value));
                            oPacket.WriteEncFloat(float.Parse(Data[0].Attributes[4].Value));
                            oPacket.WriteEncFloat(float.Parse(Data[0].Attributes[5].Value));
                        }
                    }
                    oPacket.WriteUInt(StartGameData.StartTimeAttack_Track); //track name hash
                    oPacket.WriteInt(10000);

                    oPacket.WriteInt();
                    oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("MissionInfo")));
                    oPacket.WriteHexString("00000000000000000000FFFFFFFF000000000000000000");
                    //oPacket.WriteString("[applied param]\r\ntransAccelFactor='1.8555' driftEscapeForce='4720' steerConstraint='24.95' normalBoosterTime='3860' \r\npartsBoosterLock='1' \r\n\r\n[equipped / default parts param]\r\ntransAccelFactor='1.86' driftEscapeForce='2120' steerConstraint='2.7' normalBoosterTime='860' \r\n\r\n\r\n[gamespeed param]\r\ntransAccelFactor='-0.0045' driftEscapeForce='2600' steerConstraint='22.25' normalBoosterTime='3000' \r\n\r\n\r\n[factory enchant param]\r\n");
                    RouterListener.MySession.Client.Send(oPacket);
                }
                //StartGameRacing.KartSpecLog();
                Console.WriteLine("Track : {0}", RandomTrack.GetTrackName(StartGameData.StartTimeAttack_Track));
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("PcReportStateInGame", 0))
            {
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChLeaveRoomRequestPacket"))
            {
                using (OutPacket oPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                {
                    oPacket.WriteByte(1);
                    RouterListener.MySession.Client.Send(oPacket);
                }
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestBasicAiPacket"))
            {
                int unk1 = iPacket.ReadInt();
                Console.WriteLine("GrRequestBasicAiPacket, unk1 = {0}", unk1);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(AiXmlFile);
                XmlNode ai = xmlDoc.SelectSingleNode("//Ai" + unk1.ToString());
                if (ai != null)
                {
                    using (OutPacket oPacket = new OutPacket("GrSlotDataBasicAi"))
                    {
                        oPacket.WriteInt(1);
                        oPacket.WriteByte(1);
                        oPacket.WriteInt(unk1);
                        oPacket.WriteHexString("0000000000000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    XmlNode parentNode = ai.ParentNode;
                    if (parentNode != null)
                    {
                        parentNode.RemoveChild(ai);
                    }
                    xmlDoc.Save(AiXmlFile);
                }
                else
                {
                    var selector = new DictionaryRandomSelector();
                    var charDict = KartExcData.aiCharacterDict;
                    var kartDict = KartExcData.aiKartDict;
                    List<short> randomCharIds = selector.GetRandomCharacterIds(charDict, 1);
                    List<short> randomKartIds = selector.GetRandomKartIds(kartDict, 1);
                    short targetCharId = randomCharIds[0];
                    short targetKartId = randomKartIds[0];
                    if (charDict.TryGetValue(targetCharId, out var targetChar))
                    {
                        short? ridIndex = selector.GetRandomRidIndex(targetChar);
                        short? balloonId = selector.GetRandomAccessoryId(targetChar.Balloons);
                        short? headbandId = selector.GetRandomAccessoryId(targetChar.Headbands);
                        short? goggleId = selector.GetRandomAccessoryId(targetChar.Goggles);
                        using (OutPacket oPacket = new OutPacket("GrSlotDataBasicAi"))
                        {
                            oPacket.WriteInt(0);
                            oPacket.WriteByte(1);
                            oPacket.WriteInt(unk1);
                            oPacket.WriteShort(targetCharId);
                            oPacket.WriteShort(ridIndex ?? 0);
                            oPacket.WriteShort(targetKartId);
                            oPacket.WriteShort(balloonId ?? 0);
                            oPacket.WriteShort(headbandId ?? 0);
                            oPacket.WriteShort(goggleId ?? 0);
                            oPacket.WriteShort(0);
                            oPacket.WriteShort(0);
                            oPacket.WriteByte(0);
                            oPacket.WriteHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
                            RouterListener.MySession.Client.Send(oPacket);
                        }
                        XmlElement element = xmlDoc.CreateElement("Ai" + unk1.ToString());
                        element.SetAttribute("character", targetCharId.ToString());
                        element.SetAttribute("rid", ridIndex?.ToString() ?? "0");
                        element.SetAttribute("kart", targetKartId.ToString());
                        element.SetAttribute("balloon", balloonId?.ToString() ?? "0");
                        element.SetAttribute("headBand", headbandId?.ToString() ?? "0");
                        element.SetAttribute("goggle", goggleId?.ToString() ?? "0");
                        XmlNode rootNode = xmlDoc.DocumentElement;
                        if (rootNode != null)
                        {
                            rootNode.AppendChild(element);
                        }
                        xmlDoc.Save(AiXmlFile);
                    }
                }
                using (OutPacket oPacket = new OutPacket("GrReplyBasicAiPacket"))
                {
                    oPacket.WriteByte(1);
                    oPacket.WriteHexString("2CFB6605");
                    RouterListener.MySession.Client.Send(oPacket);
                }
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameAiGoalinPacket"))
            {
                var AiNum = iPacket.ReadInt();
                var AiTime = iPacket.ReadUInt();
                using (OutPacket oPacket = new OutPacket("GameRaceTimePacket"))
                {
                    oPacket.WriteInt(AiNum);
                    oPacket.WriteUInt(AiTime);
                    RouterListener.MySession.Client.Send(oPacket);
                    Console.WriteLine("AiTime = {0}", AiTime);
                }
                if (AiTimeData.Count == 0 && FinishTime == 0)
                {
                    using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                    {
                        EndTicks = GetUpTime() + 15000;
                        oPacket.WriteInt(3);
                        oPacket.WriteByte(0);
                        oPacket.WriteLong(GetUpTime() + 10000);
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    //Console.Write("GameControlPacket, Finish. Finish Time = {0}", AiTime);
                    //Console.WriteLine(" , End - Start Ticks : {0}", AiTime - StartTicks);
                    Set_settleTrigger();
                }
                if (!AiTimeData.ContainsKey(AiNum))
                {
                    AiTimeData.Add(AiNum, AiTime);
                }
                return;
            }
        }

        static void GrSlotDataPacket()
        {
            using (OutPacket oPacket = new OutPacket("GrSlotDataPacket"))
            {
                GrSlotDataPacket(oPacket);
                RouterListener.MySession.Client.Send(oPacket);
            }
        }

        static void GrSlotDataPacket(OutPacket outPacket)
        {
            outPacket.WriteUInt(track); //track name hash
            outPacket.WriteInt(0);
            outPacket.WriteBytes(RoomUnkBytes);
            outPacket.WriteInt(0); //RoomMaster 
            outPacket.WriteInt(2);
            outPacket.WriteInt(0); // outPacket.WriteShort(); outPacket.WriteShort(3);
            outPacket.WriteShort(0); // 797
            outPacket.WriteByte(0);
            var unk1 = 0;
            outPacket.WriteInt(unk1);
            //for (int i = 0; i < unk1; i++) outPacket.WriteByte();
            for (int i = 0; i < 4; i++) outPacket.WriteInt();

            /* ---- One/First player ---- */
            outPacket.WriteInt(2);//Player Type, 2 = RoomMaster, 3 = AutoReady, 4 = Observer, 5 = ? , 7 = AI
            outPacket.WriteUInt(SetRider.UserNO);

            outPacket.WriteEndPoint(IPAddress.Parse(RouterListener.client.Address.ToString()), (ushort)RouterListener.client.Port);
            //outPacket.WriteEndPoint(IPAddress.Parse(RouterListener.forceConnect), 39311);
            //outPacket.WriteHexString("3a 16 01 31 7d 48");
            outPacket.WriteInt();
            outPacket.WriteShort();

            outPacket.WriteString(SetRider.Nickname);
            outPacket.WriteShort(SetRider.Emblem1);
            outPacket.WriteShort(SetRider.Emblem2);
            outPacket.WriteShort(0);
            GameSupport.GetRider(outPacket);
            outPacket.WriteShort(0);
            outPacket.WriteString(SetRider.Card);
            outPacket.WriteUInt(SetRider.RP);
            outPacket.WriteByte();
            outPacket.WriteByte();
            outPacket.WriteByte();
            for (int i = 0; i < 8; i++) outPacket.WriteInt();
            outPacket.WriteInt(1500); //outPacket.WriteInt(1500);
            outPacket.WriteInt(2512); //outPacket.WriteInt(2000);
            outPacket.WriteInt(162); //outPacket.WriteInt();
            outPacket.WriteInt(2000); //outPacket.WriteInt(2000);
            outPacket.WriteInt(5); //outPacket.WriteInt(5);
            outPacket.WriteByte(255);
            outPacket.WriteByte(0);
            outPacket.WriteByte(0);
            outPacket.WriteByte(0);

            outPacket.WriteByte(3); //3
            outPacket.WriteString("");
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteByte();
            outPacket.WriteInt();

            /*---- One/First player ----*/
            /*
            outPacket.WriteInt(0);
            outPacket.WriteInt(0);
            outPacket.WriteInt(0);
            outPacket.WriteInt(0);
            outPacket.WriteInt(0);
            outPacket.WriteInt(0);
            outPacket.WriteInt(0);
            outPacket.WriteBytes(new byte[38]);
            outPacket.WriteHexString("FFFFFFFF");
            outPacket.WriteHexString("FFFFFFFF");
            outPacket.WriteHexString("FFFFFFFF");
            outPacket.WriteHexString("FFFFFFFF");
            outPacket.WriteHexString("FFFFFFFF");
            outPacket.WriteHexString("FFFFFFFF");
            outPacket.WriteHexString("FFFFFFFF");
            outPacket.WriteInt(0);
            */
            //outPacket.WriteHexString("030000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00000000");

            // AI Data
            XmlDocument doc = new XmlDocument();
            outPacket.WriteShort(0);
            doc.Load(AiXmlFile);
            string parentNodePath = "";
            if (StartGameData.StartTimeAttack_RandomTrackGameType == 0)
            {
                parentNodePath = "//SpeedAI";
            }
            else if (StartGameData.StartTimeAttack_RandomTrackGameType == 1)
            {
                parentNodePath = "//ItemAI";
            }
            XmlNode ai1 = doc.SelectSingleNode(parentNodePath + "/Ai1");
            XmlNode ai2 = doc.SelectSingleNode(parentNodePath + "/Ai2");
            XmlNode ai3 = doc.SelectSingleNode(parentNodePath + "/Ai3");
            XmlNode ai4 = doc.SelectSingleNode(parentNodePath + "/Ai4");
            XmlNode ai5 = doc.SelectSingleNode(parentNodePath + "/Ai5");
            XmlNode ai6 = doc.SelectSingleNode(parentNodePath + "/Ai6");
            XmlNode ai7 = doc.SelectSingleNode(parentNodePath + "/Ai7");
            if (ai1 != null)
            {
                outPacket.WriteInt(7);
                XmlElement xe = (XmlElement)ai1;
                outPacket.WriteShort(short.Parse(xe.GetAttribute("character")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("rid")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("kart")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("balloon")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("headBand")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("goggle")));
                outPacket.WriteByte(0);
            }
            else
            {
                outPacket.WriteInt(0);
            }
            if (ai2 != null)
            {
                outPacket.WriteInt(7);
                XmlElement xe = (XmlElement)ai2;
                outPacket.WriteShort(short.Parse(xe.GetAttribute("character")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("rid")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("kart")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("balloon")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("headBand")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("goggle")));
                outPacket.WriteByte(0);
            }
            else
            {
                outPacket.WriteInt(0);
            }
            if (ai3 != null)
            {
                outPacket.WriteInt(7);
                XmlElement xe = (XmlElement)ai3;
                outPacket.WriteShort(short.Parse(xe.GetAttribute("character")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("rid")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("kart")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("balloon")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("headBand")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("goggle")));
                outPacket.WriteByte(0);
            }
            else
            {
                outPacket.WriteInt(0);
            }
            if (ai4 != null)
            {
                outPacket.WriteInt(7);
                XmlElement xe = (XmlElement)ai4;
                outPacket.WriteShort(short.Parse(xe.GetAttribute("character")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("rid")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("kart")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("balloon")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("headBand")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("goggle")));
                outPacket.WriteByte(0);
            }
            else
            {
                outPacket.WriteInt(0);
            }
            if (ai5 != null)
            {
                outPacket.WriteInt(7);
                XmlElement xe = (XmlElement)ai5;
                outPacket.WriteShort(short.Parse(xe.GetAttribute("character")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("rid")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("kart")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("balloon")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("headBand")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("goggle")));
                outPacket.WriteByte(0);
            }
            else
            {
                outPacket.WriteInt(0);
            }
            if (ai6 != null)
            {
                outPacket.WriteInt(7);
                XmlElement xe = (XmlElement)ai6;
                outPacket.WriteShort(short.Parse(xe.GetAttribute("character")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("rid")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("kart")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("balloon")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("headBand")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("goggle")));
                outPacket.WriteByte(0);
            }
            else
            {
                outPacket.WriteInt(0);
            }
            if (ai7 != null)
            {
                outPacket.WriteInt(7);
                XmlElement xe = (XmlElement)ai7;
                outPacket.WriteShort(short.Parse(xe.GetAttribute("character")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("rid")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("kart")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("balloon")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("headBand")));
                outPacket.WriteShort(short.Parse(xe.GetAttribute("goggle")));
                outPacket.WriteByte(0);
            }
            else
            {
                outPacket.WriteInt(0);
            }
            outPacket.WriteBytes(new byte[36]);
            if (ai1 != null)
            {
                outPacket.WriteInt(1);
            }
            else
            {
                outPacket.WriteHexString("FFFFFFFF");
            }
            if (ai2 != null)
            {
                outPacket.WriteInt(2);
            }
            else
            {
                outPacket.WriteHexString("FFFFFFFF");
            }
            if (ai3 != null)
            {
                outPacket.WriteInt(3);
            }
            else
            {
                outPacket.WriteHexString("FFFFFFFF");
            }
            if (ai4 != null)
            {
                outPacket.WriteInt(4);
            }
            else
            {
                outPacket.WriteHexString("FFFFFFFF");
            }
            if (ai5 != null)
            {
                outPacket.WriteInt(5);
            }
            else
            {
                outPacket.WriteHexString("FFFFFFFF");
            }
            if (ai6 != null)
            {
                outPacket.WriteInt(6);
            }
            else
            {
                outPacket.WriteHexString("FFFFFFFF");
            }
            if (ai7 != null)
            {
                outPacket.WriteInt(7);
            }
            else
            {
                outPacket.WriteHexString("FFFFFFFF");
            }
            outPacket.WriteInt(0);
        }

        static void GrSlotStatePacket(int Data)
        {
            using (OutPacket oPacket = new OutPacket("GrSlotStatePacket"))
            {
                oPacket.WriteInt(Data);
                oPacket.WriteBytes(new byte[60]);
                RouterListener.MySession.Client.Send(oPacket);
            }
        }

        static void GrReplySetSlotStatePacket(int Data)
        {
            using (OutPacket oPacket = new OutPacket("GrReplySetSlotStatePacket"))
            {
                oPacket.WriteUInt(SetRider.UserNO);
                oPacket.WriteInt(1);
                oPacket.WriteByte(0);
                oPacket.WriteInt(Data);
                RouterListener.MySession.Client.Send(oPacket);
            }
        }

        static void GrSessionDataPacket()
        {
            using (OutPacket oPacket = new OutPacket("GrSessionDataPacket"))
            {
                GrSessionDataPacket(oPacket);
                RouterListener.MySession.Client.Send(oPacket);
            }
        }

        static void GrSessionDataPacket(OutPacket outPacket)
        {
            outPacket.WriteString(RoomName);
            outPacket.WriteInt(0);
            outPacket.WriteByte((byte)(StartGameData.StartTimeAttack_RandomTrackGameType + 1));
            outPacket.WriteByte(StartGameData.StartTimeAttack_SpeedType); //7
            outPacket.WriteInt(0);
            if (StartGameData.StartTimeAttack_RandomTrackGameType == 0)
            {
                outPacket.WriteHexString("083483D162");
            }
            else if (StartGameData.StartTimeAttack_RandomTrackGameType == 1)
            {
                outPacket.WriteHexString("0802176848"); //08 24 72 F5 9E
            }
            else
            {
                outPacket.WriteHexString("0000000000"); //08 24 72 F5 9E
            }
            //outPacket.WriteHexString("083483D162"); //08 24 72 F5 9E
            outPacket.WriteInt(0);
            outPacket.WriteByte(0);
            outPacket.WriteByte(0);
            outPacket.WriteByte(0);
        }

        // 移除所有 Ai* 节点（保留 Spec）
        static void RemoveAiNodes(XElement targetParent)
        {
            // 查找并删除所有以"Ai"开头的子元素
            var aiElementsToRemove = targetParent.Elements()
                .Where(e => e.Name.LocalName.StartsWith("Ai", StringComparison.OrdinalIgnoreCase))
                .ToList(); // 先转换为列表避免迭代时修改集合

            int removedCount = aiElementsToRemove.Count;
            aiElementsToRemove.ForEach(e => e.Remove());
        }

        // 添加指定数量的 Ai 节点
        static void AddAiNodes(XElement targetParent, int count)
        {
            var selector = new DictionaryRandomSelector();
            var charDict = KartExcData.aiCharacterDict;
            var kartDict = KartExcData.aiKartDict;
            List<short> randomCharIds = selector.GetRandomCharacterIds(charDict, count);
            List<short> randomKartIds = null;
            if (StartGameData.StartTimeAttack_RandomTrackGameType == 0)
            {
                randomKartIds = selector.GetRandomKartIds(kartDict, count, true, false);
            }
            else if (StartGameData.StartTimeAttack_RandomTrackGameType == 1)
            {
                randomKartIds = selector.GetRandomKartIds(kartDict, count, false, true);
            }
            for (int i = 0; i < randomCharIds.Count; i++)
            {
                short targetCharId = randomCharIds[i];
                short targetKartId = randomKartIds[i];
                if (charDict.TryGetValue(targetCharId, out var targetChar))
                {
                    short? ridIndex = selector.GetRandomRidIndex(targetChar);
                    short? balloonId = selector.GetRandomAccessoryId(targetChar.Balloons);
                    short? headbandId = selector.GetRandomAccessoryId(targetChar.Headbands);
                    short? goggleId = selector.GetRandomAccessoryId(targetChar.Goggles);
                    if (StartGameData.StartTimeAttack_RandomTrackGameType == 0)
                    {
                        balloonId = 0;
                        headbandId = 0;
                        goggleId = 0;
                    }

                    string nodeName = $"Ai{i + 1}";

                    // 添加属性
                    targetParent.Add(new XElement(nodeName,
                    new XAttribute("character", targetCharId.ToString()),
                    new XAttribute("rid", ridIndex?.ToString() ?? "0"),
                    new XAttribute("kart", targetKartId.ToString()),
                    new XAttribute("balloon", balloonId?.ToString() ?? "0"),
                    new XAttribute("headBand", headbandId?.ToString() ?? "0"),
                    new XAttribute("goggle", goggleId?.ToString() ?? "0")
                    ));
                }
            }
        }
    }
}
