using Microsoft.EntityFrameworkCore;
using ConnectHub.Notification.API.Models;

namespace ConnectHub.Notification.API.Repository.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

        public DbSet<Models.Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Models.Notification>().HasKey(n => n.NotificationId);
        }
    }
}
