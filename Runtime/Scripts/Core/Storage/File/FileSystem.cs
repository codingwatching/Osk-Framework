// FileSystem.cs
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

namespace OSK
{
    public class FileSystem : IFile
    {
        private static string EnsureExtension(string fileName, string ext)
        {
            if (Path.HasExtension(fileName)) return fileName;
            return fileName + ext;
        }

        private string ResolvePath(string fileName)
        {
            string filename = EnsureExtension(fileName, ".dat");
            return IOUtility.GetPath(filename);
        }

        public void Save<T>(string fileName, T data, bool encrypt = false)
        {
            string path = ResolvePath(fileName);
            SaveResolved(path, data, encrypt);
        }

        private void SaveResolved<T>(string path, T data, bool encrypt = false)
        {
            string tempPath = path + ".tmp";
            try
            {
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters = { new Vector3Converter(), new QuaternionConverter() }
                };

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    // 🚀 ULTRA OPTIMIZATION: Stream straight to bytes, avoid strings completely
                    using (var sw = new StreamWriter(ms, new UTF8Encoding(false), 1024, true))
                    using (var jw = new JsonTextWriter(sw) { Formatting = Formatting.None })
                    {
                        JsonSerializer.CreateDefault(settings).Serialize(jw, data);
                    }
                    bytes = ms.ToArray();
                }

                bytes = DataCompressor.Compress(bytes);

                if (encrypt)
                {
                    bytes = FileSecurity.Encrypt(bytes, IOUtility.encryptKey);
                }

                using (var fs = new FileStream(tempPath, FileMode.Create))
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(bytes.Length);
                    bw.Write(bytes);
                }

                // Safe Save: Swap temp file with real file
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);

                MyLogger.Log($"✅ Saved (Safe Save): {path}");
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Save Error: {Path.GetFileName(path)} → {ex.Message}");
            }
        }

        public async UniTask SaveAsync<T>(string fileName, T data, bool encrypt = false)
        {
            MyLogger.Log($"⏳ [Async] Starting background save for: {fileName}");
            string path = ResolvePath(fileName); // MUST be on Main Thread
            await UniTask.RunOnThreadPool(() => SaveResolved(path, data, encrypt));
        }

        public T Load<T>(string fileName, bool decrypt = false)
        {
            string path = ResolvePath(fileName);
            return LoadResolved<T>(path, decrypt);
        }

        private T LoadResolved<T>(string path, bool decrypt = false)
        {
            if (!File.Exists(path))
            {
                MyLogger.LogError($"❌ File not found: {path}");
                return default;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (decrypt)
                    bytes = FileSecurity.Decrypt(bytes, IOUtility.encryptKey);

                using var reader = new BinaryReader(new MemoryStream(bytes));
                int len = reader.ReadInt32();
                byte[] jsonBytes = reader.ReadBytes(len);

                jsonBytes = DataCompressor.Decompress(jsonBytes);

                MyLogger.Log($"✅ Loaded: {path}");
                var settings = new JsonSerializerSettings { Converters = { new Vector3Converter(), new QuaternionConverter() } };
                
                // 🚀 ULTRA OPTIMIZATION: Stream bytes straight to deserializer
                using (var ms = new MemoryStream(jsonBytes))
                using (var sr = new StreamReader(ms, Encoding.UTF8))
                using (var jr = new JsonTextReader(sr))
                {
                    var serializer = JsonSerializer.CreateDefault(settings);
                    return serializer.Deserialize<T>(jr);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Load Error: {Path.GetFileName(path)} → {ex.Message}");
                return default;
            }
        }

        public async UniTask<T> LoadAsync<T>(string fileName, bool decrypt = false)
        {
            MyLogger.Log($"⏳ [Async] Starting background load for: {fileName}");
            string path = ResolvePath(fileName); // MUST be on Main Thread
            return await UniTask.RunOnThreadPool(() => LoadResolved<T>(path, decrypt));
        }

        public void Delete(string fileName) => IOUtility.DeleteFile(EnsureExtension(fileName, ".dat"));

        public T Query<T>(string fileName, bool condition) => condition ? Load<T>(fileName) : default;

        public void WriteAllLines(string fileName, string[] lines)
        {
            string path = IOUtility.GetPath(EnsureExtension(fileName, ".txt"));
            File.WriteAllLines(path, lines);
            MyLogger.Log($"📝 Wrote lines to: {path}");
        }

        public bool Exists(string fileName)
        {
            string path = ResolvePath(fileName);
            return File.Exists(path);
        }


    }
}
