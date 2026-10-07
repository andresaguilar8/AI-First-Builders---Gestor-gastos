using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestorGastos.Time;

/// <summary>Serializa un <see cref="Period"/> como "aaaa-mm".</summary>
public class PeriodJsonConverter : JsonConverter<Period>
{
    public override Period Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Period.TryParse(reader.GetString(), out var period)
            ? period
            : throw new JsonException("Período inválido. Formato esperado: aaaa-mm.");

    public override void Write(Utf8JsonWriter writer, Period value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
