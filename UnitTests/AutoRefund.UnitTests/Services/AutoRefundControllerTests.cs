namespace AutoRefund.UnitTests.Services;

public class AutoRefundControllerTests
{
    private readonly MatchPoolService matchPoolService;

    private readonly RefundController refundController;

    private readonly AutoRefundController controller;

    public AutoRefundControllerTests()
    {
        matchPoolService = Substitute.For<MatchPoolService>();
        refundController = Substitute.For<RefundController>();
        controller = new AutoRefundControllerImp(matchPoolService, refundController);
    }

    [Fact]
    public async Task ProcessAsync_HasMatches_InvokeProcessMethod()
    {
        var match = new Match { MatchId = 123 };
        var matches = new List<Match> { match };
        matchPoolService.PeekNewMatch().Returns(matches);

        await controller.ProcessAsync();

        await refundController.Received().Process(match);
    }
}