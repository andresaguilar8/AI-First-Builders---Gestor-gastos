using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestorGastos.Expenses;
using GestorGastos.Tests.Data;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Tests.Expenses;

[Collection(DatabaseCollection.Name)]
public sealed class ExpenseEditEndpointsTests(DatabaseFixture fixture) : IAsyncLifetime, IDisposable
{
    private readonly ApiFactory factory = new(fixture);

    private HttpClient Client => field ??= factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => factory.Dispose();

    private static readonly Period October2026 = new(2026, 10);

    private async Task<Expense> AddAsync(ExpenseKind kind = ExpenseKind.OneOff, Action<Expense>? configure = null)
    {
        var expense = new Expense
        {
            Name = "Regalo",
            Description = "Cumpleaños",
            Amount = 15_000m,
            DueDate = new DateOnly(2026, 10, 20),
            Kind = kind,
            StartPeriod = October2026,
            EndPeriod = kind == ExpenseKind.OneOff ? October2026 : null,
        };
        configure?.Invoke(expense);

        await using var db = fixture.CreateContext();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return expense;
    }

    private Task<HttpResponseMessage> PutAsync(string period, int id, object body) =>
        Client.PutAsJsonAsync($"/api/periods/{period}/expenses/{id}", body);

    private Task<HttpResponseMessage> DeleteAsync(string period, int id) =>
        Client.DeleteAsync($"/api/periods/{period}/expenses/{id}");

    private async Task<Expense?> ReloadAsync(int id)
    {
        await using var db = fixture.CreateContext();
        return await db.Expenses.Include(e => e.Months).SingleOrDefaultAsync(e => e.Id == id);
    }

    private static readonly object ValidEdit = new
    {
        name = "Regalo de cumpleaños",
        description = "Para Ana",
        amount = 18_500.50m,
        dueDate = "2026-11-02",
    };

    [Fact]
    public async Task Edita_todos_los_valores_de_un_puntual()
    {
        // AC-14
        var expense = await AddAsync();

        var response = await PutAsync("2026-10", expense.Id, ValidEdit);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Regalo de cumpleaños", body.GetProperty("name").GetString());
        Assert.Equal("2026-11-02", body.GetProperty("dueDate").GetString());

        var saved = (await ReloadAsync(expense.Id))!;
        Assert.Equal("Regalo de cumpleaños", saved.Name);
        Assert.Equal("Para Ana", saved.Description);
        Assert.Equal(18_500.50m, saved.Amount);
        Assert.Equal(new DateOnly(2026, 11, 2), saved.DueDate);
        Assert.Equal(ExpenseKind.OneOff, saved.Kind);
        Assert.Equal(October2026, saved.StartPeriod);
    }

    [Fact]
    public async Task Permite_quitar_la_descripcion_y_el_vencimiento()
    {
        var expense = await AddAsync();

        await PutAsync("2026-10", expense.Id, new { name = "Regalo", description = "  ", amount = 15000, dueDate = (string?)null });

        var saved = (await ReloadAsync(expense.Id))!;
        Assert.Null(saved.Description);
        Assert.Null(saved.DueDate);
    }

    [Fact]
    public async Task Rechaza_una_edicion_invalida_sin_cambiar_el_gasto()
    {
        var expense = await AddAsync();

        var response = await PutAsync("2026-10", expense.Id, new { name = " ", amount = 10.999m, dueDate = "2026-09-30" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal(["amount", "dueDate", "name"], errors.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal("Regalo", (await ReloadAsync(expense.Id))!.Name);
    }

    [Fact]
    public async Task Editar_actualiza_la_instancia_del_mes_y_conserva_el_pago()
    {
        var expense = await AddAsync(configure: e => e.Months.Add(new ExpenseMonth
        {
            Period = October2026,
            Name = "Regalo",
            Amount = 15_000m,
            PaidOn = new DateOnly(2026, 10, 3),
        }));

        await PutAsync("2026-10", expense.Id, ValidEdit);

        var month = Assert.Single((await ReloadAsync(expense.Id))!.Months);
        Assert.Equal("Regalo de cumpleaños", month.Name);
        Assert.Equal(18_500.50m, month.Amount);
        Assert.Equal(new DateOnly(2026, 11, 2), month.DueDate);
        Assert.Equal(new DateOnly(2026, 10, 3), month.PaidOn);
    }

    [Fact]
    public async Task Elimina_un_puntual_y_deja_de_aparecer_en_su_mes()
    {
        // AC-23
        var expense = await AddAsync(configure: e => e.Months.Add(new ExpenseMonth
        {
            Period = October2026,
            Name = "Regalo",
            Amount = 15_000m,
        }));

        var response = await DeleteAsync("2026-10", expense.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var october = await Client.GetFromJsonAsync<JsonElement>("/api/periods/2026-10/expenses");
        Assert.Empty(october.EnumerateArray());
        await using var db = fixture.CreateContext();
        Assert.Equal(0, await db.ExpenseMonths.CountAsync());
    }

    [Fact]
    public async Task No_edita_ni_elimina_recurrentes_todavia()
    {
        var expense = await AddAsync(ExpenseKind.Recurring);

        var put = await PutAsync("2026-11", expense.Id, ValidEdit);
        var delete = await DeleteAsync("2026-11", expense.Id);

        Assert.Equal(HttpStatusCode.Conflict, put.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
        var saved = (await ReloadAsync(expense.Id))!;
        Assert.Equal("Regalo", saved.Name);
    }

    [Theory]
    [InlineData("2026-11")]
    [InlineData("2026-09")]
    public async Task Responde_404_si_el_gasto_no_aparece_en_ese_mes(string period)
    {
        var expense = await AddAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await PutAsync(period, expense.Id, ValidEdit)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync(period, expense.Id)).StatusCode);
        Assert.NotNull(await ReloadAsync(expense.Id));
    }

    [Fact]
    public async Task Responde_404_si_el_gasto_no_existe()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await PutAsync("2026-10", 999, ValidEdit)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync("2026-10", 999)).StatusCode);
    }
}
