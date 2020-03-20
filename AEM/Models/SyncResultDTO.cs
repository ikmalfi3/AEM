using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AEM.Models
{
    public class SyncResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> UpdatedMessage { get; set; } = new List<string>();
    }
}
