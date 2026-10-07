using GestorGastos.Time;

namespace GestorGastos.Expenses;

/// <summary>Reglas para marcar y desmarcar el pago de un gasto en un mes (RF-27 a RF-31).</summary>
public static class ExpensePayments
{
    /// <summary>Pendiente con un vencimiento que ya pasó (RF-33).</summary>
    public static bool IsOverdue(MonthlyExpense expense, DateOnly today) =>
        DueStatuses.Of(expense, today) == DueStatus.Overdue;

    /// <summary>
    /// La fecha de pago a registrar, o los errores por campo si la pedida no
    /// es válida.
    /// </summary>
    public static (DateOnly? PaidOn, Dictionary<string, string[]> Errors) ResolvePaidOn(
        MonthlyExpense expense, DateOnly? requested, DateOnly today)
    {
        var errors = new Dictionary<string, string[]>();

        if (requested is not { } paidOn || paidOn == today)
        {
            return (today, errors);
        }

        if (paidOn > today)
        {
            errors["paidOn"] = ["La fecha de pago no puede ser posterior a hoy."];
        }
        else if (!IsOverdue(expense, today))
        {
            errors["paidOn"] = ["Solo se puede indicar otra fecha de pago si el gasto está vencido."];
        }

        return errors.Count > 0 ? (null, errors) : (paidOn, errors);
    }

    /// <summary>
    /// Registra el pago en la instancia del mes, creándola con los valores que
    /// se ven en ese mes si todavía no existe (RNF-06).
    /// </summary>
    public static ExpenseMonth MarkAsPaid(Expense expense, ExpenseMonth? ownMonth, MonthlyExpense shown, DateOnly paidOn)
    {
        ownMonth ??= CreateOwnMonth(expense, shown);
        ownMonth.PaidOn = paidOn;
        return ownMonth;
    }

    /// <summary>
    /// Deja el gasto pendiente y sin fecha de pago (RF-31). Devuelve true si la
    /// instancia ya no guarda nada propio del mes y se puede eliminar (RNF-05).
    /// </summary>
    public static bool MarkAsPending(Expense expense, ExpenseMonth ownMonth, Period period)
    {
        ownMonth.PaidOn = null;

        var projected = ExpenseProjection.Project(expense, ownMonth: null, period)!;
        return ownMonth.Name == projected.Name
            && ownMonth.Description == projected.Description
            && ownMonth.Amount == projected.Amount
            && ownMonth.DueDate == projected.DueDate;
    }

    private static ExpenseMonth CreateOwnMonth(Expense expense, MonthlyExpense shown)
    {
        var ownMonth = new ExpenseMonth
        {
            Expense = expense,
            Period = shown.Period,
            Name = shown.Name,
            Description = shown.Description,
            Amount = shown.Amount,
            DueDate = shown.DueDate,
        };
        expense.Months.Add(ownMonth);
        return ownMonth;
    }
}
