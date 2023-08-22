namespace AutoRefundOtherSports.Services;

public interface AutoRefundController
{
    Task ProcessAsync();
}

public class AutoRefundControllerImp : AutoRefundController
{
    private readonly MatchPoolService matchPoolService;

    private readonly RefundController refundController;

    public AutoRefundControllerImp(
        MatchPoolService matchPoolService,
        RefundController refundController
    )
    {
        this.matchPoolService = matchPoolService;
        this.refundController = refundController;
    }

    public async Task ProcessAsync()
    {
        var newMatches = (await matchPoolService.PeekNewMatch()).ToList();
        //newMatches.ForEach(m =>
        //{
        //    Task.Run(() => refundController.Process(m))
        //        .ConfigureAwait(false);
        //});
        foreach (var match in newMatches)
        {
            await refundController.Process(match);
        }
    }
}