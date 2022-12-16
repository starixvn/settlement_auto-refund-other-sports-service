using AutoRefundOtherSports.Criteria;
using AutoRefundOtherSports.Models;
using AutoRefundOtherSports.Services;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AutoRefund.UnitTests.Services
{
    public class VerificationStatusServiceTests
    {
        private readonly ICreateQueryService createQueryService;

        private readonly IDynamicRepository repository;

        private readonly IVerificationStatusService service;

        public VerificationStatusServiceTests()
        {
            createQueryService = Substitute.For<ICreateQueryService>();
            repository = Substitute.For<IDynamicRepository>();
            var appSettingsOptions = Options.Create(new AppSettingsKey { SiteDbServer = new List<string> { "01", "02" } });
            service = new VerificationStatusServiceImp(createQueryService, repository, appSettingsOptions);
        }

        [Fact]
        public async Task CheckRunningOdds_Always_ResultCriteria()
        {
            var criteria = new CheckOddsCriteria();
            createQueryService.CreateQueryCheckOdds(Arg.Any<Match>()).Returns(criteria);
            repository.When(fake => fake.ExecuteAsync(criteria)).Do(call =>
            {
                criteria.ErrCode = 1;
            });
            var result = await service.CheckRunningOdds(new Match());
            Assert.Equal(criteria, result);
        }

        [Fact]
        public async Task CheckTicketExclusion_Always_ResultCriteria()
        {
            var match = new Match
            {
                MatchId = 1,
                ResultDetails = new List<ResultDetail>
                {
                    new ResultDetail()
                }
            };
            await repository.ExecuteAsync(Arg.Do<CheckMarkedTicketsCriteria>(item => item.IsExistMarkedTicket = true));
            var result = await service.CheckTicketExclusion(match);
            Assert.True(result);
        }

        [Fact]
        public async Task CheckTicketSettled_Always_ResultCriteria()
        {
            var criteria = new TicketCheckingCriteria();
            createQueryService.CreateQueryCheckTicketRunning(Arg.Any<Match>()).Returns(criteria);
            repository.When(fake => fake.ExecuteAsync(criteria)).Do(call =>
            {
                criteria.ErrCode = 1;
            });

            var result = await service.CheckTicketSettled(new Match());
            Assert.Equal(criteria, result);
        }
    }
}