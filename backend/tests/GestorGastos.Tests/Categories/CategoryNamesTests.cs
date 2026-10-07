using GestorGastos.Categories;

namespace GestorGastos.Tests.Categories;

public class CategoryNamesTests
{
    [Theory]
    [InlineData("Educación")]
    [InlineData("educacion")]
    [InlineData("EDUCACIÓN")]
    [InlineData("  Educación  ")]
    [InlineData("EdUcAcIoN")]
    public void Normaliza_sin_distinguir_mayusculas_ni_acentos(string name)
    {
        // AC-26
        Assert.Equal("educacion", CategoryNames.Normalize(name));
    }

    [Fact]
    public void Ignora_los_espacios_de_mas_entre_palabras()
    {
        Assert.Equal(CategoryNames.Normalize("Servicios del hogar"), CategoryNames.Normalize("Servicios   del  hogar"));
        Assert.Equal("Servicios del hogar", CategoryNames.Clean("  Servicios   del  hogar "));
    }

    [Fact]
    public void Distingue_la_enie()
    {
        // La ñ no es una n con acento: "Año" y "Ano" son nombres distintos.
        Assert.NotEqual(CategoryNames.Normalize("Año"), CategoryNames.Normalize("Ano"));
        Assert.Equal("año", CategoryNames.Normalize("AÑO"));
        Assert.Equal("cañeria", CategoryNames.Normalize("Cañería"));
    }

    [Theory]
    [InlineData("Sin categoría")]
    [InlineData("sin categoria")]
    [InlineData("SIN CATEGORÍA")]
    [InlineData(" Sin  categoría ")]
    public void Reconoce_el_nombre_reservado(string name)
    {
        // AC-27
        Assert.True(CategoryNames.IsReserved(name));
    }

    [Fact]
    public void Un_nombre_que_contiene_el_reservado_no_es_reservado()
    {
        Assert.False(CategoryNames.IsReserved("Sin categoría fija"));
    }
}
