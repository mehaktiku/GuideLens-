using System.Text.Json;
using GuideLens.Services;
using GuideLens.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 1) Razor Pages
builder.Services.AddRazorPages();

// Add in-memory caching for query/result caching
builder.Services.AddMemoryCache();

// 2) Your filtering service
builder.Services.AddSingleton<RecommendationService>();

// 3) HttpClient for external APIs (Open-Meteo, Nominatim, Wikipedia)
builder.Services.AddHttpClient();

var app = builder.Build();

// 4) Standard middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 5) Razor Pages endpoints (/, /Index, etc.)
app.MapRazorPages();

//
// 6) YOUR OWN JSON API: /api/recommendations
//    Example:
//    /api/recommendations?city=Cincinnati&q=Eden&category=All&neighborhood=&sortBy=name&page=1&pageSize=12
//
app.MapGet("/api/recommendations",
    ([AsParameters] RecommendationQuery query, RecommendationService svc) =>
    {
        var result = svc.Query(query);
        return Results.Ok(result);
    });

//
// 7) EXTERNAL-API WRAPPER: /api/photo-time-hint
//    Uses Nominatim (OpenStreetMap) + Open-Meteo to get sunset time.
//
app.MapGet("/api/photo-time-hint", async (
    string place,
    string? city,
    string? country,
    IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(place))
    {
        return Results.BadRequest(new { error = "Query parameter 'place' is required." });
    }

    // Build a human-readable query like "Ault Park, Cincinnati, USA"
    var locationParts = new List<string> { place };
    if (!string.IsNullOrWhiteSpace(city)) locationParts.Add(city!);
    if (!string.IsNullOrWhiteSpace(country)) locationParts.Add(country!);
    var locationQuery = string.Join(", ", locationParts);

    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GuideLens/1.0 (+https://localhost)");

    // --- 1) Look up lat/lon via Nominatim (OpenStreetMap) ---
    var nominatimUrl =
        $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(locationQuery)}&limit=1";

    using var nominatimResponse = await client.GetAsync(nominatimUrl);
    if (!nominatimResponse.IsSuccessStatusCode)
    {
        return Results.Problem("Could not look up coordinates from OpenStreetMap.");
    }

    await using var nominatimStream = await nominatimResponse.Content.ReadAsStreamAsync();
    using var nominatimJson = await JsonDocument.ParseAsync(nominatimStream);

    if (nominatimJson.RootElement.GetArrayLength() == 0)
    {
        return Results.NotFound(new { error = $"No location found for '{locationQuery}'." });
    }

    var first = nominatimJson.RootElement[0];
    var lat = first.GetProperty("lat").GetString();
    var lon = first.GetProperty("lon").GetString();

    if (lat is null || lon is null)
    {
        return Results.Problem("Location found but latitude/longitude missing.");
    }

    // --- 2) Get sunset time from Open-Meteo ---
    var meteoUrl =
        $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&daily=sunset&timezone=auto";

    using var meteoResponse = await client.GetAsync(meteoUrl);
    if (!meteoResponse.IsSuccessStatusCode)
    {
        return Results.Problem("Could not fetch sunset time from Open-Meteo.");
    }

    await using var meteoStream = await meteoResponse.Content.ReadAsStreamAsync();
    using var meteoJson = await JsonDocument.ParseAsync(meteoStream);

    var daily = meteoJson.RootElement.GetProperty("daily");
    var sunsets = daily.GetProperty("sunset");

    if (sunsets.GetArrayLength() == 0)
    {
        return Results.Problem("Sunset data not available.");
    }

    var sunset = sunsets[0].GetString();

    return Results.Ok(new
    {
        place,
        city,
        country,
        latitude = lat,
        longitude = lon,
        sunset
    });
});

//
// 8) WIKIPEDIA WRAPPER: /api/about-place
//    Example:
//    /api/about-place?title=Skyline%20Chili&lang=en
//
app.MapGet("/api/about-place", async (
    string title,
    string? lang,
    IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(title))
    {
        return Results.BadRequest(new { error = "Query parameter 'title' is required." });
    }

    var language = string.IsNullOrWhiteSpace(lang) ? "en" : lang.Trim();
    var encodedTitle = Uri.EscapeDataString(title);

    var url = $"https://{language}.wikipedia.org/api/rest_v1/page/summary/{encodedTitle}";

    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GuideLens/1.0 (+https://localhost)");

    using var resp = await client.GetAsync(url);
    if (!resp.IsSuccessStatusCode)
    {
        // 404 or other issue from Wikipedia
        return Results.NotFound(new { error = $"No Wikipedia summary found for '{title}'." });
    }

    await using var stream = await resp.Content.ReadAsStreamAsync();
    using var json = await JsonDocument.ParseAsync(stream);

    string extract = json.RootElement.TryGetProperty("extract", out var extractProp)
        ? extractProp.GetString() ?? ""
        : "";

    string? pageUrl = null;
    if (json.RootElement.TryGetProperty("content_urls", out var contentUrls) &&
        contentUrls.TryGetProperty("desktop", out var desktop) &&
        desktop.TryGetProperty("page", out var pageProp))
    {
        pageUrl = pageProp.GetString();
    }

    return Results.Ok(new
    {
        title,
        extract,
        pageUrl
    });
});

app.Run();
