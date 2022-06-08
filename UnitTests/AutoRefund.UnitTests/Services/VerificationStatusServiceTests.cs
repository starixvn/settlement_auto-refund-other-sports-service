using AutoRefundOtherSports.Criteria;
using AutoRefundOtherSports.Models;
using AutoRefundOtherSports.Services;
using Fanex.Data.Repository;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
            service = new VerificationStatusServiceImp(createQueryService, repository);
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

            var expected = new CheckMarkedTicketsCriteria
            {
                MatchId = match.MatchId,
                ResourceIds = "",
                IsExistMarkedTicket = true
            };
            await repository.ExecuteAsync(Arg.Do<CheckMarkedTicketsCriteria>(item => item.IsExistMarkedTicket = true));
            var result = await service.CheckTicketExclusion(match);
            Assert.Equal(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(result));
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