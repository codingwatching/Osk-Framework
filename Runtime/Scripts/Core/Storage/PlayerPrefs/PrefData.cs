using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace OSK
{
    /// <summary>
    /// A unified, EasySave3-like PlayerPrefs wrapper.
    /// Usage: 
    /// PrefData.Save("key", value);
    /// var val = PrefData.Load("key", defaultValue);
    /// PrefData.HasKey("key");
    /// </summary>
    public static class PrefData
    {
        #region 1. CONFIGURATION & KEYS
        public static bool IsEncrypt = false;
        private static string _encryptionKey = "1234567890123456"; 
        private static string _initializationVector = "8855221144778899";
        #endregion

        #region 2. CORE API (ES3 Style)

        /// <summary>
        /// Saves a value to PlayerPrefs. Zero boxing allocation for primitives.
        /// Example: PrefData.Save("myIntKey", 42);
        /// </summary>
        public static void Save(string key, int value) => SetInt(key, value);
        public static void Save(string key, float value) => SetFloat(key, value);
        public static void Save(string key, bool value) => SetBool(key, value);
        public static void Save(string key, string value) => SetString(key, value);
        public static void Save(string key, Vector2 value) => SetVector2(key, value);
        public static void Save(string key, Vector3 value) => SetVector3(key, value);
        public static void Save(string key, Quaternion value) => SetQuaternion(key, value);
        public static void Save(string key, Color value) => SetColor(key, value);
        
        public static void Save(string key, Dictionary<string, int> dict)
        {
            if (dict == null) { DeleteKey(key); return; }
            SetDictionary(key, dict);
        }

        /// <summary>
        /// Explicit overload for Lists to prevent AOT (Ahead-of-Time) compilation issues on IL2CPP platforms (Switch, WebGL, iOS).
        /// </summary>
        public static void Save<TItem>(string key, List<TItem> list)
        {
            if (list == null) { DeleteKey(key); return; }
            var wrapper = new ListWrapper<TItem> { list = list };
            string content = JsonUtility.ToJson(wrapper);
            if (IsEncrypt) content = Encrypt(content);
            PlayerPrefs.SetString(key, content);
        }

        /// <summary>
        /// Generic Save for Objects and Classes.
        /// </summary>
        public static void Save<T>(string key, T value)
        {
            if (value == null)
            {
                DeleteKey(key);
                return;
            }

            Type type = typeof(T);

            // Primitives
            if (type == typeof(int)) { SetInt(key, (int)(object)value); return; }
            if (type == typeof(float)) { SetFloat(key, (float)(object)value); return; }
            if (type == typeof(bool)) { SetBool(key, (bool)(object)value); return; }
            if (type == typeof(string)) { SetString(key, (string)(object)value); return; }

            // Unity Types
            if (type == typeof(Vector2)) { SetVector2(key, (Vector2)(object)value); return; }
            if (type == typeof(Vector3)) { SetVector3(key, (Vector3)(object)value); return; }
            if (type == typeof(Quaternion)) { SetQuaternion(key, (Quaternion)(object)value); return; }
            if (type == typeof(Color)) { SetColor(key, (Color)(object)value); return; }

            // Specific Collections
            if (type == typeof(Dictionary<string, int>)) { SetDictionary(key, (Dictionary<string, int>)(object)value); return; }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                SetListGeneric<T>(key, value);
                return;
            }

            // Custom Objects
            string content = SerializeObject(value);
            if (IsEncrypt) content = Encrypt(content);
            PlayerPrefs.SetString(key, content);
        }

        /// <summary>
        /// Loads a value from PlayerPrefs. Supports primitives, Unity types, Lists, Dictionaries, and custom classes.
        /// Example: int myInt = PrefData.Load("myIntKey", 0);
        /// </summary>
        public static T Load<T>(string key, T defaultValue = default)
        {
            if (!HasKey(key)) return defaultValue;

            Type type = typeof(T);

            // Primitives
            if (type == typeof(int)) return (T)(object)GetInt(key, (int)(object)defaultValue);
            if (type == typeof(float)) return (T)(object)GetFloat(key, (float)(object)defaultValue);
            if (type == typeof(bool)) return (T)(object)GetBool(key, (bool)(object)defaultValue);
            if (type == typeof(string)) return (T)(object)GetString(key, (string)(object)defaultValue);

            // Unity Types
            if (type == typeof(Vector2)) return (T)(object)GetVector2(key, (Vector2)(object)defaultValue);
            if (type == typeof(Vector3)) return (T)(object)GetVector3(key, (Vector3)(object)defaultValue);
            if (type == typeof(Quaternion)) return (T)(object)GetQuaternion(key, (Quaternion)(object)defaultValue);
            if (type == typeof(Color)) return (T)(object)GetColor(key, (Color)(object)defaultValue);

            // Specific Collections
            if (type == typeof(Dictionary<string, int>)) return (T)(object)GetDictionary(key);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return GetListGeneric<T>(key, defaultValue);
            }

            // Custom Objects
            string content = PlayerPrefs.GetString(key);
            if (IsEncrypt)
            {
                if (string.IsNullOrEmpty(content)) return defaultValue;
                try { content = Decrypt(content); }
                catch { return defaultValue; }
            }

            return DeserializeObject<T>(content, defaultValue);
        }

        public static bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public static void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public static void DeleteAll() => PlayerPrefs.DeleteAll();
        public static void SaveAll() => PlayerPrefs.Save();

        #endregion

        #region 3. INTERNAL PRIMITIVES HANDLING
        private static void SetBool(string key, bool value)
        {
            if (IsEncrypt) SetString(key, value.ToString());
            else PlayerPrefs.SetInt(key, value ? 1 : 0);
        }
        private static bool GetBool(string key, bool defaultValue)
        {
            if (IsEncrypt)
            {
                string decryptedVal = GetString(key, "");
                if (bool.TryParse(decryptedVal, out bool result)) return result;
                return defaultValue;
            }
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        private static void SetFloat(string key, float value)
        {
            if (IsEncrypt) SetString(key, value.ToString());
            else PlayerPrefs.SetFloat(key, value);
        }
        private static float GetFloat(string key, float defaultValue)
        {
            if (IsEncrypt)
            {
                string val = GetString(key, "");
                if (float.TryParse(val, out float result)) return result;
                return defaultValue;
            }
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        private static void SetInt(string key, int value)
        {
            if (IsEncrypt) SetString(key, value.ToString());
            else PlayerPrefs.SetInt(key, value);
        }
        private static int GetInt(string key, int defaultValue)
        {
            if (IsEncrypt)
            {
                string val = GetString(key, "");
                if (int.TryParse(val, out int result)) return result;
                return defaultValue;
            }
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        private static void SetString(string key, string value)
        {
            if (IsEncrypt) PlayerPrefs.SetString(key, Encrypt(value));
            else PlayerPrefs.SetString(key, value);
        }
        private static string GetString(string key, string defaultValue)
        {
            string value = PlayerPrefs.GetString(key, defaultValue);
            if (IsEncrypt)
            {
                if (value == defaultValue) return defaultValue;
                string decrypted = Decrypt(value);
                if (string.IsNullOrEmpty(decrypted) && !string.IsNullOrEmpty(value)) return defaultValue; 
                return decrypted;
            }
            return value;
        }
        #endregion

        #region 4. INTERNAL UNITY TYPES
        private static void SetVector2(string key, Vector2 v) => SetString(key, JsonUtility.ToJson(v, false));
        private static Vector2 GetVector2(string key, Vector2 defaultValue)
        {
            string json = GetString(key, null); 
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Vector2>(json);
        }

        private static void SetVector3(string key, Vector3 v) => SetString(key, JsonUtility.ToJson(v, false));
        private static Vector3 GetVector3(string key, Vector3 defaultValue)
        {
            string json = GetString(key, null);
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Vector3>(json);
        }

        private static void SetQuaternion(string key, Quaternion q) => SetString(key, JsonUtility.ToJson(q, false));
        private static Quaternion GetQuaternion(string key, Quaternion defaultValue)
        {
            string json = GetString(key, null);
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Quaternion>(json);
        }

        private static void SetColor(string key, Color c) => SetString(key, JsonUtility.ToJson(c, false));
        private static Color GetColor(string key, Color defaultValue)
        {
            string json = GetString(key, null);
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Color>(json);
        }
        #endregion

        #region 5. INTERNAL COLLECTIONS
        [System.Serializable]
        private class ListWrapper<TItem> { public List<TItem> list; }
        
        private static void SetListGeneric<T>(string key, T value)
        {
            // T is List<TItem>
            Type itemType = typeof(T).GetGenericArguments()[0];
            Type wrapperType = typeof(ListWrapper<>).MakeGenericType(itemType);
            object wrapper = Activator.CreateInstance(wrapperType);
            wrapperType.GetField("list").SetValue(wrapper, value);
            SetString(key, JsonUtility.ToJson(wrapper, false));
        }

        private static T GetListGeneric<T>(string key, T defaultValue)
        {
            string json = GetString(key, null);
            if (string.IsNullOrEmpty(json)) return defaultValue;
            
            Type itemType = typeof(T).GetGenericArguments()[0];
            Type wrapperType = typeof(ListWrapper<>).MakeGenericType(itemType);
            object wrapper = JsonUtility.FromJson(json, wrapperType);
            if (wrapper == null) return defaultValue;
            
            return (T)wrapperType.GetField("list").GetValue(wrapper);
        }

        [System.Serializable]
        private class DictWrapper { public List<string> keys; public List<int> values; }

        private static void SetDictionary(string key, Dictionary<string, int> dict)
        {
            var wrapper = new DictWrapper { keys = new List<string>(), values = new List<int>() };
            foreach (var kv in dict) { wrapper.keys.Add(kv.Key); wrapper.values.Add(kv.Value); }
            SetString(key, JsonUtility.ToJson(wrapper, false));
        }

        private static Dictionary<string, int> GetDictionary(string key)
        {
            string json = GetString(key, null);
            if (string.IsNullOrEmpty(json)) return new Dictionary<string, int>();
            try 
            {
                var wrapper = JsonUtility.FromJson<DictWrapper>(json);
                var dict = new Dictionary<string, int>();
                for (int i = 0; i < wrapper.keys.Count; i++) dict[wrapper.keys[i]] = wrapper.values[i];
                return dict;
            }
            catch { return new Dictionary<string, int>(); }
        }
        #endregion

        #region 6. INTERNAL OBJECT SERIALIZATION
        [System.Serializable]
        private class Wrapper<TObj> { public TObj data; }

        private static string SerializeObject<T>(T value)
        {
            Type type = typeof(T);
            if (type.IsEnum) return value.ToString();
            
            // Default object wrapper for JsonUtility
            var wrapper = new Wrapper<T> { data = value };
            return JsonUtility.ToJson(wrapper);
        }
        
        private static T DeserializeObject<T>(string content, T defaultValue)
        {
            if (string.IsNullOrEmpty(content)) return defaultValue;

            try
            {
                Type type = typeof(T);
                if (type.IsEnum) return (T)Enum.Parse(type, content);

                Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(content);
                return wrapper != null ? wrapper.data : defaultValue;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PrefData] Parse error for key type {typeof(T)}: {e.Message}. Returning default.");
                return defaultValue;
            }
        }
        #endregion

        #region 7. ENCRYPTION
        private static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                using (Aes aes = Aes.Create())
            {
                    aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
                    aes.IV = Encoding.UTF8.GetBytes(_initializationVector);
                    using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                        return Convert.ToBase64String(encryptedBytes);
                    }
                }
            }
            catch { return plainText; }
        }

        private static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return "";
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
                    aes.IV = Encoding.UTF8.GetBytes(_initializationVector);
                    using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                }
            }
            catch { return ""; }
        }
        #endregion
    }
}