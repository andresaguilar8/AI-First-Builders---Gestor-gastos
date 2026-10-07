using GestorGastos.Data;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IClock, BuenosAiresClock>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // En desarrollo la base queda al día con solo correr `dotnet run`.
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.MapGet("/api/health", async (AppDbContext db, IClock clock) =>
{
    var database = await db.Database.CanConnectAsync() ? "ok" : "unreachable";
    return Results.Ok(new { status = "ok", database, today = clock.Today });
});

app.Run();

// Expuesto para tests de integración con WebApplicationFactory.
public partial class Program;
