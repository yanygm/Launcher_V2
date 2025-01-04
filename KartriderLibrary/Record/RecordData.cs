using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace KartLibrary.Record
{
    public struct RecordData
    {
        public RecordData()
        {
            
        }

        public RecordStamp[] Stamps { get; set; } = new RecordStamp[0];

        public RecordStamp this[double time] //Unit: ms
        {
            get
            {
                if (time < 0)
                    throw new ArgumentOutOfRangeException("Negtive time is not allowed.");
                if (Stamps.Length < 1)
                    throw new IndexOutOfRangeException("This record data do not have any stamps.");
                RecordStamp t1 = Array.FindLast(Stamps, x => x.Time <= time); //nowTime or previousTime
                RecordStamp t2 = Array.Find(Stamps, x => x.Time > time); //NextTime
                if (t2.IsInitialObject)
                    return t1;
                float time21 = (float)(t2.Time - t1.Time);
                float timec1 = (float)((time - t1.Time))/(time21);
                RecordStamp curTime = new RecordStamp();
                curTime.Time = (int)time;
                curTime.X = t1.X * (1 - timec1) + t2.X * (timec1);
                curTime.Y = t1.Y * (1 - timec1) + t2.Y * (timec1);
                curTime.Z = t1.Z * (1 - timec1) + t2.Z * (timec1);
                curTime.Angle = Quaternion.Slerp(t1.Angle, t2.Angle, timec1);
                /*
                curTime.angle_W = t1.angle_W * (1 - timec1) + t2.angle_W * (timec1);
                curTime.angle_X = t1.angle_X * (1 - timec1) + t2.angle_X * (timec1);
                curTime.angle_Y = t1.angle_Y * (1 - timec1) + t2.angle_Y * (timec1);
                curTime.angle_Z = t1.angle_Z * (1 - timec1) + t2.angle_Z * (timec1);
                */
                curTime.Status = t1.Status;
                return curTime;
            }
        }
    }
}
