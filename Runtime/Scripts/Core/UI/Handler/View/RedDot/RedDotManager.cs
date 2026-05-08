using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OSK
{
    public class RedDotManager : MonoBehaviour
    {
        public static RedDotManager Instance => SingletonManager.Instance.Get<RedDotManager>();
        private readonly Dictionary<string, bool> _manualStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, Func<bool>> _conditions = new Dictionary<string, Func<bool>>();
        
        public event Action<string, bool> OnChanged;

        private void Awake() => SingletonManager.Instance.RegisterGlobal(this);

        public void AddCondition(string key, Func<bool> condition)
        {
            _conditions[key] = condition;
            Refresh(key);
        }

        public void SetStatus(string key, bool isActive)
        {
            _manualStates[key] = isActive;
            Refresh(key);
        }

        public bool GetStatus(string key)
        {
            if (IsSelfActive(key)) return true;

            return _manualStates.Any(x => x.Key.StartsWith(key + "/") && x.Value) ||
                   _conditions.Any(x => x.Key.StartsWith(key + "/") && x.Value.Invoke());
        }

        private bool IsSelfActive(string key)
        {
            if (_manualStates.TryGetValue(key, out bool m) && m) return true;
            if (_conditions.TryGetValue(key, out var c) && c.Invoke()) return true;
            return false;
        }

        public void Refresh(string key = "")
        {
            if (string.IsNullOrEmpty(key))
            {
                var allKeys = _manualStates.Keys.Concat(_conditions.Keys).Distinct().ToList();
                foreach (var k in allKeys) Notify(k);
            }
            else
            {
                Notify(key);
                string[] parts = key.Split('/');
                string path = "";
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    path += (i == 0 ? "" : "/") + parts[i];
                    Notify(path);
                }
            }
        }

        private void Notify(string key)
        {
            bool status = GetStatus(key);
            MyLogger.Log($"[RedDot] Key: {key} -> {status}");
            OnChanged?.Invoke(key, status);
        }
    }
}
