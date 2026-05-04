using UnityEngine;

namespace OSK
{
    /// <summary>
    /// Integrates PrefData into the global DataManager / IFile system.
    /// </summary>
    public class PlayerPrefsSystem : IFile
    {
        public void Save<T>(string fileName, T data, bool isEncrypt = false)
        {
            // Sync encryption state
            bool oldEncrypt = PrefData.IsEncrypt;
            PrefData.IsEncrypt = isEncrypt;
            
            PrefData.Save<T>(fileName, data);
            
            // Restore previous state just in case
            PrefData.IsEncrypt = oldEncrypt;
            
            //MyLogger.Log($"✅ Saved (PlayerPrefs is instantly safe): {fileName}");
        }

        public Cysharp.Threading.Tasks.UniTask SaveAsync<T>(string fileName, T data, bool isEncrypt = false)
        {
            //MyLogger.Log($"⏳ [Async] PlayerPrefs save is instantaneous, wrapping in UniTask: {fileName}");
            Save(fileName, data, isEncrypt);
            return Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }

        public T Load<T>(string fileName, bool isDecrypt = false)
        {
            //MyLogger.Log($"✅ Loaded (PlayerPrefs): {fileName}");
            return PrefData.Load<T>(fileName, default);
        }

        public Cysharp.Threading.Tasks.UniTask<T> LoadAsync<T>(string fileName, bool isDecrypt = false)
        {
            //MyLogger.Log($"⏳ [Async] PlayerPrefs load is instantaneous, wrapping in UniTask: {fileName}");
            return Cysharp.Threading.Tasks.UniTask.FromResult(Load<T>(fileName, isDecrypt));
        }

        public void Delete(string fileName)
        {
            PrefData.DeleteKey(fileName);
        }

        public bool Exists(string fileName)
        {
            return PrefData.HasKey(fileName);
        }

        public void WriteAllLines(string fileName, string[] lines)
        {
            // WriteAllLines isn't natively standard for PlayerPrefs, but we can save it as a List<string>
            PrefData.Save<string>(fileName, new System.Collections.Generic.List<string>(lines));
        }
    }
}
