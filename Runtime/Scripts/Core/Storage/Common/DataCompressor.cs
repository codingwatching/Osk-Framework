using System.IO;
using System.IO.Compression;

namespace OSK
{
    public static class DataCompressor
    {
        public static byte[] Compress(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            if (!IsCompressed(data)) return data;

            using (var ms = new MemoryStream(data))
            using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
            using (var outStream = new MemoryStream())
            {
                gzip.CopyTo(outStream);
                return outStream.ToArray();
            }
        }

        public static bool IsCompressed(byte[] data)
        {
            return data != null && data.Length >= 2 && data[0] == 0x1F && data[1] == 0x8B;
        }
    }
}
