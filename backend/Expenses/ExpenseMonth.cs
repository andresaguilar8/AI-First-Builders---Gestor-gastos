using GestorGastos.Time;

namespace GestorGastos.Expenses;

/// <summary>
/// La instancia propia de un gasto en un mes. Solo existe cuando ese mes tiene
/// información específica (un pago o una modificación) y hay como máximo una por
/// gasto y mes (RNF-06).
/// </summary>
/// <remarks>
/// Guarda una copia completa de los valores del mes, no solo los que cambiaron.
/// Así null significa "sin descripción" o "sin vencimiento" en ese mes, y no
/// "heredar del gasto".
/// </remarks>
public class ExpenseMonth
{
    public int Id { get; set; }

    public int ExpenseId { get; set; }

    public Expense Expense { get; set; } = null!;

    public Period Period { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public decimal Amount { get; set; }

    public DateOnly? DueDate { get; set; }

    /// <summary>Fecha de pago. Null mientras el gasto esté pendiente en este mes.</summary>
    public DateOnly? PaidOn { get; set; }
}
