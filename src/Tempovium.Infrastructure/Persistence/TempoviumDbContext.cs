using Microsoft.EntityFrameworkCore;
using Tempovium.Core.Entities;

namespace Tempovium.Infrastructure.Persistence;

public class TempoviumDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<MediaItem> MediaItems => Set<MediaItem>();

    public DbSet<MediaNote> MediaNotes => Set<MediaNote>();

    public TempoviumDbContext(DbContextOptions<TempoviumDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TempoviumDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}