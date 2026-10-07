using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestorGastos.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Tests.Categories;

[Collection(DatabaseCollection.Name)]
public sealed class CategoryEndpointsTests(DatabaseFixture fixture) : IAsyncLifetime, IDisposable
{
    private readonly ApiFactory factory = new(fixture);

    private HttpClient Client => field ??= factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => factory.Dispose();

    private Task<HttpResponseMessage> CreateAsync(string? name) =>
        Client.PostAsJsonAsync("/api/categories", new { name });

    private Task<HttpResponseMessage> RenameAsync(int id, string? name) =>
        Client.PutAsJsonAsync($"/api/categories/{id}", new { name });

    private async Task<int> CreateIdAsync(string name)
    {
        var response = await CreateAsync(name);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<List<string>> NamesAsync()
    {
        var list = await Client.GetFromJsonAsync<JsonElement>("/api/categories");
        return list.EnumerateArray().Select(c => c.GetProperty("name").GetString()!).ToList();
    }

    private static async Task<string> NameErrorAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("errors").GetProperty("name")[0].GetString()!;
    }

    [Fact]
    public async Task Crea_una_categoria_y_queda_disponible()
    {
        // AC-25
        var response = await CreateAsync("Educación");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Educación", created.GetProperty("name").GetString());
        Assert.Equal(["Educación"], await NamesAsync());
    }

    [Fact]
    public async Task Lista_las_categorias_en_orden_alfabetico()
    {
        await CreateIdAsync("Vivienda");
        await CreateIdAsync("educación");
        await CreateIdAsync("Ocio");
        await CreateIdAsync("Ésta");

        Assert.Equal(["educación", "Ésta", "Ocio", "Vivienda"], await NamesAsync());
    }

    [Theory]
    [InlineData("Educación")]
    [InlineData("educacion")]
    [InlineData("EDUCACIÓN")]
    [InlineData("  educación ")]
    public async Task Rechaza_crear_una_categoria_repetida(string name)
    {
        // AC-26
        await CreateIdAsync("Educación");

        var error = await NameErrorAsync(await CreateAsync(name));

        Assert.Equal("Ya existe una categoría con ese nombre.", error);
        Assert.Equal(["Educación"], await NamesAsync());
    }

    [Theory]
    [InlineData("Educación")]
    [InlineData("educacion")]
    [InlineData("EDUCACIÓN")]
    public async Task Rechaza_renombrar_con_el_nombre_de_otra_categoria(string name)
    {
        // AC-26
        await CreateIdAsync("Educación");
        var otherId = await CreateIdAsync("Cursos");

        await NameErrorAsync(await RenameAsync(otherId, name));

        Assert.Equal(["Cursos", "Educación"], await NamesAsync());
    }

    [Theory]
    [InlineData("Sin categoría")]
    [InlineData("sin categoria")]
    public async Task Rechaza_el_nombre_reservado_al_crear_y_al_renombrar(string name)
    {
        // AC-27
        var id = await CreateIdAsync("Varios");

        Assert.Contains("reservado", await NameErrorAsync(await CreateAsync(name)));
        Assert.Contains("reservado", await NameErrorAsync(await RenameAsync(id, name)));
        Assert.Equal(["Varios"], await NamesAsync());
    }

    [Fact]
    public async Task Renombra_una_categoria()
    {
        // AC-28 (la parte de los gastos se prueba al asociarlos, en el paso 14)
        var id = await CreateIdAsync("Servicios");

        var response = await RenameAsync(id, "Servicios del hogar");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["Servicios del hogar"], await NamesAsync());
    }

    [Fact]
    public async Task Permite_renombrar_una_categoria_cambiando_solo_mayusculas_o_acentos()
    {
        var id = await CreateIdAsync("educacion");

        var response = await RenameAsync(id, "Educación");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["Educación"], await NamesAsync());
    }

    [Fact]
    public async Task Elimina_una_categoria()
    {
        // AC-29
        var id = await CreateIdAsync("Ocio");
        await CreateIdAsync("Varios");

        var response = await Client.DeleteAsync($"/api/categories/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["Varios"], await NamesAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rechaza_un_nombre_vacio(string? name)
    {
        Assert.Equal("El nombre es obligatorio.", await NameErrorAsync(await CreateAsync(name)));
    }

    [Fact]
    public async Task Rechaza_un_nombre_demasiado_largo()
    {
        Assert.Contains("100 caracteres", await NameErrorAsync(await CreateAsync(new string('a', 101))));
    }

    [Fact]
    public async Task Guarda_el_nombre_sin_espacios_de_mas()
    {
        await CreateIdAsync("  Servicios   del hogar ");

        Assert.Equal(["Servicios del hogar"], await NamesAsync());
    }

    [Fact]
    public async Task Responde_404_si_la_categoria_no_existe()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await RenameAsync(999, "Ocio")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/api/categories/999")).StatusCode);
    }

    [Fact]
    public async Task La_base_rechaza_nombres_normalizados_repetidos()
    {
        // El índice único protege aunque dos pedidos validen al mismo tiempo.
        await using var db = fixture.CreateContext();
        db.Categories.Add(new() { Name = "Ocio", NormalizedName = "ocio" });
        db.Categories.Add(new() { Name = "OCIO", NormalizedName = "ocio" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
