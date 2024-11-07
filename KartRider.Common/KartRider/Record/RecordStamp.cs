using System.Collections.Generic;
using System.Numerics;

namespace KartLibrary.Record;

public struct RecordStamp
{
    public int Time { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public Quaternion Angle { get; set; }

    public ushort Status { get; set; }

    public bool IsInitialObject => Time == -1;

    public RecordStamp()
    {
        Time = -1;
        X = 0f;
        Y = 0f;
        Z = 0f;
        Status = 0;
        Angle = default(Quaternion);
    }

    public string[] GetCarStatus()
    {
        string[] array = new string[8] { "", "噴紅氣", "噴藍氣", "短噴", "開前噴", "gas(101)", "gas(110)", "開噴" };
        string[] array2 = new string[8] { "", "左擺頭", "右擺頭", "閃到頭", "倒退頭", "倒左頭", "倒右頭", "撞到頭" };
        string[] array3 = new string[4] { "", "加速特效", "甩尾特效", "甩+加速" };
        List<string> list = new List<string>();
        if (array[Status & 7] != "")
        {
            list.Add(array[Status & 7]);
        }

        if (array2[(Status >> 3) & 7] != "")
        {
            list.Add(array2[(Status >> 3) & 7]);
        }

        if (array3[(Status >> 6) & 3] != "")
        {
            list.Add(array3[(Status >> 6) & 3]);
        }

        return list.ToArray();
    }
}