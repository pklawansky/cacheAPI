using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CacheAPI.BL;
using CacheAPI.Helpers;
using CacheAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CacheAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TestController : BaseController
    {
        #region Initialization

        public TestController(IMemoryCache memoryCache) : base(memoryCache)
        {
        }

        #endregion

        #region Endpoints

        public string Index()
        {
            return "Welcome to caching api";
        }

        [Route("Test")]
        public IActionResult Test()
        {
            var result = TestsBL.TestPost(out bool success, "a");
            result = TestsBL.TestPost(out success, "b");
            return Ok(new { result });
        }

        [Route("LoadTestSync")]
        public IActionResult LoadTestSync(int requestCount, string cacheKey, string authorization)
        {
            bool success = true;
            CacheEntry result = null;
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < requestCount; i++)
            {
                result = TestsBL.TestGet(out success, cacheKey, authorization);
            }
            watch.Stop();
            return Ok(new
            {
                message = $"{requestCount} requests in {watch.ElapsedMilliseconds}ms",
                result
            });
        }

        [Route("LoadTestAsync")]
        public IActionResult LoadTestAsync(int requestCount, string cacheKey, string authorization)
        {
            bool success = true;
            CacheEntry result = null;
            var completed = 0;

            var watch = Stopwatch.StartNew();
            for (int i = 0; i < requestCount; i++)
            {
                var t = new Thread(() =>
                {
                    result = TestsBL.TestGet(out success, cacheKey, authorization);
                    completed++;
                    if (completed == requestCount)
                    {
                        watch.Stop();
                    }
                });
                t.Start();
            }

            while (completed < requestCount)
            {
                Thread.Sleep(10);
            }

            return Ok(new
            {
                message = $"{requestCount} requests in {watch.ElapsedMilliseconds}ms",
                result
            });
        }

        [Route("TestSockets")]
        public IActionResult TestSockets()
        {
            int requestCount = 1000;
            WebSocketResponse response = null;
            Stopwatch watch = new Stopwatch();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress remoteIPAddress = ipHostInfo.AddressList[0];
            int remotePort = GlobalSettings.WebSocketPort;
            
            watch.Start();
            for (int i = 0; i < requestCount; i++)
            {
                response = SynchronousSocketClient.StartClient(new WebSocketRequest
                {
                    authorization = "Testing",
                    cacheKey = "ComplexData",
                    method = "GET"
                }, remoteIPAddress, remotePort);
            }
            watch.Stop();

            return Ok($"{requestCount} requests done in {watch.ElapsedMilliseconds}ms {JsonSerializer.Serialize(response)}");
        }

        [Route("TestAPI")]
        public IActionResult TestAPI()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 1000; i++)
            {
                using (HttpClient client = new HttpClient())
                {
                    client.GetAsync($"https://localhost:44363/Test/APIEndpoint?payload={HttpUtility.UrlEncode("This is a test")}").GetAwaiter().GetResult();
                }
            }
            watch.Stop();

            return Ok($"Done in {watch.ElapsedMilliseconds}ms");
        }

        [Route("APIEndpoint")]
        public IActionResult APIEndpoint(string payload)
        {
            return Ok(payload);
        }

        #endregion

        #region Stubbed Data

        [Route("LoadAlphabet")]
        public IActionResult LoadAlphabet()
        {
            var result = new AutoPopulateResult
            {
                CacheSecondsOverride = 30,
                Data = new string[]
                {
                    "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"
                }
            };

            return Ok(result);
        }

        [Route("LoadComplexData")]
        public IActionResult LoadComplexData()
        {
            var result = new AutoPopulateResult
            {
                CacheSecondsOverride = 30,
                Data = new
                {
                    Name = "Alphabet",
                    Description = "This lists the alphabet",
                    Alphabet = new string[]
                    {
                        "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"
                    }
                }
            };

            return Ok(result);
        }

        #endregion
    }
}