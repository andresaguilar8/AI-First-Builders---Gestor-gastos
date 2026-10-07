using GestorGastos.Categories;
using GestorGastos.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorGastos.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses", table =>
        {
            table.HasCheckConstraint("ck_expenses_amount_positive", "amount > 0");
            table.HasCheckConstraint(
                "ck_expenses_end_after_start",
                "end_period IS NULL OR end_period >= start_period");
        });

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Amount).HasPrecision(14, 2);
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => new { e.StartPeriod, e.EndPeriod });

        // RF-21: al eliminar la categoría, sus gastos quedan sin categoría.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Months)
            .WithOne(m => m.Expense)
            .HasForeignKey(m => m.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
