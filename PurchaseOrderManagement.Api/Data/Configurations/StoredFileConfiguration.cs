using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        // Entity is named StoredFile because "File" clashes with System.IO.File,
        // but the table itself is named "Files" per docs/02-SUPPLIERS-AND-PROCUREMENT.md.
        builder.ToTable("Files");

        builder.Property(f => f.SourceType)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(f => f.Source)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(f => f.OriginalFileName).HasMaxLength(512);
        builder.Property(f => f.ContentType).HasMaxLength(256);
        builder.Property(f => f.FileSizeBytes);
    }
}
