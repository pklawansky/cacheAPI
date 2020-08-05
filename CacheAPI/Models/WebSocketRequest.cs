using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.Models
{
    public class WebSocketRequest
    {
        public string method { get; set; }
        public string cacheKey { get; set; }
        public string authorization { get; set; }
        public double? cacheLifespanSeconds { get; set; }
        public AutoPopulateEndpoint autoPopulateEndpoint { get; set; }
        public object values { get; set; }
    }
}
