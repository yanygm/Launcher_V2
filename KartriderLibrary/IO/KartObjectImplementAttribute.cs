using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.IO
{
    /// <summary>
    /// The implement of KartObject. This attribute can be used on the class that derived of KartObject.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class KartObjectImplementAttribute : Attribute
    {
        public CreateObjectFunc? CreateObjectMethod;

        public KartObjectImplementAttribute()
        {
            this.CreateObjectMethod = null;
            
        }

        public KartObjectImplementAttribute(CreateObjectFunc? createObjectMethod)
        {
            this.CreateObjectMethod = createObjectMethod;
        }
    }

    public delegate KartObject CreateObjectFunc();
}
