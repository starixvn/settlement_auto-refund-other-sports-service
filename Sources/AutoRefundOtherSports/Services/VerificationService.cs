using AutoRefundOtherSports.Exceptions;
using Microsoft.Extensions.Options;

namespace AutoRefundOtherSports.Services;

public interface IVerificationService
{
    void VerifyResultRunning(Match match);

    Task VerifyResultStatus(Match match);
}

public class VerificationServiceImp : IVerificationService
{
    private const byte SettlementStatusCalculated = 4;

    private const byte ResultStatusUnCalculate = 4;

    private readonly IVerificationStatusService verificationStatusService;

    private readonly AppSettingsKey appSettingsKey;

    private readonly ILoggingService loggingService;

    public VerificationServiceImp(IVerificationStatusService verificationStatusService,
        IOptions<AppSettingsKey> appSettingsKey,
        ILoggingService loggingService)
    {
        this.appSettingsKey = appSettingsKey.Value;
        this.verificationStatusService = verificationStatusService;
        this.loggingService = loggingService;
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
        for (var i = 0; i < appSettingsKey.RetryCount + 1; i++)
        {
            var resultCheckingOdds = await verificationStatusService.CheckRunningOdds(match);
            if (resultCheckingOdds.ErrCode == 0 && !(await verificationStatusService.CheckTicketExclusion(match)))
            {
                return;
            }

            if (i < appSettingsKey.RetryCount)
            {
                await Task.Delay(appSettingsKey.RetryInterval);
            }
            else
            {
                throw new OddsAndMarkedTicketException();
            }
        }
    }
}