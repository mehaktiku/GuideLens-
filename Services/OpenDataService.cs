using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;

namespace GuideLens.Services
{
    // Minimal Nominatim response (we only care about lat/lon)
    public class NominatimResult
    {
        public string? lat { get; set; }
        public string? lon { get; set; }
        public string? display_name { get; set; }
    }

    // Minimal Open-Meteo response (sunset only)
    public class OpenMeteoDaily
    {
        public string[]? time { get; set; }
        public string[]? sunset { get; set; }
    }

    public class OpenMeteoResponse
    {
        public OpenMeteoDaily? daily { get; set; }
    }

    public class OpenDataService
    {
        private readonly HttpClient _http;

        public OpenDataService(HttpClient http)
        {
            _http = http;
        }

        // 1) Call Nominatim to get coordinates
        public async Task<(double lat, double lon)?> GeocodeAsync(string place, string city)
        {
            var query = $"{place}, {city}";
            var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(query)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            // Nominatim requires a User-Agent
            req.Headers.UserAgent.ParseAdd("GuideLens/1.0 (your-email@example.com)");

            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var results = await resp.Content.ReadFromJsonAsync<List<NominatimResult>>();
            var first = results?.FirstOrDefault();
            if (first == null || string.IsNullOrWhiteSpace(first.lat) || string.IsNullOrWhiteSpace(first.lon))
                return null;

            var lat = double.Parse(first.lat, CultureInfo.InvariantCulture);
            var lon = double.Parse(first.lon, CultureInfo.InvariantCulture);
            return (lat, lon);
        }

        // 2) Call Open-Meteo to get today's sunset for those coordinates
        public async Task<string?> GetTodaySunsetAsync(double lat, double lon)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var url =
                $"https://api.open-meteo.com/v1/forecast" +
                $"?latitude={lat}&longitude={lon}" +
                $"&daily=sunset&timezone=auto" +
                $"&start_date={today:yyyy-MM-dd}&end_date={today:yyyy-MM-dd}";

            var resp = await _http.GetFromJsonAsync<OpenMeteoResponse>(url);
            return resp?.daily?.sunset?.FirstOrDefault();
        }
    }
}
