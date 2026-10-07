using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestorGastos.Expenses;
using GestorGastos.Tests.Data;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Tests.Expenses;

[Collection(DatabaseCollection.Name)]
public sealed class ExpensePaymentEndpointsTests(DatabaseFixture fixture) : IAsyncLifetime, IDisposable
{
    private readonly ApiFactory factory = new(fixture);

    private HttpClient Client => field ??= factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => factory.Dispose();

    private async Task<Expense> AddAsync(
        ExpenseKind kind, Period start, DateOnly? dueDate, decimal amount = 20_000m, Action<Expense>? configure = null)
    {
        var expense = new Expense
        {
            Name = "Cuota",
            Amount = amount,
            DueDate = dueDate,
            Kind = kind,
            StartPeriod = start,
            EndPeriod = kind == ExpenseKind.OneOff ? start : null,
        };
        configure?.Invoke(expense);

        await using var db = fixture.CreateContext();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return expense;
    }

    private Task<HttpResponseMessage> PayAsync(string period, int id, string? paidOn = null) =>
        Client.PutAsJsonAsync($"/api/periods/{period}/expenses/{id}/payment", new { paidOn });

    private Task<HttpResponseMessage> UnpayAsync(string period, int id) =>
        Client.DeleteAsync($"/api/periods/{period}/expenses/{id}/payment");

    private async Task<JsonElement> ShownAsync(string period, int id)
    {
        var list = await Client.GetFromJsonAsync<JsonElement>($"/api/periods/{period}/expenses");
        return list.EnumerateArray().Single(e => e.GetProperty("expenseId").GetInt32() == id);
    }

    private static string? PaidOn(JsonElement expense) => expense.GetProperty("paidOn").GetString();

    private async Task<List<ExpenseMonth>> OwnMonthsAsync(int id)
    {
        await using var db = fixture.CreateContext();
        return await db.ExpenseMonths.Where(m => m.ExpenseId == id).ToListAsync();
    }

