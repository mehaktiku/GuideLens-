// Services/NominatimService.cs
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace GuideLens.Services;

public record GeoPoint(double Lat, double Lon, string DisplayName);

public class NominatimService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public NominatimService(HttpClient http, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
    }

    // Returns the best single match for a query (e.g., "Eden Park, Cincinnati")
    public async Task<GeoPoint?> FindAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        // Basic cache to stay polite with Nominatim
        if (_cache.TryGetValue(query, out GeoPoint cached))
            return cached;

        // ?format=jsonv2&q=...&limit=1
        var url = $"search?format=jsonv2&limit=1&q={Uri.EscapeDataString(query)}";
        var results = await _http.GetFromJsonAsync<List<NominatimRow>>(url, ct);
        var row = results?.FirstOrDefault();
        if (row == null) return null;

        var point = new GeoPoint(double.Parse(row.lat), double.Parse(row.lon), row.display_name);

        _cache.Set(query, point, TimeSpan.FromHours(12));
        return point;
    }

    // Helper to construct an OSM pin link
    public static string OpenStreetMapPin(double lat, double lon, int zoom = 15) =>
        $"https://www.openstreetmap.org/?mlat={lat}&mlon={lon}#map={zoom}/{lat}/{lon}";

    private record NominatimRow(string lat, string lon, string display_name);
}
