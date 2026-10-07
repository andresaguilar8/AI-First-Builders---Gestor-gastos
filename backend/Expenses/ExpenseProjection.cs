using GestorGastos.Time;

namespace GestorGastos.Expenses;

/// <summary>
/// Reglas para ver un gasto en un mes, sin acceso a la base. Los recurrentes no
/// tienen registros por mes: se proyectan con estas reglas (RNF-05).
/// </summary>
public static class ExpenseProjection
{
    /// <summary>
    /// Un puntual aparece solo en su mes (RF-08). Un recurrente aparece desde su
    /// mes de alta hasta <see cref="Expense.EndPeriod"/>, o sin límite si sigue
    /// activo (RF-06).
    /// </summary>
    public static bool AppearsIn(Expense expense, Period period) =>
        expense.Kind == ExpenseKind.OneOff
            ? period == expense.StartPeriod
            : period >= expense.StartPeriod && (expense.EndPeriod is null || period <= expense.EndPeriod);

    /// <summary>
    /// En el mes de alta, el vencimiento es el indicado (puede caer en un mes
    /// posterior, RF-05). En los demás meses, el mismo día dentro del mes, o el
    /// último día si el mes no lo tiene (RF-07).
    /// </summary>
    public static DateOnly? DueDateIn(Expense expense, Period period) =>
        expense.DueDate is not { } dueDate || period == expense.StartPeriod
            ? expense.DueDate
            : period.DateOnDay(dueDate.Day);

    /// <summary>
    /// El gasto visto en <paramref name="period"/>, o null si no aparece en ese mes.
    /// Si el mes tiene instancia propia, se usan sus valores.
    /// </summary>
    public static MonthlyExpense? Project(Expense expense, ExpenseMonth? ownMonth, Period period)
    {
        if (!AppearsIn(expense, period))
        {
            return null;
        }

        if (ownMonth is not null)
        {
            if (ownMonth.ExpenseId != expense.Id || ownMonth.Period != period)
            {
                throw new ArgumentException("La instancia no corresponde a este gasto y mes.", nameof(ownMonth));
            }

            return new MonthlyExpense(
                expense.Id, period, expense.Kind,
                ownMonth.Name, ownMonth.Description, ownMonth.Amount, ownMonth.DueDate, ownMonth.PaidOn, ownMonth.CategoryId);
        }

        return new MonthlyExpense(
            expense.Id, period, expense.Kind,
            expense.Name, expense.Description, expense.Amount, DueDateIn(expense, period), PaidOn: null, expense.CategoryId);
    }
}
