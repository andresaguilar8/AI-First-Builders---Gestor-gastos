using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestorGastos.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Tests.Expenses;

[Collection(DatabaseCollection.Name)]
public sealed class ExpenseEndpointsTests(DatabaseFixture fixture) : IAsyncLifetime, IDisposable
{
    private readonly ApiFactory factory = new(fixture);

    private HttpClient Client => field ??= factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => factory.Dispose();

    private Task<HttpResponseMessage> PostAsync(string period, object body) =>
        Client.PostAsJsonAsync($"/api/periods/{period}/expenses", body);

    private async Task<JsonElement> GetAsync(string period)
    {
        var response = await Client.GetAsync($"/api/periods/{period}/expenses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<int> ExpenseCountAsync()
    {
        await using var db = fixture.CreateContext();
        return await db.Expenses.CountAsync();
    }

    private static async Task<JsonElement> ErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("errors");
    }

    [Fact]
    public async Task Crea_un_recurrente_en_el_mes_visualizado()
    {
        // AC-01
        var response = await PostAsync("2026-10", new { name = "Alquiler", amount = 300000, kind = "recurring" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var october = await GetAsync("2026-10");
        var expense = Assert.Single(october.EnumerateArray());
        Assert.Equal("Alquiler", expense.GetProperty("name").GetString());
        Assert.Equal(300000m, expense.GetProperty("amount").GetDecimal());
        Assert.Equal("recurring", expense.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task Crea_un_puntual_con_todos_sus_valores()
    {
        // AC-02 (sin categoría: se agrega en el paso 14)
        var response = await PostAsync("2026-10", new
        {
            name = "Regalo",
            description = "Cumpleaños",
            amount = 15000,
            dueDate = "2026-10-20",
            kind = "oneOff",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var listed = Assert.Single((await GetAsync("2026-10")).EnumerateArray());
        foreach (var expense in new[] { created, listed })
        {
            Assert.Equal("2026-10", expense.GetProperty("period").GetString());
            Assert.Equal("oneOff", expense.GetProperty("kind").GetString());
            Assert.Equal("Regalo", expense.GetProperty("name").GetString());
            Assert.Equal("Cumpleaños", expense.GetProperty("description").GetString());
            Assert.Equal(15000m, expense.GetProperty("amount").GetDecimal());
            Assert.Equal("2026-10-20", expense.GetProperty("dueDate").GetString());
            Assert.Equal(JsonValueKind.Null, expense.GetProperty("paidOn").ValueKind);
        }
    }

    [Theory]
    [InlineData(null, 1000, "name")]
    [InlineData("   ", 1000, "name")]
    [InlineData("Luz", null, "amount")]
    public async Task Rechaza_un_alta_sin_nombre_o_sin_monto(string? name, int? amount, string field)
    {
        // AC-03
        var response = await PostAsync("2026-10", new { name, amount, kind = "oneOff" });

        Assert.True((await ErrorsAsync(response)).TryGetProperty(field, out _));
        Assert.Equal(0, await ExpenseCountAsync());
    }

    [Fact]
    public async Task Asigna_el_gasto_al_mes_visualizado_y_no_al_actual()
    {
        // AC-04
        await PostAsync("2026-12", new { name = "Seguro", amount = 5000, kind = "oneOff" });

        Assert.Single((await GetAsync("2026-12")).EnumerateArray());
        Assert.Empty((await GetAsync("2026-10")).EnumerateArray());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-100")]
    [InlineData("10.999")]
    public async Task Rechaza_montos_no_positivos_o_con_mas_de_2_decimales(string amount)
    {
        // AC-05
        var response = await PostAsync("2026-10", new { name = "Luz", amount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), kind = "oneOff" });

        Assert.True((await ErrorsAsync(response)).TryGetProperty("amount", out _));
        Assert.Equal(0, await ExpenseCountAsync());
    }

    [Fact]
    public async Task Registra_montos_con_decimales()
    {
        // AC-06: el formato 1.234,56 lo convierte el frontend (paso 7).
        await PostAsync("2026-10", new { name = "Luz", amount = 1234.56m, kind = "oneOff" });

        await using var db = fixture.CreateContext();
        Assert.Equal(1234.56m, (await db.Expenses.SingleAsync()).Amount);
    }

    [Fact]
    public async Task Acepta_un_vencimiento_en_un_mes_posterior()
    {
        // AC-07
        var response = await PostAsync("2026-10", new { name = "Tarjeta", amount = 1000, dueDate = "2026-11-05", kind = "oneOff" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var expense = Assert.Single((await GetAsync("2026-10")).EnumerateArray());
        Assert.Equal("2026-11-05", expense.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Rechaza_un_vencimiento_de_un_mes_anterior()
    {
        // AC-08
        var response = await PostAsync("2026-10", new { name = "Tarjeta", amount = 1000, dueDate = "2026-09-30", kind = "oneOff" });

        Assert.True((await ErrorsAsync(response)).TryGetProperty("dueDate", out _));
        Assert.Equal(0, await ExpenseCountAsync());
    }

    [Fact]
    public async Task Rechaza_un_alta_sin_tipo()
    {
        var response = await PostAsync("2026-10", new { name = "Luz", amount = 1000 });

        Assert.True((await ErrorsAsync(response)).TryGetProperty("kind", out _));
    }

    [Fact]
    public async Task Informa_todos_los_errores_juntos()
    {
        var response = await PostAsync("2026-10", new { name = "", amount = 0, dueDate = "2026-01-01" });

        var errors = await ErrorsAsync(response);
        Assert.Equal(["amount", "dueDate", "kind", "name"], errors.EnumerateObject().Select(p => p.Name).Order());
    }

    [Fact]
    public async Task Guarda_el_nombre_sin_espacios_y_la_descripcion_vacia_como_nula()
    {
        await PostAsync("2026-10", new { name = "  Luz  ", description = "   ", amount = 1000, kind = "oneOff" });

        var expense = Assert.Single((await GetAsync("2026-10")).EnumerateArray());
        Assert.Equal("Luz", expense.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, expense.GetProperty("description").ValueKind);
    }

    [Fact]
    public async Task Proyecta_el_recurrente_creado_en_los_meses_siguientes()
    {
        await PostAsync("2027-01", new { name = "Cuota", amount = 1000, dueDate = "2027-01-30", kind = "recurring" });

        var february = Assert.Single((await GetAsync("2027-02")).EnumerateArray());
        Assert.Equal("2027-02-28", february.GetProperty("dueDate").GetString());
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("octubre")]
    public async Task Rechaza_un_periodo_invalido(string period)
    {
        var get = await Client.GetAsync($"/api/periods/{period}/expenses");
        var post = await PostAsync(period, new { name = "Luz", amount = 1000, kind = "oneOff" });

        Assert.Equal(HttpStatusCode.BadRequest, get.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
        Assert.Equal(0, await ExpenseCountAsync());
    }

    [Fact]
    public async Task Rechaza_un_tipo_desconocido()
    {
        var response = await PostAsync("2026-10", new { name = "Luz", amount = 1000, kind = "mensual" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ExpenseCountAsync());
    }
}
