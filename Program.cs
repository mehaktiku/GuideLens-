using System.Text.Json;
using GuideLens.Services;
using GuideLens.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// 1) Razor Pages
builder.Services.AddRazorPages();

// 2) Your filtering service
builder.Services.AddSingleton<RecommendationService>();

// 3) HttpClient for external APIs (Open-Meteo, Nominatim, Wikipedia)
builder.Services.AddHttpClient();

// 4) Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5) Response caching service (used for /api/recommendations)
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

var app = builder.Build();

// 6) Standard middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 7) Response caching middleware
app.UseResponseCaching();

// 8) Razor Pages endpoints (/, /Index, etc.)
app.MapRazorPages();

//
// 9) JSON API: /api/recommendations
//    Example:
//    /api/recommendations?city=Cincinnati&q=Eden&category=All&neighborhood=&sortBy=name&page=1&pageSize=12
//
var recEndpoint = app.MapGet("/api/recommendations",
    ([AsParameters] RecommendationQuery query,
     RecommendationService svc,
     HttpContext httpContext) =>
    {
        // Small safety guard on PageSize (avoid someone requesting 10,000 items)
        if (query.PageSize <= 0 || query.PageSize > 100)
        {
            query.PageSize = 12;
        }

        var result = svc.Query(query);

        // Set HTTP cache headers for clients / ResponseCaching middleware
        httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(60)  // cache for 60 seconds
        };

        return Results.Ok(result);
    });

// Attach ResponseCache metadata so ASP.NET Core's ResponseCaching can use it
recEndpoint.WithMetadata(new ResponseCacheAttribute
{
    Duration = 60,
    Location = ResponseCacheLocation.Any,
    NoStore = false
});

//
// 10) EXTERNAL-API WRAPPER: /api/photo-time-hint
//    Uses Nominatim (OpenStreetMap) + Open-Meteo to get sunset time.
//
app.MapGet("/api/photo-time-hint", async (
    string place,
    string? city,
    string? country,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    var cacheKey = $"photo-time-hint:{place}:{city}:{country}";
    if (cache.TryGetValue(cacheKey, out var cachedResult))
    {
        return Results.Ok(cachedResult);
    }

    if (string.IsNullOrWhiteSpace(place))
    {
        return Results.BadRequest(new { error = "Query parameter 'place' is required." });
    }

    // Build a query like "Ault Park, Cincinnati, USA"
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

    var result = new
    {
        place,
        city,
        country,
        latitude = lat,
        longitude = lon,
        sunset
    };

    // Cache for 10 minutes
    cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));

    return Results.Ok(result);
});

//
// 11) WIKIPEDIA WRAPPER: /api/about-place
//     Example:
//     /api/about-place?title=Skyline%20Chili&lang=en
//
app.MapGet("/api/about-place", async (
    string title,
    string? lang,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    var cacheKey = $"about-place:{title}:{lang}";
    if (cache.TryGetValue(cacheKey, out var cachedResult))
    {
        return Results.Ok(cachedResult);
    }

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

    var result = new
    {
        title,
        extract,
        pageUrl
    };

    // Cache for 10 minutes
    cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));

    return Results.Ok(result);
});

app.Run();
