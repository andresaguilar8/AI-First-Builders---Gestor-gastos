using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestorGastos.Categories;
using GestorGastos.Expenses;
using GestorGastos.Tests.Data;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Tests.Expenses;

[Collection(DatabaseCollection.Name)]
public sealed class ExpenseCategoryEndpointsTests(DatabaseFixture fixture) : IAsyncLifetime, IDisposable
{
    private readonly ApiFactory factory = new(fixture);

    private HttpClient Client => field ??= factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => factory.Dispose();

    private static readonly Period August = new(2026, 8);
    private static readonly Period October = new(2026, 10);

    private async Task<int> AddCategoryAsync(string name)
    {
        await using var db = fixture.CreateContext();
        var category = new Category { Name = name, NormalizedName = CategoryNames.Normalize(name) };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task<Expense> AddExpenseAsync(string name, ExpenseKind kind, Period start, int? categoryId, Action<Expense>? configure = null)
    {
        var expense = new Expense
        {
            Name = name,
            Amount = 1_000m,
            Kind = kind,
            StartPeriod = start,
            EndPeriod = kind == ExpenseKind.OneOff ? start : null,
            CategoryId = categoryId,
        };
        configure?.Invoke(expense);

        await using var db = fixture.CreateContext();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return expense;
    }

    /// <summary>Nombre de la categoría de cada gasto del mes; null es "Sin categoría".</summary>
    private async Task<Dictionary<string, string?>> CategoriesInAsync(Period period)
    {
        var list = await Client.GetFromJsonAsync<JsonElement>($"/api/periods/{period}/expenses");
        return list.EnumerateArray().ToDictionary(
            e => e.GetProperty("name").GetString()!,
            e => e.GetProperty("category") is { ValueKind: JsonValueKind.Object } category
                ? category.GetProperty("name").GetString()
                : null);
    }

    [Fact]
    public async Task Crea_un_gasto_con_categoria()
    {
        // AC-02, con la categoría "Varios".
        var varios = await AddCategoryAsync("Varios");

        var response = await Client.PostAsJsonAsync("/api/periods/2026-10/expenses", new
        {
            name = "Regalo",
            description = "Cumpleaños",
            amount = 15000,
            dueDate = "2026-10-20",
            kind = "oneOff",
            categoryId = varios,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(varios, created.GetProperty("category").GetProperty("id").GetInt32());
        Assert.Equal("Varios", created.GetProperty("category").GetProperty("name").GetString());
        Assert.Equal("Varios", (await CategoriesInAsync(October))["Regalo"]);
    }

    [Fact]
    public async Task Un_gasto_sin_categoria_devuelve_category_null()
    {
        await Client.PostAsJsonAsync("/api/periods/2026-10/expenses", new { name = "Luz", amount = 1000, kind = "oneOff" });

        Assert.Null((await CategoriesInAsync(October))["Luz"]);
    }

    [Fact]
    public async Task Asocia_una_categoria_a_un_gasto_que_no_tenia()
    {
        // AC-31
        var hogar = await AddCategoryAsync("Hogar");
        var expense = await AddExpenseAsync("Luz", ExpenseKind.OneOff, October, categoryId: null);

        var response = await Client.PutAsJsonAsync($"/api/periods/2026-10/expenses/{expense.Id}",
            new { name = "Luz", amount = 1000, categoryId = hogar });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hogar", (await CategoriesInAsync(October))["Luz"]);
    }

    [Fact]
    public async Task Permite_quitar_la_categoria_de_un_gasto()
    {
        var hogar = await AddCategoryAsync("Hogar");
        var expense = await AddExpenseAsync("Luz", ExpenseKind.OneOff, October, hogar);

        await Client.PutAsJsonAsync($"/api/periods/2026-10/expenses/{expense.Id}", new { name = "Luz", amount = 1000, categoryId = (int?)null });

        Assert.Null((await CategoriesInAsync(October))["Luz"]);
    }

    [Fact]
    public async Task Rechaza_una_categoria_inexistente()
    {
        var create = await Client.PostAsJsonAsync("/api/periods/2026-10/expenses",
            new { name = "Luz", amount = 1000, kind = "oneOff", categoryId = 999 });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        var errors = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("categoryId", out _));
        await using var db = fixture.CreateContext();
        Assert.Equal(0, await db.Expenses.CountAsync());
    }

    [Fact]
    public async Task Renombrar_una_categoria_se_ve_en_todos_los_meses()
    {
        // AC-28: gastos de agosto y octubre, uno con instancia propia en agosto.
        var servicios = await AddCategoryAsync("Servicios");
        await AddExpenseAsync("Internet", ExpenseKind.Recurring, August, servicios, e => e.Months.Add(new ExpenseMonth
        {
            Period = August,
            Name = "Internet",
            Amount = 1_000m,
            CategoryId = servicios,
            PaidOn = new DateOnly(2026, 8, 3),
        }));
        await AddExpenseAsync("Arreglo", ExpenseKind.OneOff, October, servicios);

        await Client.PutAsJsonAsync($"/api/categories/{servicios}", new { name = "Servicios del hogar" });

        Assert.Equal("Servicios del hogar", (await CategoriesInAsync(August))["Internet"]);
        Assert.Equal(
            new Dictionary<string, string?> { ["Internet"] = "Servicios del hogar", ["Arreglo"] = "Servicios del hogar" },
            await CategoriesInAsync(October));
    }

    [Fact]
    public async Task Eliminar_una_categoria_deja_sus_gastos_sin_categoria_en_todos_los_meses()
    {
        // AC-30
        var ocio = await AddCategoryAsync("Ocio");
        var cine = await AddExpenseAsync("Cine", ExpenseKind.Recurring, August, ocio, e => e.Months.Add(new ExpenseMonth
        {
            Period = August,
            Name = "Cine",
            Amount = 1_000m,
            CategoryId = ocio,
            PaidOn = new DateOnly(2026, 8, 3),
        }));
        await AddExpenseAsync("Teatro", ExpenseKind.OneOff, October, ocio);

        var response = await Client.DeleteAsync($"/api/categories/{ocio}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null((await CategoriesInAsync(August))["Cine"]);
        Assert.Equal(new Dictionary<string, string?> { ["Cine"] = null, ["Teatro"] = null }, await CategoriesInAsync(October));
        await using var db = fixture.CreateContext();
        Assert.Null((await db.ExpenseMonths.SingleAsync(m => m.ExpenseId == cine.Id)).CategoryId);
    }

    [Fact]
    public async Task Pagar_copia_la_categoria_a_la_instancia_del_mes()
    {
        var hogar = await AddCategoryAsync("Hogar");
        var expense = await AddExpenseAsync("Alquiler", ExpenseKind.Recurring, August, hogar);

        await Client.PutAsJsonAsync($"/api/periods/2026-09/expenses/{expense.Id}/payment", new { paidOn = (string?)null });

        await using var db = fixture.CreateContext();
        Assert.Equal(hogar, (await db.ExpenseMonths.SingleAsync()).CategoryId);
        Assert.Equal("Hogar", (await CategoriesInAsync(new Period(2026, 9)))["Alquiler"]);
    }

    [Fact]
    public async Task Desmarcar_borra_la_instancia_si_su_categoria_no_cambio()
    {
        var hogar = await AddCategoryAsync("Hogar");
        var expense = await AddExpenseAsync("Alquiler", ExpenseKind.Recurring, August, hogar);
        await Client.PutAsJsonAsync($"/api/periods/2026-09/expenses/{expense.Id}/payment", new { paidOn = (string?)null });

        await Client.DeleteAsync($"/api/periods/2026-09/expenses/{expense.Id}/payment");

        await using var db = fixture.CreateContext();
        Assert.Equal(0, await db.ExpenseMonths.CountAsync());
    }
}
