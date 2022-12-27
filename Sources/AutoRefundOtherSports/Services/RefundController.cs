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

    public RefundControllerImp(
        MatchService matchService,
        VerificationService verificationService,
        MatchPoolService matchPoolService,
        LoggingService loggingService)
    {
        this.matchService = matchService;
        this.verificationService = verificationService;
        this.matchPoolService = matchPoolService;
        this.loggingService = loggingService;
    }

    public async Task Process(Match match)
    {
        await loggingService.Update(match, "", MatchStatus.Running);
        try
        {
            verificationService.VerifyResultRunning(match);
            await verificationService.VerifyResultStatus(match);
            await matchService.Push(match);
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