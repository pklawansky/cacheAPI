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
    public class CacheController : BaseController
    {
        #region Initialization

        public CacheController(IMemoryCache memoryCache, IConfiguration configuration) : base(memoryCache, configuration)
        {
        }

        #endregion

        #region Endpoints

        [Route("List")]
        [HttpGet]
        public IActionResult List()
        {
            try
            {
                var results = new CacheBL(MemoryCache, Configuration, GetAuthorization()).ListFromDictionary();
                return Ok(results);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpGet]
        public IActionResult Get(string cacheKey)
        {
            try
            {
                var results = new CacheBL(MemoryCache, Configuration, GetAuthorization()).GetFromDictionary(cacheKey);
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
                new CacheBL(MemoryCache, Configuration, GetAuthorization()).DeleteFromDictionary(cacheKey);
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
                new CacheBL(MemoryCache, Configuration, GetAuthorization(), overrideDefaultCacheSeconds: cacheSeconds).PostToDictionary(cacheKey, values);
                return Ok();
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }

        #endregion
    }
}