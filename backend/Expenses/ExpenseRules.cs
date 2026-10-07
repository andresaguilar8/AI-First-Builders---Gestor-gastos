using GestorGastos.Time;

namespace GestorGastos.Expenses;

/// <summary>Validaciones de los datos de un gasto (RF-01, RF-03, RF-05).</summary>
public static class ExpenseRules
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    /// <summary>Mayor monto que entra en la columna numeric(14,2).</summary>
    public const decimal MaxAmount = 999_999_999_999.99m;

    /// <summary>
    /// Valida un alta en <paramref name="period"/> y devuelve los errores por campo.
    /// Sin errores, el diccionario queda vacío.
    /// </summary>
    public static Dictionary<string, string[]> ValidateCreate(CreateExpenseRequest request, Period period)
    {
        var errors = new Dictionary<string, string[]>();

        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            errors["name"] = ["El nombre es obligatorio."];
        }
        else if (name.Length > NameMaxLength)
        {
            errors["name"] = [$"El nombre no puede superar los {NameMaxLength} caracteres."];
        }

        if (request.Description?.Trim().Length > DescriptionMaxLength)
        {
            errors["description"] = [$"La descripción no puede superar los {DescriptionMaxLength} caracteres."];
        }

        if (AmountError(request.Amount) is { } amountError)
        {
            errors["amount"] = [amountError];
        }

        if (request.DueDate is { } dueDate && dueDate < period.FirstDay)
        {
            errors["dueDate"] = ["El vencimiento no puede ser de un mes anterior al del gasto."];
        }

        if (request.Kind is null)
        {
            errors["kind"] = ["Indicá si el gasto es puntual o recurrente."];
        }

        return errors;
    }

    private static string? AmountError(decimal? amount) => amount switch
    {
        null => "El monto es obligatorio.",
        <= 0 => "El monto debe ser mayor a 0.",
        > MaxAmount => "El monto es demasiado grande.",
        var value when value != Math.Round(value.Value, 2) => "El monto admite hasta 2 decimales.",
        _ => null,
    };

    /// <summary>Crea el gasto a partir de un alta ya validada.</summary>
    public static Expense CreateExpense(CreateExpenseRequest request, Period period)
    {
        var kind = request.Kind!.Value;
        var description = request.Description?.Trim();

        return new Expense
        {
            Name = request.Name!.Trim(),
            Description = string.IsNullOrEmpty(description) ? null : description,
            Amount = request.Amount!.Value,
            DueDate = request.DueDate,
            Kind = kind,
            StartPeriod = period,
            EndPeriod = kind == ExpenseKind.OneOff ? period : null,
        };
    }
}
