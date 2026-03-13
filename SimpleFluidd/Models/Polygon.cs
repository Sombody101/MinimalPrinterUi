using Newtonsoft.Json;

namespace SimpleFluidd.Models;

public sealed class Polygon
{
    public string ObjectName { get; } = string.Empty;

    public PolyVec Center { get; }

    public IReadOnlyList<PolyVec> Vertices { get; }

    public Polygon(IEnumerable<PolyVec> vec)
    {
        Vertices = [.. vec];
    }

    [JsonConstructor]
    public Polygon(
        [JsonProperty("name")]
        string name,
        [JsonProperty("center")]
        PolyVec center,
        [JsonProperty("polygon")]
        PolyVec[] polygon
    )
    {
        ObjectName = name;
        Center = center;
        Vertices = polygon;
    }
}