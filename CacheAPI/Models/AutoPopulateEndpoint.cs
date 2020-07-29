using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.Models
{
    public class AutoPopulateEndpoint
    {
        public string Url { get; set; }
        public string Authorization { get; set; }
        public string CacheKey { get; set; }
        public double? CacheSeconds { get; set; }
        public string EndpointAuthorizationKey { get; set; }
        public string EndpointAuthorizationValue { get; set; }
    }
}
