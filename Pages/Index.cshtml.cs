using GuideLens.Models;
using GuideLens.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly RecommendationService _svc;
    public IndexModel(RecommendationService svc) => _svc = svc;

    [BindProperty(SupportsGet = true)]
    public RecommendationQuery Query { get; set; } = new();

    public PagedResult<Recommendation> Results { get; private set; } = new();
    public IReadOnlyList<string> Neighborhoods { get; private set; } = Array.Empty<string>();

    public void OnGet()
    {
        Neighborhoods = _svc.Neighborhoods();
        Results = _svc.Query(Query);
    }
}
