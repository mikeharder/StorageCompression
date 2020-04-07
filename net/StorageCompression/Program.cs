using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StorageCompression
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // HttpClient.DefaultProxy = new WebProxy("http://localhost:8888");

            var connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING") ??
                throw new InvalidOperationException("Undefined environment variable STORAGE_CONNECTION_STRING");


            var serviceClient = new BlobServiceClient(connectionString, new BlobClientOptions
            {
                Transport = PerfStressTransport.Create(insecure: true, "localhost", 7778),
                Retry =
                {
                    MaxRetries = 1,
                    NetworkTimeout = TimeSpan.FromSeconds(10)
                }
            });

            var containerClient = serviceClient.GetBlobContainerClient("test");

            //var uncompressedBlobClient = containerClient.GetBlobClient("uncompressed");
            //var ms = new MemoryStream();
            //await uncompressedBlobClient.DownloadToAsync(ms);
            //var text = Encoding.UTF8.GetString(ms.ToArray());
            //Console.WriteLine(text);

            //var compressedBlobClient = containerClient.GetBlobClient("compressed");
            //var ms2 = new MemoryStream();
            //await compressedBlobClient.DownloadToAsync(ms2);
            //var text2 = Decompress(ms2.ToArray());
            //Console.WriteLine(text2);

            var compressedHeadersBlobClient = containerClient.GetBlobClient("compressedheaders");
            var ms3 = new MemoryStream();
            await compressedHeadersBlobClient.DownloadToAsync(ms3);
            var text3 = Decompress(ms3.ToArray());
            Console.WriteLine(text3);
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
