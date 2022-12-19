namespace AutoRefund.UnitTests.Services;

public class AutoRefundControllerTests
{
    private readonly IMatchPoolService matchPoolService;

    private readonly IRefundController refundController;

    private readonly IAutoRefundController controller;

    public AutoRefundControllerTests()
    {
        matchPoolService = Substitute.For<IMatchPoolService>();
        refundController = Substitute.For<IRefundController>();
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