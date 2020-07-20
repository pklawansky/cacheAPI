using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheAPI.BL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CacheAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        public string Index()
        {
            return "Welcome to caching api";
        }

        [Route("Test")]
        public IActionResult Test()
        {
            var result = TestsBL.TestGetA();
            return Ok(new { result });
        }
    }
}