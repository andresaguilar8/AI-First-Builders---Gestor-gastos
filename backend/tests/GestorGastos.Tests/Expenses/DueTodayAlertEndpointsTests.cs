using System.Net.Http.Json;
using System.Text.Json;
using GestorGastos.Expenses;
using GestorGastos.Tests.Data;
using GestorGastos.Time;

namespace GestorGastos.Tests.Expenses;

[Collection(DatabaseCollection.Name)]
public sealed class DueTodayAlertEndpointsTests(DatabaseFixture fixture) : IAsyncLifetime, IDisposable
{
    private readonly ApiFactory factory = new(fixture);

    private HttpClient Client => field ??= factory.CreateClient();

    private static readonly DateOnly Today = new(2026, 10, 5);

    public Task InitializeAsync()
    {
        factory.SetToday(Today);
        return fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => factory.Dispose();

    private async Task<Expense> AddAsync(
        string name, ExpenseKind kind, Period start, DateOnly? dueDate, Action<Expense>? configure = null)
    {
        var expense = new Expense
        {
            Name = name,
            Amount = 1_000m,
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

    private async Task<JsonElement> AlertAsync() =>
        await Client.GetFromJsonAsync<JsonElement>("/api/alerts/due-today");

    private static List<string> Names(JsonElement alert) =>
        alert.GetProperty("expenses").EnumerateArray().Select(e => e.GetProperty("name").GetString()!).ToList();

    [Fact]
    public async Task Incluye_los_gastos_pendientes_que_vencen_hoy()
    {
        // AC-40, AC-41: la API responde igual al iniciar sesión o al abrir la app.
        await AddAsync("Luz", ExpenseKind.OneOff, new Period(2026, 10), Today);
        await AddAsync("Gas", ExpenseKind.OneOff, new Period(2026, 10), Today.AddDays(1));

        var alert = await AlertAsync();

        Assert.Equal("2026-10-05", alert.GetProperty("date").GetString());
        Assert.Equal(["Luz"], Names(alert));
        Assert.Equal("dueToday", alert.GetProperty("expenses")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task No_incluye_un_gasto_que_vence_hoy_si_ya_esta_pagado()
    {
        // AC-42
        await AddAsync("Luz", ExpenseKind.OneOff, new Period(2026, 10), Today, e => e.Months.Add(new ExpenseMonth
        {
            Period = new Period(2026, 10),
            Name = "Luz",
            Amount = 1_000m,
            DueDate = Today,
            PaidOn = new DateOnly(2026, 10, 2),
        }));

        Assert.Empty(Names(await AlertAsync()));
    }

    [Fact]
    public async Task Incluye_un_recurrente_proyectado_que_vence_hoy()
    {
        await AddAsync("Alquiler", ExpenseKind.Recurring, new Period(2026, 6), new DateOnly(2026, 6, 5));

        Assert.Equal(["Alquiler"], Names(await AlertAsync()));
    }

    [Fact]
    public async Task Incluye_un_gasto_de_un_mes_anterior_cuyo_vencimiento_es_hoy()
    {
        // RF-05: un gasto de septiembre puede vencer en octubre.
        await AddAsync("Tarjeta", ExpenseKind.OneOff, new Period(2026, 9), Today);

        var alert = await AlertAsync();

        Assert.Equal(["Tarjeta"], Names(alert));
        Assert.Equal("2026-09", alert.GetProperty("expenses")[0].GetProperty("period").GetString());
    }

    [Fact]
    public async Task Incluye_una_instancia_de_un_mes_anterior_con_vencimiento_hoy()
    {
        await AddAsync("Seguro", ExpenseKind.Recurring, new Period(2026, 8), new DateOnly(2026, 8, 20), e => e.Months.Add(new ExpenseMonth
        {
            Period = new Period(2026, 9),
            Name = "Seguro",
            Amount = 1_000m,
            DueDate = Today,
        }));

        var alert = await AlertAsync();

        Assert.Equal(["Seguro"], Names(alert));
        Assert.Equal("2026-09", alert.GetProperty("expenses")[0].GetProperty("period").GetString());
    }

    [Fact]
    public async Task No_incluye_un_mes_de_alta_cuyo_vencimiento_se_cambio_en_su_instancia()
    {
        await AddAsync("Tarjeta", ExpenseKind.OneOff, new Period(2026, 9), Today, e => e.Months.Add(new ExpenseMonth
        {
            Period = new Period(2026, 9),
            Name = "Tarjeta",
            Amount = 1_000m,
            DueDate = new DateOnly(2026, 10, 9),
        }));

        Assert.Empty(Names(await AlertAsync()));
    }

    [Fact]
    public async Task No_incluye_recurrentes_terminados_ni_gastos_de_meses_futuros()
    {
        await AddAsync("Gimnasio", ExpenseKind.Recurring, new Period(2026, 6), new DateOnly(2026, 6, 5), e => e.EndPeriod = new Period(2026, 9));
        await AddAsync("Viaje", ExpenseKind.OneOff, new Period(2026, 11), new DateOnly(2026, 11, 5));

        Assert.Empty(Names(await AlertAsync()));
    }

    [Fact]
    public async Task Usa_la_fecha_de_Buenos_Aires()
    {
        // RNF-07: 22:00 del 05/10 en Buenos Aires = 01:00 del 06/10 UTC.
        factory.Time.UtcNow = DateTimeOffset.Parse("2026-10-06T01:00:00Z");
        await AddAsync("Luz", ExpenseKind.OneOff, new Period(2026, 10), Today);

        Assert.Equal(["Luz"], Names(await AlertAsync()));
    }
}
