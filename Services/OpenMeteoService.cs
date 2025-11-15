// Services/OpenMeteoService.cs
using System.Net.Http.Json;

namespace GuideLens.Services;

public record SunInfo(DateTimeOffset SunriseLocal, DateTimeOffset SunsetLocal);

public class OpenMeteoService
{
    private readonly HttpClient _http;

    public OpenMeteoService(HttpClient http) => _http = http;

    // Gets today's sunrise/sunset for the given location (local timezone via 'auto')
    public async Task<SunInfo?> GetSunriseSunsetAsync(double lat, double lon, CancellationToken ct = default)
    {
        var url = $"v1/forecast?latitude={lat}&longitude={lon}&daily=sunrise,sunset&timezone=auto";
        var payload = await _http.GetFromJsonAsync<Root>(url, ct);
        if (payload?.daily == null || payload.daily.sunrise?.Any() != true || payload.daily.sunset?.Any() != true)
            return null;

        // Take the first day (today)
        var sunrise = DateTimeOffset.Parse(payload.daily.sunrise[0]);
        var sunset = DateTimeOffset.Parse(payload.daily.sunset[0]);

        return new SunInfo(sunrise, sunset);
    }

    private class Root
    {
        public Daily? daily { get; set; }
        public class Daily
        {
            public List<string>? sunrise { get; set; }
            public List<string>? sunset { get; set; }
        }
    }
}
