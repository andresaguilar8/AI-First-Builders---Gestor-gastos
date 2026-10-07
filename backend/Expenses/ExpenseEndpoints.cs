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
        // RF-32: lo consulta la app cada vez que se abre o se recarga.
        app.MapGet("/api/alerts/due-today", async (DueTodayExpenses dueToday, IClock clock, CancellationToken cancellationToken) =>
        {
            var today = clock.Today;
            var expenses = await dueToday.ForAsync(today, cancellationToken);
            return TypedResults.Ok(new DueTodayAlertResponse(today, expenses.Select(e => ExpenseResponse.From(e, today)).ToList()));
        });

        var period = app.MapGroup("/api/periods/{period}/expenses");

        period.MapGet("/", async (Period period, MonthlyExpenses monthlyExpenses, IClock clock, CancellationToken cancellationToken) =>
        {
            var today = clock.Today;
            var expenses = await monthlyExpenses.ForPeriodAsync(period, cancellationToken);
            return TypedResults.Ok(expenses.Select(e => ExpenseResponse.From(e, today)).ToList());
        });

        // RF-02: el gasto se asigna al mes que el usuario está visualizando.
        period.MapPost("/", async (Period period, CreateExpenseRequest request, AppDbContext db, IClock clock, CancellationToken cancellationToken) =>
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
            return Results.Created($"/api/periods/{period}/expenses/{expense.Id}", ExpenseResponse.From(created, clock.Today));
        });

        // RF-09: por ahora solo gastos puntuales.
        period.MapPut("/{expenseId:int}", async (Period period, int expenseId, UpdateExpenseRequest request, AppDbContext db, IClock clock, CancellationToken cancellationToken) =>
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

            return Results.Ok(Response(expense, ownMonth, period, clock));
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

        // RF-27 a RF-30: marcar como pagado en el mes visualizado.
        period.MapPut("/{expenseId:int}/payment", async (Period period, int expenseId, MarkAsPaidRequest request, AppDbContext db, IClock clock, CancellationToken cancellationToken) =>
        {
            var expense = await FindInPeriodAsync(db, expenseId, period, cancellationToken);
            if (expense is null)
            {
                return Results.NotFound();
            }

            var ownMonth = expense.Months.SingleOrDefault();
            var shown = ExpenseProjection.Project(expense, ownMonth, period)!;
            if (shown.PaidOn is not null)
            {
                return Results.Problem("El gasto ya está pagado en este mes.", statusCode: StatusCodes.Status409Conflict);
            }

            var (paidOn, errors) = ExpensePayments.ResolvePaidOn(shown, request.PaidOn, clock.Today);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            ownMonth = ExpensePayments.MarkAsPaid(expense, ownMonth, shown, paidOn!.Value);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(Response(expense, ownMonth, period, clock));
        });

        // RF-31: desmarcar el pago; el gasto vuelve a quedar pendiente y sin fecha de pago.
        period.MapDelete("/{expenseId:int}/payment", async (Period period, int expenseId, AppDbContext db, IClock clock, CancellationToken cancellationToken) =>
        {
            var expense = await FindInPeriodAsync(db, expenseId, period, cancellationToken);
            if (expense is null)
            {
                return Results.NotFound();
            }

            var ownMonth = expense.Months.SingleOrDefault();
            if (ownMonth?.PaidOn is not null && ExpensePayments.MarkAsPending(expense, ownMonth, period))
            {
                db.ExpenseMonths.Remove(ownMonth);
                ownMonth = null;
            }

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(Response(expense, ownMonth, period, clock));
        });

        return app;
    }

    private static ExpenseResponse Response(Expense expense, ExpenseMonth? ownMonth, Period period, IClock clock) =>
        ExpenseResponse.From(ExpenseProjection.Project(expense, ownMonth, period)!, clock.Today);

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
