namespace AutoRefundOtherSports.Services;

public interface RetryValidationService
{
    Task<bool> IsCheckingOddsAndMarkedTicket(Match match);
}

public class RetryValidationServiceImp : RetryValidationService
{
    private readonly AppSettingsKey appSettingsKey;

    private readonly VerificationStatusService verificationStatusService;

    public RetryValidationServiceImp(
        VerificationStatusService verificationStatusService,
        IOptions<AppSettingsKey> appSettingsKey)
    {
        this.appSettingsKey = appSettingsKey.Value;
        this.verificationStatusService = verificationStatusService;
    }

    public async Task<bool> IsCheckingOddsAndMarkedTicket(Match match)
    {
        for (var i = 0; i < appSettingsKey.RetryCount; i++)
        {
            var resultCheckingOdds = await verificationStatusService.CheckRunningOdds(match);
            var isExistMarkedTicket = await verificationStatusService.IsExistMarkedTicket(match);
            if (resultCheckingOdds.ErrCode == 0 && !isExistMarkedTicket)
            {
                return true;
            }

            if (i < appSettingsKey.RetryCount - 1)
            {
                await Task.Delay(appSettingsKey.RetryInterval);
            }
        }

        return false;
    }
}