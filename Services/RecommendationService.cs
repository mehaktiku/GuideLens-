using GuideLens.Data;     // <— to reach your static DataLoader
using GuideLens.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GuideLens.Services;

public class RecommendationService
{
    private readonly List<Recommendation> _all;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    // We need ContentRoot to find JsonData/CincinnatiData.json
    public RecommendationService(IHostEnvironment env, IMemoryCache cache)
    {
        _all = DataLoader.LoadFromContentRoot(env.ContentRootPath);
        _cache = cache;
        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        };
    }

    public PagedResult<Recommendation> Query(RecommendationQuery q)
    {
        // Basic validation/clamping
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize <= 0 ? 12 : Math.Min(q.PageSize, 100);

        var cacheKey = $"recs::{q.City ?? ""}|{q.Q ?? ""}|{q.Category}|{q.Neighborhood ?? ""}|{q.SortBy ?? ""}|{page}|{pageSize}";
        if (_cache.TryGetValue<PagedResult<Recommendation>>(cacheKey, out var cached))
        {
            return cached!;
        }

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

        // Search across Name / TheBestOffer / NoteTip using precomputed lower-case fields
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var term = q.Q.Trim().ToLowerInvariant();
            data = data.Where(r =>
                (r.NameLower?.Contains(term) ?? false) ||
                (r.TheBestOfferLower?.Contains(term) ?? false) ||
                (r.NoteTipLower?.Contains(term) ?? false));
        }

        // Sorting
        data = q.SortBy switch
        {
            "area" => data.OrderBy(r => r.Neighborhood).ThenBy(r => r.Name),
            "category" => data.OrderBy(r => r.Category).ThenBy(r => r.Name),
            _ => data.OrderBy(r => r.Name)
        };

        // Materialize once to avoid double enumeration
        var list = data.ToList();

        // Paging
        var total = list.Count;
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var result = new PagedResult<Recommendation>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        _cache.Set(cacheKey, result, _cacheOptions);

        return result;
    }

    // For the Neighborhood dropdown
    public IReadOnlyList<string> Neighborhoods()
    {
        const string key = "neighborhoods";
        if (_cache.TryGetValue<IReadOnlyList<string>>(key, out var cached))
            return cached!;

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in _all)
        {
            if (!string.IsNullOrWhiteSpace(r.Neighborhood))
                set.Add(r.Neighborhood);
        }

        var list = set.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        _cache.Set(key, list, _cacheOptions);
        return list;
    }
}
