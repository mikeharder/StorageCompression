using Azure.Core;
using Azure.Core.Pipeline;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StorageCompression
{
    public static class PerfStressTransport
    {
        public static HttpPipelineTransport Create(bool insecure, string host, int port)
        {
            HttpClient httpClient;
            if (insecure)
            {
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                httpClient = new HttpClient(httpClientHandler);
            }
            else
            {
                httpClient = new HttpClient();
            }

            var httpClientTransport = new HttpClientTransport(httpClient);

            if (!string.IsNullOrEmpty(host))
            {
                return new ChangeUriTransport(httpClientTransport, host, port);
            }
            else
            {
                return httpClientTransport;
            }
        }

        private class ChangeUriTransport : HttpPipelineTransport
        {
            private readonly HttpPipelineTransport _transport;
            private readonly string _host;
            private readonly int? _port;

            public ChangeUriTransport(HttpPipelineTransport transport, string host, int? port)
            {
                _transport = transport;
                _host = host;
                _port = port;
            }

            public override Request CreateRequest()
            {
                return _transport.CreateRequest();
            }

            public override void Process(HttpMessage message)
            {
                ChangeUri(message);
                _transport.Process(message);
            }

            public override ValueTask ProcessAsync(HttpMessage message)
            {
                ChangeUri(message);
                return _transport.ProcessAsync(message);
            }

            private void ChangeUri(HttpMessage message)
            {
                // Ensure Host header is only set once, since the same HttpMessage will be reused on retries
                if (!message.Request.Headers.Contains("Host"))
                {
                    message.Request.Headers.Add("Host", message.Request.Uri.Host);
                }

                message.Request.Uri.Host = _host;
                if (_port.HasValue)
                {
                    message.Request.Uri.Port = _port.Value;
                }
            }
        }
    }
}
