using AutoRefundOtherSports.Criteria;
using AutoRefundOtherSports.Models;
using AutoRefundOtherSports.Services;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AutoRefund.UnitTests.Services
{
    public class MatchServiceTests
    {
        private readonly IDynamicRepository repository;

        private readonly ICache cache;

        private readonly ICreateQueryService createQueryService;

        private readonly IMatchService service;

        private readonly ILoggingService loggingService;

        private readonly IEnumerable<ResultDetail> resultDetails;

        public MatchServiceTests()
        {
            repository = Substitute.For<IDynamicRepository>();
            createQueryService = Substitute.For<ICreateQueryService>();
            loggingService = Substitute.For<ILoggingService>();
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

            service = new MatchServiceImp(repository, cache, appSettingsOptions, createQueryService, loggingService);
        }

        private void SetLeague(int leagueId)
        {
            const string keyIncludeLeagueIds = "Settlement-AutoPayout-AutoInstant-LeagueId";
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

            var tempService = new MatchServiceImp(repository, cache, appSettingsOptions, createQueryService, loggingService);
            SetLeague(123);

            repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>()).Returns(resultDetails);

            var list = await tempService.GetMatches();

            Assert.Equal(123, list[0].LeagueId);
        }

        [Fact]
        public async Task CheckMissedMatch_Always_ExecuteCriteria()
        {
            await service.CheckMissedMatch();

            await repository.Received().ExecuteAsync(Arg.Any<UpdateResultStatusCriteria>());
        }

        [Fact]
        public async Task UpdateRemark_Always_ExecuteCriteria()
        {
            var criteria = new UpdateResultStatusCriteria();
            createQueryService.QueryUpdateResult(Arg.Any<Match>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(criteria);

            await service.Update(new Match(), "", 0);

            await repository.Received().ExecuteAsync(criteria);
        }

        [Fact]
        public async Task Push_Always_ExecuteCriteria()
        {
            var match = new Match { MatchId = 1 };
            createQueryService.QuerySettle(match).Returns(new SettleCriteria());

            await service.Push(match);

            await repository.Received().ExecuteAsync(Arg.Any<SettleCriteria>());
        }

        [Fact]
        public async Task Push_SettleFailed_LogFailedAndUpdateStatus()
        {
            var match = new Match();
            createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria());
            createQueryService.QueryUpdateResult(match, Arg.Any<string>(), Arg.Any<int>())
                .Returns(new UpdateResultStatusCriteria());
            await repository.ExecuteAsync(Arg.Do<SettleCriteria>(item =>
            {
                item.ErrCode = 1;
                item.ErrMsg = "Error";
            }));

            await service.Push(match);

            await loggingService.Received().WriteLog(match, "Error");
            createQueryService.Received().QueryUpdateResult(match, Arg.Any<string>(), 3);
        }

        [Fact]
        public async Task Push_SettledSuccess_ExecutePushHedgeMatch()
        {
            var match = new Match { MatchId = 1 };
            createQueryService.QuerySettle(match).Returns(new SettleCriteria());
            repository.GetAsync<HedgeMatchDetail>(Arg.Any<GetHedgeMatchCriteria>()).Returns(new HedgeMatchDetail { MatchId = 2 });

            await service.Push(match);

            await repository.Received(2).ExecuteAsync(Arg.Any<SettleCriteria>());
        }

        [Fact]
        public async Task Push_ReturnDeployError_SetCache()
        {
            var settings = new AppSettingsKey()
            {
                DeploymentDelay = "2",
                DeploymentSpName = "Deploy"
            };

            var appSettingsOptions = Options.Create(settings);
            var tempCache = Substitute.For<ICache>();
            var newService = new MatchServiceImp(repository, tempCache, appSettingsOptions, createQueryService, loggingService);
            var match = new Match { MatchId = 1 };
            createQueryService.QuerySettle(match).Returns(new SettleCriteria());
            await repository.ExecuteAsync(Arg.Do<SettleCriteria>(item =>
            {
                item.ErrCode = 99;
            }));

            await newService.Push(match);

            await tempCache.Received().SetAsync("Deploy", 1, Arg.Any<CacheItemOptions>());
        }
    }
}