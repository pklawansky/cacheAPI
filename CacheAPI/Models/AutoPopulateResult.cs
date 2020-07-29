using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheAPI.Models
{
    public class AutoPopulateResult
    {
        public object Data { get; set; }
        public double? CacheSecondsOverride { get; set; }
    }
}
