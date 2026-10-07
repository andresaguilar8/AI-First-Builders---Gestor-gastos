using GestorGastos.Data;
using GestorGastos.Time;

namespace GestorGastos.Expenses;

public static class ExpenseEndpoints
{
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var period = app.MapGroup("/api/periods/{period}/expenses");

        period.MapGet("/", async (Period period, MonthlyExpenses monthlyExpenses, CancellationToken cancellationToken) =>
            TypedResults.Ok(await monthlyExpenses.ForPeriodAsync(period, cancellationToken)));

        // RF-02: el gasto se asigna al mes que el usuario está visualizando.
        period.MapPost("/", async (Period period, CreateExpenseRequest request, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var errors = ExpenseRules.ValidateCreate(request, period);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var expense = ExpenseRules.CreateExpense(request, period);
            db.Expenses.Add(expense);
            await db.SaveChangesAsync(cancellationToken);

            var created = ExpenseProjection.Project(expense, ownMonth: null, period)!;
            return Results.Created($"/api/periods/{period}/expenses", created);
        });

        return app;
    }
}
