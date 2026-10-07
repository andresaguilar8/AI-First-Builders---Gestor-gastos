using GestorGastos.Expenses;
using GestorGastos.Tests.Data;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Tests.Expenses;

[Collection(DatabaseCollection.Name)]
public class MonthlyExpensesTests(DatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly Period October2026 = new(2026, 10);

    private async Task<Expense> AddAsync(Expense expense)
    {
        await using var db = fixture.CreateContext();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return expense;
    }

    private async Task<IReadOnlyList<MonthlyExpense>> ForPeriodAsync(Period period)
    {
        await using var db = fixture.CreateContext();
        return await new MonthlyExpenses(db).ForPeriodAsync(period);
    }

    private static Expense Recurring(string name, DateOnly? dueDate = null) => new()
    {
        Name = name,
        Amount = 20_000m,
        DueDate = dueDate,
        Kind = ExpenseKind.Recurring,
        StartPeriod = October2026,
    };

    [Fact]
    public async Task Proyecta_un_recurrente_sin_crear_registros_mensuales()
    {
        // AC-60
        var expense = await AddAsync(Recurring("Alquiler", new DateOnly(2026, 10, 10)));

        var december = await ForPeriodAsync(new Period(2026, 12));

        var projected = Assert.Single(december);
        Assert.Equal(expense.Id, projected.ExpenseId);
        Assert.Equal(new DateOnly(2026, 12, 10), projected.DueDate);

        await using var db = fixture.CreateContext();
        Assert.Equal(0, await db.ExpenseMonths.CountAsync());
    }

    [Fact]
    public async Task Usa_la_instancia_propia_solo_en_su_mes()
    {
        var expense = Recurring("Cuota");
        expense.Months.Add(new ExpenseMonth
        {
            Period = October2026.Next(),
            Name = "Cuota",
            Amount = 25_000m,
            PaidOn = new DateOnly(2026, 11, 3),
        });
        await AddAsync(expense);

        var october = Assert.Single(await ForPeriodAsync(October2026));
        var november = Assert.Single(await ForPeriodAsync(October2026.Next()));
        var december = Assert.Single(await ForPeriodAsync(new Period(2026, 12)));

        Assert.Equal((20_000m, (DateOnly?)null), (october.Amount, october.PaidOn));
        Assert.Equal((25_000m, (DateOnly?)new DateOnly(2026, 11, 3)), (november.Amount, november.PaidOn));
        Assert.Equal((20_000m, (DateOnly?)null), (december.Amount, december.PaidOn));
    }

    [Fact]
    public async Task Incluye_puntuales_solo_en_su_mes_y_excluye_recurrentes_terminados()
    {
        await AddAsync(new Expense
        {
            Name = "Regalo",
            Amount = 15_000m,
            Kind = ExpenseKind.OneOff,
            StartPeriod = October2026,
            EndPeriod = October2026,
        });
        var ended = Recurring("Gimnasio");
        ended.StartPeriod = new Period(2026, 8);
        ended.EndPeriod = new Period(2026, 9);
        await AddAsync(ended);

        var october = await ForPeriodAsync(October2026);
        var november = await ForPeriodAsync(October2026.Next());

        Assert.Equal(["Regalo"], october.Select(e => e.Name));
        Assert.Empty(november);
    }

    [Fact]
    public async Task Ordena_por_vencimiento_y_deja_al_final_los_que_no_tienen()
    {
        await AddAsync(Recurring("Sin vencimiento"));
        await AddAsync(Recurring("Luz", new DateOnly(2026, 10, 20)));
        await AddAsync(Recurring("Alquiler", new DateOnly(2026, 10, 5)));

        var october = await ForPeriodAsync(October2026);

        Assert.Equal(["Alquiler", "Luz", "Sin vencimiento"], october.Select(e => e.Name));
    }
}
