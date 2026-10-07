using System.Net.Http.Json;
using System.Text.Json;
using GestorGastos.Categories;
using GestorGastos.Expenses;
using GestorGastos.Tests.Data;
using GestorGastos.Time;

namespace GestorGastos.Tests.Summary;

[Collection(DatabaseCollection.Name)]
public sealed class SummaryEndpointsTests(DatabaseFixture fixture) : IAsyncLifetime, IDisposable
{
    private readonly ApiFactory factory = new(fixture);

    private HttpClient Client => field ??= factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => factory.Dispose();

    private async Task<int> AddCategoryAsync(string name)
    {
        await using var db = fixture.CreateContext();
        var category = new Category { Name = name, NormalizedName = CategoryNames.Normalize(name) };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task AddAsync(decimal amount, int? categoryId, Period start, ExpenseKind kind = ExpenseKind.OneOff, DateOnly? paidOn = null)
    {
        var expense = new Expense
        {
            Name = "Gasto",
            Amount = amount,
            Kind = kind,
            StartPeriod = start,
            EndPeriod = kind == ExpenseKind.OneOff ? start : null,
            CategoryId = categoryId,
        };
        if (paidOn is not null)
        {
            expense.Months.Add(new ExpenseMonth { Period = start, Name = "Gasto", Amount = amount, CategoryId = categoryId, PaidOn = paidOn });
        }

        await using var db = fixture.CreateContext();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
    }

    private Task<JsonElement> SummaryAsync(string period) =>
        Client.GetFromJsonAsync<JsonElement>($"/api/periods/{period}/summary");

    [Fact]
    public async Task Resume_el_mes_por_categoria_con_total_y_pendiente()
    {
        // AC-32, AC-33 y AC-34 en septiembre de 2026.
        var september = new Period(2026, 9);
        var vivienda = await AddCategoryAsync("Vivienda");
        var servicios = await AddCategoryAsync("Servicios");
        await AddAsync(200_000m, vivienda, september, paidOn: new DateOnly(2026, 9, 2));
        await AddAsync(100_000m, vivienda, september);
        await AddAsync(100_000m, servicios, september);
        await AddAsync(50_000m, categoryId: null, september);

        var summary = await SummaryAsync("2026-09");

        Assert.Equal("2026-09", summary.GetProperty("period").GetString());
        Assert.Equal(450_000m, summary.GetProperty("total").GetDecimal());
        Assert.Equal(250_000m, summary.GetProperty("pending").GetDecimal());
        var groups = summary.GetProperty("byCategory").EnumerateArray()
            .Select(g => (
                g.GetProperty("category") is { ValueKind: JsonValueKind.Object } c ? c.GetProperty("name").GetString() : null,
                g.GetProperty("total").GetDecimal()))
            .ToList();
        Assert.Equal([("Vivienda", 300_000m), ("Servicios", 100_000m), (null, 50_000m)], groups);
    }

    [Fact]
    public async Task Incluye_los_recurrentes_proyectados_y_no_los_de_otros_meses()
    {
        await AddAsync(30_000m, null, new Period(2026, 6), ExpenseKind.Recurring);
        await AddAsync(5_000m, null, new Period(2026, 9));

        var summary = await SummaryAsync("2026-10");

        Assert.Equal(30_000m, summary.GetProperty("total").GetDecimal());
        Assert.Equal(30_000m, summary.GetProperty("pending").GetDecimal());
    }

    [Fact]
    public async Task Un_mes_sin_gastos_responde_en_cero()
    {
        var summary = await SummaryAsync("2026-10");

        Assert.Equal(0m, summary.GetProperty("total").GetDecimal());
        Assert.Equal(0m, summary.GetProperty("pending").GetDecimal());
        Assert.Empty(summary.GetProperty("byCategory").EnumerateArray());
    }
}
