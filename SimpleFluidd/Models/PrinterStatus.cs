using Newtonsoft.Json;

namespace SimpleFluidd.Models;

public sealed record PrintStats
{
    [JsonProperty("filename")]
    public string Filename { get; init => field = value.DefaultIfEmpty(); } = "None";

    [JsonProperty("total_duration")]
    public float TotalDuration { get; init; }

    [JsonProperty("print_duration")]
    public float PrintDuration { get; init; }

    [JsonProperty("filament_used")]
    public float FilamentUsed { get; init; }

    [JsonProperty("state")]
    public string State { get; init; }

    [JsonProperty("message")]
    public string Message { get; init => field = value.DefaultIfEmpty(); } = "None";

    [JsonProperty("info")]
    public LayerInfo Info { get; init; }
}

public sealed record LayerInfo
{
    [JsonProperty("total_layer")]
    public int? TotalLayers { get; init; }

    [JsonProperty("current_layer")]
    public int? CurrentLayer { get; init; }

    [JsonIgnore]
    public string PresentableLayers => $"{CurrentLayer ?? 0} / {TotalLayers ?? 0}";

    [JsonIgnore]
    public bool NoLayers => TotalLayers is null || CurrentLayer is null;
}

public sealed record Extruder
{
    [JsonProperty("temperature")]
    public float Temperature { get; set; }

    [JsonProperty("target")]
    public float TargetTemp { get; set; }
}

public sealed record HeaterBed
{
    [JsonProperty("temperature")]
    public float Temperature { get; init; }

    [JsonProperty("target")]
    public float TargetTemp { get; set; }
}

public sealed record Fan
{
    [JsonProperty("value")]
    public float PowerPercentage
    {
        get;
        init
        {
            On = value is not 0;
            if (value < 2f)
            {
                field = value * 200;
            }
        }
    }

    [JsonIgnore]
    public bool On { get; private set; }
}

public sealed record HotendFan
{
    [JsonProperty("speed")]
    public float Speed
    {
        get;
        init
        {
            On = value is 1;
            field = value;
        }
    }

    [JsonIgnore]
    public bool On { get; private set; }
}

public sealed record DisplayStatus
{
    [JsonProperty("message")]
    public string Message { get; init => field = value.DefaultIfEmpty(); } = "None";

    [JsonProperty("progress")]
    public float PrintProgress { get; init; }

    [JsonIgnore]
    public float PrintProgressPercent => PrintProgress * 100;
}

// public sealed record PrinterCamera
// {
//     [JsonProperty("name")]
//     public string Name { get; init; }
// 
//     [JsonProperty("location")]
//     public string Location { get; init; }
// 
//     [JsonProperty("service")]
//     public string ServiceUrl { get; init; }
// 
//     [JsonProperty("target_fps")]
//     public int TargetFps { get; init; }
// 
//     [JsonProperty("stream_url")]
//     public string StreamUrl { get; init; }
// 
//     [JsonProperty("snapshot_url")]
//     public string SnapshotUrl { get; init; }
// 
//     [JsonProperty("flip_horizontal")]
//     public bool FlipH { get; init; }
// 
//     [JsonProperty("flip_vertical")]
//     public bool FlipV { get; init; }
// 
//     [JsonProperty("rotation")]
//     public int Rotation { get; init; }
// 
//     [JsonProperty("source")]
//     public string Source { get; init; }
// }

public sealed record PrinterCamera(
    [JsonProperty("name")]
    string Name,

    [JsonProperty("location")]
    string Location,

    [JsonProperty("service")]
    string ServiceType,

    [JsonProperty("target_fps")]
    int TargetFps,

    [JsonProperty("stream_url")]
    string StreamUrl,

    [JsonProperty("snapshot_url")]
    string SnapshotUrl,

    [JsonProperty("flip_horizontal")]
    bool FlipH,

    [JsonProperty("flip_vertical")]
    bool FlipV,

    [JsonProperty("rotation")]
    int Rotation,

    [JsonProperty("source")]
    string Source
);

public static class StatusSanitizer
{
    public static string DefaultIfEmpty(this string? value, string defaultValue = "None")
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}