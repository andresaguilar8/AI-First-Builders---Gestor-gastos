namespace GestorGastos.Time;

/// <summary>
/// Fuente única de "la fecha actual" para todo el dominio (RNF-07).
/// </summary>
public interface IClock
{
    /// <summary>Fecha actual en America/Argentina/Buenos_Aires.</summary>
    DateOnly Today { get; }

    /// <summary>Mes actual en America/Argentina/Buenos_Aires.</summary>
    Period CurrentPeriod => Period.FromDate(Today);
}
