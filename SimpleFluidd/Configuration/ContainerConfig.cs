namespace SimpleFluidd.Configuration;

public sealed class ContainerConfig
{
    [EnvironmentVar("PRINTER_HOSTNAME", true)]
    public string? PrinterHostname { get; init; }

    [EnvironmentVar("MOONRAKER_PORT")]
    public int MoonrakerPort { get; init; } = 7125;

    [EnvironmentVar("BED_X")]
    public int BedX { get; init; } = 260;
    [EnvironmentVar("BED_Y")]
    public int BedY { get; init; } = 260;
    [EnvironmentVar("BED_Z")]
    public int BedZ { get; init; } = 300;

    [EnvironmentVar("REFRESH_TIME")]
    public int RefreshCooldownSeconds { get; init; } = 5;

    [EnvironmentVar("LOG_LEVEL")]
    public LogLevel LogLevel { get; init; } = LogLevel.Information;
}
