using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheAPI.BL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace CacheAPI.Controllers
{
    // TODO: put in authentication

    [Route("api/[controller]")]
    [ApiController]
    public class CacheController : ControllerBase
    {
        IMemoryCache _cache;
        IConfiguration _iConfiguration;
        public CacheController(IMemoryCache memoryCache, IConfiguration iConfiguration)
        {
            _cache = memoryCache;
            _iConfiguration = iConfiguration;
        }

        [HttpGet]
        public IActionResult Get(string cacheKey)
        {
            try
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
                var results = new CacheBL(_cache, _iConfiguration, auth).GetFromDictionary(cacheKey);
                return Ok(results);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpDelete]
        public IActionResult Delete(string cacheKey)
        {
            try
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
                new CacheBL(_cache, _iConfiguration, auth).DeleteFromDictionary(cacheKey);
                return Ok();
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }

        }

        [HttpPost]
        public IActionResult Post(string cacheKey, [FromBody] object values, double? cacheSeconds = null)
        {
            try
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
                new CacheBL(_cache, _iConfiguration, auth, overrideDefaultCacheSeconds: cacheSeconds).PostToDictionary(cacheKey, values);
                return Ok();
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }
      
    }
}