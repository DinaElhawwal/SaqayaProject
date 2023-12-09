using Microsoft.EntityFrameworkCore;
using SaqayaProject.models;
using System;

namespace SaqayaProject.Data
{
    public class DataContext : DbContext { 
         public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }
    
        public DbSet<user>users { get; set; }
    }
}
