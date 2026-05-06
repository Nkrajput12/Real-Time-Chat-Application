using ConnectHub.MessageService.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.Messages.Repository.Data;
public class MessageDbContext : DbContext
{
    public MessageDbContext(DbContextOptions<MessageDbContext> options) : base(options) { }

    public DbSet<Message> Messages { get; set; }
}