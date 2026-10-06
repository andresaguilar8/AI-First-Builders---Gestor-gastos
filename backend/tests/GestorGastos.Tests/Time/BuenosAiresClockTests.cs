using GestorGastos.Time;
using Microsoft.Extensions.Time.Testing;

namespace GestorGastos.Tests.Time;

public class BuenosAiresClockTests
{
    private static BuenosAiresClock ClockAt(string utcInstant) =>
        new(new FakeTimeProvider(DateTimeOffset.Parse(utcInstant)));

    [Fact]
    public void Today_usa_la_fecha_de_Buenos_Aires_aunque_en_UTC_ya_sea_el_dia_siguiente()
    {
        // AC-48: 22:00 del 05/10/2026 en Buenos Aires = 01:00 del 06/10/2026 UTC.
        var clock = ClockAt("2026-10-06T01:00:00Z");

        Assert.Equal(new DateOnly(2026, 10, 5), clock.Today);
    }

    [Fact]
    public void Today_cambia_de_dia_a_la_medianoche_de_Buenos_Aires()
    {
        var clock = ClockAt("2026-10-06T03:00:00Z");

        Assert.Equal(new DateOnly(2026, 10, 6), clock.Today);
    }

    [Fact]
    public void CurrentPeriod_usa_el_mes_de_Buenos_Aires_en_el_cambio_de_mes()
    {
        // 31/10/2026 23:30 en Buenos Aires = 01/11/2026 02:30 UTC.
        IClock clock = ClockAt("2026-11-01T02:30:00Z");

        Assert.Equal(new Period(2026, 10), clock.CurrentPeriod);
    }
}
