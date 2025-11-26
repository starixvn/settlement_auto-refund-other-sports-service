using AutoRefundOtherSports.Constants;
using AutoRefundOtherSports.Exceptions;
using Fanex.Caching;
using Microsoft.Extensions.Options;

namespace AutoRefund.UnitTests.Services;

public class MatchServiceTests
{
    private readonly IDynamicRepository repository;
    private readonly ICache cache;
    private readonly IOptions<AppSettingsKey> appSettingsKey;
    private readonly LoggingService loggingService;
    private readonly MatchService matchService;

    public MatchServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        cache = Substitute.For<ICache>();
        appSettingsKey = Substitute.For<IOptions<AppSettingsKey>>();
        loggingService = Substitute.For<LoggingService>();

        matchService = new MatchServiceImp(repository, cache, appSettingsKey, loggingService);
    }

    [Fact]
    public async Task GetMatches_WhenRepositoryReturnsData_ReturnsGroupedMatches()
    {
        // Arrange
        var resultDetails = new List<ResultDetail>
        {
            new() { MatchId = 1, LeagueId = 100, TypeId = 1, SportType = SportType.Tennis, ParentId = 0, ResourceId = "R1" },
            new() { MatchId = 1, LeagueId = 100, TypeId = 1, SportType = SportType.Tennis, ParentId = 0, ResourceId = "R2" },
            new() { MatchId = 2, LeagueId = 200, TypeId = 2, SportType = SportType.Volleyball, ParentId = 0, ResourceId = "R3" }
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>())
            .Returns(resultDetails.Where(r => r.SportType == SportType.Tennis).ToList(),
                    resultDetails.Where(r => r.SportType == SportType.Volleyball).ToList(),
                    new List<ResultDetail>(),
                    new List<ResultDetail>(),
                    new List<ResultDetail>(),
                    new List<ResultDetail>(),
                    new List<ResultDetail>());

        cache.TryGetAsync<string>(CacheKey.LeagueId).Returns((true, ""));
        appSettingsKey.Value.Returns(new AppSettingsKey { EnvironmentName = "DEV" });

        // Act
        var result = await matchService.GetMatches();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.MatchId == 1 && m.ResultDetails.Count == 2);
        Assert.Contains(result, m => m.MatchId == 2 && m.ResultDetails.Count == 1);
    }

    [Fact]
    public async Task GetMatches_InProductionEnvironment_FiltersOutSpecifiedLeagues()
    {
        // Arrange
        var resultDetails = new List<ResultDetail>
        {
            new() { MatchId = 1, LeagueId = 100, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 },
            new() { MatchId = 2, LeagueId = 200, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 }
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>())
            .Returns(resultDetails);

        cache.TryGetAsync<string>(CacheKey.LeagueId).Returns((true, "100,300"));
        appSettingsKey.Value.Returns(new AppSettingsKey { EnvironmentName = "PRO" });

        // Act
        var result = await matchService.GetMatches();

        // Assert
        Assert.Single(result);
        Assert.Equal(200, result.First().LeagueId);
    }

    [Fact]
    public async Task GetMatches_InUATEnvironment_IncludesOnlySpecifiedLeagues()
    {
        // Arrange
        var resultDetails = new List<ResultDetail>
        {
            new() { MatchId = 1, LeagueId = 100, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 },
            new() { MatchId = 2, LeagueId = 200, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 }
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>())
            .Returns(resultDetails);

        cache.TryGetAsync<string>(CacheKey.LeagueId).Returns((true, "100,300"));
        appSettingsKey.Value.Returns(new AppSettingsKey { EnvironmentName = "UAT" });

        // Act
        var result = await matchService.GetMatches();

        // Assert
        Assert.Single(result);
        Assert.Equal(100, result.First().LeagueId);
    }

    [Fact]
    public async Task GetMatches_InProductionWithEmptyCache_ReturnsAllMatches()
    {
        // Arrange
        var resultDetails = new List<ResultDetail>
        {
            new() { MatchId = 1, LeagueId = 100, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 },
            new() { MatchId = 2, LeagueId = 200, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 }
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>())
            .Returns(resultDetails);

        cache.TryGetAsync<string>(CacheKey.LeagueId).Returns((true, ""));
        appSettingsKey.Value.Returns(new AppSettingsKey { EnvironmentName = "PRO" });

        // Act
        var result = await matchService.GetMatches();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetMatches_InProductionWithNullCache_ReturnsAllMatches()
    {
        // Arrange
        var resultDetails = new List<ResultDetail>
        {
            new() { MatchId = 1, LeagueId = 100, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 }
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>())
            .Returns(resultDetails);

        cache.TryGetAsync<string>(CacheKey.LeagueId).Returns((false, null));
        appSettingsKey.Value.Returns(new AppSettingsKey { EnvironmentName = "PRO" });

        // Act
        var result = await matchService.GetMatches();

        // Assert
        Assert.Single(result);
    }

    [Theory]
    [InlineData("DEV")]
    [InlineData("LOCAL")]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetMatches_InNonProdNonUATEnvironment_ReturnsAllMatches(string environmentName)
    {
        // Arrange
        var resultDetails = new List<ResultDetail>
        {
            new() { MatchId = 1, LeagueId = 100, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 },
            new() { MatchId = 2, LeagueId = 200, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 }
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>())
            .Returns(resultDetails);

        cache.TryGetAsync<string>(CacheKey.LeagueId).Returns((true, "100"));
        appSettingsKey.Value.Returns(new AppSettingsKey { EnvironmentName = environmentName });

        // Act
        var result = await matchService.GetMatches();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetMatches_CallsRepositoryForEachSportType()
    {
        // Arrange
        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>())
            .Returns(new List<ResultDetail>());

        cache.TryGetAsync<string>(CacheKey.LeagueId).Returns((true, ""));
        appSettingsKey.Value.Returns(new AppSettingsKey { EnvironmentName = "DEV" });

        // Act
        await matchService.GetMatches();

        // Assert
        await repository.Received(7).FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>());

        // Verify each sport type is called
        await repository.Received().FetchAsync<ResultDetail>(
            Arg.Is<ResultListingCriteria>(c => c.SportType == SportType.Volleyball));
        await repository.Received().FetchAsync<ResultDetail>(
            Arg.Is<ResultListingCriteria>(c => c.SportType == SportType.Badminton));
        await repository.Received().FetchAsync<ResultDetail>(
            Arg.Is<ResultListingCriteria>(c => c.SportType == SportType.Tennis));
        await repository.Received().FetchAsync<ResultDetail>(
            Arg.Is<ResultListingCriteria>(c => c.SportType == SportType.ESports));
        await repository.Received().FetchAsync<ResultDetail>(
            Arg.Is<ResultListingCriteria>(c => c.SportType == SportType.TableTennis));
        await repository.Received().FetchAsync<ResultDetail>(
            Arg.Is<ResultListingCriteria>(c => c.SportType == SportType.BaseBall));
        await repository.Received().FetchAsync<ResultDetail>(
            Arg.Is<ResultListingCriteria>(c => c.SportType == SportType.BeachVolleyball));
    }

    [Fact]
    public async Task CheckMissedMatch_ExecutesUpdateResultStatusCriteria()
    {
        // Arrange & Act
        await matchService.CheckMissedMatch();

        // Assert
        await repository.Received().ExecuteAsync(
            Arg.Is<UpdateResultStatusCriteria>(c => c.StatusId == MatchStatus.Failed));
    }

    [Fact]
    public async Task HandleAutoRefundException_CallsLoggingServiceMethods()
    {
        // Arrange
        var match = new Match { MatchId = 1 };
        var exception = new AutoRefundException { Description = "Test error" };

        // Act
        await matchService.HandleAutoRefundException(match, exception);

        // Assert
        await loggingService.Received().WriteLog(match, exception);
        await loggingService.Received().Update(match, exception.Description, MatchStatus.Failed);
    }

    [Fact]
    public async Task HandleAutoRefundException_WithNullException_StillCallsLoggingService()
    {
        // Arrange
        var match = new Match { MatchId = 1 };
        var exception = new AutoRefundException();

        // Act
        await matchService.HandleAutoRefundException(match, exception);

        // Assert
        await loggingService.Received().WriteLog(match, exception);
        await loggingService.Received().Update(match, exception.Description, MatchStatus.Failed);
    }

    [Fact]
    public async Task GetMatches_WithSpecialCharactersInCacheValue_HandlesCorrectly()
    {
        // Arrange
        var resultDetails = new List<ResultDetail>
        {
            new() { MatchId = 1, LeagueId = 100, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 }
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>())
            .Returns(resultDetails);

        cache.TryGetAsync<string>(CacheKey.LeagueId).Returns((true, "100,,200,"));
        appSettingsKey.Value.Returns(new AppSettingsKey { EnvironmentName = "UAT" });

        // Act
        var result = await matchService.GetMatches();

        // Assert
        Assert.Single(result); // Should still work despite empty strings in split result
    }

    //[Fact]
    //public async Task GetMatches_WhenRepositoryThrowsException_PropagatesException()
    //{
    //    // Arrange
    //    repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>())
    //        .ThrowsAsync(new InvalidOperationException("Database error"));

    //    cache.TryGetAsync<string>(CacheKey.LeagueId).Returns((true, ""));
    //    appSettingsKey.Value.Returns(new AppSettingsKey { EnvironmentName = "DEV" });

    //    // Act & Assert
    //    await Assert.ThrowsAsync<InvalidOperationException>(() => matchService.GetMatches());
    //}

    //[Fact]
    //public async Task GetMatches_WhenCacheThrowsException_PropagatesException()
    //{
    //    // Arrange
    //    var resultDetails = new List<ResultDetail>
    //    {
    //        new() { MatchId = 1, LeagueId = 100, TypeId = 1, SportType = SportType.Tennis, ParentId = 0 }
    //    };

    //    repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>())
    //        .Returns(resultDetails);

    //    cache.TryGetAsync<string>(CacheKey.LeagueId).ThrowsAsync(new InvalidOperationException("Cache error"));
    //    appSettingsKey.Value.Returns(new AppSettingsKey { EnvironmentName = "DEV" });

    //    // Act & Assert
    //    await Assert.ThrowsAsync<InvalidOperationException>(() => matchService.GetMatches());
    //}
}