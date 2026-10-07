namespace GestorGastos.Categories;

/// <summary>Una categoría de gastos (RF-18).</summary>
public class Category
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// <see cref="Name"/> sin mayúsculas, acentos ni espacios de más. Tiene un
    /// índice único: así la base rechaza nombres repetidos (RF-19).
    /// </summary>
    public required string NormalizedName { get; set; }
}
