using AutoRefundOtherSports.Constants;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Microsoft.Extensions.Options;

namespace AutoRefund.UnitTests.Services;

public class MatchServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly ICache cache;

    private readonly MatchService service;

    private readonly LoggingService loggingService;

    private readonly IEnumerable<ResultDetail> resultDetails;

    public MatchServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        loggingService = Substitute.For<LoggingService>();
        cache = new RedisCache(new RedisCacheOptions
        {
            ConnectionString = "10.18.200.131:16379,password=foo1@bared,defaultDatabase=11",
            ApplicationName = "Settlement-LOCAL"
        });
        var settings = new AppSettingsKey()
        {
            RetryInterval = 1000,
            RetryCount = 15,
            EnvironmentName = "PRO",
        };

        var appSettingsOptions = Options.Create(settings);

        resultDetails = new List<ResultDetail>
            {
                new ResultDetail() { MatchId = 1, LeagueId = 123 },
                new ResultDetail { MatchId = 2, LeagueId = 1 , ResourceId = "0102"},
                new ResultDetail() { MatchId = 2, LeagueId = 1, ResourceId = "0101" }
            };

        service = new MatchServiceImp(repository, cache, appSettingsOptions, loggingService);
    }

    private async Task SetLeague(string leagueId)
    {
        const int cacheExpiredInDays = 1095;
        var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromDays(cacheExpiredInDays));
        await cache.SetAsync(CacheKey.LeagueId, leagueId, cachePolicy);
    }

    [Fact]
    public async Task GetMatches_ExistedIncludeLeagueIdAndExcludeLeagueId_ReturnsRightMatches()
    {
        await SetLeague("123");
        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>()).Returns(resultDetails);

        var list = await service.GetMatches();

        Assert.Equal(1, list[0].LeagueId);
    }

    [Fact]
    public async Task GetMatches_NotExistLeagueTest_ReturnAllMatches()
    {
        await SetLeague("0");
        repository.FetchAsync<ResultDetail>(Arg.Is<ResultListingCriteria>(i => i.SportType == SportType.Volleyball)).Returns(resultDetails);

        var list = await service.GetMatches();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetMatches_Always_ReturnAllMatchesTwoSport()
    {
        await SetLeague("0");
        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>()).Returns(resultDetails);

        var list = await service.GetMatches();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetMatches_ExcludeLeagueIdAndInUAT_ReturnsRightMatches()
    {
        var settings = new AppSettingsKey()
        {
            EnvironmentName = "UAT"
        };

        var appSettingsOptions = Options.Create(settings);

        var tempService = new MatchServiceImp(repository, cache, appSettingsOptions, loggingService);
        await SetLeague("123");

        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>()).Returns(resultDetails);

        var list = await tempService.GetMatches();

        Assert.Equal(123, list[0].LeagueId);
    }

    [Fact]
    public async Task GetMatches_EnvironmentIsLocal_ReturnsALlMatches()
    {
        var settings = new AppSettingsKey()
        {
            EnvironmentName = "Local"
        };

        var appSettingsOptions = Options.Create(settings);
        var tempService = new MatchServiceImp(repository, cache, appSettingsOptions, loggingService);
        repository.FetchAsync<ResultDetail>(Arg.Is<ResultListingCriteria>(i => i.SportType == SportType.Volleyball)).Returns(resultDetails);

        var list = await tempService.GetMatches();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task CheckMissedMatch_Always_ExecuteCriteria()
    {
        await service.CheckMissedMatch();

        await repository.Received().ExecuteAsync(Arg.Any<UpdateResultStatusCriteria>());
    }
}