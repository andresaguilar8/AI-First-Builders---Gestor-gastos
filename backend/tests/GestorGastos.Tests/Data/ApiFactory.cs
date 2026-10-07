using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GestorGastos.Tests.Data;

/// <summary>La API real, apuntando a la base de tests.</summary>
public sealed class ApiFactory(DatabaseFixture database) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Fuera de Development: la fixture ya migró la base de tests.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", database.ConnectionString);
    }
}
