using CacheAPI.Helpers;
using CacheAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace CacheAPI.BL
{
    public class CacheBL
    {
        #region Props

        private readonly IMemoryCache MemoryCache;
        private readonly IConfiguration Configuration;
        private readonly string Authorization;
        private double CacheSeconds;
        private static Dictionary<string, object> MemLocks = new Dictionary<string, object>();
        private static object GenericMemLock = new object();
        private static string AllKeysKey = $"AllKeys_{Guid.NewGuid().ToString()}";
        private string CacheKey;

        #endregion

        #region Initialization

        public CacheBL(IMemoryCache memoryCache, IConfiguration configuration, string authorization, string cacheKey, double? overrideDefaultCacheSeconds = null)
        {
            Authorization = authorization;
            MemoryCache = memoryCache;
            Configuration = configuration;
            CacheSeconds = overrideDefaultCacheSeconds.HasValue && overrideDefaultCacheSeconds.Value >= 0 ?
                overrideDefaultCacheSeconds.Value :
                new ConfigurationBL(Configuration).DefaultCacheExpirationSeconds;
            CacheKey = cacheKey;

            lock (GenericMemLock)
            {
                if (!MemoryCache.TryGetValue(AllKeysKey, out List<CacheEntry> keys))
                {
                    MemoryCache.Set(AllKeysKey, new List<CacheEntry>());
                }
            }
        }

        #endregion

        #region Helper Methods

        private List<CacheEntry> GetAllKeys(string authorization = null)
        {
            var allKeys = MemoryCache.Get<List<CacheEntry>>(AllKeysKey);
            allKeys = allKeys.Where(x => !x.Expiration.HasValue || x.Expiration.Value > DateTime.Now).ToList();

            if (authorization == null)
            {
                return allKeys;
            }
            else
            {
                return allKeys.Where(x => x.Authorization == authorization).ToList();
            }
        }

        private void RemoveKeyFromAllKeys(List<CacheEntry> allKeys, string cacheKey)
        {
            var entry = allKeys.FirstOrDefault(x => x.CacheKey == cacheKey && x.Authorization == Authorization);
            if (entry != null)
            {
                allKeys.Remove(entry);
            }
            MemoryCache.Set(AllKeysKey, allKeys);
        }

        private void AddKeyToAllKeys(List<CacheEntry> allKeys, CacheEntry entry)
        {
            RemoveKeyFromAllKeys(allKeys, entry.CacheKey);
            allKeys.Add(entry.NoPayload());
            MemoryCache.Set(AllKeysKey, allKeys);
        }

        private string GetAuthKey(string cacheKey, string authorization = null)
        {
            return $"{(authorization ?? Authorization)} {cacheKey}";
        }

        private object GetMemlock()
        {
            var authKey = GetAuthKey(CacheKey);
            if (!MemLocks.TryGetValue(authKey, out object memlock))
            {
                memlock = new object();
                MemLocks.Add(authKey, memlock);
            }
            return memlock;
        }

        #endregion

        #region Public Methods

        public List<CacheEntry> ListFromDictionary()
        {
            lock (GenericMemLock)
            {
                return GetAllKeys(authorization: Authorization);
            }
        }

        public CacheEntry GetFromDictionary()
        {
            lock (GetMemlock())
            {
                if (MemoryCache.TryGetValue(GetAuthKey(CacheKey), out CacheEntry value))
                {
                    return value;
                }
            }

            return PopulateFromEndpoint();
        }

        public void DeleteFromDictionary()
        {
            lock (GetMemlock())
            {

                if (MemoryCache.TryGetValue(GetAuthKey(CacheKey), out CacheEntry value))
                {
                    MemoryCache.Remove(GetAuthKey(CacheKey));
                    lock (GenericMemLock)
                    {
                        var allKeys = GetAllKeys();
                        RemoveKeyFromAllKeys(allKeys, CacheKey);
                    }
                }
            }
            PersistCacheToFile();
        }

        public void PostToDictionary(object values)
        {
            lock (GetMemlock())
            {
                var entry = new CacheEntry(Authorization, CacheKey, values, expirationSeconds: CacheSeconds);

                lock (GenericMemLock)
                {
                    var allKeys = GetAllKeys();
                    AddKeyToAllKeys(allKeys, entry);
                }

                MemoryCache.Set(GetAuthKey(CacheKey), entry, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheSeconds > 0 ? TimeSpan.FromSeconds(CacheSeconds) : (TimeSpan?)null
                });
            }
            PersistCacheToFile();
        }

        #endregion

        #region Private Methods

        private CacheEntry PopulateFromEndpoint()
        {
            // TODO: get the endpoint from request headers in case it is a specialized querystring for this specific CacheKey

            //// DO request header stuff, which overrides configuration


            // Otherwise use configuration here

            var endpoint = new ConfigurationBL(Configuration).AutoPopulateEndpoints.FirstOrDefault(x => x.Authorization == Authorization && x.CacheKey == CacheKey);

            // TODO: make api call to configured endpoint if it exists, expect AutoPopulateResult back
            if (endpoint != null)
            {
                var request = new APIRequest(endpoint.BaseURL);
                if (!string.IsNullOrWhiteSpace(endpoint.EndpointAuthorizationKey))
                {
                    request.AddHeader(endpoint.EndpointAuthorizationKey, endpoint.EndpointAuthorizationValue);
                }

                var result = request.Get<AutoPopulateResult>(endpoint.EndpointMethod);

                if (result.CacheSecondsOverride.HasValue && result.CacheSecondsOverride.Value >= 0)
                {
                    CacheSeconds = result.CacheSecondsOverride.Value;
                }
                else if (endpoint.CacheSeconds.HasValue && endpoint.CacheSeconds.Value >= 0)
                {
                    CacheSeconds = endpoint.CacheSeconds.Value;
                }

                object dataObj = result.Data;
                if (result.Data is JArray)
                {
                    dataObj = ((JArray)result.Data).ToObject<object[]>();
                }
                else if (result.Data is JsonElement)
                {
                    var dataRaw = ((JsonElement)result.Data).GetRawText();
                    dataObj = JsonSerializer.Deserialize<object>(dataRaw, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else if (result.Data is JObject)
                {
                    var dataRaw = ((JObject)result.Data).ToString();
                    dataObj = JsonSerializer.Deserialize<object>(dataRaw, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                PostToDictionary(dataObj);

                return GetFromDictionary();
            }

            throw new Exception("cacheKey does not have a value");
        }

        private void PersistCacheToFile()
        {
            if (!new ConfigurationBL(Configuration).PersistCacheToFile) return;

            var t = new Thread(() =>
            {
                // locking cache only for the get procedure
                var keyEntries = new List<object>();
                var allKeys = new List<CacheEntry>();
                lock (GenericMemLock)
                {
                    allKeys = GetAllKeys();

                    foreach (var key in allKeys)
                    {
                        try
                        {
                            var authKey = GetAuthKey(key.CacheKey, authorization: key.Authorization);
                            var entry = MemoryCache.Get<CacheEntry>(authKey);
                            var dataRaw = ((JsonElement)entry.Data).GetRawText();
                            var dataObj = JsonSerializer.Deserialize<object>(dataRaw, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            keyEntries.Add(new
                            {
                                Key = authKey,
                                Entry = new CacheEntry(entry.Authorization, entry.CacheKey, dataObj, expirationDateTime: entry.Expiration)
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }

                var fileName = new ConfigurationBL(Configuration).PersistentDataFileName;
                using (var file = File.Create(fileName))
                {
                    var dataToPrint = JsonSerializer.Serialize(keyEntries, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    var data = Encoding.ASCII.GetBytes(dataToPrint);
                    file.Write(data, 0, data.Length);
                }
            })
            {
                IsBackground = false // we want this to finish execution
            };
            t.Start();
        }

        private object GetCacheFromDrive()
        {
            var fileName = new ConfigurationBL(Configuration).PersistentDataFileName;
            using (var file = File.Open(fileName, FileMode.Open))
            {
                var data = new byte[file.Length];
                file.Read(data, 0, Convert.ToInt32(file.Length));
                var dataFromFile = Encoding.ASCII.GetString(data);
                var objToReturn = JsonSerializer.Deserialize<object[]>(dataFromFile, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return objToReturn;
            }
        }

        #endregion
    }
}
