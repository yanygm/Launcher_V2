using System;
using System.Numerics;

namespace KartLibrary.Record;

public struct RecordData
{
    public RecordStamp[] Stamps { get; set; }

    public RecordStamp this[double time]
    {
        get
        {
            if (time < 0.0)
            {
                throw new ArgumentOutOfRangeException("Negtive time is not allowed.");
            }

            if (Stamps.Length < 1)
            {
                throw new IndexOutOfRangeException("This record data do not have any stamps.");
            }

            RecordStamp result = Array.FindLast(Stamps, (RecordStamp x) => (double)x.Time <= time);
            RecordStamp recordStamp = Array.Find(Stamps, (RecordStamp x) => (double)x.Time > time);
            if (recordStamp.IsInitialObject)
            {
                return result;
            }

            float num = recordStamp.Time - result.Time;
            float num2 = (float)(time - (double)result.Time) / num;
            RecordStamp result2 = new RecordStamp();
            result2.Time = (int)time;
            result2.X = result.X * (1f - num2) + recordStamp.X * num2;
            result2.Y = result.Y * (1f - num2) + recordStamp.Y * num2;
            result2.Z = result.Z * (1f - num2) + recordStamp.Z * num2;
            result2.Angle = Quaternion.Slerp(result.Angle, recordStamp.Angle, num2);
            result2.Status = result.Status;
            return result2;
        }
    }

    public RecordData()
    {
        Stamps = new RecordStamp[0];
    }
}