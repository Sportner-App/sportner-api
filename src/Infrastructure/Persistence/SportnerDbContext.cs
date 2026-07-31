using Microsoft.EntityFrameworkCore;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence;

public class SportnerDbContext(DbContextOptions<SportnerDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<UserEvent> UserEvents => Set<UserEvent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Sport> Sports => Set<Sport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SportnerDbContext).Assembly);
    }
}
