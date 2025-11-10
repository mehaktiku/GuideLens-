using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class CitiesModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    public CitiesModel(IWebHostEnvironment env) => _env = env;

    public record CityTile(string Slug, string Name, string Bg);
    public List<CityTile> Items { get; private set; } = new();

    public void OnGet()
    {
        var cities = new (string Slug, string Name)[]
        {
            ("cincinnati","Cincinnati"),
            ("columbus","Columbus"),
            ("cleveland","Cleveland"),
            ("dayton","Dayton")
        };

        Items = cities
            .Select(c => new CityTile(c.Slug, c.Name, CityImg(c.Slug)))
            .ToList();
    }

    private string CityImg(string slug)
    {
        // Try .webp, .jpg, .jpeg, .png in that order
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

        return "/img/Hero-img.jpg"; // fallback
    }
}
