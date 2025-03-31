using ExcData;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider_PacketName;
using Set_Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace KartRider
{
    public static class MultyPlayer
    {
        static string RoomName;
        static byte[] RoomUnkBytes;
        static uint ArrivalTicks, EndTicks, SettleTicks;
        static int channeldata2 = 0;
        static uint track = 0;
        public static uint BootTicksPrev, BootTicksNow;
        public static uint StartTicks = 0;
        static uint FinishTime;

        [DllImport("kernel32")]
        extern static UInt32 GetTickCount();

        public static void milTime(int time)
        {
            GameType.min = time / 60000;
            int sec = time - GameType.min * 60000;
            GameType.sec = sec / 1000;
            GameType.mil = time % 1000;
        }

        public static uint GetUpTime()
        {
            var temp = TimeSpan.FromMilliseconds(GetTickCount()).Ticks;
            temp /= 10000;
            return (uint)temp;
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
            SettleTicks = EndTicks + 3100;
            using (OutPacket outPacket = new OutPacket("GameNextStagePacket"))
            {
                outPacket.WriteByte(2);
                outPacket.WriteInt();
                outPacket.WriteInt();
                RouterListener.MySession.Client.Send(outPacket);
            }
            using (OutPacket outPacket = new OutPacket("GameResultPacket"))
            {
                outPacket.WriteByte();
                outPacket.WriteInt(1);
                outPacket.WriteInt();
                outPacket.WriteUInt(FinishTime);
                outPacket.WriteByte();
                outPacket.WriteShort(SetRiderItem.Set_Kart);
                outPacket.WriteShort();
                outPacket.WriteShort();
                outPacket.WriteHexString("d0 78");
                outPacket.WriteByte();
                outPacket.WriteUInt(SetRider.RP);
                outPacket.WriteInt(0); //Earned RP
                outPacket.WriteInt(0); //Earned Lucci
                outPacket.WriteUInt(SetRider.Lucci);
                outPacket.WriteBytes(new byte[41]);
                outPacket.WriteInt(1);
                outPacket.WriteShort(768);
                outPacket.WriteBytes(new byte[50]);
                outPacket.WriteInt(255);
                outPacket.WriteInt();
                outPacket.WriteHexString("a8 b8 65 40");
                outPacket.WriteBytes(new byte[162]);
                outPacket.WriteHexString("01 77 00 2d 00 66 00 6f");
                outPacket.WriteBytes(new byte[20]);
                outPacket.WriteHexString("20 00 74 00 ff ff ff ff");
                RouterListener.MySession.Client.Send(outPacket);
            }
            using (OutPacket outPacket = new OutPacket("GameControlPacket"))
            {
                outPacket.WriteInt(4);
                outPacket.WriteHexString("fa 10 69 7f 6a ff 7f 00");
                outPacket.WriteByte();
                outPacket.WriteUInt(SettleTicks);
                outPacket.WriteInt(32767);
                outPacket.WriteInt();
                outPacket.WriteInt(1);
                outPacket.WriteBytes(new byte[21]);
                outPacket.WriteInt(1);
                outPacket.WriteBytes(new byte[28]);
                outPacket.WriteInt(8);
                outPacket.WriteBytes(new byte[6]);
                outPacket.WriteByte(10);
                RouterListener.MySession.Client.Send(outPacket);
            }
            Console.WriteLine("GameSlotPacket, Settle. Ticks = {0}", SettleTicks);
        }

        public static void Clientsession(uint hash, InPacket iPacket)
        {
            if (hash == Adler32Helper.GenerateAdler32_ASCII("GameSlotPacket", 0))
            {
                iPacket.ReadInt();
                iPacket.ReadInt();
                iPacket.ReadInt();
                var nextpacketlenth = iPacket.ReadInt();
                var nextpackethash = iPacket.ReadUInt();
                if (nextpackethash == Adler32Helper.GenerateAdler32_ASCII("GopCourse", 0))
                {
                    iPacket.ReadBytes(nextpacketlenth - 4 - 4);
                    ArrivalTicks = iPacket.ReadUInt();
                }
                Console.WriteLine("GameSlotPacket, Arrivaled. Ticks = {0}", ArrivalTicks);
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameControlPacket"))
            {
                var state = iPacket.ReadByte();
                //start
                if (state == 0)
                {
                    BootTicksNow = GetUpTime();
                    StartTicks += (StartTicks == 0) ? (BootTicksNow + 8000) : (BootTicksNow - BootTicksPrev);
                    BootTicksPrev = BootTicksNow;
                    using (OutPacket oPacket = new OutPacket("GameAiMasterSlotNoticePacket"))
                    {
                        oPacket.WriteInt();
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                    {
                        oPacket.WriteInt(1);
                        oPacket.WriteByte();
                        oPacket.WriteUInt(StartTicks);
                        oPacket.WriteBytes(new byte[71]);
                        oPacket.WriteByte(0x0a);
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    Console.WriteLine("GameControlPacket, Start. Ticks = {0}", StartTicks);
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
                        EndTicks = ArrivalTicks + 8000;
                        oPacket.WriteByte(3);
                        oPacket.WriteInt();
                        oPacket.WriteUInt(EndTicks);
                        oPacket.WriteBytes(new byte[71]);
                        oPacket.WriteByte(0x85);
                    }
                    Console.Write("GameControlPacket, Finish. Finish Time = {0}", FinishTime);
                    Console.WriteLine(" , End - Start Ticks : {0}", EndTicks - StartTicks - 8000);
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
                using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
                {
                    oPacket.WriteInt(0);
                    //oPacket.WriteInt(channeldata1);
                    oPacket.WriteInt(4);
                    oPacket.WriteEndPoint(IPAddress.Parse(RouterListener.client.Address.ToString()), 39312);
                    RouterListener.Listener.BeginAcceptSocket(new AsyncCallback(RouterListener.OnAcceptSocket), null);
                    RouterListener.MySession.Client.Send(oPacket);
                }
                GameSupport.OnDisconnect();
                return;
            }
            else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChannelMovein", 0))
            {
                using (OutPacket oPacket = new OutPacket("PrChannelMoveIn"))
                {
                    //oPacket.WriteHexString("01 3d a4 3d 49 8f 99 3d a4 3d 49 90 99");
                    oPacket.WriteByte(1);
                    oPacket.WriteEndPoint(IPAddress.Parse(RouterListener.forceConnect), 39311);
                    oPacket.WriteEndPoint(IPAddress.Parse(RouterListener.forceConnect), 39312);
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
                var Playernum = iPacket.ReadInt();
                iPacket.ReadInt();
                iPacket.ReadInt();
                RoomUnkBytes = iPacket.ReadBytes(32);
                using (OutPacket oPacket = new OutPacket("ChCreateRoomReplyPacket"))
                {
                    oPacket.WriteShort(1);
                    oPacket.WriteByte((byte)Playernum);
                    oPacket.WriteByte(unk1);
                    RouterListener.MySession.Client.Send(oPacket);
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
                    StartGameData.StartTimeAttack_RandomTrackGameType = 0;
                    StartGameData.StartTimeAttack_SpeedType = 7;
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
                    doc.Load(@"Profile\AI.xml");
                    int listCount = 0;
                    XmlNodeList lis = doc.GetElementsByTagName("AiList");
                    if (lis.Count > 0)
                    {
                        listCount = lis.Count;
                    }
                    oPacket.WriteInt(listCount); //AI count
                    XmlNodeList Data = doc.GetElementsByTagName("AiData");
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
                xmlDoc.Load(@"Profile\AI.xml");
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
                    xmlDoc.Save(@"Profile\AI.xml");
                }
                else
                {
                    using (OutPacket oPacket = new OutPacket("GrSlotDataBasicAi"))
                    {
                        oPacket.WriteInt(0);
                        oPacket.WriteByte(1);
                        oPacket.WriteInt(unk1);
                        oPacket.WriteShort(1);
                        oPacket.WriteShort((short)unk1);
                        oPacket.WriteShort(1508);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteByte(0);
                        oPacket.WriteHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
                        RouterListener.MySession.Client.Send(oPacket);
                    }
                    XmlElement element = xmlDoc.CreateElement("Ai" + unk1.ToString());
                    element.SetAttribute("character", "1");
                    element.SetAttribute("rid", "0");
                    element.SetAttribute("kart", "1508");
                    element.SetAttribute("balloon", "0");
                    element.SetAttribute("headBand", "0");
                    element.SetAttribute("goggle", "0");
                    XmlNode rootNode = xmlDoc.DocumentElement;
                    if (rootNode != null)
                    {
                        rootNode.AppendChild(element);
                    }
                    xmlDoc.Save(@"Profile\AI.xml");
                }
                using (OutPacket oPacket = new OutPacket("GrReplyBasicAiPacket"))
                {
                    oPacket.WriteByte(1);
                    oPacket.WriteHexString("2CFB6605");
                    RouterListener.MySession.Client.Send(oPacket);
                }
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
            doc.Load(@"Profile\AI.xml");
            XmlNode ai1 = doc.SelectSingleNode("//Ai1");
            XmlNode ai2 = doc.SelectSingleNode("//Ai2");
            XmlNode ai3 = doc.SelectSingleNode("//Ai3");
            XmlNode ai4 = doc.SelectSingleNode("//Ai4");
            XmlNode ai5 = doc.SelectSingleNode("//Ai5");
            XmlNode ai6 = doc.SelectSingleNode("//Ai6");
            XmlNode ai7 = doc.SelectSingleNode("//Ai7");
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
            if (ai4 != null)
            {
                outPacket.WriteInt(4);
            }
            else
            {
                outPacket.WriteHexString("FFFFFFFF");
            }
            if (ai1 != null)
            {
                outPacket.WriteInt(1);
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
            if (ai2 != null)
            {
                outPacket.WriteInt(2);
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
            if (ai3 != null)
            {
                outPacket.WriteInt(3);
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
            outPacket.WriteByte(1);
            outPacket.WriteByte(7); //(byte)channeldata2
            outPacket.WriteInt(0);
            outPacket.WriteHexString("083483D162"); //08 24 72 F5 9E
            outPacket.WriteInt(0);
            outPacket.WriteByte(0);
            outPacket.WriteByte(0);
            outPacket.WriteByte(0);
        }
    }
}
