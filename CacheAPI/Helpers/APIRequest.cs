using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CacheAPI.Helpers
{
    public class APIRequest
    {
        #region Initialization

        public APIRequest(string baseURL)
        {
            BaseURL = baseURL;
        }

        #endregion

        #region Parameters

        public string BaseURL { get; }
        private Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(new List<KeyValuePair<string, string>>
        {
            //new KeyValuePair<string, string>("Accept", "application/json"),
            //new KeyValuePair<string, string>("Accept-Encoding", "gzip, deflate, br"),
            //new KeyValuePair<string, string>("Content-Type", "application/json")
        });

        #endregion

        #region Private Methods

        private string GetQueryString(List<KeyValuePair<string, string>> parameters)
        {
            if (parameters?.Any() ?? false == false) return string.Empty;

            StringBuilder queryString = new StringBuilder("?");
            var first = true;
            parameters.ForEach(parameter =>
            {
                queryString.Append($"{(!first ? "&" : "")}{parameter.Key}={HttpUtility.UrlEncode(parameter.Value ?? "")}");
                first = false;
            });

            return queryString.ToString();
        }

        #endregion

        #region Public Methods

        public void AddHeader(string key, string value)
        {
            if (Headers.ContainsKey(key))
            {
                Headers[key] = value;
            }
            else
            {
                Headers.Add(key, value);
            }
        }

        public T Get<T>(string endpoint, List<KeyValuePair<string, string>> parameters = null)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BaseURL);
                var queryString = GetQueryString(parameters);
                foreach (var key in Headers.Keys)
                {
                    client.DefaultRequestHeaders.Add(key, Headers[key]);
                }

                var response = client.GetAsync(endpoint + queryString).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return JsonSerializer.Deserialize<T>(responseString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    throw new Exception($"{response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }

        #endregion
    }
}
