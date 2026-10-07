namespace GestorGastos.Expenses;

/// <summary>La alerta de vencimientos de hoy: vacía si no hay ninguno pendiente (RF-32).</summary>
public sealed record DueTodayAlertResponse(DateOnly Date, IReadOnlyList<ExpenseResponse> Expenses);
