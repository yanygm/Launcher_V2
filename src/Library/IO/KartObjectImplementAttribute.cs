namespace Launcher.Library.IO
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
            CreateObjectMethod = null;

        }

        public KartObjectImplementAttribute(CreateObjectFunc? createObjectMethod)
        {
            CreateObjectMethod = createObjectMethod;
        }
    }

    public delegate KartObject CreateObjectFunc();
}
