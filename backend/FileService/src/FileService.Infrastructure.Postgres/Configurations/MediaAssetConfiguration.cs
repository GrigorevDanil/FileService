using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using FileService.Domain.PreviewAssets;
using FileService.Domain.VideoAssets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");

        builder.HasKey(e => e.Id)
            .HasName("pk_media_asset");

        builder.Property(e => e.Id)
            .HasConversion(dId => dId.Value, id => MediaAssetId.Of(id))
            .HasColumnName("id");

        builder.HasDiscriminator(e => e.AssetType)
            .HasValue<VideoAsset>(AssetType.VIDEO)
            .HasValue<PreviewAsset>(AssetType.PREVIEW);

        builder.Property(x => x.AssetType)
            .HasConversion<string>()
            .HasColumnName("asset_type")
            .HasMaxLength(7);

        builder.OwnsOne(e => e.MediaData, mdb =>
        {
            mdb.ToJson("media_data");

            mdb.OwnsOne(mde => mde.FileName, fnb =>
            {
                fnb.Property(x => x.Name).HasJsonPropertyName("name");

                fnb.Property(x => x.Extension).HasJsonPropertyName("extension");
            });

            mdb.OwnsOne(mde => mde.ContentType, ctb =>
            {
                ctb.Property(x => x.Category)
                    .HasConversion<string>()
                    .HasJsonPropertyName("category");

                ctb.Property(x => x.Value).HasJsonPropertyName("content_type");
            });

            mdb.OwnsOne(mde => mde.ExpectedChunksCount, eccb =>
            {
                eccb.Property(x => x.Value).HasJsonPropertyName("expected_chunks_count");
            });

            mdb.OwnsOne(mde => mde.Size, sb =>
            {
                sb.Property(x => x.Value).HasJsonPropertyName("size");
            });
        });

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(9)
            .IsRequired();

        builder.OwnsOne(e => e.RawKey, rkb =>
        {
            rkb.ToJson("raw_key");

            rkb.Property(rke => rke.Location).HasJsonPropertyName("location");

            rkb.Property(rke => rke.Prefix).HasJsonPropertyName("prefix");

            rkb.Property(rke => rke.Key).HasJsonPropertyName("key");
        });

        builder.OwnsOne(e => e.FinalKey, fkb =>
        {
            fkb.ToJson("final_key");

            fkb.Property(fke => fke.Location).HasJsonPropertyName("location");

            fkb.Property(fke => fke.Prefix).HasJsonPropertyName("prefix");

            fkb.Property(fke => fke.Key).HasJsonPropertyName("key");
        });

        builder.ComplexProperty(e => e.Owner, ob =>
        {
            ob.Property(oe => oe.Context)
                .HasColumnName("context")
                .HasMaxLength(MediaOwner.MAX_CONTEXT_LENGTH)
                .IsRequired();

            ob.Property(oe => oe.EntityId)
                .HasColumnName("entity_id")
                .IsRequired();
        });

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("ix_media_assets_status_created_at");
    }
}