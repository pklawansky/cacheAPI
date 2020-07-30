using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheAPI.BL;
using CacheAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CacheAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TestController : BaseController
    {
        #region Initialization

        public TestController(IMemoryCache memoryCache, IConfiguration configuration) : base(memoryCache, configuration)
        {
        }

        #endregion

        #region Endpoints

        public string Index()
        {
            return "Welcome to caching api";
        }

        [Route("Test")]
        public IActionResult Test()
        {
            var result = TestsBL.TestPost(out bool success, "a");
            result = TestsBL.TestPost(out success, "b");
            return Ok(new { result });
        }

        [Route("LoadAlphabet")]
        public IActionResult LoadAlphabet()
        {
            var result = new AutoPopulateResult
            {
                CacheSecondsOverride = 30,
                Data = new string[]
                {
                    "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"
                }
            };

            return Ok(result);
        }

        [Route("LoadComplexData")]
        public IActionResult LoadComplexData()
        {
            var result = new AutoPopulateResult
            {
                CacheSecondsOverride = 30,
                Data = new
                {
                    Name = "Alphabet",
                    Description = "This lists the alphabet",
                    Alphabet = new string[]
                    {
                        "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"
                    }
                }
            };

            return Ok(result);
        }

        #endregion
    }
}