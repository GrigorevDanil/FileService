using System.Text.Json;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.ValueObjects;
using FileService.Domain.VideoAssets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public class VideoAssetConfiguration : IEntityTypeConfiguration<VideoAsset>
{
    public void Configure(EntityTypeBuilder<VideoAsset> builder)
    {
        builder.HasBaseType<MediaAsset>();

        builder.Property(e => e.HlsRootKey)
            .HasColumnName("hls_root_key")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<StorageKey>(v, JsonSerializerOptions.Default)!)
            .HasColumnType("jsonb");
    }
}