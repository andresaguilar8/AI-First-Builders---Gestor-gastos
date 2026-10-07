using GestorGastos.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Tests.Data;

/// <summary>
/// Base de datos real de PostgreSQL para tests de integración. Usa la base
/// gestor_gastos_test del contenedor de docker compose, que se recrea desde las
/// migraciones al iniciar la corrida.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=gestor_gastos_test;Username=gestor;Password=gestor";

    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("GESTOR_GASTOS_TEST_DB") ?? DefaultConnectionString;

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options);

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Vacía las tablas para que cada test arranque de cero.</summary>
    public async Task ResetAsync()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE expense_months, expenses RESTART IDENTITY CASCADE");
    }
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
