using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CacheAPI.BL
{
    public class CacheBL
    {
        private IMemoryCache _cache;
        private IConfiguration _iConfiguration;
        private readonly double _cacheSeconds;
        private static object _memLock = new object();
        private static string _allKeysKey = $"AllKeys_{Guid.NewGuid().ToString()}";

        public CacheBL(IMemoryCache memoryCache, IConfiguration iConfiguration, double? overrideDefaultCacheSeconds = null)
        {
            _cache = memoryCache;
            _iConfiguration = iConfiguration;
            _cacheSeconds = overrideDefaultCacheSeconds.HasValue && overrideDefaultCacheSeconds.Value > 0 ?
                overrideDefaultCacheSeconds.Value :
                new ConfigurationBL(_iConfiguration).GetDefaultCacheExpirationSeconds();

            lock (_memLock)
            {
                if (!_cache.TryGetValue(_allKeysKey, out List<string> keys))
                {
                    _cache.Set(_allKeysKey, new List<string>());
                }
            }
        }

        public object GetFromDictionary(string cacheKey)
        {
            lock (_memLock)
            {
                if (_cache.TryGetValue(cacheKey, out object value))
                {
                    return value;
                }
                else
                {
                    throw new Exception("cacheKey does not have a value");
                }
            }
        }

        public void DeleteFromDictionary(string cacheKey)
        {
            lock (_memLock)
            {
                var allKeys = _cache.Get<List<string>>(_allKeysKey);
                if (_cache.TryGetValue(cacheKey, out Dictionary<string, object> value))
                {
                    _cache.Remove(cacheKey);
                    allKeys.Remove(cacheKey);
                }
                else
                {
                    throw new Exception("cacheKey does not have a value");
                }
            }
            PersistCacheToDrive();
        }

        public void PostToDictionary(string cacheKey, object values)
        {
            lock (_memLock)
            {
                var allKeys = _cache.Get<List<string>>(_allKeysKey);
                if (!allKeys.Contains(cacheKey))
                {
                    allKeys.Add(cacheKey);
                }

                _cache.Set(cacheKey, values, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_cacheSeconds)
                });
            }
            PersistCacheToDrive();
        }

        internal void PersistCacheToDrive()
        {
            var t = new Thread(() =>
            {
                // locking cache only for the get procedure
                var keyValues = new List<object>();
                var allKeys = new List<string>();
                lock (_memLock)
                {
                    var keysToRemove = new List<string>();
                    allKeys = _cache.Get<List<string>>(_allKeysKey);

                    foreach (var key in allKeys)
                    {
                        try
                        {
                            var value = (System.Text.Json.JsonElement)_cache.Get(key);
                            var valueRaw = value.GetRawText();
                            var valueObj = JsonConvert.DeserializeObject<object>(valueRaw);
                            keyValues.Add(new
                            {
                                Key = key,
                                Value = valueObj
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            keysToRemove.Add(key);
                        }
                    }

                    keysToRemove.ForEach(key =>
                    {
                        allKeys.Remove(key);
                    });
                }

                var fileName = new ConfigurationBL(_iConfiguration).GetPersistentDataFileName();
                using (var file = File.Create(fileName))
                {
                    var dataToPrint = JsonConvert.SerializeObject(keyValues);
                    var data = Encoding.ASCII.GetBytes(dataToPrint);
                    file.Write(data, 0, data.Length);
                }
            })
            { 
                IsBackground = false // we want this to finish execution
            };
            t.Start();
        }

        internal object GetCacheFromDrive()
        {
            var fileName = new ConfigurationBL(_iConfiguration).GetPersistentDataFileName();
            using (var file = File.Open(fileName, FileMode.Open))
            {
                var data = new byte[file.Length];
                file.Read(data, 0, Convert.ToInt32(file.Length));
                var dataFromFile = Encoding.ASCII.GetString(data);
                var objToReturn = JsonConvert.DeserializeObject<object[]>(dataFromFile);
                return objToReturn;
            }
        }
    }


}
