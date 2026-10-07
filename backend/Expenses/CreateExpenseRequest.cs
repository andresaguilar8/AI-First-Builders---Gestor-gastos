namespace GestorGastos.Expenses;

/// <summary>
/// Alta de un gasto en el mes visualizado. Los campos obligatorios son
/// anulables para poder informar cuáles faltan en lugar de tomar valores por defecto.
/// </summary>
public sealed record CreateExpenseRequest(
    string? Name,
    string? Description,
    decimal? Amount,
    DateOnly? DueDate,
    ExpenseKind? Kind,
    int? CategoryId = null);
