using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CacheAPI.BL
{
    public class CacheBL
    {
        private IMemoryCache _cache;
        private IConfiguration _iConfiguration;
        private readonly double _cacheSeconds;
        private static object _memLock = new object();

        public CacheBL(IMemoryCache memoryCache, IConfiguration iConfiguration, double? overrideDefaultCacheSeconds = null)
        {
            _cache = memoryCache;
            _iConfiguration = iConfiguration;
            _cacheSeconds = overrideDefaultCacheSeconds.HasValue && overrideDefaultCacheSeconds.Value > 0 ? 
                overrideDefaultCacheSeconds.Value : 
                new ConfigurationBL(_iConfiguration).GetDefaultCacheExpirationSeconds();
        }

        public object GetFromDictionary(string cacheKey, string dictionaryKey)
        {
            lock (_memLock)
            {
                if (_cache.TryGetValue(cacheKey, out Dictionary<string, object> value))
                {
                    if (dictionaryKey == null)
                    {
                        return value.ToArray();
                    }
                    else
                    {
                        if (value.TryGetValue(dictionaryKey, out object innerValue))
                        {
                            return innerValue;
                        }
                        else
                        {
                            throw new Exception("dictionaryKey does not have a value");
                        }
                    }
                }
                else
                {
                    throw new Exception("cacheKey does not have a value");
                }
            }
        }

        public void DeleteFromDictionary(string cacheKey, string dictionaryKey)
        {
            lock (_memLock)
            {
                if (_cache.TryGetValue(cacheKey, out Dictionary<string, object> value))
                {
                    if (dictionaryKey == null)
                    {
                        _cache.Remove(cacheKey);
                    }
                    else
                    {
                        if (value.ContainsKey(dictionaryKey))
                        {
                            value.Remove(dictionaryKey);
                        }
                        else
                        {
                            throw new Exception("dictionaryKey does not have a value");
                        }
                    }
                }
                else
                {
                    throw new Exception("cacheKey does not have a value");
                }
            }
        }

        public void PostToDictionary(string cacheKey, List<KeyValuePair<string, object>> values)
        {
            lock (_memLock)
            {
                var dicValues = values.ToDictionary(x => x.Key, x => x.Value);
                _cache.Set(cacheKey, dicValues, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_cacheSeconds)
                });
            }
        }

        public void PutToDictionary(string cacheKey, List<KeyValuePair<string, object>> values)
        {
            lock (_memLock)
            {
                var dicValues = values.ToDictionary(x => x.Key, x => x.Value);
                if (_cache.TryGetValue(cacheKey, out Dictionary<string, object> cacheValues))
                {
                    foreach (var key in dicValues.Keys)
                    {
                        if (cacheValues.ContainsKey(key))
                        {
                            cacheValues[key] = dicValues[key];
                        }
                        else
                        {
                            cacheValues.Add(key, dicValues[key]);
                        }
                    }
                }
                else
                {
                    throw new Exception("cacheKey does not have a value");
                }
            }
        }
    }
}
