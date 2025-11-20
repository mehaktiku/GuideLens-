using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Collections.Generic;

public class CityModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    public CityModel(IWebHostEnvironment env) => _env = env;

    [BindProperty(SupportsGet = true)]
    public string Slug { get; set; } = "cincinnati";

    public string CityDisplay => string.IsNullOrWhiteSpace(Slug)
        ? "Cincinnati"
        : char.ToUpper(Slug[0]) + Slug[1..];

    public bool IsCincinnati => string.Equals(Slug, "cincinnati", System.StringComparison.OrdinalIgnoreCase);

   public string HeroBg { get; private set; } = "/img/Hero-img.jpg";

    public record CityTile(string Title, string Bg, string TargetUrl, bool ComingSoon = false);
    public List<CityTile> Tiles { get; private set; } = new();

    public void OnGet()
    {
        HeroBg = CityImg(Slug);

        Tiles = new()
        {
            new("Things to do",  CatBg("things"),      Url.Page("/Index", new { City = CityDisplay })!,                         !IsCincinnati),
            new("Museums",      CatBg("museum"),      Url.Page("/Index", new { City = CityDisplay, Category = "Museums" })!,   !IsCincinnati),
            new("Food",         CatBg("food"),        Url.Page("/Index", new { City = CityDisplay, Category = "Food" })!,       !IsCincinnati),
            new("Parks",        CatBg("parks"),       Url.Page("/Index", new { City = CityDisplay, Category = "Parks" })!,      !IsCincinnati),
            new("Fall Photos",  CatBg("fall"),        Url.Page("/Index", new { City = CityDisplay, Category = "FallPhotos" })!, !IsCincinnati),
            new("Landmarks",    CatBg("landmarks"),   Url.Page("/Index", new { City = CityDisplay, Category = "Landmarks" })!,  !IsCincinnati),
            new("Itineraries",  CatBg("itineraries"), "#", true),
            new("Travel Tips",  CatBg("tips"),        "#", true),
            new("Tourist Map",  CatBg("map"),         "#", true),
        };
    }

    // ---------- helpers ----------
    private string CityImg(string slug)
    {
        string[] candidates = new[]
        {
            $"/img/cities/{slug}.webp",
            $"/img/cities/{slug}.jpg",
            $"/img/cities/{slug}.jpeg",
            $"/img/cities/{slug}.png"
        };
        foreach (var rel in candidates)
        {
            var physical = Path.Combine(_env.WebRootPath,
                rel.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(physical)) return rel;
        }
        return HeroBg;
    }

    private string CatBg(string key)
    {
        // Prefer city-scoped category image first: /img/cat/{slug}/{key}.*
        string[] cityScoped = new[]
        {
            $"/img/cat/{Slug}/{key}.webp",
            $"/img/cat/{Slug}/{key}.jpg",
            $"/img/cat/{Slug}/{key}.jpeg",
            $"/img/cat/{Slug}/{key}.png"
        };
        foreach (var rel in cityScoped)
        {
            var physical = Path.Combine(_env.WebRootPath,
                rel.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(physical)) return rel;
        }

        // Then shared category image: /img/cat/{key}.*
        string[] shared = new[]
        {
            $"/img/cat/{key}.webp",
            $"/img/cat/{key}.jpg",
            $"/img/cat/{key}.jpeg",
            $"/img/cat/{key}.png"
        };
        foreach (var rel in shared)
        {
            var physical = Path.Combine(_env.WebRootPath,
                rel.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(physical)) return rel;
        }

        return HeroBg; // fallback
    }
}
