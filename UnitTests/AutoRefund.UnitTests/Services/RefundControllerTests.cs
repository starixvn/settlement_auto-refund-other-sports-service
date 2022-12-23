using AutoRefundOtherSports;

namespace AutoRefund.UnitTests.Services;

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