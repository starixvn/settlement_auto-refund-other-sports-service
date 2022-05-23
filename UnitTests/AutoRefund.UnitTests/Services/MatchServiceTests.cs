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

        public MatchServiceTests()
        {
            repository = Substitute.For<IDynamicRepository>();
            createQueryService = Substitute.For<ICreateQueryService>();
            cache = new RedisCache(new RedisCacheOptions
            {
                ConnectionString = $"ha.nexdev.net:16379,password=foo1@bared,defaultDatabase=11",
                ApplicationName = "Settlement-LOCAL"
            });
            var settings = new AppSettingsKey()
            {
                RetryInterval = 1000,
                RetryCount = 15,
                EnvironmentName = "PRO"
            };

            var appSettingsOptions = Options.Create(settings);

            service = new MatchServiceImp(repository, cache, appSettingsOptions, createQueryService);
        }

        [Fact]
        public async Task GetMatches_ExistedIncludeLeagueIdAndExcludeLeagueId_ReturnsRightMatches()
        {
            IEnumerable<ResultDetail> resultDetails = new List<ResultDetail>
            {
                new ResultDetail() { MatchId = 1, LeagueId = 123 },
                new ResultDetail { MatchId = 2, LeagueId = 1 , ResourceId = "0102"},
                new ResultDetail() { MatchId = 2, LeagueId = 1, ResourceId = "0101" }
            };
            repository.FetchAsync<ResultDetail>(Arg.Any<ResultListingCriteria>()).Returns(resultDetails);
            const string KEY_INCLUDE_LEAGUEIDS = "Settlement-AutoPayout-AutoInstant-LeagueId";
            const int CACHE_EXPIRED_IN_DAYS = 1095;
            var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromDays(CACHE_EXPIRED_IN_DAYS));
            cache.Set(KEY_INCLUDE_LEAGUEIDS, 123, cachePolicy);

            var list = await service.GetMatches();

            Assert.Single(list);
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
            // Arrange
            var match = new Match { MatchId = 1 };
            createQueryService.QuerySettle(match).Returns(new SettleCriteria());

            // Act
            var result = await service.Push(match);

            // Assert
            await repository.Received().ExecuteAsync(Arg.Any<SettleCriteria>());
            Assert.NotNull(result);
        }
    }
}