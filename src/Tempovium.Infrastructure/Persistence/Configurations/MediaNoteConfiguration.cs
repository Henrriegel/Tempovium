using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tempovium.Core.Entities;

namespace Tempovium.Infrastructure.Persistence.Configurations;

public class MediaNoteConfiguration : IEntityTypeConfiguration<MediaNote>
{
    public void Configure(EntityTypeBuilder<MediaNote> builder)
    {
        builder.ToTable("MediaNotes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.TimestampSeconds)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => new { x.MediaItemId, x.TimestampSeconds });

        builder.HasOne<MediaItem>()
            .WithMany()
            .HasForeignKey(x => x.MediaItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}