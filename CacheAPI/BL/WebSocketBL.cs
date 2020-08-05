using CacheAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.BL
{
    public class WebSocketBL
    {
        public async static Task<WebSocketResponse> ProcessRequest(WebSocketRequest request, IMemoryCache memoryCache)
        {
            var bl = new CacheBL(memoryCache, request.authorization, request.cacheKey, request.cacheLifespanSeconds);
            var response = new WebSocketResponse
            {
                cacheEntry = null,
                responseCode = "OK",
                responseMessage = ""
            };

            switch (request.method)
            {
                case "GET":
                    response.cacheEntry = await bl.GetFromDictionary(request.autoPopulateEndpoint);
                    break;
                case "POST":
                    bl.PostToDictionary(request.values);
                    break;
                case "DELETE":
                    bl.DeleteFromDictionary();
                    break;
            }

            return response;
        }
    }
}
