using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GuideLens.Models;
using GuideLens.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly RecommendationService _svc;

    // Simple shared HttpClient for API calls
    private static readonly HttpClient _http = new HttpClient();

    public IndexModel(RecommendationService svc) => _svc = svc;

    // Your existing query (now assumed to include Query.City = "Cincinnati" by default)
    [BindProperty(SupportsGet = true)]
    public RecommendationQuery Query { get; set; } = new();

    // Ohio city dropdown options (UI-only; safe to change later)
    public IReadOnlyList<string> Cities { get; } =
        new[] { "Cincinnati", "Columbus", "Cleveland", "Dayton" };

    // Optional: show a “coming soon” note if a non-Cincinnati city is chosen
    public bool ComingSoon { get; private set; }

    public PagedResult<Recommendation> Results { get; private set; } = new();
    public IReadOnlyList<string> Neighborhoods { get; private set; } = Array.Empty<string>();

    public void OnGet()
    {
        // Ensure a default city so links without &City= still work
        if (string.IsNullOrWhiteSpace(Query.City))
            Query.City = "Cincinnati";

        // UI hint only (we still return Cincinnati data so nothing breaks)
        ComingSoon = !string.Equals(Query.City, "Cincinnati", StringComparison.OrdinalIgnoreCase);

        Neighborhoods = _svc.Neighborhoods();

        // Keep current behavior: always query the existing Cincinnati dataset.
        // (When you add other cities' data, switch on Query.City here.)
        Results = _svc.Query(Query);
    }

    // ---------- HELPER: Geocode via Nominatim ----------
    private async Task<(double lat, double lon)?> GeocodeAsync(string name, string city)
    {
        // Basic query: "Eden Park, Cincinnati, Ohio"
        var query = $"{name}, {city}, Ohio";
        var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(query)}";

        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);

        var first = doc.RootElement.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object)
            return null;

        if (!first.TryGetProperty("lat", out var latEl) ||
            !first.TryGetProperty("lon", out var lonEl))
            return null;

        if (!double.TryParse(latEl.GetString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var lat))
            return null;

        if (!double.TryParse(lonEl.GetString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var lon))
            return null;

        return (lat, lon);
    }

    // ---------- HANDLER 1: Open Map (OSM) ----------
    // URL example: /Index?handler=Map&name=Eden%20Park&city=Cincinnati
    public async Task<IActionResult> OnGetMapAsync(string name, string city)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Missing place name.");

        if (string.IsNullOrWhiteSpace(city))
            city = Query.City ?? "Cincinnati";

        var coords = await GeocodeAsync(name, city);
        if (coords == null)
            return NotFound("Location not found.");

        var (lat, lon) = coords.Value;

        // OpenStreetMap link with a marker
        var url = $"https://www.openstreetmap.org/?mlat={lat}&mlon={lon}#map=15/{lat}/{lon}";
        return Redirect(url);
    }

    // ---------- HANDLER 2: Sunset Time (Open-Meteo) ----------
    // URL example: /Index?handler=Sunset&name=Ault%20Park&city=Cincinnati
    public async Task<IActionResult> OnGetSunsetAsync(string name, string city)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new JsonResult(new { success = false, error = "Missing place name." });

        if (string.IsNullOrWhiteSpace(city))
            city = Query.City ?? "Cincinnati";

        var coords = await GeocodeAsync(name, city);
        if (coords == null)
            return new JsonResult(new { success = false, error = "Location not found." });

        var (lat, lon) = coords.Value;

        // Open-Meteo daily sunrise/sunset
        var url =
            $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&daily=sunrise,sunset&timezone=auto";

        try
        {
            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            var daily = doc.RootElement.GetProperty("daily");
            var sunriseArr = daily.GetProperty("sunrise");
            var sunsetArr = daily.GetProperty("sunset");

            var sunrise = sunriseArr[0].GetString();
            var sunset = sunsetArr[0].GetString();

            return new JsonResult(new
            {
                success = true,
                sunrise,
                sunset
            });
        }
        catch
        {
            return new JsonResult(new
            {
                success = false,
                error = "Unexpected API response."
            });
        }
    }
}
