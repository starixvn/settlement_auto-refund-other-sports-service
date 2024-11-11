using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;

namespace AutoRefundOtherSports.Services;

public interface RetryValidationService
{
    Task<bool> IsCheckingOddsAndMarkedTicket(Match match);

    Task<Result> ValidateEventTicket(EventTicketRequest request);
}

public class RetryValidationServiceImp(
    VerificationStatusService verificationStatusService,
    IOptions<AppSettingsKey> appSettingsKey,
    ScaleOutSettleService scaleOutSettleService)
    : RetryValidationService
{
    public async Task<bool> IsCheckingOddsAndMarkedTicket(Match match)
    {
        for (var i = 0; i <= appSettingsKey.Value.RetryCount; i++)
        {
            var resultCheckingOdds = await verificationStatusService.CheckRunningOdds(match);
            if (resultCheckingOdds.ErrCode == 0 && !await verificationStatusService.IsExistMarkedTicket(match))
            {
                return true;
            }

            if (i < appSettingsKey.Value.RetryCount)
            {
                await Task.Delay(appSettingsKey.Value.RetryInterval);
                match.TryCount++;
            }
        }

        return false;
    }

    public async Task<Result> ValidateEventTicket(EventTicketRequest request)
    {
        for (var i = 0; i <= appSettingsKey.Value.RetryValidateCount; i++)
        {
            var resultMessage = await scaleOutSettleService.ValidateEventAndTicket(request);
            if (resultMessage.ErrorCode == ErrorCode.MatchInQueue && i < appSettingsKey.Value.RetryValidateCount)
            {
                await Task.Delay(appSettingsKey.Value.RetryValidate);
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