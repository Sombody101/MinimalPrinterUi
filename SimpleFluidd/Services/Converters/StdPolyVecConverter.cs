using SimpleFluidd.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleFluidd.Services.Converters;

public class StdPolyVecConverter : JsonConverter<PolyVec>
{
    public override PolyVec Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var x = doc.RootElement[0].GetDouble();
        var y = doc.RootElement[1].GetDouble();
        return new PolyVec(x, y);
    }

    public override void Write(Utf8JsonWriter writer, PolyVec value, JsonSerializerOptions options) => throw new NotImplementedException();
}