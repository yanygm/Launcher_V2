using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Tontrollers
{
    public interface IKeyframe<T>
    {
        int Time { get; set; }

        T Value { get; set; }

        T CalculateKeyFrame(float t, IKeyframe<T>? nextKeyframe);
    }
}
