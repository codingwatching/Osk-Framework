using System;

namespace OSK
{
    /// <summary>
    /// Marks a field for automatic module injection via Main.Inject().
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectModuleAttribute : Attribute
    {
    }
}
