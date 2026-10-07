namespace GestorGastos.Expenses;

/// <summary>
/// Edición de los valores de un gasto (RF-09). El tipo no se edita acá: pasar
/// de puntual a recurrente o al revés es otra operación (RF-12, RF-13).
/// </summary>
public sealed record UpdateExpenseRequest(
    string? Name,
    string? Description,
    decimal? Amount,
    DateOnly? DueDate,
    int? CategoryId = null);
