using GestorGastos.Data;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Expenses;

public static class ExpenseEndpoints
{
    private const string RecurringNotSupported =
        "Los gastos recurrentes todavía no se pueden editar ni eliminar.";

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
            return Results.Created($"/api/periods/{period}/expenses/{expense.Id}", created);
        });

        // RF-09: por ahora solo gastos puntuales.
        period.MapPut("/{expenseId:int}", async (Period period, int expenseId, UpdateExpenseRequest request, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var expense = await FindInPeriodAsync(db, expenseId, period, cancellationToken);
            if (expense is null)
            {
                return Results.NotFound();
            }

            if (expense.Kind != ExpenseKind.OneOff)
            {
                return Results.Problem(RecurringNotSupported, statusCode: StatusCodes.Status409Conflict);
            }

            var errors = ExpenseRules.ValidateUpdate(request, period);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var ownMonth = expense.Months.SingleOrDefault();
            ExpenseRules.ApplyUpdate(expense, ownMonth, request);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(ExpenseProjection.Project(expense, ownMonth, period));
        });

        // RF-16: eliminar un gasto puntual. Sus registros del mes se borran en cascada.
        period.MapDelete("/{expenseId:int}", async (Period period, int expenseId, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var expense = await FindInPeriodAsync(db, expenseId, period, cancellationToken);
            if (expense is null)
            {
                return Results.NotFound();
            }

            if (expense.Kind != ExpenseKind.OneOff)
            {
                return Results.Problem(RecurringNotSupported, statusCode: StatusCodes.Status409Conflict);
            }

            db.Expenses.Remove(expense);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });

        return app;
    }

    /// <summary>
    /// El gasto, con su instancia de <paramref name="period"/> si la tiene, o null
    /// si no existe o no aparece en ese mes.
    /// </summary>
    private static async Task<Expense?> FindInPeriodAsync(
        AppDbContext db, int expenseId, Period period, CancellationToken cancellationToken)
    {
        var expense = await db.Expenses
            .Include(e => e.Months.Where(m => m.Period == period))
            .SingleOrDefaultAsync(e => e.Id == expenseId, cancellationToken);

        return expense is not null && ExpenseProjection.AppearsIn(expense, period) ? expense : null;
    }
}
