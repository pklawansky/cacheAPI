using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.Models
{
    public class WebSocketResponse
    {
        public CacheEntry cacheEntry { get; set; }
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
    }
}
