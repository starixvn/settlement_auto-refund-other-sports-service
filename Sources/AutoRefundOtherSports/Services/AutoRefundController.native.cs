namespace AutoRefundOtherSports.Services;

public interface AutoRefundController
{
    Task ProcessAsync();
}

public class AutoRefundControllerImp(
    MatchPoolService matchPoolService,
    RefundController refundController)
    : AutoRefundController
{
    public async Task ProcessAsync()
    {
        var newMatches = await matchPoolService.PeekNewMatch();
        newMatches.ForEach(m =>
        {
            Task.Run(() => refundController.Process(m))
                .ConfigureAwait(false);
        });
    }
}