    [Fact]
    public async Task Pagar_un_recurrente_solo_afecta_al_mes_visualizado()
    {
        // AC-24
        factory.SetToday(new DateOnly(2026, 9, 10));
        var expense = await AddAsync(ExpenseKind.Recurring, new Period(2026, 9), new DateOnly(2026, 9, 15));

        var response = await PayAsync("2026-09", expense.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2026-09-10", PaidOn(await ShownAsync("2026-09", expense.Id)));
        var october = await ShownAsync("2026-10", expense.Id);
        Assert.Null(PaidOn(october));
        Assert.Equal(20_000m, october.GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task Un_gasto_no_vencido_se_paga_con_la_fecha_actual()
    {
        // AC-35
        factory.SetToday(new DateOnly(2026, 10, 5));
        var expense = await AddAsync(ExpenseKind.OneOff, new Period(2026, 10), new DateOnly(2026, 10, 10));

        var response = await PayAsync("2026-10", expense.Id);

        var paid = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-10-05", PaidOn(paid));
    }

    [Fact]
    public async Task Un_gasto_no_vencido_no_acepta_otra_fecha_de_pago()
    {
        factory.SetToday(new DateOnly(2026, 10, 5));
        var expense = await AddAsync(ExpenseKind.OneOff, new Period(2026, 10), new DateOnly(2026, 10, 10));

        var response = await PayAsync("2026-10", expense.Id, "2026-10-03");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(PaidOn(await ShownAsync("2026-10", expense.Id)));
    }

    [Fact]
    public async Task Un_gasto_vencido_acepta_la_fecha_real_de_pago()
    {
        // AC-36
        factory.SetToday(new DateOnly(2026, 10, 5));
        var expense = await AddAsync(ExpenseKind.OneOff, new Period(2026, 10), new DateOnly(2026, 10, 1));

        await PayAsync("2026-10", expense.Id, "2026-10-03");

        Assert.Equal("2026-10-03", PaidOn(await ShownAsync("2026-10", expense.Id)));
    }

    [Fact]
    public async Task Un_gasto_vencido_sin_fecha_se_paga_con_la_fecha_actual()
    {
        // AC-37
        factory.SetToday(new DateOnly(2026, 10, 5));
        var expense = await AddAsync(ExpenseKind.OneOff, new Period(2026, 10), new DateOnly(2026, 10, 1));

        await PayAsync("2026-10", expense.Id);

        Assert.Equal("2026-10-05", PaidOn(await ShownAsync("2026-10", expense.Id)));
    }

    [Fact]
    public async Task Rechaza_una_fecha_de_pago_futura_y_el_gasto_sigue_pendiente()
    {
        // AC-38
        factory.SetToday(new DateOnly(2026, 10, 5));
        var expense = await AddAsync(ExpenseKind.OneOff, new Period(2026, 10), new DateOnly(2026, 10, 1));

        var response = await PayAsync("2026-10", expense.Id, "2026-10-06");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("paidOn", out _));
        Assert.Null(PaidOn(await ShownAsync("2026-10", expense.Id)));
        Assert.Empty(await OwnMonthsAsync(expense.Id));
    }

    [Fact]
    public async Task Usa_la_fecha_de_Buenos_Aires_y_no_la_UTC()
    {
        // RNF-07: 22:00 del 05/10 en Buenos Aires = 01:00 del 06/10 UTC.
        factory.Time.UtcNow = DateTimeOffset.Parse("2026-10-06T01:00:00Z");
        var expense = await AddAsync(ExpenseKind.OneOff, new Period(2026, 10), new DateOnly(2026, 10, 10));

        await PayAsync("2026-10", expense.Id);

        Assert.Equal("2026-10-05", PaidOn(await ShownAsync("2026-10", expense.Id)));
    }

    [Fact]
    public async Task Desmarcar_deja_el_gasto_pendiente_y_sin_fecha_de_pago()
    {
        // AC-39
        factory.SetToday(new DateOnly(2026, 10, 5));
        var expense = await AddAsync(ExpenseKind.OneOff, new Period(2026, 10), new DateOnly(2026, 10, 10));
        await PayAsync("2026-10", expense.Id);

        var response = await UnpayAsync("2026-10", expense.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(PaidOn(await ShownAsync("2026-10", expense.Id)));
    }

    [Fact]
    public async Task Pagar_crea_una_sola_instancia_del_mes_aunque_se_pague_varias_veces()
    {
        // RNF-06
        var expense = await AddAsync(ExpenseKind.Recurring, new Period(2026, 8), new DateOnly(2026, 8, 15));

        await PayAsync("2026-09", expense.Id);
        await UnpayAsync("2026-09", expense.Id);
        await PayAsync("2026-09", expense.Id);

        var month = Assert.Single(await OwnMonthsAsync(expense.Id));
        Assert.Equal(new Period(2026, 9), month.Period);
        Assert.Equal("Cuota", month.Name);
        Assert.Equal(20_000m, month.Amount);
        Assert.Equal(new DateOnly(2026, 9, 15), month.DueDate);
        Assert.Equal(new DateOnly(2026, 10, 5), month.PaidOn);
    }

    [Fact]
    public async Task Desmarcar_un_mes_sin_cambios_propios_no_deja_registros()
    {
        // RNF-05: sin pago ni modificaciones, el mes no guarda nada.
        var expense = await AddAsync(ExpenseKind.Recurring, new Period(2026, 8), new DateOnly(2026, 8, 15));
        await PayAsync("2026-09", expense.Id);

        await UnpayAsync("2026-09", expense.Id);

        Assert.Empty(await OwnMonthsAsync(expense.Id));
    }

    [Fact]
    public async Task Desmarcar_conserva_la_instancia_si_tiene_valores_propios()
    {
        var expense = await AddAsync(ExpenseKind.Recurring, new Period(2026, 8), new DateOnly(2026, 8, 15), configure: e =>
            e.Months.Add(new ExpenseMonth
            {
                Period = new Period(2026, 9),
                Name = "Cuota",
                Amount = 25_000m,
                DueDate = new DateOnly(2026, 9, 15),
                PaidOn = new DateOnly(2026, 9, 14),
            }));

        await UnpayAsync("2026-09", expense.Id);

        var month = Assert.Single(await OwnMonthsAsync(expense.Id));
        Assert.Equal(25_000m, month.Amount);
        Assert.Null(month.PaidOn);
    }

    [Fact]
    public async Task Rechaza_pagar_un_gasto_ya_pagado()
    {
        var expense = await AddAsync(ExpenseKind.OneOff, new Period(2026, 10), dueDate: null);
        await PayAsync("2026-10", expense.Id);

        var response = await PayAsync("2026-10", expense.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Un_gasto_sin_vencimiento_no_esta_vencido_y_se_paga_con_la_fecha_actual()
    {
        var expense = await AddAsync(ExpenseKind.OneOff, new Period(2026, 10), dueDate: null);

        Assert.Equal(HttpStatusCode.BadRequest, (await PayAsync("2026-10", expense.Id, "2026-10-01")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PayAsync("2026-10", expense.Id)).StatusCode);
    }

    [Theory]
    [InlineData("2026-09")]
    [InlineData("2026-11")]
    public async Task Responde_404_si_el_gasto_no_aparece_en_ese_mes(string period)
    {
        var expense = await AddAsync(ExpenseKind.OneOff, new Period(2026, 10), dueDate: null);

        Assert.Equal(HttpStatusCode.NotFound, (await PayAsync(period, expense.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await UnpayAsync(period, expense.Id)).StatusCode);
    }
}
