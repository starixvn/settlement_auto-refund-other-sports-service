using AutoRefundOtherSports.Exceptions;

namespace AutoRefund.UnitTests.Services;

public class RefundControllerTests
{
    private readonly MatchService matchService;

    private readonly VerificationService verificationService;

    private readonly MatchPoolService matchPoolService;

    private readonly SettleService settleService;

    private readonly RefundController controller;

    public RefundControllerTests()
    {
        matchPoolService = Substitute.For<MatchPoolService>();
        verificationService = Substitute.For<VerificationService>();
        matchService = Substitute.For<MatchService>();
        settleService = Substitute.For<SettleService>();

        controller = new RefundControllerImp(matchService, verificationService, matchPoolService, settleService);
    }

    [Fact]
    public async Task Process_PassedValidated_SettleMatch()
    {
        var match = new Match();

        await controller.Process(match);

        await settleService.Received().SettleMatch(match);
        await matchService.DidNotReceive().HandleAutoRefundException(Arg.Any<Match>(), Arg.Any<AutoRefundException>());
        matchPoolService.Received().RemoveMatch(match);
    }

    [Fact]
    public async Task Process_ThrowException_InvokeRelatedMethodsAndStopLoop()
    {
        var match = new Match();
        verificationService.When(fake => fake.VerifyResultRunning(match)).Do(_ => { throw new AutoRefundException(); });

        await controller.Process(match);

        await settleService.DidNotReceive().SettleMatch(match);
        await matchService.Received().HandleAutoRefundException(Arg.Any<Match>(), Arg.Any<AutoRefundException>());
        matchPoolService.Received().RemoveMatch(match);
    }
}