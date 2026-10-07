using GestorGastos.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorGastos.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.Property(c => c.Name).HasMaxLength(CategoryNames.MaxLength);
        builder.Property(c => c.NormalizedName).HasMaxLength(CategoryNames.MaxLength);

        // RF-19: no puede haber dos categorías con el mismo nombre normalizado.
        builder.HasIndex(c => c.NormalizedName).IsUnique();
    }
}
