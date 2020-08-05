using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.Models
{
    public class CacheEntry
    {
        public CacheEntry() { }
        public CacheEntry(string authorization, string cacheKey, object data, double? expirationSeconds = null, DateTime? expirationDateTime = null)
        {
            this.authorization = authorization;
            this.cacheKey = cacheKey;
            this.data = data;
            expiration = expirationSeconds.HasValue && expirationSeconds.Value > 0 ? DateTime.Now.AddSeconds(expirationSeconds.Value) : (DateTime?)null;
            expiration = expirationDateTime.HasValue ? expirationDateTime.Value : expiration;
            entryDate = DateTime.Now;
        }

        public string authorization { get; set; }
        public string cacheKey { get; set; }
        public DateTime entryDate { get; set; }
        public DateTime? expiration { get; set; }
        public object data { get; set; }

        public CacheEntry NoPayload()
        {
            return new CacheEntry(authorization, cacheKey, null, expirationDateTime: expiration);
        }
    }
}
