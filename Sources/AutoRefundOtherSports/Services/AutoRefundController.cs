namespace AutoRefundOtherSports.Services;

public interface IAutoRefundController
{
    Task ProcessAsync();
}

public class AutoRefundControllerImp : IAutoRefundController
{
    private readonly IMatchPoolService matchPoolService;

    private readonly IRefundController refundController;

    public AutoRefundControllerImp(
        IMatchPoolService matchPoolService,
        IRefundController refundController
    )
    {
        this.matchPoolService = matchPoolService;
        this.refundController = refundController;
    }

    public async Task ProcessAsync()
    {
        var newMatches = (await matchPoolService.PeekNewMatch()).ToList();
        newMatches.ForEach(m =>
        {
            Task.Run(() => refundController.Process(m))
                .ConfigureAwait(false);
        });
    }
}