using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine
{
    public class FrameTimeSource: ITimeSource
    {
        private long? _firstFrameTime;

        private long? _lastFrameTime;

        public FrameTimeSource() 
        {
            
        }

        public void OnUpdateFrame()
        {
            if (_firstFrameTime is null)
            {
                _lastFrameTime = _firstFrameTime = Environment.TickCount64;
            }
            else
            {
                _lastFrameTime = Environment.TickCount64;
            }
        }

        public long GetTimeStamp()
        {
            return (_lastFrameTime - _firstFrameTime) ?? 0;
        }
    }
}
