using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleFluidd.Models;

namespace SimpleFluidd.Services.Converters;

public sealed class NewtPolyVecConverter : JsonConverter<PolyVec>
{
    public override PolyVec ReadJson(JsonReader reader, Type objectType, PolyVec existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.Null)
        {
            return default;
        }

        JArray jArray = JArray.Load(reader);

        if (jArray.Count < 2)
        {
            throw new JsonSerializationException("PolyVec array must have at least 2 elements [x, y]");
        }

        double x = jArray[0].Value<double>();
        double y = jArray[1].Value<double>();

        return new PolyVec(x, y);
    }

    public override void WriteJson(JsonWriter writer, PolyVec value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}