using AutoRefundOtherSports.Constants;

namespace AutoRefundOtherSports.Services;

public interface IRefundController
{
    Task Process(Match match);
}

public class RefundControllerImp : IRefundController
{
    private readonly IMatchService matchService;

    private readonly IVerificationService verificationService;

    private readonly IMatchPoolService matchPoolService;

    private readonly ILoggingService loggingService;

    public RefundControllerImp(
        IMatchService matchService,
        IVerificationService verificationService,
        IMatchPoolService matchPoolService,
        ILoggingService loggingService)
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