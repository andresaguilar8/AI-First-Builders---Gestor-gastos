using System.Net.Http.Json;
using System.Text.Json;
using GestorGastos.Expenses;
using GestorGastos.Tests.Data;
using GestorGastos.Time;

namespace GestorGastos.Tests.Expenses;

[Collection(DatabaseCollection.Name)]
public sealed class DueStatusEndpointsTests(DatabaseFixture fixture) : IAsyncLifetime, IDisposable
{
    private readonly ApiFactory factory = new(fixture);

    private HttpClient Client => field ??= factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => factory.Dispose();

    private async Task AddOneOffAsync(string name, DateOnly? dueDate, DateOnly? paidOn = null)
    {
        var october = new Period(2026, 10);
        var expense = new Expense
        {
            Name = name,
            Amount = 1_000m,
            DueDate = dueDate,
            Kind = ExpenseKind.OneOff,
            StartPeriod = october,
            EndPeriod = october,
        };
        if (paidOn is not null)
        {
            expense.Months.Add(new ExpenseMonth { Period = october, Name = name, Amount = 1_000m, DueDate = dueDate, PaidOn = paidOn });
        }

        await using var db = fixture.CreateContext();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
    }

    private async Task<Dictionary<string, string>> StatusesAsync(string period = "2026-10")
    {
        var list = await Client.GetFromJsonAsync<JsonElement>($"/api/periods/{period}/expenses");
        return list.EnumerateArray().ToDictionary(
            e => e.GetProperty("name").GetString()!,
            e => e.GetProperty("status").GetString()!);
    }

    [Fact]
    public async Task Informa_el_estado_de_cada_gasto_del_mes()
    {
        // AC-43 a AC-47, con fecha actual 05/10/2026.
        factory.SetToday(new DateOnly(2026, 10, 5));
        await AddOneOffAsync("Al día", new DateOnly(2026, 10, 9));
        await AddOneOffAsync("Sin vencimiento", dueDate: null);
        await AddOneOffAsync("Próximo 1", new DateOnly(2026, 10, 8));
        await AddOneOffAsync("Próximo 2", new DateOnly(2026, 10, 6));
        await AddOneOffAsync("Hoy", new DateOnly(2026, 10, 5));
        await AddOneOffAsync("Vencido", new DateOnly(2026, 10, 4));
        await AddOneOffAsync("Pagado", new DateOnly(2026, 10, 1), paidOn: new DateOnly(2026, 10, 1));

        var statuses = await StatusesAsync();

        Assert.Equal(new Dictionary<string, string>
        {
            ["Al día"] = "upToDate",
            ["Sin vencimiento"] = "upToDate",
            ["Próximo 1"] = "dueSoon",
            ["Próximo 2"] = "dueSoon",
            ["Hoy"] = "dueToday",
            ["Vencido"] = "overdue",
            ["Pagado"] = "paid",
        }, statuses);
    }

    [Fact]
    public async Task Usa_la_fecha_de_Buenos_Aires_para_el_estado()
    {
        // AC-48: 22:00 del 05/10/2026 en Buenos Aires = 01:00 del 06/10/2026 UTC.
        factory.Time.UtcNow = DateTimeOffset.Parse("2026-10-06T01:00:00Z");
        await AddOneOffAsync("Luz", new DateOnly(2026, 10, 5));

        Assert.Equal("dueToday", (await StatusesAsync())["Luz"]);
    }

    [Fact]
    public async Task Las_respuestas_de_alta_y_pago_tambien_traen_el_estado()
    {
        factory.SetToday(new DateOnly(2026, 10, 5));

        var createResponse = await Client.PostAsJsonAsync("/api/periods/2026-10/expenses",
            new { name = "Luz", amount = 1000, dueDate = "2026-10-05", kind = "oneOff" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("expenseId").GetInt32();
        var payResponse = await Client.PutAsJsonAsync($"/api/periods/2026-10/expenses/{id}/payment", new { paidOn = (string?)null });
        var paid = await payResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("dueToday", created.GetProperty("status").GetString());
        Assert.Equal("paid", paid.GetProperty("status").GetString());
    }
}
