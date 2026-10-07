using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GestorGastos.Categories;

/// <summary>Reglas para los nombres de categoría (RF-19).</summary>
public static partial class CategoryNames
{
    public const int MaxLength = 100;

    /// <summary>El grupo de los gastos sin categoría. No puede usarse como nombre.</summary>
    public const string Uncategorized = "Sin categoría";

    /// <summary>Saca los espacios de los costados y deja uno solo entre palabras.</summary>
    public static string Clean(string name) => ExtraSpaces().Replace(name.Trim(), " ");

    /// <summary>
    /// La forma de comparar nombres: sin distinguir mayúsculas, minúsculas ni
    /// acentos. "EDUCACIÓN", "educacion" y "Educación" dan lo mismo. La ñ se
    /// conserva: es una letra, no una n con acento.
    /// </summary>
    public static string Normalize(string name)
    {
        const char CombiningTilde = '\u0303';

        var decomposed = Clean(name).Normalize(NormalizationForm.FormD);
        var withoutAccents = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            var isAccent = CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark;
            var isEnie = character == CombiningTilde && withoutAccents.Length > 0 && withoutAccents[^1] is 'n' or 'N';
            if (!isAccent || isEnie)
            {
                withoutAccents.Append(character);
            }
        }

        return withoutAccents.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    public static bool IsReserved(string name) => Normalize(name) == Normalize(Uncategorized);

    [GeneratedRegex(@"\s+")]
    private static partial Regex ExtraSpaces();
}
