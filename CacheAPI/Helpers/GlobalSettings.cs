using CacheAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.Helpers
{
    public class GlobalSettings
    {
        public static int WebSocketPort { get; set; }
        public static int SemaphoreInitial { get; set; }
        public static double DefaultCacheExpirationSeconds { get; set; }
        public static List<AutoPopulateEndpoint> AutoPopulateEndpoints { get; set; }
        public static bool PersistCacheToFile { get; set; }
        public static string PersistentDataFileName { get; set; }
    }
}
