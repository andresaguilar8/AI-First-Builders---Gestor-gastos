using GestorGastos.Time;

namespace GestorGastos.Tests.Time;

public class PeriodTests
{
    [Fact]
    public void Parse_y_ToString_usan_formato_aaaa_mm()
    {
        var period = Period.Parse("2026-09");

        Assert.Equal(new Period(2026, 9), period);
        Assert.Equal("2026-09", period.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026-13")]
    [InlineData("2026-00")]
    [InlineData("2026-9")]
    [InlineData("09-2026")]
    [InlineData("2026-09-01")]
    public void TryParse_rechaza_valores_invalidos(string? value)
    {
        Assert.False(Period.TryParse(value, out _));
    }

    [Fact]
    public void Previous_de_enero_es_diciembre_del_anio_anterior()
    {
        // AC-51
        Assert.Equal(new Period(2026, 12), new Period(2027, 1).Previous());
    }

    [Fact]
    public void Next_de_diciembre_es_enero_del_anio_siguiente()
    {
        Assert.Equal(new Period(2027, 1), new Period(2026, 12).Next());
    }

    [Theory]
    [InlineData(2027, 2, 28)]
    [InlineData(2028, 2, 29)]
    [InlineData(2026, 9, 30)]
    [InlineData(2026, 10, 31)]
    public void LastDay_respeta_la_cantidad_de_dias_del_mes(int year, int month, int expectedDay)
    {
        Assert.Equal(new DateOnly(year, month, expectedDay), new Period(year, month).LastDay);
    }

    [Fact]
    public void Los_periodos_se_ordenan_cronologicamente()
    {
        Assert.True(new Period(2026, 12) < new Period(2027, 1));
        Assert.True(new Period(2026, 10) > new Period(2026, 9));
        Assert.True(new Period(2026, 10) <= new Period(2026, 10));
    }

    [Fact]
    public void Contains_indica_si_una_fecha_cae_en_el_mes()
    {
        var october = new Period(2026, 10);

        Assert.True(october.Contains(new DateOnly(2026, 10, 31)));
        Assert.False(october.Contains(new DateOnly(2026, 11, 1)));
    }
}
