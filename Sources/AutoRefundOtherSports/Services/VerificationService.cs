using AutoRefundOtherSports.Exceptions;

namespace AutoRefundOtherSports.Services;

public interface VerificationService
{
    void VerifyResultRunning(Match match);

    Task VerifyResultStatus(Match match);
}

public class VerificationServiceImp : VerificationService
{
    private const byte SettlementStatusCalculated = 4;

    private const byte ResultStatusUnCalculate = 4;

    private readonly RetryValidationService retryValidationService;

    public VerificationServiceImp(RetryValidationService retryValidationService)
    {
        this.retryValidationService = retryValidationService;
    }

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
}