using GestorGastos.Categories;
using GestorGastos.Expenses;
using GestorGastos.Time;

namespace GestorGastos.Summary;

/// <summary>El total de una categoría en el mes. Category null es el grupo "Sin categoría" (RF-24).</summary>
public sealed record CategoryTotal(CategoryRef? Category, decimal Total, int Count);

/// <summary>
/// El resumen de un mes: total (RF-25), pendiente (RF-26) y total por categoría,
/// sumando pagados y pendientes (RF-23).
/// </summary>
public sealed record MonthlySummary(Period Period, decimal Total, decimal Pending, IReadOnlyList<CategoryTotal> ByCategory)
{
    /// <param name="expenses">Los gastos que se ven en el mes, ya proyectados.</param>
    /// <param name="categoryNames">Los nombres actuales de las categorías, por id.</param>
    public static MonthlySummary From(Period period, IReadOnlyList<MonthlyExpense> expenses, IReadOnlyDictionary<int, string> categoryNames)
    {
        var byCategory = expenses
            .GroupBy(e => e.CategoryId is { } id && categoryNames.ContainsKey(id) ? id : (int?)null)
            .Select(group => new CategoryTotal(
                group.Key is { } id ? new CategoryRef(id, categoryNames[id]) : null,
                group.Sum(e => e.Amount),
                group.Count()))
            .OrderByDescending(c => c.Total)
            .ThenBy(c => c.Category is null)
            .ThenBy(c => c.Category?.Name ?? CategoryNames.Uncategorized, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new MonthlySummary(
            period,
            Total: expenses.Sum(e => e.Amount),
            Pending: expenses.Where(e => e.PaidOn is null).Sum(e => e.Amount),
            byCategory);
    }
}
