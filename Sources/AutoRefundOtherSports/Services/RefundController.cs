using AutoRefundOtherSports.Exceptions;

namespace AutoRefundOtherSports.Services;

public interface RefundController
{
    Task Process(Match match);
}

public class RefundControllerImp(
    MatchService matchService,
    VerificationService verificationService,
    MatchPoolService matchPoolService,
    SettleService settleService)
    : RefundController
{
    public async Task Process(Match match)
    {
        try
        {
            await VerifyResult(match);
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

    private async Task VerifyResult(Match match)
    {
        if (match.IsExistsGameWinner)
        {
            await verificationService.VerifyResultForGameWinner(match);
        }

        verificationService.VerifyResultRunning(match);
        await verificationService.VerifyResultStatus(match);
    }
}