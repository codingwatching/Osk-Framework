using System;

namespace OSK
{
    /// <summary>
    /// Declares that a GameFrameworkComponent depends on another module.
    /// Used by Main to validate dependencies during initialization.
    /// Example: [RequireModule(typeof(PoolManager))]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireModuleAttribute : Attribute
    {
        public Type RequiredType { get; }

        public RequireModuleAttribute(Type requiredType)
        {
            RequiredType = requiredType;
        }
    }
}
