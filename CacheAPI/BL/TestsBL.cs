using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CacheAPI.BL
{
    public class TestsBL
    {
        public static string TestGet(out bool success, string dictionaryKey = null)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync($"https://localhost:44363/api/Cache?cacheKey=a&dictionaryKey={dictionaryKey}").GetAwaiter().GetResult();
                success = response.IsSuccessStatusCode;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    Console.WriteLine(data);
                    return data;
                }
                else
                {
                    return response.ReasonPhrase;
                }

            }
        }

        public static string TestPost(out bool success, double? cacheSeconds = null)
        {
            using (var client = new HttpClient())
            {
                var stubData = new List<KeyValuePair<string, object>>
                {
                    new KeyValuePair<string, object>("a", "testing A"),
                    new KeyValuePair<string, object>("b", "testing B"),
                    new KeyValuePair<string, object>("c", "testing C"),
                };
                string json = JsonConvert.SerializeObject(stubData);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = client.PostAsync($"https://localhost:44363/api/Cache?cacheKey=a&cacheSeconds={cacheSeconds}", content).GetAwaiter().GetResult();
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

        public static string TestGetA()
        {
            var getResult = TestGet(out bool getSuccess, dictionaryKey: "a");
            return getResult;
            if (getSuccess)
            {
                return getResult;
            }
            var postResult = TestPost(out bool postSuccess, cacheSeconds: 1);
            if (postSuccess)
            {
                getResult = TestGet(out getSuccess, dictionaryKey: "a");
                return getResult;
            }
            else
            {
                return postResult;
            }
        }
    }
}
