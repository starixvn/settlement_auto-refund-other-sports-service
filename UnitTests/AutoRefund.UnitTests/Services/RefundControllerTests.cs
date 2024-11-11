using AutoRefundOtherSports.Constants;
using AutoRefundOtherSports.Exceptions;

namespace AutoRefund.UnitTests.Services;

public class RefundControllerTests
{
    private readonly MatchService matchService;

    private readonly VerificationService verificationService;

    private readonly MatchPoolService matchPoolService;

    private readonly SettleService settleService;

    private readonly RefundControllerImp controller;

    public RefundControllerTests()
    {
        matchPoolService = Substitute.For<MatchPoolService>();
        verificationService = Substitute.For<VerificationService>();
        matchService = Substitute.For<MatchService>();
        settleService = Substitute.For<SettleService>();

        controller = new RefundControllerImp(matchService, verificationService, matchPoolService, settleService);
    }

    [Fact]
    public async Task Process_ExistedGameWinnerAndPassedValidated_SettleMatch()
    {
        var match = new Match
        {
            ResultDetails =
            [
                new ResultDetail()
                {
                    SourceId = SourceIds.TennisGameWinner
                }
            ]
        };

        await controller.Process(match);

        Received.InOrder(() =>
        {
            verificationService.VerifyResultForGameWinner(Arg.Any<Match>());
            verificationService.VerifyResultRunning(Arg.Any<Match>());
            verificationService.VerifyResultStatus(Arg.Any<Match>());
            settleService.SettleMatch(Arg.Any<Match>());
            matchPoolService.RemoveMatch(Arg.Any<Match>());
        });

        await matchService.DidNotReceive().HandleAutoRefundException(Arg.Any<Match>(), Arg.Any<AutoRefundException>());
        await matchService.DidNotReceive().HandleAutoRefundException(Arg.Any<Match>(), Arg.Any<AutoRefundException>());
    }

    [Fact]
    public async Task Process_NotExistedGameWinnerAndPassedValidated_SettleMatch()
    {
        var match = new Match
        {
            ResultDetails = []
        };

        await controller.Process(match);

        Received.InOrder(() =>
        {
            verificationService.VerifyResultRunning(Arg.Any<Match>());
            verificationService.VerifyResultStatus(Arg.Any<Match>());
            settleService.SettleMatch(Arg.Any<Match>());
            matchPoolService.RemoveMatch(Arg.Any<Match>());
        });

        await verificationService.DidNotReceive().VerifyResultForGameWinner(Arg.Any<Match>());
        await matchService.DidNotReceive().HandleAutoRefundException(Arg.Any<Match>(), Arg.Any<AutoRefundException>());
        await matchService.DidNotReceive().HandleAutoRefundException(Arg.Any<Match>(), Arg.Any<AutoRefundException>());
    }

    [Fact]
    public async Task Process_ThrowException_InvokeRelatedMethodsAndStopLoop()
    {
        var match = new Match
        {
            ResultDetails = []
        };
        verificationService.When(fake => fake.VerifyResultRunning(match)).Do(_ => throw new AutoRefundException());

        await controller.Process(match);

        await settleService.DidNotReceive().SettleMatch(match);
        await matchService.Received().HandleAutoRefundException(Arg.Any<Match>(), Arg.Any<AutoRefundException>());
        matchPoolService.Received().RemoveMatch(match);
    }
}