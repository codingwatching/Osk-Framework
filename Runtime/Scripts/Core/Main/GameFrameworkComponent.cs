using System;
using System.Linq;
using UnityEngine;

namespace OSK
{
    [DefaultExecutionOrder(-1001)]
    public abstract class GameFrameworkComponent : MonoBehaviour
    {
        public virtual void Awake()
        {
            Main.Register(this);
        }
        
        public virtual void OnDestroy() 
        {
            Main.UnRegister(this);
        }

        public abstract void OnInit();

        /// <summary>
        /// Returns the types this module depends on via [RequireModule] attributes.
        /// </summary>
        public Type[] GetDependencies()
        {
            return GetType()
                .GetCustomAttributes(typeof(RequireModuleAttribute), true)
                .Cast<RequireModuleAttribute>()
                .Select(a => a.RequiredType)
                .ToArray();
        }
    }
}