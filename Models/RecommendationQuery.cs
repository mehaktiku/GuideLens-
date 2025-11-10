namespace GuideLens.Models;

public enum CategoryFilter { All = 0, Food, Museums, FallPhotos, Parks, Landmarks }

public class RecommendationQuery
{
    public string? Q { get; set; }
    public CategoryFilter Category { get; set; } = CategoryFilter.All;
    public string? Neighborhood { get; set; }
    public string SortBy { get; set; } = "name";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    public string City { get; set; } = "Cincinnati";
}        
