using CacheAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        private readonly double CacheSeconds;
        private static object MemLock = new object();
        private static string AllKeysKey = $"AllKeys_{Guid.NewGuid().ToString()}";

        #endregion

        #region Initialization

        public CacheBL(IMemoryCache memoryCache, IConfiguration configuration, string authorization, double? overrideDefaultCacheSeconds = null)
        {
            Authorization = authorization;
            MemoryCache = memoryCache;
            Configuration = configuration;
            CacheSeconds = overrideDefaultCacheSeconds.HasValue && overrideDefaultCacheSeconds.Value >= 0 ?
                overrideDefaultCacheSeconds.Value :
                new ConfigurationBL(Configuration).DefaultCacheExpirationSeconds;

            lock (MemLock)
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
            allKeys = allKeys.Where(x => !x.ExpirationDateTime.HasValue || x.ExpirationDateTime.Value < DateTime.Now).ToList();

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
        }

        private void AddKeyToAllKeys(List<CacheEntry> allKeys, CacheEntry entry)
        {
            RemoveKeyFromAllKeys(allKeys, entry.CacheKey);
            allKeys.Add(entry.NoPayload());
        }

        private string GetAuthKey(string cacheKey, string authorization = null)
        {
            return $"{(authorization ?? Authorization)} {cacheKey}";
        }

        #endregion

        #region Public Methods

        public List<CacheEntry> ListFromDictionary()
        {
            lock (MemLock)
            {
                return GetAllKeys(authorization: Authorization);
            }
        }

        public CacheEntry GetFromDictionary(string cacheKey)
        {
            lock (MemLock)
            {
                if (MemoryCache.TryGetValue(GetAuthKey(cacheKey), out CacheEntry value))
                {
                    return value;
                }
                else
                {
                    return PopulateFromEndpoint(cacheKey);
                }
            }
        }

        public void DeleteFromDictionary(string cacheKey)
        {
            lock (MemLock)
            {
                var allKeys = GetAllKeys();
                if (MemoryCache.TryGetValue(GetAuthKey(cacheKey), out CacheEntry value))
                {
                    MemoryCache.Remove(GetAuthKey(cacheKey));
                    RemoveKeyFromAllKeys(allKeys, cacheKey);
                }
                else
                {
                    throw new Exception("cacheKey does not have a value");
                }
            }
            PersistCacheToFile();
        }

        public void PostToDictionary(string cacheKey, object values)
        {
            lock (MemLock)
            {
                var allKeys = GetAllKeys();

                var entry = new CacheEntry(Authorization, cacheKey, values, expirationSeconds: CacheSeconds);
                AddKeyToAllKeys(allKeys, entry);

                MemoryCache.Set(GetAuthKey(cacheKey), entry, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheSeconds > 0 ? TimeSpan.FromSeconds(CacheSeconds) : (TimeSpan?)null
                });
            }
            PersistCacheToFile();
        }

        #endregion

        #region Private Methods

        private CacheEntry PopulateFromEndpoint(string cacheKey)
        {
            var endpoint = new ConfigurationBL(Configuration).AutoPopulateEndpoints.FirstOrDefault(x => x.Authorization == Authorization && x.CacheKey == cacheKey);

            // TODO: make api call to configured endpoint if it exists, expect AutoPopulateResult back


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
                lock (MemLock)
                {
                    allKeys = GetAllKeys();

                    foreach (var key in allKeys)
                    {
                        try
                        {
                            var authKey = GetAuthKey(key.CacheKey, authorization: key.Authorization);
                            var entry = MemoryCache.Get<CacheEntry>(authKey);
                            var dataRaw = ((JsonElement)entry.Data).GetRawText();
                            var dataObj = JsonConvert.DeserializeObject<object>(dataRaw);
                            keyEntries.Add(new
                            {
                                Key = authKey,
                                Entry = new CacheEntry(entry.Authorization, entry.CacheKey, dataObj, expirationDateTime: entry.ExpirationDateTime)
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
                    var dataToPrint = JsonConvert.SerializeObject(keyEntries);
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
                var objToReturn = JsonConvert.DeserializeObject<object[]>(dataFromFile);
                return objToReturn;
            }
        }

        #endregion
    }
}
