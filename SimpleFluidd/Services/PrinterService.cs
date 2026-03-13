using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleFluidd.Configuration;
using SimpleFluidd.Models;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimpleFluidd.Services;

public interface IPrinterService
{
    public Task<bool> TestPrinterConnection();

    public Task<IEnumerable<Polygon>> FetchLayerObjectsAsync(CancellationToken token);

    public Task<MoonrakerData> FetchPrinterStatusAsync(CancellationToken token);

    public Task<List<PrinterCamera>> FetchPrinterCamerasAsync(CancellationToken token);
}

public sealed class PrinterService(HttpClient _httpClient, ContainerConfig _config, ILogger<PrinterService> _logger) : IPrinterService
{
    private readonly QueryProvider _queryProvider = new(_config);

    public async Task<bool> TestPrinterConnection()
    {
        try
        {
            if (_config.PrinterHostname is null)
            {
                return false;
            }

            return await PingPrinterAsync(_config.PrinterHostname)
                && await IsMoonrakerResponsive();
        }
        catch (HttpRequestException hrex)
        {
            _logger.LogError("Failed to verify printer status: {Ex}", hrex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to verify moonraker status: {Ex}", ex.Message);
            return false;
        }
    }

    public async Task<IEnumerable<Polygon>> FetchLayerObjectsAsync(CancellationToken token)
    {
        string queryUrl = _queryProvider.GetObjectQueryUrl("exclude_object");

        var json = await _httpClient.GetStringAsync(queryUrl, token);

        var jsonDoc = JObject.Parse(json)
            ?["result"]
            ?["status"]
            ?["exclude_object"]
            ?["objects"];

        List<Polygon>? polygons = jsonDoc?.ToObject<List<Polygon>>();

        return polygons ?? [];
    }

    public async Task<MoonrakerData> FetchPrinterStatusAsync(CancellationToken token)
    {
        const string PRINT_STATS = "print_stats",
                     EXTRUDER = "extruder",
                     HEATER_BED = "heater_bed",
                     DISPLAY_STATUS = "display_status",
                     FAN0 = "output_pin fan0",
                     FANP0 = "output_pin fanp0",
                     BOARD_FAN = "output_pin board_fan",
                     HOTEND_FAN = "heater_fan hotend_fan";

        string queryUrl = _queryProvider.GetObjectQueryUrl(
            PRINT_STATS, EXTRUDER, HEATER_BED, DISPLAY_STATUS,
            FAN0, FANP0, BOARD_FAN, HOTEND_FAN
        );

        var rawJson = await _httpClient.GetStringAsync(queryUrl, token);
        var response = JsonConvert.DeserializeObject<MoonrakerResponse>(rawJson);

        return response?.Result.Status ?? new();
    }

    public async Task<List<PrinterCamera>> FetchPrinterCamerasAsync(CancellationToken token)
    {
        string queryUrl = _queryProvider.GetPrimitiveUrl("/server/webcams/list");

        string rawJson = await _httpClient.GetStringAsync(queryUrl, token);
        var response = JsonConvert.DeserializeObject<MoonrakerResultObject<CameraCollection>>(rawJson)?.Result.Cameras;

        return response ?? [];
    }

    public class NullPayloadException(string message) : Exception(message)
    {
        public static void ThrowIfNull([NotNull] object? obj, string message, [CallerArgumentExpression(nameof(obj))] string paramName = "")
        {
            if (obj is null)
            {
                Throw(message, paramName);
            }
        }

        [DoesNotReturn]
        private static void Throw(string message, string paramName)
        {
            throw new NullPayloadException($"{paramName}: {message}");
        }
    }

    private async Task<bool> PingPrinterAsync(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 1000);
            return reply.Status == IPStatus.Success;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to ping printer: {Message}", ex.Message);
            return false;
        }
    }

    private async Task<bool> IsMoonrakerResponsive()
    {
        try
        {
            var response = await _httpClient.GetAsync(_queryProvider.GetPrimitiveUrl("/server/info"));
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private sealed class QueryProvider
    {
        private const string QUERY_ROOT = "printer/objects/query";
        private readonly string _moonrakerObjectQueryRoot;
        private readonly string _moonrakerUrl;

        public QueryProvider(ContainerConfig _config)
        {
            _moonrakerUrl = $"http://{_config.PrinterHostname}:{_config.MoonrakerPort}";
            _moonrakerObjectQueryRoot = $"{_moonrakerUrl}/{QUERY_ROOT}?";
        }

        public string GetObjectQueryUrl(params string[] objects)
        {
            return string.Concat(_moonrakerObjectQueryRoot, string.Join('&', objects));
        }

        public string GetPrimitiveUrl(string endpoint)
        {
            return Path.Join(_moonrakerUrl, endpoint);
        }
    }
}

public class PrinterCoordinator(IPrinterService _printer, ContainerConfig _config, ILogger<PrinterCoordinator> _logger) : BackgroundService
{
    public event Action? OnDataUpdated;

    public List<Polygon> CurrentPolygons { get; private set; } = [];

    public MoonrakerData PrinterStatus { get; private set; } = new();

    public List<PrinterCamera> Cameras { get; private set; } = [];

    public bool PrinterOnline { get; private set; } = false;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var refreshActions = new Func<CancellationToken, Task>[] {
            RefreshPolygons,
            RefreshPrinterStats,
            RefreshCamsList,
        };

        bool cycle = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            // Also locks the loop until the printer is responsive again
            if (cycle && !await CheckPrinterStatus())
            {
                _logger.LogWarning("Unable to connect to {Host}", _config.PrinterHostname);
                
                // Ping already has a 1000ms timeout, if it reaches this, add 4 for 5 total
                await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
                continue;
            }

            cycle = !cycle;

            foreach (var action in refreshActions)
            {
                try
                {
                    await action(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Name}: {Ex}", action.Method.Name, ex.Message);
                }
            }

            OnDataUpdated?.Invoke();
            await Task.Delay(TimeSpan.FromSeconds(_config.RefreshCooldownSeconds), stoppingToken);
        }
    }

    private async Task<bool> CheckPrinterStatus()
    {
        bool online = await _printer.TestPrinterConnection();

        if (!PrinterOnline)
        {
            if (online)
            {
                OnPrinterAvailable();
            }
        }
        else
        {
            OnPrinterAvailable();
        }

        return online;
    }

    private async Task RefreshPolygons(CancellationToken token)
    {
        var polygonData = await _printer.FetchLayerObjectsAsync(token);
        CurrentPolygons = [.. polygonData];
    }

    private async Task RefreshPrinterStats(CancellationToken token)
    {
        var printerStatus = await _printer.FetchPrinterStatusAsync(token);
        PrinterStatus = printerStatus;
    }

    private async Task RefreshCamsList(CancellationToken token)
    {
        var cameras = await _printer.FetchPrinterCamerasAsync(token);
        if (cameras.Count != Cameras.Count)
        {
            Cameras = cameras;
        }
    }

    private void OnPrinterAvailable()
    {
        PrinterOnline = true;
    }

    private void OnPrinterUnavailable()
    {
        PrinterOnline = false;
    }

    public string GenerateBlueprint(List<Polygon> objects)
    {
        var svg = new StringBuilder();

        svg.Append($"<svg viewBox='0 0 {_config.BedX} {_config.BedY}' xmlns='http://www.w3.org/2000/svg' style='background: #1a1a1a;'>");

        foreach (var obj in objects)
        {
            var points = string.Join(' ', obj.Vertices.Select(p => $"{p.X},{_config.BedY - p.Y}"));
            svg.Append($"<polygon points='{points}' fill='rgba(0, 150, 255, 0.2)' stroke='#0096ff' stroke-width='1' />");
        }

        svg.Append("</svg>");
        return svg.ToString();
    }
}
