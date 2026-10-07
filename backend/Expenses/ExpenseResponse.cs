using GestorGastos.Time;

namespace GestorGastos.Expenses;

/// <summary>Un gasto del mes como lo devuelve la API, con su estado de vencimiento.</summary>
public sealed record ExpenseResponse(
    int ExpenseId,
    Period Period,
    ExpenseKind Kind,
    string Name,
    string? Description,
    decimal Amount,
    DateOnly? DueDate,
    DateOnly? PaidOn,
    DueStatus Status)
{
    public static ExpenseResponse From(MonthlyExpense expense, DateOnly today) => new(
        expense.ExpenseId,
        expense.Period,
        expense.Kind,
        expense.Name,
        expense.Description,
        expense.Amount,
        expense.DueDate,
        expense.PaidOn,
        DueStatuses.Of(expense, today));
}
