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
    public IReadOnlyList<string> Cities { get; } =
        new[] { "Cincinnati", "Columbus", "Cleveland", "Dayton" };

    // “Coming soon” notice for non-Cincinnati cities
    public bool ComingSoon { get; private set; }

    // true when user is opening the page for the first time (no query string yet)
    public bool IsInitialSearch { get; private set; }

    public PagedResult<Recommendation> Results { get; private set; } = new();
    public IReadOnlyList<string> Neighborhoods { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> AllNames { get; private set; } = Array.Empty<string>();

    public void OnGet()
    {
        // First visit? (no query parameters in URL)
        IsInitialSearch = !Request.Query.Keys.Any();

        // UI hint only: any city other than Cincinnati is "coming soon"
        ComingSoon = !string.Equals(Query.City, "Cincinnati", StringComparison.OrdinalIgnoreCase);

        Neighborhoods = _svc.Neighborhoods();
        AllNames = _svc.GetAllNames();

        // Only hit the recommendation service:
        // - after the user has interacted with the form
        // - AND when the city is Cincinnati (the only one with data right now)
        if (!IsInitialSearch &&
            string.Equals(Query.City, "Cincinnati", StringComparison.OrdinalIgnoreCase))
        {
            Results = _svc.Query(Query);
        }
        // For non-Cincinnati cities, Results stays empty; the view will just show
        // the "<City> data coming soon." banner and no cards.
    }
}
