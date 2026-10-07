using GestorGastos.Data;
using GestorGastos.Expenses;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Summary;

public static class SummaryEndpoints
{
    public static IEndpointRouteBuilder MapSummaryEndpoints(this IEndpointRouteBuilder app)
    {
        // RF-23 a RF-26: incluye los recurrentes proyectados, igual que la lista del mes.
        app.MapGet("/api/periods/{period}/summary", async (Period period, MonthlyExpenses monthlyExpenses, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var expenses = await monthlyExpenses.ForPeriodAsync(period, cancellationToken);
            var categoryNames = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
            return TypedResults.Ok(MonthlySummary.From(period, expenses, categoryNames));
        });

        return app;
    }
}
