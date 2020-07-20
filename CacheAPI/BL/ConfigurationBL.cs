using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.BL
{
    public class ConfigurationBL
    {
        private readonly IConfiguration _iConfiguration;
        private const string MySettings = "MySettings";
        private const string DefaultCacheExpirationSeconds = "DefaultCacheExpirationSeconds";

        public ConfigurationBL(IConfiguration iConfiguration)
        {
            _iConfiguration = iConfiguration;
        }

        public double GetDefaultCacheExpirationSeconds()
        {
            return _iConfiguration.GetValue<double>($"{MySettings}:{DefaultCacheExpirationSeconds}");
        }
    }
}
