using GuideLens.Services;

var builder = WebApplication.CreateBuilder(args);

// 1) Register Razor Pages
builder.Services.AddRazorPages();

// 2) Register your filtering service
builder.Services.AddSingleton<RecommendationService>();

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

// 4) Map Razor Pages endpoints (this enables /, /Index, etc.)
app.MapRazorPages();

app.Run();
