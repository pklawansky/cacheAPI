using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.Models
{
    public class AutoPopulateEndpoint
    {
        public string baseURL { get; set; }
        public string endpointMethod { get; set; }
        public string authorization { get; set; }
        public string cacheKey { get; set; }
        public double? cacheLifespanSeconds { get; set; }
        public string endpointAuthorizationKey { get; set; }
        public string endpointAuthorizationValue { get; set; }
    }
}
