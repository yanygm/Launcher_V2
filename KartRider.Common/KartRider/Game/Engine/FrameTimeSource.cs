using System;

namespace KartLibrary.Game.Engine;

public class FrameTimeSource : ITimeSource
{
    private long? _firstFrameTime;

    private long? _lastFrameTime;

    public void OnUpdateFrame()
    {
        long? firstFrameTime = _firstFrameTime;
        if (!firstFrameTime.HasValue)
        {
            _lastFrameTime = (_firstFrameTime = Environment.TickCount64);
        }
        else
        {
            _lastFrameTime = Environment.TickCount64;
        }
    }

    public long GetTimeStamp()
    {
        return (_lastFrameTime - _firstFrameTime).GetValueOrDefault();
    }
}