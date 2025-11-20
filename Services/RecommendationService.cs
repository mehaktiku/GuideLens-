using GuideLens.Data;     // <— to reach your static DataLoader
using GuideLens.Models;

namespace GuideLens.Services;

public class RecommendationService
{
    private readonly List<Recommendation> _all;

    // We need ContentRoot to find JsonData/CincinnatiData.json
    public RecommendationService(IHostEnvironment env)
    {
        _all = DataLoader.LoadFromContentRoot(env.ContentRootPath);
    }

    // Constructor for Unit Testing
    public RecommendationService(List<Recommendation> data)
    {
        _all = data;
    }

    public PagedResult<Recommendation> Query(RecommendationQuery q)
    {
        IEnumerable<Recommendation> data = _all;

        // Category filter (enum text must match your string Category)
        if (q.Category != CategoryFilter.All)
        {
            var cat = q.Category.ToString(); // "Food", "Museums", etc.
            data = data.Where(r => string.Equals(r.Category, cat, StringComparison.OrdinalIgnoreCase));
        }

        // Neighborhood filter
        if (!string.IsNullOrWhiteSpace(q.Neighborhood))
        {
            data = data.Where(r => string.Equals(r.Neighborhood, q.Neighborhood, StringComparison.OrdinalIgnoreCase));
        }

        // Search across Name / TheBestOffer / NoteTip
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var term = q.Q.Trim();
            data = data.Where(r =>
                (r.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.TheBestOffer?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.NoteTip?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Sorting
        data = q.SortBy switch
        {
            "area" => data.OrderBy(r => r.Neighborhood).ThenBy(r => r.Name),
            "category" => data.OrderBy(r => r.Category).ThenBy(r => r.Name),
            _ => data.OrderBy(r => r.Name)
        };

        // Paging
        var total = data.Count();
        var items = data.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList();

        return new PagedResult<Recommendation>
        {
            Items = items,
            TotalCount = total,
            Page = q.Page,
            PageSize = q.PageSize
        };
    }

    // For the Neighborhood dropdown
    public IReadOnlyList<string> Neighborhoods() =>
        _all.Select(r => r.Neighborhood)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
