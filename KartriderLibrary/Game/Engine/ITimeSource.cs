using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine
{
    public interface ITimeSource
    {
        long GetTimeStamp();
    }
}
