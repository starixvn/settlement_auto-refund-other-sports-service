using AutoRefundOtherSports.Exceptions;
using AutoRefundOtherSports.Models;
using AutoRefundOtherSports.Services;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

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
        public async Task VerifyResultRunning_ResultRunning_CallRelatedMethod()
        {
            var match = new Match
            {
                ResultDetails = new List<ResultDetail>
                {
                    new ResultDetail { ResultStatusId = 1 }
                }
            };
            await service.VerifyResultRunning(match, 0);

            await verificationStatusService.Received().CheckTicketSettled(match);
        }

        [Fact]
        public async Task VerifyResultRunning_NoExceptionExist_CallRelatedMethods()
        {
            var match = new Match();
            verificationStatusService.When(fake => fake.CheckRunningOdds(match)).Do(call => { });
            verificationStatusService.When(fake => fake.CheckTicketExclusion(match)).Do(call => { });

            await service.VerifyResultStatus(match);

            await verificationStatusService.Received().CheckRunningOdds(match);
            await verificationStatusService.Received().CheckTicketExclusion(match);
        }
    }
}