namespace GestorGastos.Expenses;

/// <summary>
/// Marcar un gasto como pagado. Sin fecha, se registra la fecha actual (RF-28).
/// Solo un gasto vencido acepta otra fecha (RF-29), que no puede ser futura (RF-30).
/// </summary>
public sealed record MarkAsPaidRequest(DateOnly? PaidOn);
