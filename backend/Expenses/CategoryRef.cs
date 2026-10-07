namespace GestorGastos.Expenses;

/// <summary>La categoría de un gasto en las respuestas. Null significa "Sin categoría".</summary>
public sealed record CategoryRef(int Id, string Name);
