using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace CacheAPI.Controllers
{
    [ApiController]
    public class BaseController : ControllerBase
    {
        #region Props

        protected IMemoryCache MemoryCache;
        protected IConfiguration Configuration;

        #endregion

        #region Initialization

        public BaseController(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
        }

        #endregion

        #region Helper Methods

        protected string GetAuthorization()
        {
            if (!Request.Headers.TryGetValue("Authorization", out StringValues auths))
            {
                throw new Exception("Authorization header not found");
            }
            var auth = auths.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(auth))
            {
                throw new Exception("Authorization header cannot be empty");
            }
            return auth;
        }

        #endregion
    }
}
