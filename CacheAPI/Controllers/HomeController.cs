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
    [Route("[controller]")]
    [ApiController]
    public class HomeController : BaseController
    {
        public HomeController(IMemoryCache memoryCache, IConfiguration configuration) : base(memoryCache, configuration)
        {
        }

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
    }
}