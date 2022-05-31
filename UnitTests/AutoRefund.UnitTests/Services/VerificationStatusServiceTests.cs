using AutoRefundOtherSports.Criteria;
using AutoRefundOtherSports.Exceptions;
using AutoRefundOtherSports.Models;
using AutoRefundOtherSports.Services;
using Fanex.Data.Repository;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoRefundOtherSports;
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
        public async Task CheckRunningOdds_RunningOdds_ThrowException()
        {
            var criteria = new CheckOddsCriteria();
            createQueryService.CreateQueryCheckOdds(Arg.Any<Match>()).Returns(criteria);
            repository.When(fake => fake.ExecuteAsync(criteria)).Do(call =>
            {
                criteria.ErrCode = 1;
            });

            await Assert.ThrowsAsync<AutoRefundException>(() => service.CheckRunningOdds(new Match()));
        }

        [Fact]
        public async Task CheckTicketExclusion_TicketExclusion_ThrowException()
        {
            var match = new Match
            {
                ResultDetails = new List<ResultDetail>
                {
                    new ResultDetail()
                }
            };
            await repository.ExecuteAsync(Arg.Do<CheckMarkedTicketsCriteria>(item => item.IsExistMarkedTicket = true));
            await Assert.ThrowsAsync<TicketExclusionException>(() => service.CheckTicketExclusion(match));
        }

        [Fact]
        public async Task CheckTicketSettled_TickedSettled_ThrowException()
        {
            var criteria = new TicketCheckingCriteria();
            createQueryService.CreateQueryCheckTicketRunning(Arg.Any<Match>()).Returns(criteria);
            repository.When(fake => fake.ExecuteAsync(criteria)).Do(call =>
            {
                criteria.ErrCode = 1;
            });

            await Assert.ThrowsAsync<AutoRefundException>(() => service.CheckTicketSettled(new Match()));
        }
    }
}