using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.Models
{
    public class CacheEntry
    {
        public CacheEntry(string authorization, string cacheKey, object data, double? expirationSeconds = null, DateTime? expirationDateTime = null)
        {
            Authorization = authorization;
            CacheKey = cacheKey;
            Data = data;
            ExpirationDateTime = expirationSeconds.HasValue && expirationSeconds.Value > 0 ? DateTime.Now.AddSeconds(expirationSeconds.Value) : (DateTime?)null;
            ExpirationDateTime = expirationDateTime.HasValue ? expirationDateTime.Value : ExpirationDateTime;
        }

        public string Authorization { get; set; }
        public string CacheKey { get; set; }
        public DateTime? ExpirationDateTime { get; set; }
        public object Data { get; set; }

        public CacheEntry NoPayload()
        {
            return new CacheEntry(Authorization, CacheKey, null, expirationDateTime: ExpirationDateTime);
        }
    }
}
