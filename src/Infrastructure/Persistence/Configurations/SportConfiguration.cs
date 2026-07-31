using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Configurations;

public class SportConfiguration : IEntityTypeConfiguration<Sport>
{
    public void Configure(EntityTypeBuilder<Sport> entity)
    {
        entity.ToTable("sports");
        entity.HasKey(s => s.Id);

        entity.Property(s => s.Id).HasColumnName("id");
        entity.Property(s => s.Name).HasColumnName("name").IsRequired();
        entity.Property(s => s.IconName).HasColumnName("icon_name");
        entity.Property(s => s.Category).HasColumnName("category");
    }
}
