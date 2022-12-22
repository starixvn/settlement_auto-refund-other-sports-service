using AutoRefundOtherSports.Exceptions;

namespace AutoRefundOtherSports.Services;

public interface IVerificationService
{
    Task VerifyResultRunning(Match match, int retryIndex);

    Task VerifyResultStatus(Match match);
}

public class VerificationServiceImp : IVerificationService
{
    private const byte SettlementStatusCalculated = 4;

    private const byte ResultStatusUnCalculate = 4;

    private readonly IVerificationStatusService verificationStatusService;

    public VerificationServiceImp(IVerificationStatusService verificationStatusService)
    {
        this.verificationStatusService = verificationStatusService;
    }

    public async Task VerifyResultRunning(Match match, int retryIndex)
    {
        if (retryIndex > 0)
        {
            return;
        }

        var resultNotRunning = match.ResultDetails.Exists(item => item.SettlementStatusId == SettlementStatusCalculated || item.ResultStatusId == ResultStatusUnCalculate);
        if (resultNotRunning)
        {
            throw new SettledException { StopLoop = true };
        }

        var resultCheckingTicket = await verificationStatusService.CheckTicketSettled(match);
        if (resultCheckingTicket.ErrCode > 0)
        {
            throw new AutoRefundException { Description = resultCheckingTicket.ErrMsg, StopLoop = true };
        }
    }

    public async Task VerifyResultStatus(Match match)
    {
        var resultCheckingOdds = await verificationStatusService.CheckRunningOdds(match);
        if (resultCheckingOdds.ErrCode > 0)
        {
            throw new AutoRefundException(resultCheckingOdds.ErrMsg);
        }

        var isExistMarkedTicket = await verificationStatusService.CheckTicketExclusion(match);
        if (isExistMarkedTicket)
        {
            throw new TicketExclusionException();
        }
    }
}