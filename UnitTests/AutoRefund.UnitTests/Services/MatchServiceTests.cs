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

    private readonly SettleService settleService;

    private readonly IEnumerable<ResultDetail> resultDetails;

    public MatchServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        loggingService = Substitute.For<LoggingService>();
        settleService = Substitute.For<SettleService>();
        cache = new RedisCache(new RedisCacheOptions
        {
            ConnectionString = $"ha.nexdev.net:16379,password=foo1@bared,defaultDatabase=11",
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

        service = new MatchServiceImp(repository, cache, appSettingsOptions, loggingService, settleService);
    }

    private void SetLeague(int leagueId)
    {
        const string keyIncludeLeagueIds = "Settlement-AutoPayoutOtherSport-LeagueId";
        const int cacheExpiredInDays = 1095;
        var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromDays(cacheExpiredInDays));
        cache.Set(keyIncludeLeagueIds, leagueId, cachePolicy);
    }

    [Fact]
    public async Task GetMatches_ExistedIncludeLeagueIdAndExcludeLeagueId_ReturnsRightMatches()
    {
        SetLeague(123);
        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>()).Returns(resultDetails);

        var list = await service.GetMatches();

        Assert.Equal(1, list[0].LeagueId);
    }

    [Fact]
    public async Task GetMatches_NotExistLeagueTest_ReturnAllMatches()
    {
        SetLeague(0);
        repository.FetchAsync<ResultDetail>(Arg.Is<ResultListingCriteria>(i => i.SportType == SportType.Volleyball)).Returns(resultDetails);

        var list = await service.GetMatches();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetMatches_Always_ReturnAllMatchesTwoSport()
    {
        SetLeague(0);
        repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>()).Returns(resultDetails);

        var list = await service.GetMatches();

        Assert.Equal(4, list.Count);
    }

    [Fact]
    public async Task GetMatches_ExcludeLeagueIdAndInUAT_ReturnsRightMatches()
    {
        var settings = new AppSettingsKey()
        {
            EnvironmentName = "UAT"
        };

        var appSettingsOptions = Options.Create(settings);

        var tempService = new MatchServiceImp(repository, cache, appSettingsOptions, loggingService, settleService);
        SetLeague(123);

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
        var tempService = new MatchServiceImp(repository, cache, appSettingsOptions, loggingService, settleService);
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

    [Fact]
    public async Task Push_SettleFailed_LogFailedAndUpdateStatus()
    {
        var match = new Match();
        settleService.SettleMatch(Arg.Any<Match>()).Returns(new Result
        {
            ErrCode = 1,
            ErrMsg = "Error"
        }); ;

        await service.Push(match);

        await loggingService.Received().WriteLog(match, "Error");
        await loggingService.Received().Update(match, Arg.Any<string>(), 3);
    }

    [Fact]
    public async Task Push_SettledSuccess_ExecutePushHedgeMatch()
    {
        var match = new Match { MatchId = 1 };
        settleService.SettleMatch(Arg.Any<Match>()).Returns(new Result());
        repository.GetAsync<HedgeMatchDetail>(Arg.Any<GetHedgeMatchCriteria>()).Returns(new HedgeMatchDetail { MatchId = 2 });

        await service.Push(match);

        await settleService.Received(2).SettleMatch(Arg.Any<Match>());
    }

    [Fact]
    public async Task Push_ReturnDeployError_UpdateMatch()
    {
        var match = new Match { MatchId = 1 };
        settleService.SettleMatch(Arg.Any<Match>()).Returns(new Result { ErrCode = 99, ErrMsg = "Deploy" });

        await service.Push(match);

        await loggingService.Received().Update(Arg.Any<Match>(), "", MatchStatus.Initial);
    }
}