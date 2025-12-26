using Microsoft.EntityFrameworkCore;
using DataManagementApi.Models;

namespace DataManagementApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<DataRecord> DataRecords { get; set; }
    }
}
