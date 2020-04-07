using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StorageCompressionV11
{
    class Program
    {
        private const string _text = "hello hello hello hello hello hello hello hello hello hello hello hello hello hello hello";
        private static readonly byte[] _bytes = Encoding.UTF8.GetBytes(_text);

        static async Task Main(string[] args)
        {
            HttpClient.DefaultProxy = new WebProxy("http://localhost:8888");

            var connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING") ??
                throw new InvalidOperationException("Undefined environment variable STORAGE_CONNECTION_STRING");

            CloudStorageAccount.TryParse(connectionString, out var account);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference("test");


            var uncompressedBlob = container.GetBlockBlobReference("uncompressed");

            await uncompressedBlob.UploadTextAsync(_text);

            var text = await uncompressedBlob.DownloadTextAsync();
            Console.WriteLine(text);


            var compressedBlob = container.GetBlockBlobReference("compressed");
            var compressedBytes = Compress(_text);
            await compressedBlob.UploadFromByteArrayAsync(compressedBytes, 0, compressedBytes.Length);

            var ms = new MemoryStream();
            await compressedBlob.DownloadToStreamAsync(ms);
            var decompressedText = Decompress(ms.ToArray());
            Console.WriteLine(decompressedText);

            var compressedHeadersBlob = container.GetBlockBlobReference("compressedheaders");

            await compressedHeadersBlob.UploadFromByteArrayAsync(compressedBytes, 0, compressedBytes.Length,
                AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions(),
                new OperationContext()
                {
                    UserHeaders = new Dictionary<string, string>
                    {
                        { "x-ms-blob-content-encoding", "gzip" },
                        { "x-ms-blob-content-type", "text/plain; charset=UTF-8" },
                    }
                });

            var ms2 = new MemoryStream();
            await compressedHeadersBlob.DownloadToStreamAsync(ms2);
            var decompressedText2 = Decompress(ms2.ToArray());
            Console.WriteLine(decompressedText2);
        }

        private static byte[] Compress(string s)
        {
            using var ms = new MemoryStream();
            using var gs = new GZipStream(ms, CompressionMode.Compress);
            using var sw = new StreamWriter(gs);
            sw.Write(s);
            sw.Flush();
            return ms.ToArray();
        }

        private static string Decompress(byte[] b)
        {
            using var ms = new MemoryStream(b);
            using var gs = new GZipStream(ms, CompressionMode.Decompress);
            using var sr = new StreamReader(gs);
            return sr.ReadToEnd();
        }
    }
}
