using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    [Serializable]
    public class MainModules
    {
        // Cache nội bộ không cần hiển thị
        [HideInInspector]
        private Dictionary<string, Type> componentTypeCache = new();

        [BoxGroup("⚙ Modules Selection")]
        [EnumToggleButtons]
        [SerializeField]
        private ModuleType _modules;
        public ModuleType Modules => _modules;

        [BoxGroup("⚙ Modules Selection")]
        [ReadOnly, InfoBox("Select the modules you want to enable for this project.", InfoMessageType.None)]
        [HideLabel]
        public string title = "";

        [BoxGroup("⚙ Modules Selection/Choose")]
        [HorizontalGroup("⚙ Modules Selection/Choose/Row")]
        [Button(ButtonSizes.Medium)]
        private void EnableAllModule()
        {
            _modules = (ModuleType)~0;
            Debug.Log("✅ All modules have been selected.");
        }

        [HorizontalGroup("⚙ Modules Selection/Choose/Row")]
        [Button(ButtonSizes.Medium)]
        private void DisableAllModule()
        {
            _modules = 0;
            Debug.Log("❌ All modules have been deselected.");
        }

        // Public method for resolving Type
        public Type GetComponentType(string moduleName)
        {
            if (componentTypeCache.TryGetValue(moduleName, out var type))
                return type;

            // Try direct lookup with namespace
            var fullTypeName = "OSK." + moduleName;
            var componentType = Type.GetType(fullTypeName);
            
            // If failed, scan assemblies (once and cache)
            if (componentType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var foundType = assembly.GetType(fullTypeName);
                    if (foundType == null) foundType = assembly.GetType(moduleName); // Try without namespace too
                    
                    if (foundType != null)
                    {
                        componentType = foundType;
                        break;
                    }
                }
            }

            if (componentType != null)
                componentTypeCache[moduleName] = componentType;

            return componentType;
        }

#if UNITY_EDITOR
        [OnInspectorGUI]
        private void ValidateModules()
        {
            if (_modules == ModuleType.None) return;

            foreach (ModuleType moduleType in Enum.GetValues(typeof(ModuleType)))
            {
                if (moduleType == ModuleType.None || (_modules & moduleType) == 0) continue;
                
                if (GetComponentType(moduleType.ToString()) == null)
                {
                    Sirenix.Utilities.Editor.SirenixEditorGUI.ErrorMessageBox($"Implementation for module '{moduleType}' not found! Check class name matching ModuleType.");
                }
            }
        }
#endif
    }
}