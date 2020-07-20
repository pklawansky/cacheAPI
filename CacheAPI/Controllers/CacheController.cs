using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheAPI.BL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CacheAPI.Controllers
{
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
        public IActionResult Get(string cacheKey, string dictionaryKey = null)
        {
            try
            {
                var results = new CacheBL(_cache, _iConfiguration).GetFromDictionary(cacheKey, dictionaryKey);
                return Ok(results);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpDelete]
        public IActionResult Delete(string cacheKey, string dictionaryKey = null)
        {
            try
            {
                new CacheBL(_cache, _iConfiguration).DeleteFromDictionary(cacheKey, dictionaryKey);
                return Ok();
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }

        }

        [HttpPost]
        public IActionResult Post(string cacheKey, [FromBody] List<KeyValuePair<string, object>> values, double? cacheSeconds = null)
        {
            try
            {
                new CacheBL(_cache, _iConfiguration, overrideDefaultCacheSeconds: cacheSeconds).PostToDictionary(cacheKey, values);
                return Ok();
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpPut]
        public IActionResult Put(string cacheKey, [FromBody] List<KeyValuePair<string, object>> values)
        {
            try
            {
                new CacheBL(_cache, _iConfiguration).PutToDictionary(cacheKey, values);
                return Ok();
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }
    }
}