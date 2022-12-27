using AutoRefundOtherSports;

namespace AutoRefund.UnitTests.Services;

public class RefundControllerTests
{
    private readonly MatchService matchService;

    private readonly VerificationService verificationService;

    private readonly MatchPoolService matchPoolService;

    private readonly LoggingService loggingService;

    private readonly RefundController controller;

    public RefundControllerTests()
    {
        matchPoolService = Substitute.For<MatchPoolService>();
        verificationService = Substitute.For<VerificationService>();
        matchService = Substitute.For<MatchService>();
        loggingService = Substitute.For<LoggingService>();

        controller = new RefundControllerImp(matchService, verificationService, matchPoolService, loggingService);
    }

    [Fact]
    public async Task Process_PassedValidated_SettleMatch()
    {
        var match = new Match();

        await controller.Process(match);

        await matchService.Received().Push(match);
        await matchService.DidNotReceive().HandleAutoRefundException(Arg.Any<Match>(), Arg.Any<AutoRefundException>());
        matchPoolService.Received().RemoveMatch(match);
    }

    [Fact]
    public async Task Process_ThrowException_InvokeRelatedMethodsAndStopLoop()
    {
        var match = new Match();
        verificationService.When(fake => fake.VerifyResultRunning(match)).Do(call => { throw new AutoRefundException(); });

        await controller.Process(match);

        await matchService.DidNotReceive().Push(match);
        await matchService.Received().HandleAutoRefundException(Arg.Any<Match>(), Arg.Any<AutoRefundException>());
        matchPoolService.Received().RemoveMatch(match);
    }
}