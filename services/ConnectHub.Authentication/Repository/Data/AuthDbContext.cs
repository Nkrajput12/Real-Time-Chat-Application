using Microsoft.EntityFrameworkCore;
using ConnectHub.Authentication.Models;

namespace ConnectHub.Authentication.Repository.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
}