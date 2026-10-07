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
        app.MapGet("/api/alerts/due-today", async (DueTodayExpenses dueToday, AppDbContext db, IClock clock, CancellationToken cancellationToken) =>
        {
            var today = clock.Today;
            var expenses = await dueToday.ForAsync(today, cancellationToken);
            var categoryNames = await CategoryNamesAsync(db, cancellationToken);
            return TypedResults.Ok(new DueTodayAlertResponse(
                today, expenses.Select(e => ExpenseResponse.From(e, today, categoryNames)).ToList()));
        });

        var period = app.MapGroup("/api/periods/{period}/expenses");

        period.MapGet("/", async (Period period, MonthlyExpenses monthlyExpenses, AppDbContext db, IClock clock, CancellationToken cancellationToken) =>
        {
            var today = clock.Today;
            var expenses = await monthlyExpenses.ForPeriodAsync(period, cancellationToken);
            var categoryNames = await CategoryNamesAsync(db, cancellationToken);
            return TypedResults.Ok(expenses.Select(e => ExpenseResponse.From(e, today, categoryNames)).ToList());
        });

        // RF-02: el gasto se asigna al mes que el usuario está visualizando.
        period.MapPost("/", async (Period period, CreateExpenseRequest request, AppDbContext db, IClock clock, CancellationToken cancellationToken) =>
        {
            var errors = ExpenseRules.ValidateCreate(request, period);
            await ValidateCategoryAsync(request.CategoryId, db, errors, cancellationToken);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var expense = ExpenseRules.CreateExpense(request, period);
            db.Expenses.Add(expense);
            await db.SaveChangesAsync(cancellationToken);

            var created = await ResponseAsync(expense, ownMonth: null, period, db, clock, cancellationToken);
            return Results.Created($"/api/periods/{period}/expenses/{expense.Id}", created);
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
            await ValidateCategoryAsync(request.CategoryId, db, errors, cancellationToken);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var ownMonth = expense.Months.SingleOrDefault();
            ExpenseRules.ApplyUpdate(expense, ownMonth, request);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(await ResponseAsync(expense, ownMonth, period, db, clock, cancellationToken));
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

            return Results.Ok(await ResponseAsync(expense, ownMonth, period, db, clock, cancellationToken));
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
            return Results.Ok(await ResponseAsync(expense, ownMonth, period, db, clock, cancellationToken));
        });

        return app;
    }

    private static async Task<ExpenseResponse> ResponseAsync(
        Expense expense, ExpenseMonth? ownMonth, Period period, AppDbContext db, IClock clock, CancellationToken cancellationToken)
    {
        var categoryNames = await CategoryNamesAsync(db, cancellationToken);
        return ExpenseResponse.From(ExpenseProjection.Project(expense, ownMonth, period)!, clock.Today, categoryNames);
    }

    /// <summary>
    /// Los nombres actuales de todas las categorías. Son pocas, y así cada gasto
    /// muestra el nombre vigente sin importar de qué mes sea (RF-20).
    /// </summary>
    private static Task<Dictionary<int, string>> CategoryNamesAsync(AppDbContext db, CancellationToken cancellationToken) =>
        db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

    /// <summary>RF-22: la categoría es opcional, pero si se indica tiene que existir.</summary>
    private static async Task ValidateCategoryAsync(
        int? categoryId, AppDbContext db, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        if (categoryId is { } id && !await db.Categories.AnyAsync(c => c.Id == id, cancellationToken))
        {
            errors["categoryId"] = ["La categoría elegida no existe."];
        }
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
