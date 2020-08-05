using CacheAPI.Helpers;
using CacheAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CacheAPI.BL
{
    public class CacheBL
    {
        #region Props

        private readonly IMemoryCache MemoryCache;
        private readonly string Authorization;
        private double CacheSeconds;
        private static ConcurrentDictionary<string, ManualResetEvent> ManualResetEvents = new ConcurrentDictionary<string, ManualResetEvent>();
        private static ConcurrentDictionary<string, SemaphoreSlim> Semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        private static object GenericMemLock = new object();
        private static string AllKeysKey = $"AllKeys_59c0da49-d66c-11ea-93f5-2c56dcd69f8b";
        private static string CacheSettingsKey = $"CacheSettings_6dff9c86-d66c-11ea-93f5-2c56dcd69f8b";
        private string CacheKey;
        private int SemaphoreInitial;

        #endregion

        #region Initialization

        public CacheBL(IMemoryCache memoryCache, string authorization, string cacheKey, double? overrideDefaultCacheSeconds = null)
        {
            Authorization = authorization;
            MemoryCache = memoryCache;
            CacheKey = cacheKey;

            if (!MemoryCache.TryGetValue(CacheSettingsKey, out Dictionary<string, string> settings))
            {
                settings = new Dictionary<string, string>();
                settings.Add("SemaphoreInitial", GlobalSettings.SemaphoreInitial.ToString());
                settings.Add("CacheSeconds", GlobalSettings.DefaultCacheExpirationSeconds.ToString());
                MemoryCache.Set(CacheSettingsKey, settings);
            }

            SemaphoreInitial = int.Parse(settings["SemaphoreInitial"]);
            CacheSeconds = overrideDefaultCacheSeconds.HasValue && overrideDefaultCacheSeconds.Value >= 0 ?
                overrideDefaultCacheSeconds.Value :
                double.Parse(settings["CacheSeconds"]);

            if (!MemoryCache.TryGetValue(AllKeysKey, out List<CacheEntry> keys))
            {
                MemoryCache.Set(AllKeysKey, new List<CacheEntry>());
            }
        }

        #endregion

        #region Helper Methods

        private List<CacheEntry> GetAllKeys(string authorization = null)
        {
            var allKeys = MemoryCache.Get<List<CacheEntry>>(AllKeysKey);
            allKeys = allKeys.Where(x => !x.expiration.HasValue || x.expiration.Value > DateTime.Now).ToList();

            if (authorization == null)
            {
                return allKeys;
            }
            else
            {
                return allKeys.Where(x => x.authorization == authorization).ToList();
            }
        }

        private void RemoveKeyFromAllKeys(List<CacheEntry> allKeys, string cacheKey)
        {
            var entry = allKeys.FirstOrDefault(x => x.cacheKey == cacheKey && x.authorization == Authorization);
            if (entry != null)
            {
                allKeys.Remove(entry);
            }
            MemoryCache.Set(AllKeysKey, allKeys);
        }

        private void AddKeyToAllKeys(List<CacheEntry> allKeys, CacheEntry entry)
        {
            RemoveKeyFromAllKeys(allKeys, entry.cacheKey);
            allKeys.Add(entry.NoPayload());
            MemoryCache.Set(AllKeysKey, allKeys);
        }

        private string GetAuthKey(string cacheKey, string authorization = null)
        {
            return $"{(authorization ?? Authorization)} {cacheKey}";
        }


        private SemaphoreSlim GetSemaphoreSlim()
        {
            var authKey = GetAuthKey(CacheKey);
            return Semaphores.GetOrAdd(authKey, x => new SemaphoreSlim(SemaphoreInitial));
        }

        private ManualResetEvent GetManualResetEvent()
        {
            var authKey = GetAuthKey(CacheKey);
            return ManualResetEvents.GetOrAdd(authKey, x => new ManualResetEvent(true));
        }

        #endregion

        #region Public Methods

        //[Obsolete("Not intended for general use")]
        //public List<CacheEntry> ListFromDictionary()
        //{
        //    lock (GenericMemLock)
        //    {
        //        return GetAllKeys(authorization: Authorization);
        //    }
        //}

        public async Task<CacheEntry> GetFromDictionary(AutoPopulateEndpoint endpoint = null)
        {
            var myMRE = GetManualResetEvent();
            myMRE.WaitOne();

            var mySemaphore = GetSemaphoreSlim();
            
            try
            {
                await mySemaphore.WaitAsync();

                if (!MemoryCache.TryGetValue(GetAuthKey(CacheKey), out CacheEntry value))
                {
                    return PopulateFromEndpoint(endpoint);
                }
                return value;
            }
            finally
            {
                mySemaphore.Release();
            }
        }

        public void DeleteFromDictionary()
        {
            var myMRE = GetManualResetEvent();

            try
            {
                myMRE.WaitOne();
                myMRE.Reset();

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
            finally
            {
                myMRE.Set();
            }

            PersistCacheToFile();
        }

        public void PostToDictionary(object values)
        {
            var myMRE = GetManualResetEvent();

            try
            {
                myMRE.WaitOne();
                myMRE.Reset();

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
            finally
            {
                myMRE.Set();
            }

            PersistCacheToFile();
        }

        #endregion

        #region Private Methods

        private CacheEntry PopulateFromEndpoint(AutoPopulateEndpoint endpoint = null)
        {
            var myMRE = GetManualResetEvent();
            
            try
            {
                myMRE.WaitOne();
                myMRE.Reset();

                endpoint = endpoint ?? GlobalSettings.AutoPopulateEndpoints.FirstOrDefault(x => x.authorization == Authorization && x.cacheKey == CacheKey);

                if (endpoint != null)
                {
                    var request = new APIRequest(endpoint.baseURL);
                    if (!string.IsNullOrWhiteSpace(endpoint.endpointAuthorizationKey))
                    {
                        request.AddHeader(endpoint.endpointAuthorizationKey, endpoint.endpointAuthorizationValue);
                    }

                    var result = request.Get<AutoPopulateResult>(endpoint.endpointMethod);

                    if (result.CacheSecondsOverride.HasValue && result.CacheSecondsOverride.Value >= 0)
                    {
                        CacheSeconds = result.CacheSecondsOverride.Value;
                    }
                    else if (endpoint.cacheLifespanSeconds.HasValue && endpoint.cacheLifespanSeconds.Value >= 0)
                    {
                        CacheSeconds = endpoint.cacheLifespanSeconds.Value;
                    }

                    object dataObj = GetDataObject(result.Data);

                    var entry = new CacheEntry(Authorization, CacheKey, dataObj, expirationSeconds: CacheSeconds);

                    lock (GenericMemLock)
                    {
                        var allKeys = GetAllKeys();
                        AddKeyToAllKeys(allKeys, entry);
                    }

                    MemoryCache.Set(GetAuthKey(CacheKey), entry, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = CacheSeconds > 0 ? TimeSpan.FromSeconds(CacheSeconds) : (TimeSpan?)null
                    });

                    return entry;
                }

                throw new Exception("cacheKey does not have a value");
            }
            finally
            {
                myMRE.Set();
            }
        }

        private object GetDataObject(object data)
        {
            object dataObj = data;
            if (dataObj is JArray)
            {
                dataObj = ((JArray)dataObj).ToObject<object[]>();
            }
            else if (dataObj is JsonElement)
            {
                var dataRaw = ((JsonElement)dataObj).GetRawText();
                dataObj = JsonSerializer.Deserialize<object>(dataRaw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else if (dataObj is JObject)
            {
                var dataRaw = ((JObject)dataObj).ToString();
                dataObj = JsonSerializer.Deserialize<object>(dataRaw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            return dataObj;
        }

        private void PersistCacheToFile()
        {
            if (!GlobalSettings.PersistCacheToFile) return;

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
                            var authKey = GetAuthKey(key.cacheKey, authorization: key.authorization);
                            var entry = MemoryCache.Get<CacheEntry>(authKey);
                            var dataRaw = ((JsonElement)entry.data).GetRawText();
                            var dataObj = JsonSerializer.Deserialize<object>(dataRaw, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            keyEntries.Add(new
                            {
                                Key = authKey,
                                Entry = new CacheEntry(entry.authorization, entry.cacheKey, dataObj, expirationDateTime: entry.expiration)
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }

                var fileName = GlobalSettings.PersistentDataFileName;
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
            var fileName = GlobalSettings.PersistentDataFileName;
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
