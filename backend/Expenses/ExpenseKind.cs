namespace GestorGastos.Expenses;

public enum ExpenseKind
{
    /// <summary>Pertenece solo al mes en que se registró (RF-08).</summary>
    OneOff,

    /// <summary>Se proyecta a los meses siguientes mientras esté activo (RF-06).</summary>
    Recurring,
}
