using Microsoft.EntityFrameworkCore;
using ConnectHub.Media.API.Models;

namespace ConnectHub.Media.API.Repository.Data
{
    public class MediaDbContext : DbContext
    {
        public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options) { }

        public DbSet<MediaFile> MediaFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MediaFile>().HasKey(m => m.FileId);
            modelBuilder.Entity<MediaFile>().Property(m => m.FileId).ValueGeneratedNever();
        }
    }
}
