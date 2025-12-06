using FileService.Domain.MediaAssets;
using FileService.Domain.VideoAssets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public class VideoAssetConfiguration : IEntityTypeConfiguration<VideoAsset>
{
    public void Configure(EntityTypeBuilder<VideoAsset> builder)
    {
        builder.HasBaseType<MediaAsset>();

        builder.OwnsOne(e => e.HlsRootKey, hrkb =>
        {
            hrkb.ToJson("hls_root_key");

            hrkb.Property(rke => rke.Location)
                .HasJsonPropertyName("location");

            hrkb.Property(rke => rke.Prefix)
                .HasJsonPropertyName("prefix");

            hrkb.Property(rke => rke.Key)
                .HasJsonPropertyName("key");
        });
    }
}