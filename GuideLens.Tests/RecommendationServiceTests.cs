using Xunit;
using GuideLens.Services;
using GuideLens.Models;
using System.Collections.Generic;
using System.Linq;

namespace GuideLens.Tests;

public class RecommendationServiceTests
{
    private readonly RecommendationService _service;
    private readonly List<Recommendation> _testData;

    public RecommendationServiceTests()
    {
        _testData = new List<Recommendation>
        {
            new Recommendation { Name = "Place A", Category = "Food", Neighborhood = "Downtown", TheBestOffer = "Burger", NoteTip = "Tip A" },
            new Recommendation { Name = "Place B", Category = "Museums", Neighborhood = "OTR", TheBestOffer = "Art", NoteTip = "Tip B" },
            new Recommendation { Name = "Place C", Category = "Food", Neighborhood = "Downtown", TheBestOffer = "Pizza", NoteTip = "Tip C" },
            new Recommendation { Name = "Place D", Category = "Parks", Neighborhood = "Hyde Park", TheBestOffer = "Walk", NoteTip = "Tip D" }
        };

        _service = new RecommendationService(_testData);
    }

    [Fact]
    public void Query_ReturnsAll_WhenNoFilters()
    {
        var result = _service.Query(new RecommendationQuery());
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(4, result.Items.Count());
    }

    [Fact]
    public void Query_FiltersByCategory()
    {
        var query = new RecommendationQuery { Category = CategoryFilter.Food };
        var result = _service.Query(query);
        
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("Food", item.Category));
    }

    [Fact]
    public void Query_FiltersByNeighborhood()
    {
        var query = new RecommendationQuery { Neighborhood = "Downtown" };
        var result = _service.Query(query);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("Downtown", item.Neighborhood));
    }

    [Fact]
    public void Query_Search_MatchesName()
    {
        var query = new RecommendationQuery { Q = "Place A" };
        var result = _service.Query(query);

        Assert.Single(result.Items);
        Assert.Equal("Place A", result.Items.First().Name);
    }

    [Fact]
    public void Query_Paging_ReturnsCorrectPageSize()
    {
        var query = new RecommendationQuery { PageSize = 2, Page = 1 };
        var result = _service.Query(query);

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }
}
