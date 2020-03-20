using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AEM.Models
{
    public class WellDTO
    {
        public int Id { get; set; }
        public string UniqueName { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int PlatformId { get; set; }
    }
}
