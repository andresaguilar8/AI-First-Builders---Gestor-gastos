using GestorGastos.Expenses;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Tests.Data;

[Collection(DatabaseCollection.Name)]
public class ExpenseModelTests(DatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly Period October2026 = new(2026, 10);

    private static Expense NewExpense(ExpenseKind kind = ExpenseKind.Recurring) => new()
    {
        Name = "Alquiler",
        Amount = 300_000m,
        Kind = kind,
        StartPeriod = October2026,
        EndPeriod = kind == ExpenseKind.OneOff ? October2026 : null,
    };

    private static ExpenseMonth NewMonth(Expense expense, Period period) => new()
    {
        Expense = expense,
        Period = period,
        Name = expense.Name,
        Amount = expense.Amount,
    };

    [Fact]
    public void El_modelo_no_tiene_cambios_sin_migracion()
    {
        using var db = fixture.CreateContext();

        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Persiste_y_recupera_un_gasto_con_todos_sus_valores()
    {
        await using (var db = fixture.CreateContext())
        {
            db.Expenses.Add(new Expense
            {
                Name = "Regalo",
                Description = "Cumpleaños",
                Amount = 1_234.56m,
                DueDate = new DateOnly(2026, 11, 5),
                Kind = ExpenseKind.OneOff,
                StartPeriod = October2026,
                EndPeriod = October2026,
            });
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            var saved = await db.Expenses.SingleAsync();

            Assert.Equal("Regalo", saved.Name);
            Assert.Equal("Cumpleaños", saved.Description);
            Assert.Equal(1_234.56m, saved.Amount);
            Assert.Equal(new DateOnly(2026, 11, 5), saved.DueDate);
            Assert.Equal(ExpenseKind.OneOff, saved.Kind);
            Assert.Equal(October2026, saved.StartPeriod);
            Assert.Equal(October2026, saved.EndPeriod);
        }
    }

    [Fact]
    public async Task Guarda_el_periodo_como_el_primer_dia_del_mes()
    {
        await using var db = fixture.CreateContext();
        db.Expenses.Add(NewExpense());
        await db.SaveChangesAsync();

        var stored = await db.Database
            .SqlQueryRaw<DateOnly>("SELECT start_period AS \"Value\" FROM expenses")
            .SingleAsync();

        Assert.Equal(new DateOnly(2026, 10, 1), stored);
    }

    [Fact]
    public async Task Filtra_por_periodo_en_la_base()
    {
        await using (var db = fixture.CreateContext())
        {
            var ended = NewExpense();
            ended.EndPeriod = new Period(2026, 11);
            db.Expenses.AddRange(NewExpense(), ended);
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            var december = new Period(2026, 12);

            var active = await db.Expenses
                .Where(e => e.StartPeriod <= december && (e.EndPeriod == null || e.EndPeriod >= december))
                .CountAsync();

            Assert.Equal(1, active);
        }
    }

    [Fact]
    public async Task Rechaza_una_segunda_instancia_del_mismo_gasto_en_el_mismo_mes()
    {
        // RNF-06
        await using var db = fixture.CreateContext();
        var expense = NewExpense();
        db.ExpenseMonths.Add(NewMonth(expense, October2026));
        await db.SaveChangesAsync();

        db.ExpenseMonths.Add(NewMonth(expense, October2026));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Permite_instancias_del_mismo_gasto_en_meses_distintos()
    {
        await using var db = fixture.CreateContext();
        var expense = NewExpense();
        db.ExpenseMonths.AddRange(NewMonth(expense, October2026), NewMonth(expense, October2026.Next()));

        await db.SaveChangesAsync();

        Assert.Equal(2, await db.ExpenseMonths.CountAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task La_base_rechaza_montos_no_positivos(decimal amount)
    {
        await using var db = fixture.CreateContext();
        var expense = NewExpense();
        expense.Amount = amount;
        db.Expenses.Add(expense);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task La_base_rechaza_un_fin_anterior_al_inicio()
    {
        await using var db = fixture.CreateContext();
        var expense = NewExpense();
        expense.EndPeriod = October2026.Previous();
        db.Expenses.Add(expense);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Eliminar_un_gasto_elimina_sus_instancias_mensuales()
    {
        await using (var db = fixture.CreateContext())
        {
            var expense = NewExpense(ExpenseKind.OneOff);
            db.ExpenseMonths.Add(NewMonth(expense, October2026));
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            db.Expenses.Remove(await db.Expenses.SingleAsync());
            await db.SaveChangesAsync();

            Assert.Equal(0, await db.ExpenseMonths.CountAsync());
        }
    }
}
