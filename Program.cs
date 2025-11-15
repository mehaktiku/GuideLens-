using GuideLens.Services;

var builder = WebApplication.CreateBuilder(args);

// 1) Razor Pages
builder.Services.AddRazorPages();

// 2) App services
builder.Services.AddSingleton<RecommendationService>();

// --- NEW: lightweight caching for API lookups ---
builder.Services.AddMemoryCache();

// --- NEW: Typed HttpClients for the open APIs ---
// Nominatim / OpenStreetMap (requires a real contact in User-Agent)
builder.Services.AddHttpClient<NominatimService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GuideLens/1.0 (+saykarai@mail.uc.edu)");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");
});

// Open-Meteo for sunrise/sunset
builder.Services.AddHttpClient<OpenMeteoService>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
});

var app = builder.Build();

// 3) Standard middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 4) Razor Pages endpoints
app.MapRazorPages();

app.Run();
