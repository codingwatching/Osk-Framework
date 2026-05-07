using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using System.IO;

namespace OSK
{
    public static class FileSecurity
    {

        public static byte[] Encrypt(byte[] data, string key)
        {
            if (data == null || data.Length == 0) return data;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    memoryStream.Write(iv, 0, iv.Length);
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                    }
                    return memoryStream.ToArray();
                }
            }
        }

        public static byte[] Decrypt(byte[] encryptedData, string key)
        {
            if (encryptedData == null || encryptedData.Length < 16)
            {
                return null;
            }

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                    byte[] iv = new byte[16];
                    System.Array.Copy(encryptedData, 0, iv, 0, 16);
                    aes.IV = iv;

                    int dataLength = encryptedData.Length - 16;
                    if (dataLength <= 0) return null;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (MemoryStream inputMemoryStream = new MemoryStream(encryptedData, 16, dataLength))
                    using (MemoryStream outputMemoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(inputMemoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            cryptoStream.CopyTo(outputMemoryStream);
                        }
                        return outputMemoryStream.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 🧩 Smart Decryption: Automatically detects if data is encrypted (raw or prefixed), compressed, or plain text.
        /// </summary>
        public static byte[] DecryptSmart(this byte[] data, string key)
        {
            if (data == null || data.Length == 0) return data;

            // 1. Try FileSystem Style: [Length(4)][Encrypted/Compressed Data]
            if (data.Length >= 4)
            {
                try
                {
                    using var ms = new MemoryStream(data);
                    using var br = new BinaryReader(ms);
                    int len = br.ReadInt32();
                    if (len > 0 && len <= data.Length - 4)
                    {
                        byte[] innerData = br.ReadBytes(len);
                        
                        // Try Decrypt
                        byte[] decrypted = Decrypt(innerData, key);
                        if (decrypted != null) return DataCompressor.Decompress(decrypted);

                        // If Decrypt failed, check if it's just GZip
                        if (DataCompressor.IsCompressed(innerData)) return DataCompressor.Decompress(innerData);
                        
                        // Last resort for prefixed: return as-is
                        return innerData;
                    }
                }
                catch { }
            }

            // 2. Try XML Style or Raw AES
            if (!IsLikelyPlainText(data))
            {
                byte[] decrypted = Decrypt(data, key);
                if (decrypted != null) return DataCompressor.Decompress(decrypted);
            }

            // 3. Final Fallback: Check if it's plain GZip or just raw text
            return DataCompressor.Decompress(data);
        }

        private static bool IsLikelyPlainText(byte[] data)
        {
            if (data == null || data.Length == 0) return true;
            byte b = data[0];
            return b == '{' || b == '[' || b == '<';
        }

        public static string Encrypt(string plainText, string Key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = new byte[16]; // Initialization vector (IV) set to 0s for simplicity

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var writer = new System.IO.StreamWriter(cs))
                        {
                            writer.Write(plainText);
                        }
                    }

                    return System.Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText , string Key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = new byte[16]; // Initialization vector (IV) set to 0s for simplicity

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var ms = new System.IO.MemoryStream(System.Convert.FromBase64String(cipherText)))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var reader = new System.IO.StreamReader(cs))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }
        public static string CalculateMD5Hash(string input)
        {
            var md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            MyLogger.Log("CalculateMD5Hash" +  sb);
            return sb.ToString();
        }
    }
}