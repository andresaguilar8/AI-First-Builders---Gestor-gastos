using GestorGastos.Time;

namespace GestorGastos.Expenses;

/// <summary>
/// Un gasto como serie: sus valores base y el rango de meses en que aparece.
/// Un gasto recurrente no tiene un registro por mes: se proyecta al consultar
/// cada período (RNF-05). Los meses con información propia (un pago o una
/// modificación) se guardan como <see cref="ExpenseMonth"/>.
/// </summary>
public class Expense
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Monto en ARS, mayor a 0 y con hasta 2 decimales (RF-03).</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Vencimiento en <see cref="StartPeriod"/>. Puede caer en ese mes o en uno
    /// posterior (RF-05). En los meses proyectados se usa su día del mes (RF-07).
    /// </summary>
    public DateOnly? DueDate { get; set; }

    public ExpenseKind Kind { get; set; }

    /// <summary>
    /// Categoría opcional (RF-22). Se guarda solo el id: renombrar la categoría
    /// se ve en todos los meses (RF-20) y eliminarla la deja en null (RF-21).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>Mes en el que se registró el gasto (RF-02).</summary>
    public Period StartPeriod { get; set; }

    /// <summary>
    /// Último mes, inclusive, en que aparece el gasto. Null mientras un recurrente
    /// siga activo. Un puntual siempre termina en su <see cref="StartPeriod"/>.
    /// </summary>
    public Period? EndPeriod { get; set; }

    public List<ExpenseMonth> Months { get; } = [];
}
