using GestorGastos.Data;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Expenses;

/// <summary>
/// Consulta los gastos de un mes. Solo lee: proyectar un recurrente nunca crea
/// registros (RNF-05).
/// </summary>
public class MonthlyExpenses(AppDbContext db)
{
    public async Task<IReadOnlyList<MonthlyExpense>> ForPeriodAsync(Period period, CancellationToken cancellationToken = default)
    {
        var expenses = await db.Expenses
            .AsNoTracking()
            .Where(e => e.StartPeriod <= period && (e.EndPeriod == null || e.EndPeriod >= period))
            .Include(e => e.Months.Where(m => m.Period == period))
            .ToListAsync(cancellationToken);

        return expenses
            .Select(e => ExpenseProjection.Project(e, e.Months.SingleOrDefault(), period))
            .OfType<MonthlyExpense>()
            .OrderBy(e => e.DueDate is null)
            .ThenBy(e => e.DueDate)
            .ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(e => e.ExpenseId)
            .ToList();
    }
}
