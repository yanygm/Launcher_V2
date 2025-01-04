using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Tontrollers
{
    public interface IKeyframeData<TValue>
    {
        TValue GetValue(float time);

        void DecodeObject(BinaryReader reader, int count);
    }
}
