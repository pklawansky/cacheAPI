using CacheAPI.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.BL
{
    public class ConfigurationBL
    {
        private readonly IConfiguration Configuration;
        private const string MySettings = "MySettings";
        private const string DefaultCacheExpirationSeconds = "DefaultCacheExpirationSeconds";
        private const string PersistentDataFileName = "PersistentDataFileName";
        private const string PersistCacheToFile = "PersistCacheToFile";
        private const string AutoPopulateEndpoints = "AutoPopulateEndpoints";

        public ConfigurationBL(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public double GetDefaultCacheExpirationSeconds()
        {
            return Configuration.GetValue<double>($"{MySettings}:{DefaultCacheExpirationSeconds}");
        }

        public string GetPersistentDataFileName()
        {
            return Configuration.GetValue<string>($"{MySettings}:{PersistentDataFileName}");
        }

        public bool GetPersistCacheToFile()
        {
            return Configuration.GetValue<bool>($"{MySettings}:{PersistCacheToFile}");
        }

        public List<AutoPopulateEndpoint> GetAutoPopulateEndpoints()
        {
            return Configuration.GetValue<List<AutoPopulateEndpoint>>($"{MySettings}:{AutoPopulateEndpoints}");
        }
    }
}
