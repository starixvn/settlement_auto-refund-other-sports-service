using AutoRefundOtherSports;
using AutoRefundOtherSports.Models;
using AutoRefundOtherSports.Services;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace AutoRefund.UnitTests.Services
{
    public class RefundControllerTests
    {
        private readonly IMatchService matchService;

        private readonly IVerificationService verificationService;

        private readonly IMatchPoolService matchPoolService;

        private readonly ILoggingService loggingService;

        private readonly IRefundController controller;

        public RefundControllerTests()
        {
            matchPoolService = Substitute.For<IMatchPoolService>();
            verificationService = Substitute.For<IVerificationService>();
            matchService = Substitute.For<IMatchService>();
            loggingService = Substitute.For<ILoggingService>();

            var appSettings = new AppSettingsKey()
            {
                RetryInterval = 500,
                RetryCount = 5
            };
            var appSettingsOptions = Options.Create(appSettings);
            controller = new RefundControllerImp(matchService, verificationService, matchPoolService, loggingService, appSettingsOptions);
        }

        [Fact]
        public async Task Process_NoExceptionHaveBeenThrown_PushMatchSucceedAndRemoveMatch()
        {
            var match = new Match();
            verificationService.When(fake => fake.VerifyResultRunning(match, 0)).Do(call => { });
            verificationService.When(fake => fake.VerifyResultStatus(match)).Do(call => { });

            await controller.Process(match);

            await matchService.Received().Push(match);
            matchPoolService.Received().RemoveMatch(match);
            await matchService.DidNotReceive().Update(match, Arg.Any<string>(), 3);
        }

        [Fact]
        public async Task Process_ThrowExceptionOnVerifyMatchAndTimeMax_InvokeRelatedMethods()
        {
            var match = new Match();
            verificationService.When(fake => fake.VerifyResultRunning(match, 0)).Do(call => { });
            verificationService.When(fake => fake.VerifyResultStatus(match)).Do(call =>
            {
                throw new AutoRefundException();
            });

            await controller.Process(match);

            await matchService.DidNotReceive().Push(match);
            await matchService.Received(8).Update(Arg.Any<Match>(), Arg.Any<string>(), Arg.Any<int>());
            await loggingService.Received().WriteLog(match, Arg.Any<AutoRefundException>());
            matchPoolService.Received().RemoveMatch(match);
        }
    }
}