using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;

namespace AutoRefundOtherSports.Services;

public interface RetryValidationService
{
    Task<bool> IsCheckingOddsAndMarkedTicket(Match match);

    Task<Result> ValidateEventTicket(EventTicketRequest request);
}

public class RetryValidationServiceImp : RetryValidationService
{
    private readonly AppSettingsKey appSettingsKey;

    private readonly VerificationStatusService verificationStatusService;

    private readonly ScaleOutSettleService scaleOutSettleService;

    public RetryValidationServiceImp(
        VerificationStatusService verificationStatusService,
        IOptions<AppSettingsKey> appSettingsKey,
        ScaleOutSettleService scaleOutSettleService)
    {
        this.appSettingsKey = appSettingsKey.Value;
        this.verificationStatusService = verificationStatusService;
        this.scaleOutSettleService = scaleOutSettleService;
    }

    public async Task<bool> IsCheckingOddsAndMarkedTicket(Match match)
    {
        for (var i = 0; i <= appSettingsKey.RetryCount; i++)
        {
            var resultCheckingOdds = await verificationStatusService.CheckRunningOdds(match);
            if (resultCheckingOdds.ErrCode == 0 && !await verificationStatusService.IsExistMarkedTicket(match))
            {
                return true;
            }

            if (i < appSettingsKey.RetryCount)
            {
                await Task.Delay(appSettingsKey.RetryInterval);
            }
        }

        return false;
    }

    public async Task<Result> ValidateEventTicket(EventTicketRequest request)
    {
        for (var i = 0; i <= appSettingsKey.RetryValidateCount; i++)
        {
            var resultMessage = await scaleOutSettleService.ValidateEventAndTicket(request);
            if (resultMessage.ErrorCode == ErrorCode.MatchInQueue && i < appSettingsKey.RetryValidateCount)
            {
                await Task.Delay(appSettingsKey.RetryValidate);
                continue;
            }
            return new Result
            {
                ErrCode = resultMessage.ErrorCode,
                ErrMsg = resultMessage.ErrorMessage
            };
        }

        return new Result();
    }
}