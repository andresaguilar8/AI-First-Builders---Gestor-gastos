namespace GestorGastos.Expenses;

/// <summary>Situación de un gasto respecto de su vencimiento (RF-33).</summary>
public enum DueStatus
{
    /// <summary>Ya está pagado: los demás estados aplican solo a los pendientes.</summary>
    Paid,

    /// <summary>Faltan más de 3 días para el vencimiento, o no tiene vencimiento.</summary>
    UpToDate,

    /// <summary>Faltan entre 1 y 3 días.</summary>
    DueSoon,

    /// <summary>Vence hoy.</summary>
    DueToday,

    /// <summary>El vencimiento ya pasó.</summary>
    Overdue,
}

public static class DueStatuses
{
    public const int DueSoonDays = 3;

    /// <summary>
    /// El estado de <paramref name="expense"/> visto el día <paramref name="today"/>,
    /// que debe ser la fecha actual de Buenos Aires (RNF-07).
    /// </summary>
    public static DueStatus Of(MonthlyExpense expense, DateOnly today)
    {
        if (expense.PaidOn is not null)
        {
            return DueStatus.Paid;
        }

        if (expense.DueDate is not { } dueDate)
        {
            return DueStatus.UpToDate;
        }

        var daysLeft = dueDate.DayNumber - today.DayNumber;
        return daysLeft switch
        {
            < 0 => DueStatus.Overdue,
            0 => DueStatus.DueToday,
            <= DueSoonDays => DueStatus.DueSoon,
            _ => DueStatus.UpToDate,
        };
    }
}
