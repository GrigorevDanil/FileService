using FileService.Core;
using FileService.Domain.MediaAssets;
using Microsoft.EntityFrameworkCore;

namespace FileService.Infrastructure.Postgres;

public class AppDbContext : DbContext, IReadDbContext
{
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    public IQueryable<MediaAsset> MediaAssetsRead => Set<MediaAsset>().AsQueryable().AsNoTracking();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}