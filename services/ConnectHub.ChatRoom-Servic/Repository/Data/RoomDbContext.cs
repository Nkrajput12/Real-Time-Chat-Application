using Microsoft.EntityFrameworkCore;
using ConnectHub.Rooms.Models;

namespace ConnectHub.Rooms.Data;

public class RoomDbContext : DbContext
{
    public RoomDbContext(DbContextOptions<RoomDbContext> options) : base(options) { }

    public DbSet<ChatRoom> ChatRooms { get; set; }
    public DbSet<RoomMember> RoomMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Requirement: Composite index on (RoomId, UserId)
        modelBuilder.Entity<RoomMember>()
            .HasIndex(rm => new { rm.RoomId, rm.UserId })
            .IsUnique();
    }
}