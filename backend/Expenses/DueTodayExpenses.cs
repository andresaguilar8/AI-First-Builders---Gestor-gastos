using GestorGastos.Data;
using GestorGastos.Time;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Expenses;

/// <summary>
/// Los gastos pendientes que vencen hoy, para la alerta al abrir la app (RF-32).
/// </summary>
/// <remarks>
/// No alcanza con mirar el mes actual: en su mes de alta un gasto puede vencer
/// en un mes posterior (RF-05), y una instancia propia de un mes anterior puede
/// tener ese vencimiento. Los meses proyectados de un recurrente siempre vencen
/// dentro de su mes (RF-07), así que de los meses anteriores no aportan nada.
/// </remarks>
public class DueTodayExpenses(AppDbContext db, MonthlyExpenses monthlyExpenses)
{
    public async Task<IReadOnlyList<MonthlyExpense>> ForAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        var current = Period.FromDate(today);
        var candidates = new List<MonthlyExpense>(await monthlyExpenses.ForPeriodAsync(current, cancellationToken));

        var startedEarlier = await db.Expenses
            .AsNoTracking()
            .Where(e => e.DueDate == today && e.StartPeriod < current)
            .Select(e => new { Expense = e, OwnMonth = e.Months.SingleOrDefault(m => m.Period == e.StartPeriod) })
            .ToListAsync(cancellationToken);
        candidates.AddRange(startedEarlier
            .Select(x => ExpenseProjection.Project(x.Expense, x.OwnMonth, x.Expense.StartPeriod))
            .OfType<MonthlyExpense>());

        var earlierMonths = await db.ExpenseMonths
            .AsNoTracking()
            .Where(m => m.DueDate == today && m.Period < current)
            .Include(m => m.Expense)
            .ToListAsync(cancellationToken);
        candidates.AddRange(earlierMonths
            .Select(m => ExpenseProjection.Project(m.Expense, m, m.Period))
            .OfType<MonthlyExpense>());

        return candidates
            .Where(e => e.DueDate == today && e.PaidOn is null)
            .DistinctBy(e => (e.ExpenseId, e.Period))
            .OrderBy(e => e.Period)
            .ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(e => e.ExpenseId)
            .ToList();
    }
}
