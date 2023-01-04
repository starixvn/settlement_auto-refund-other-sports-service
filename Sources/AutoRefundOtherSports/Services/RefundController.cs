namespace AutoRefundOtherSports.Services;

public interface RefundController
{
    Task Process(Match match);
}

public class RefundControllerImp : RefundController
{
    private readonly MatchService matchService;

    private readonly VerificationService verificationService;

    private readonly MatchPoolService matchPoolService;

    private readonly LoggingService loggingService;

    private readonly SettleService settleService;

    public RefundControllerImp(
        MatchService matchService,
        VerificationService verificationService,
        MatchPoolService matchPoolService,
        LoggingService loggingService,
        SettleService settleService)
    {
        this.matchService = matchService;
        this.verificationService = verificationService;
        this.matchPoolService = matchPoolService;
        this.loggingService = loggingService;
        this.settleService = settleService;
    }

    public async Task Process(Match match)
    {
        try
        {
            verificationService.VerifyResultRunning(match);
            await verificationService.VerifyResultStatus(match);
            await settleService.SettleMatch(match);
        }
        catch (AutoRefundException exception)
        {
            await matchService.HandleAutoRefundException(match, exception);
        }
        finally
        {
            matchPoolService.RemoveMatch(match);
        }
    }
}