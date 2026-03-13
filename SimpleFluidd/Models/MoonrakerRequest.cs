using Newtonsoft.Json;

namespace SimpleFluidd.Models;

public sealed record MoonrakerResponse
{
    [JsonProperty("result")]
    public MoonrakerResult Result { get; init; } = new();
}

public sealed record MoonrakerResult
{
    [JsonProperty("status")]
    public MoonrakerData? Status { get; init; }
}

public sealed record MoonrakerData
{
    [JsonProperty("print_stats")]
    public PrintStats? PrintStats { get; init; }

    [JsonProperty("extruder")]
    public Extruder? Extruder { get; init; }

    [JsonProperty("heater_bed")]
    public HeaterBed? Bed { get; init; }

    [JsonProperty("output_pin fan0")]
    public Fan? PartFan { get; init; }

    [JsonProperty("output_pin fanp0")]
    public Fan? AuxFan { get; init; }

    [JsonProperty("heater_fan hotend_fan")]
    public HotendFan? HotendFan { get; init; }

    [JsonProperty("display_status")]
    public DisplayStatus? DisplayStatus { get; init; }
}

public sealed record MoonrakerResultObject<T> where T : IMoonrakerObject
{
    [JsonProperty("result")]
    public T Result { get; init; }
}

public sealed record CameraCollection : IMoonrakerObject
{
    [JsonProperty("webcams")]
    public List<PrinterCamera>? Cameras { get; init; }
}

public interface IMoonrakerObject
{
}