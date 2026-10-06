namespace GestorGastos.Time;

public class BuenosAiresClock(TimeProvider timeProvider) : IClock
{
    public const string TimeZoneId = "America/Argentina/Buenos_Aires";

    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    public DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeZone).DateTime);
}
