using System;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace OSK
{
    public class WebJsonSystem : IFile
    {
        public void Save<T>(string fileName, T data, bool encrypt = false)
        {
            string json = JsonConvert.SerializeObject(data);
            if (encrypt)
                json = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            PlayerPrefs.SetString(fileName, json);
            PlayerPrefs.Save();
            
            MyLogger.Log($"✅ Saved (Web/PlayerPrefs is instantly safe): {fileName}");
        }

        public Cysharp.Threading.Tasks.UniTask SaveAsync<T>(string fileName, T data, bool encrypt = false)
        {
            MyLogger.Log($"⏳ [Async] Web save is instantaneous, wrapping in UniTask: {fileName}");
            Save(fileName, data, encrypt);
            return Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }

        public T Load<T>(string fileName, T defaultValue = default, bool decrypt = false)
        {
            if (!PlayerPrefs.HasKey(fileName))
                return defaultValue;

            string json = PlayerPrefs.GetString(fileName);

            if (decrypt)
            {
                var bytes = Convert.FromBase64String(json);
                json = Encoding.UTF8.GetString(bytes);
            }

            MyLogger.Log($"✅ Loaded (Web): {fileName}");
            return JsonConvert.DeserializeObject<T>(json);
        }

        public Cysharp.Threading.Tasks.UniTask<T> LoadAsync<T>(string fileName, T defaultValue = default, bool decrypt = false)
        {
            MyLogger.Log($"⏳ [Async] Web load is instantaneous, wrapping in UniTask: {fileName}");
            return Cysharp.Threading.Tasks.UniTask.FromResult(Load<T>(fileName, defaultValue, decrypt));
        }

        public void Delete(string fileName)
        {
            PlayerPrefs.DeleteKey(fileName);
        }

        public bool Exists(string fileName)
        {
            return PlayerPrefs.HasKey(fileName);
        }

        public void WriteAllLines(string fileName, string[] lines)
        {
            string json = JsonConvert.SerializeObject(lines);
            PlayerPrefs.SetString(fileName, json);
        }
    }

}
