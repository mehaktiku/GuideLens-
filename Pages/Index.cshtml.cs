using System;
using System.Linq;
using GuideLens.Models;
using GuideLens.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly RecommendationService _svc;
    public IndexModel(RecommendationService svc) => _svc = svc;

    // Current query (city, search text, filters, paging)
    [BindProperty(SupportsGet = true)]
    public RecommendationQuery Query { get; set; } = new();

    // City dropdown
    public static class CityConstants { public const string Cincinnati = "Cincinnati"; /* ... other cities */ }
    public IReadOnlyList<string> Cities { get; } = new List<string> { CityConstants.Cincinnati, "Columbus", "Cleveland", "Dayton" }.AsReadOnly();

    // “Coming soon” notice for non-Cincinnati cities
    public bool ComingSoon { get; private set; }

    // NEW: true when user is opening the page for the first time (no query string yet)
    public bool IsInitialSearch { get; private set; }

    public PagedResult<Recommendation> Results { get; private set; } = new();
    public IReadOnlyList<string> Neighborhoods { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> AllNames { get; private set; } = Array.Empty<string>();

    public void OnGet()
    {
        // First visit? (no query parameters in URL)
        IsInitialSearch = !Request.Query.Keys.Any();

        

        // UI hint only
        ComingSoon = !string.Equals(Query.City, nameof(Cities.Cincinnati), StringComparison.OrdinalIgnoreCase);

        Neighborhoods = _svc.Neighborhoods();
        AllNames = _svc.GetAllNames();

        // ? IMPORTANT:
        // Only hit your recommendation service AFTER the user has interacted
        // with the form at least once.
        if (!IsInitialSearch)
        {
            Results = _svc.Query(Query);
        }
        // If IsInitialSearch == true, Results stays empty and the page will
        // show only the search box (no cards, no counts, no pagination).
    }
}
