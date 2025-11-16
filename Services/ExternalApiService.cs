using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GuideLens.Services
{
    public class ExternalApiService
    {
        private readonly HttpClient _http;

        public ExternalApiService(HttpClient http)
        {
            _http = http;

            // Nominatim requires a User-Agent header
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("GuideLens/1.0 (https://localhost)");
        }

        // 1) Nominatim – get coordinates for "place, city, Ohio"
        public async Task<(double lat, double lon)?> GetCoordinatesAsync(string placeName, string city)
        {
            var query = $"{placeName}, {city}, Ohio";
            var url = $"https://nominatim.openstreetmap.org/search?format=json&limit=1&q={Uri.EscapeDataString(query)}";

            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.ValueKind != JsonValueKind.Array ||
                !doc.RootElement.EnumerateArray().Any())
            {
                return null;
            }

            var first = doc.RootElement.EnumerateArray().First();

            var latString = first.GetProperty("lat").GetString();
            var lonString = first.GetProperty("lon").GetString();

            if (!double.TryParse(latString, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                return null;

            if (!double.TryParse(lonString, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                return null;

            return (lat, lon);
        }

        // 2) Open-Meteo – get today’s sunset time for given coordinates
        public async Task<string?> GetTodaySunsetAsync(double lat, double lon)
        {
            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lonStr = lon.ToString(CultureInfo.InvariantCulture);

            var url =
                $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}" +
                $"&daily=sunset&timezone=auto";

            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (!doc.RootElement.TryGetProperty("daily", out var daily))
                return null;

            if (!daily.TryGetProperty("sunset", out var sunsetArray) ||
                sunsetArray.ValueKind != JsonValueKind.Array ||
                sunsetArray.GetArrayLength() == 0)
            {
                return null;
            }

            // First element = today's sunset time (ISO string)
            return sunsetArray[0].GetString();
        }

        // 3) Convenience method – combine both APIs
        public async Task<SunsetResult> GetSunsetForPlaceAsync(string placeName, string city)
        {
            var coords = await GetCoordinatesAsync(placeName, city);
            if (coords == null)
            {
                return new SunsetResult
                {
                    Place = placeName,
                    City = city,
                    Error = "Could not find coordinates for this place."
                };
            }

            var (lat, lon) = coords.Value;
            var sunset = await GetTodaySunsetAsync(lat, lon);

            if (sunset == null)
            {
                return new SunsetResult
                {
                    Place = placeName,
                    City = city,
                    Latitude = lat,
                    Longitude = lon,
                    Error = "Could not retrieve sunset time."
                };
            }

            return new SunsetResult
            {
                Place = placeName,
                City = city,
                Latitude = lat,
                Longitude = lon,
                SunsetTime = sunset
            };
        }
    }

    public class SunsetResult
    {
        public string? Place { get; set; }
        public string? City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? SunsetTime { get; set; }
        public string? Error { get; set; }
    }
}
