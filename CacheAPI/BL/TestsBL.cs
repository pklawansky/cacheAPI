using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using CacheAPI.Models;

namespace CacheAPI.BL
{
    public class TestsBL
    {
        #region Tests

        public static CacheEntry TestGet(out bool success, string cacheKey, string authorization = null)
        {
            using (var client = new HttpClient())
            {
                if (!string.IsNullOrWhiteSpace(authorization))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authorization);
                }
                var response = client.GetAsync($"https://localhost:44363/api/Cache?cacheKey={cacheKey}").GetAwaiter().GetResult();
                success = response.IsSuccessStatusCode;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var value = JsonSerializer.Deserialize<CacheEntry>(data);

                    Console.WriteLine(value);
                    return value;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }

            }
        }

        public static string TestPost(out bool success, string key, double? cacheSeconds = null)
        {
            using (var client = new HttpClient())
            {
                var stubData = new List<object>();
                for (double i = 0; i < 40000; i++)
                {
                    stubData.Add(new
                    {
                        id = i,
                        message = "testing a very long string that takes up a lot of space, the point of this is to see how much data this caching can handle",
                        error = "no error that we know of right now",
                        remarks = "hopefully this works nicely",
                        number = 33.4 / (i + 0.1),
                        active = true,
                        sub_obj = new
                        {
                            inner_id = -i,
                            name = "test inner object"
                        }
                    });
                }

                string json = JsonSerializer.Serialize(stubData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = client.PostAsync($"https://localhost:44363/api/Cache?cacheKey={key}&cacheSeconds={cacheSeconds}", content).GetAwaiter().GetResult();
                success = response.IsSuccessStatusCode;
                if (response.IsSuccessStatusCode)
                {
                    //var data = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    //Console.WriteLine(data);
                    return "Successfully posted";
                }
                else
                {
                    return response.ReasonPhrase;
                }

            }
        }

        public static CacheEntry TestGetA()
        {
            var getResult = TestGet(out bool getSuccess, "a");

            if (getSuccess)
            {
                return getResult;
            }
            var postResult = TestPost(out bool postSuccess, "a", cacheSeconds: 1);
            if (postSuccess)
            {
                getResult = TestGet(out getSuccess, "a");
                return getResult;
            }
            else
            {
                return getResult;
            }
        }

        #endregion
    }
}
