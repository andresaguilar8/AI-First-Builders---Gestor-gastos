using GestorGastos.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorGastos.Data.Configurations;

public class ExpenseMonthConfiguration : IEntityTypeConfiguration<ExpenseMonth>
{
    public void Configure(EntityTypeBuilder<ExpenseMonth> builder)
    {
        builder.ToTable("expense_months", table =>
            table.HasCheckConstraint("ck_expense_months_amount_positive", "amount > 0"));

        builder.Property(m => m.Name).HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(1000);
        builder.Property(m => m.Amount).HasPrecision(14, 2);

        // RNF-06: como máximo una instancia propia por gasto y mes.
        builder.HasIndex(m => new { m.ExpenseId, m.Period }).IsUnique();
    }
}
