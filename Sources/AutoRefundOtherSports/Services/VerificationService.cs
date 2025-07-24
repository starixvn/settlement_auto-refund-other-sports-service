using AutoRefundOtherSports.Exceptions;

namespace AutoRefundOtherSports.Services;

public interface VerificationService
{
    void VerifyResultRunning(Match match);

    Task VerifyResultStatus(Match match);

    Task VerifyResultForGameWinner(Match match);
}

public class VerificationServiceImp(
    RetryValidationService retryValidationService,
    VerificationStatusService verificationStatusService) : VerificationService
{
    private const byte SettlementStatusCalculated = 4;

    private const byte ResultStatusUnCalculate = 4;

    public void VerifyResultRunning(Match match)
    {
        var resultNotRunning = match.ResultDetails.Exists(item => item.SettlementStatusId == SettlementStatusCalculated || item.ResultStatusId == ResultStatusUnCalculate);
        if (resultNotRunning)
        {
            throw new SettledException();
        }
    }

    public async Task VerifyResultStatus(Match match)
    {
        var isChecking = await retryValidationService.IsCheckingOddsAndMarkedTicket(match);
        if (!isChecking)
        {
            throw new OddsAndMarkedTicketException();
        }
    }

    public async Task VerifyResultForGameWinner(Match match)
    {
        var (isExistTicket, isExistTicketSettled) = await verificationStatusService.IsTicketExisting(match);

        if (!isExistTicket)
        {
            throw new MatchNoTicketException();
        }

        if (isExistTicketSettled)
        {
            throw new ChildMatchExistSettledTicket();
        }
    }
}