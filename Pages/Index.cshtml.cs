using System;
using GuideLens.Models;
using GuideLens.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly RecommendationService _svc;
    public IndexModel(RecommendationService svc) => _svc = svc;

    // Your existing query (now assumed to include Query.City = "Cincinnati" by default)
    [BindProperty(SupportsGet = true)]
    public RecommendationQuery Query { get; set; } = new();

    // NEW: Ohio city dropdown options (UI-only; safe to change later)
    public IReadOnlyList<string> Cities { get; } =
        new[] { "Cincinnati", "Columbus", "Cleveland", "Dayton" };

    // Optional: show a “coming soon” note if a non-Cincinnati city is chosen
    public bool ComingSoon { get; private set; }

    public PagedResult<Recommendation> Results { get; private set; } = new();
    public IReadOnlyList<string> Neighborhoods { get; private set; } = Array.Empty<string>();

    public void OnGet()
    {
        // Ensure a default city so links without &City= still work
        if (string.IsNullOrWhiteSpace(Query.City))
            Query.City = "Cincinnati";

        // UI hint only (we still return Cincinnati data so nothing breaks)
        ComingSoon = !string.Equals(Query.City, "Cincinnati", StringComparison.OrdinalIgnoreCase);

        Neighborhoods = _svc.Neighborhoods();

        // Keep current behavior: always query the existing Cincinnati dataset.
        // (When you add other cities' data, switch on Query.City here.)
        Results = _svc.Query(Query);
    }
}
