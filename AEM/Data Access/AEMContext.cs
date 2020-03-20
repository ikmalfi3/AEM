using AEM.Domains;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AEM.Data_Access
{
    public class AEMContext : DbContext
    {
        public AEMContext(DbContextOptions options) : base(options) { }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Well> Wells { get; set; } 
    }
}
