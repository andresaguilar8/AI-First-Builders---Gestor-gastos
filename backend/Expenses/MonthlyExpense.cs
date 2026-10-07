using GestorGastos.Time;

namespace GestorGastos.Expenses;

/// <summary>
/// Un gasto tal como se ve en un mes: con los valores de su instancia propia
/// si la tiene, o proyectado desde el gasto si no.
/// </summary>
public sealed record MonthlyExpense(
    int ExpenseId,
    Period Period,
    ExpenseKind Kind,
    string Name,
    string? Description,
    decimal Amount,
    DateOnly? DueDate,
    DateOnly? PaidOn);
