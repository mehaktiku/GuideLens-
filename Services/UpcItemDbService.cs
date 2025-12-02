using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GuideLens.Services
{
    /// <summary>
    /// Tiny wrapper around the UPCitemdb public API.
    /// Uses the free "trial" endpoint (no API key) for simple product lookup by UPC.
    /// </summary>
    public class UpcItemDbService
    {
        private readonly HttpClient _httpClient;

        // Free plan / trial base URL
        private const string BaseUrl = "https://api.upcitemdb.com/prod/trial";

        public UpcItemDbService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Looks up a product by UPC / EAN / GTIN / ISBN.
        /// Returns null if the request fails or nothing is found.
        /// </summary>
        public async Task<UpcLookupResult?> LookupAsync(string upc, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(upc))
                throw new ArgumentException("UPC must not be empty.", nameof(upc));

            var trimmed = upc.Trim();
            var url = $"{BaseUrl}/lookup?upc={Uri.EscapeDataString(trimmed)}";

            using var resp = await _httpClient.GetAsync(url, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                // For now we just treat non-success as "no data" and return null.
                return null;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = await JsonSerializer.DeserializeAsync<UpcLookupResult>(stream, options, cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// Top-level response from UPCitemdb /lookup.
    /// Only the fields we actually care about are included here.
    /// </summary>
    public class UpcLookupResult
    {
        public string? Code { get; set; }   // e.g. "OK" or "INVALID_UPC"
        public int? Total { get; set; }     // number of items returned

        public List<UpcItem> Items { get; set; } = new();
    }

    /// <summary>
    /// Single product entry returned by UPCitemdb.
    /// </summary>
    public class UpcItem
    {
        public string? Title { get; set; }
        public string? Brand { get; set; }
        public string? Category { get; set; }
        public string? Upc { get; set; }

        public string[]? Images { get; set; }  // array of image URLs

        // You can add more fields later if you need them (e.g., size, model, etc.)
        // public string? Model { get; set; }
        // public string? Description { get; set; }
    }
}
