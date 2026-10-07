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
        var errors = ValidateValues(request.Name, request.Description, request.Amount, request.DueDate, period);

        if (request.Kind is null)
        {
            errors["kind"] = ["Indicá si el gasto es puntual o recurrente."];
        }

        return errors;
    }

    /// <summary>Valida una edición de un gasto de <paramref name="period"/>.</summary>
    public static Dictionary<string, string[]> ValidateUpdate(UpdateExpenseRequest request, Period period) =>
        ValidateValues(request.Name, request.Description, request.Amount, request.DueDate, period);

    private static Dictionary<string, string[]> ValidateValues(
        string? name, string? description, decimal? amount, DateOnly? dueDate, Period period)
    {
        var errors = new Dictionary<string, string[]>();

        name = name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            errors["name"] = ["El nombre es obligatorio."];
        }
        else if (name.Length > NameMaxLength)
        {
            errors["name"] = [$"El nombre no puede superar los {NameMaxLength} caracteres."];
        }

        if (description?.Trim().Length > DescriptionMaxLength)
        {
            errors["description"] = [$"La descripción no puede superar los {DescriptionMaxLength} caracteres."];
        }

        if (AmountError(amount) is { } amountError)
        {
            errors["amount"] = [amountError];
        }

        if (dueDate is { } date && date < period.FirstDay)
        {
            errors["dueDate"] = ["El vencimiento no puede ser de un mes anterior al del gasto."];
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

        return new Expense
        {
            Name = request.Name!.Trim(),
            Description = NormalizeDescription(request.Description),
            Amount = request.Amount!.Value,
            DueDate = request.DueDate,
            CategoryId = request.CategoryId,
            Kind = kind,
            StartPeriod = period,
            EndPeriod = kind == ExpenseKind.OneOff ? period : null,
        };
    }

    /// <summary>
    /// Aplica una edición ya validada a un gasto puntual y, si la tiene, a su
    /// instancia del mes, que conserva su estado de pago.
    /// </summary>
    public static void ApplyUpdate(Expense expense, ExpenseMonth? ownMonth, UpdateExpenseRequest request)
    {
        var name = request.Name!.Trim();
        var description = NormalizeDescription(request.Description);
        var amount = request.Amount!.Value;

        expense.Name = name;
        expense.Description = description;
        expense.Amount = amount;
        expense.DueDate = request.DueDate;
        expense.CategoryId = request.CategoryId;

        if (ownMonth is not null)
        {
            ownMonth.Name = name;
            ownMonth.Description = description;
            ownMonth.Amount = amount;
            ownMonth.DueDate = request.DueDate;
            ownMonth.CategoryId = request.CategoryId;
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        description = description?.Trim();
        return string.IsNullOrEmpty(description) ? null : description;
    }
}
