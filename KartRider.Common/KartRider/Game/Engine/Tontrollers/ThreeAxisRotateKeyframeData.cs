using System;
using System.IO;
using System.Numerics;

namespace KartLibrary.Game.Engine.Tontrollers;

public class ThreeAxisRotateKeyframeData : RotateKeyframeData
{
    private IFloatKeyframeData _xAxisKeyframeData;

    private IFloatKeyframeData _yAxisKeyframeData;

    private IFloatKeyframeData _zAxisKeyframeData;

    private float _u2;

    private float _u3;

    private float _u4;

    private float _u5;

    public IFloatKeyframeData XAxisKeyframeData => _xAxisKeyframeData;

    public IFloatKeyframeData YAxisKeyframeData => _yAxisKeyframeData;

    public IFloatKeyframeData ZAxisKeyframeData => _zAxisKeyframeData;

    public override RotateKeyframeDataType ListType => RotateKeyframeDataType.ThreeAxis;

    public ThreeAxisRotateKeyframeData()
    {
    }

    public ThreeAxisRotateKeyframeData(IFloatKeyframeData xAxisKeyframeData, IFloatKeyframeData yAxisKeyframeData, IFloatKeyframeData zAxisKeyframeData)
    {
        _xAxisKeyframeData = xAxisKeyframeData;
        _yAxisKeyframeData = yAxisKeyframeData;
        _zAxisKeyframeData = zAxisKeyframeData;
    }

    public override void DecodeObject(BinaryReader reader, int count)
    {
        reader.ReadInt32();
        _u2 = reader.ReadSingle();
        _u3 = reader.ReadSingle();
        _u4 = reader.ReadSingle();
        _u5 = reader.ReadSingle();
        int num = reader.ReadInt32();
        int count2 = reader.ReadInt32();
        switch (num)
        {
            case 0:
                _xAxisKeyframeData = new CubicFloatKeyframeData();
                _xAxisKeyframeData.DecodeObject(reader, count2);
                break;
            case 1:
                _xAxisKeyframeData = new LinearFloatKeyframeData();
                _xAxisKeyframeData.DecodeObject(reader, count2);
                break;
            default:
                throw new NotSupportedException("Sorry, Author is too stupid to finish this section.");
        }

        num = reader.ReadInt32();
        count2 = reader.ReadInt32();
        switch (num)
        {
            case 0:
                _yAxisKeyframeData = new CubicFloatKeyframeData();
                _yAxisKeyframeData.DecodeObject(reader, count2);
                break;
            case 1:
                _yAxisKeyframeData = new LinearFloatKeyframeData();
                _yAxisKeyframeData.DecodeObject(reader, count2);
                break;
            default:
                throw new NotSupportedException("Sorry, Author is too stupid to finish this section.");
        }

        num = reader.ReadInt32();
        count2 = reader.ReadInt32();
        switch (num)
        {
            case 0:
                _zAxisKeyframeData = new CubicFloatKeyframeData();
                _zAxisKeyframeData.DecodeObject(reader, count2);
                break;
            case 1:
                _zAxisKeyframeData = new LinearFloatKeyframeData();
                _zAxisKeyframeData.DecodeObject(reader, count2);
                break;
            default:
                throw new NotSupportedException("Sorry, Author is too stupid to finish this section.");
        }
    }

    public override Quaternion GetValue(float time)
    {
        float value = _xAxisKeyframeData.GetValue(time);
        float value2 = _yAxisKeyframeData.GetValue(time);
        float value3 = _zAxisKeyframeData.GetValue(time);
        Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitX, value);
        Quaternion quaternion2 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, value2);
        return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, value3) * quaternion2 * quaternion;
    }
}