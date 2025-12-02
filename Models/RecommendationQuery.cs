namespace GuideLens.Models;

public enum CategoryFilter { All = 0, Food, Museums, FallPhotos, Parks, Landmarks }


public class RecommendationQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageLength = 12;
    public const string DefaultSortBy = "name";
    public const string DefaultCity = "Cincinnati";


    public string? Q { get; set; }
    public CategoryFilter Category { get; set; } = CategoryFilter.All;
    public string? Neighborhood { get; set; }
    public string SortBy { get; set; } = DefaultSortBy;
    public int Page { get; set; } = DefaultPage;
    public int PageSize { get; set; } = DefaultPageLength;

    public string? City { get; set; } = "";

}
