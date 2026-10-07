using GestorGastos.Time;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GestorGastos.Data;

/// <summary>Persiste un <see cref="Period"/> como el primer día del mes (columna date).</summary>
public class PeriodConverter() : ValueConverter<Period, DateOnly>(
    period => period.FirstDay,
    date => Period.FromDate(date));
