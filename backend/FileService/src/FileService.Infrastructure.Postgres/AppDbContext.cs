using FileService.Domain.MediaAssets;
using Microsoft.EntityFrameworkCore;

namespace FileService.Infrastructure.Postgres;

public class AppDbContext : DbContext
{
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}