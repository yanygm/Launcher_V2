using System;
using KartRider.IO.Packet;
using KartRider;
using ExcData;
using System.Collections.Generic;

namespace RiderData
{
    public static class RiderSchool
    {
        public static byte catLevel = 0;
        public static byte maxStep = 0;
        public static List<byte> evenProStep = new List<byte>();
        public static List<byte> oddProStep = new List<byte>();

        public static void PrStartRiderSchool(SessionGroup Parent, string Nickname)
        {
            using (OutPacket oPacket = new OutPacket("PrStartRiderSchool"))
            {
                oPacket.WriteByte(1);
                StartGameData.GetSchoolSpac(oPacket, Nickname);
                Parent.Client.Send(oPacket);
            }
        }

        public static void PrRiderSchoolData(SessionGroup Parent)
        {

            using (OutPacket outPacket = new OutPacket("PrRiderSchoolDataPacket"))
            {
                outPacket.WriteByte(catLevel);//라이센스 등급
                outPacket.WriteByte(maxStep);//마지막 클리어
                outPacket.WriteUShort((ushort)RouterListener.DataTime()[0]);
                outPacket.WriteUShort((ushort)RouterListener.DataTime()[1]);
                outPacket.WriteInt(0);
                outPacket.WriteByte(0);
                Parent.Client.Send(outPacket);
            }
        }

        public static void PrRiderSchoolPro(SessionGroup Parent)
        {
            int remainder = (RouterListener.DataTime()[2] - 1) % evenProStep.Count + 1;
            using (OutPacket oPacket = new OutPacket("PrRiderSchoolProPacket"))
            {
                oPacket.WriteByte(1);//엠블럼 체크
                oPacket.WriteByte(evenProStep[remainder - 1]);
                oPacket.WriteByte(catLevel);
                oPacket.WriteByte(oddProStep[remainder - 1]);
                oPacket.WriteInt(0);
                oPacket.WriteInt(0);
                oPacket.WriteInt(0);
                Parent.Client.Send(oPacket);
            }
        }
    }
}
