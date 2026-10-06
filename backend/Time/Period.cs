using System.Globalization;

namespace GestorGastos.Time;

/// <summary>
/// Un mes calendario (año + mes). Se serializa como "aaaa-mm".
/// </summary>
public readonly record struct Period : IComparable<Period>
{
    public int Year { get; }
    public int Month { get; }

    public Period(int year, int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 9999);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
        Year = year;
        Month = month;
    }

    public static Period FromDate(DateOnly date) => new(date.Year, date.Month);

    public static bool TryParse(string? value, out Period period)
    {
        period = default;
        if (!DateOnly.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return false;
        }

        period = FromDate(date);
        return true;
    }

    public static Period Parse(string value) =>
        TryParse(value, out var period)
            ? period
            : throw new FormatException($"Período inválido: '{value}'. Formato esperado: aaaa-mm.");

    public Period AddMonths(int months) => FromDate(FirstDay.AddMonths(months));

    public Period Next() => AddMonths(1);

    public Period Previous() => AddMonths(-1);

    public DateOnly FirstDay => new(Year, Month, 1);

    public DateOnly LastDay => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    public bool Contains(DateOnly date) => date.Year == Year && date.Month == Month;

    public int CompareTo(Period other) => (Year, Month).CompareTo((other.Year, other.Month));

    public static bool operator <(Period left, Period right) => left.CompareTo(right) < 0;
    public static bool operator >(Period left, Period right) => left.CompareTo(right) > 0;
    public static bool operator <=(Period left, Period right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Period left, Period right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
