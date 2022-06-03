using AutoRefundOtherSports.Exceptions;
using AutoRefundOtherSports.Models;
using AutoRefundOtherSports.Services;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoRefundOtherSports.Criteria;
using Xunit;
using AutoRefundOtherSports;

namespace AutoRefund.UnitTests.Services
{
    public class VerificationServiceTests
    {
        private readonly IVerificationStatusService verificationStatusService;

        private readonly IVerificationService service;

        public VerificationServiceTests()
        {
            verificationStatusService = Substitute.For<IVerificationStatusService>();
            service = new VerificationServiceImp(verificationStatusService);
        }

        [Fact]
        public async Task VerifyResultRunning_RetryFirstTime_ReturnAndNotCallRelatedMethods()
        {
            await service.VerifyResultRunning(new Match(), 1);

            await verificationStatusService.DidNotReceive().CheckTicketSettled(Arg.Any<Match>());
        }

        [Fact]
        public async Task VerifyResultRunning_ResultSettled_ThrowException()
        {
            var match = new Match
            {
                ResultDetails = new List<ResultDetail>
                {
                    new ResultDetail { ResultStatusId = 4 }
                }
            };
            await Assert.ThrowsAsync<SettledException>(() => service.VerifyResultRunning(match, 0));
        }

        [Fact]
        public async Task VerifyResultRunning_ResultRunning_ThrowException()
        {
            var match = new Match
            {
                ResultDetails = new List<ResultDetail>
                {
                    new ResultDetail { ResultStatusId = 1 }
                }
            };
            verificationStatusService.CheckTicketSettled(match).Returns(new TicketCheckingCriteria { ErrCode = 1 });

            await Assert.ThrowsAsync<AutoRefundException>(() => service.VerifyResultRunning(match, 0));
        }

        [Fact]
        public async Task VerifyResultStatus_ExistRunningOdds_ThrowException()
        {
            var match = new Match();
            verificationStatusService.CheckRunningOdds(match).Returns(new CheckOddsCriteria { ErrCode = 1 });

            await Assert.ThrowsAsync<AutoRefundException>(() => service.VerifyResultStatus(match));
        }

        [Fact]
        public async Task VerifyResultStatus_ExistMarkedTicket_ThrowException()
        {
            var match = new Match();
            verificationStatusService.CheckRunningOdds(match).Returns(new CheckOddsCriteria());
            verificationStatusService.CheckTicketExclusion(match).Returns(new CheckMarkedTicketsCriteria { IsExistMarkedTicket = true });

            await Assert.ThrowsAsync<TicketExclusionException>(() => service.VerifyResultStatus(match));
        }
    }
}