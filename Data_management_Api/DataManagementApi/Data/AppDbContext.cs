using Microsoft.EntityFrameworkCore;
using DataManagementApi.Models;

namespace DataManagementApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<DataRecord> DataRecords { get; set; } // Keeping for backward compatibility or migration if needed, but primary usage will shift
        public DbSet<Brand> Brands { get; set; }

        public DbSet<SteriliteRecord> SteriliteRecords { get; set; }
        public DbSet<NikeRecord> NikeRecords { get; set; }
        public DbSet<TJXRecord> TJXRecords { get; set; }
        public DbSet<LandmarkSplashRecord> LandmarkSplashRecords { get; set; }
        public DbSet<LandmarkBBSRecord> LandmarkBBSRecords { get; set; }
        public DbSet<LandmarkMAXRecord> LandmarkMAXRecords { get; set; }
        public DbSet<NilronRecord> NilronRecords { get; set; }
        public DbSet<WalmartRecord> WalmartRecords { get; set; }
        public DbSet<HMRecord> HMRecords { get; set; }
        public DbSet<TTIRecord> TTIRecords { get; set; }
        public DbSet<TATARecord> TATARecords { get; set; }
        public DbSet<InditexRecord> InditexRecords { get; set; }
        public DbSet<DCLRecord> DCLRecords { get; set; }
        public DbSet<PadiniRecord> PadiniRecords { get; set; }
        public DbSet<KMARTRecord> KMARTRecords { get; set; }

        // Brand Site Order Data
        public DbSet<KmartDailyRecord> KmartDailyRecords { get; set; }
    }
}
