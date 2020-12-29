using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;

namespace EnergyMonitor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private int i = 0;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {


            while (!stoppingToken.IsCancellationRequested)
            {
                i++;
                var value = await ReadData();
                await WriteData(value);
                await Task.Delay(100, stoppingToken);
            }
        }

        private async Task<string> ReadData()
        {
            var html = string.Empty;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), "http://192.168.188.28/?action=5"))
                    {
                        var response = await httpClient.SendAsync(request);
                        html = await response.Content.ReadAsStringAsync();
                    }
                }

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var value = string.Empty;

                foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//div[@class='whats']"))
                {
                    value = node.InnerText;
                }

                return value;

            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.Message);
                return string.Empty;
            }
        }
        private async Task WriteData(string i)
        {
            string json = @"{'frames': [
                                {
                                    'text': '   " + i + @"',
                                    'icon': 'a23725'
                                }
                            ]
                        }";


            try
            {
                using (var httpClientHandler = new HttpClientHandler())
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                    using (var httpClient = new HttpClient(httpClientHandler))
                    {
                        using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://192.168.188.23:4343/api/v1/dev/widget/update/com.lametric.03b64c2af6dbf153a170cf36dea278a9/1"))
                        {
                            httpClient.DefaultRequestHeaders
                                    .Accept
                                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            request.Headers.TryAddWithoutValidation("X-Access-Token", "ZWNhNjBkNmZlNmZiM2Q1YjM5OGFmNWQ5MTNlMTIyMzUxODRjZTRlMDY3NTU1ZjlhZTgxYzAwYjA5MDE1NmMwOA==");
                            request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");

                            request.Content = new StringContent(JObject.Parse(json).ToString(), Encoding.UTF8, "application/json");

                            var response = await httpClient.SendAsync(request);

                            if (response.IsSuccessStatusCode)
                                _logger.LogInformation(i);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
