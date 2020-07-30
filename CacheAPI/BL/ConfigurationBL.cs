using CacheAPI.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.BL
{
    public class ConfigurationBL
    {
        #region Props

        private readonly IConfiguration Configuration;
        private const string _MySettings = "MySettings";
        private const string _DefaultCacheExpirationSeconds = "DefaultCacheExpirationSeconds";
        private const string _PersistentDataFileName = "PersistentDataFileName";
        private const string _PersistCacheToFile = "PersistCacheToFile";
        private const string _AutoPopulateEndpoints = "AutoPopulateEndpoints";

        #endregion

        #region Initialization

        public ConfigurationBL(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        #endregion

        #region Public Configurations

        public double DefaultCacheExpirationSeconds => Configuration.GetValue<double>($"{_MySettings}:{_DefaultCacheExpirationSeconds}");
        public string PersistentDataFileName => Configuration.GetValue<string>($"{_MySettings}:{_PersistentDataFileName}");
        public bool PersistCacheToFile => Configuration.GetValue<bool>($"{_MySettings}:{_PersistCacheToFile}");
        public List<AutoPopulateEndpoint> AutoPopulateEndpoints
        {
            get
            {
                var val = (List<AutoPopulateEndpoint>)Configuration.GetSection(_MySettings).GetChildren().First(x=>x.Key == _AutoPopulateEndpoints).Get(typeof(List<AutoPopulateEndpoint>));
                return val;
            }
        }

        #endregion
    }
}
