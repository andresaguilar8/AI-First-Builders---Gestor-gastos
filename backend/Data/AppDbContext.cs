using GestorGastos.Expenses;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<ExpenseMonth> ExpenseMonths => Set<ExpenseMonth>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Period>().HaveConversion<PeriodConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
