using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GestorGastos.Tests.Data;

/// <summary>La API real, apuntando a la base de tests y con un reloj controlable.</summary>
public sealed class ApiFactory(DatabaseFixture database) : WebApplicationFactory<Program>
{
    /// <summary>Por defecto, el 05/10/2026 a las 12:00 en Buenos Aires.</summary>
    public SettableTimeProvider Time { get; } = new(DateTimeOffset.Parse("2026-10-05T15:00:00Z"));

    /// <summary>Fija la hora actual a las 12:00 de <paramref name="today"/> en Buenos Aires.</summary>
    public void SetToday(DateOnly today) =>
        Time.UtcNow = new DateTimeOffset(today.ToDateTime(new TimeOnly(15, 0)), TimeSpan.Zero);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Fuera de Development: la fixture ya migró la base de tests.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", database.ConnectionString);
        builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Time)));
    }
}

/// <summary>
/// Un reloj fijo que se puede mover a cualquier fecha, también hacia atrás
/// (FakeTimeProvider no lo permite).
/// </summary>
public sealed class SettableTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
