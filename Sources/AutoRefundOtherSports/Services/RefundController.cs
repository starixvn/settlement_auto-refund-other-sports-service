using AutoRefundOtherSports.Models;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Services
{
    public interface IRefundController
    {
        Task Process(Match match);
    }

    public class RefundControllerImp : IRefundController
    {
        private const int Failed = 3;

        private const int Running = 1;

        private readonly IMatchService matchService;

        private readonly IVerificationService verificationService;

        private readonly IMatchPoolService matchPoolService;

        private readonly ILoggingService loggingService;

        private readonly AppSettingsKey appSettingsKey;

        public RefundControllerImp(
            IMatchService matchService,
            IVerificationService verificationService,
            IMatchPoolService matchPoolService,
            ILoggingService loggingService,
            IOptions<AppSettingsKey> appSettingsKey)
        {
            this.matchService = matchService;
            this.verificationService = verificationService;
            this.matchPoolService = matchPoolService;
            this.loggingService = loggingService;
            this.appSettingsKey = appSettingsKey.Value;
        }

        public async Task Process(Match match)
        {
            var started = true;
            var retryCount = 0;
            await matchService.Update(match, "", Running);
            while (started && retryCount < appSettingsKey.RetryCount + 1)
            {
                try
                {
                    await verificationService.VerifyResultRunning(match, retryCount);
                    await verificationService.VerifyResultStatus(match);
                    var result = await matchService.Push(match);
                    await ProcessAfterRefund(match, result);
                    started = false;
                }
                catch (AutoRefundException exception)
                {
                    if (!exception.StopLoop)
                    {
                        retryCount = await ProcessWhenCachedException(retryCount, match, exception);
                    }
                    else
                    {
                        await HandleAutoRefundException(match, exception);
                        started = false;
                    }
                }
            }
        }

        private async Task HandleAutoRefundException(Match match, AutoRefundException exception)
        {
            await loggingService.WriteLog(match, exception);
            await matchService.Update(match, exception.Description, Failed);
            matchPoolService.RemoveMatch(match);
        }

        private async Task ProcessAfterRefund(Match match, Result result)
        {
            if (!result.IsSuccessful)
            {
                await loggingService.WriteLog(match, result.ErrMsg);
                await matchService.Update(match, result.ErrMsg, Failed);
            }
            matchPoolService.RemoveMatch(match);
        }

        private async Task<int> ProcessWhenCachedException(int retryCount, Match match, AutoRefundException exception)
        {
            await matchService.Update(match, exception.Description, Running);
            if (retryCount >= appSettingsKey.RetryCount)
            {
                await HandleAutoRefundException(match, exception);
            }
            else
            {
                await Task.Delay(appSettingsKey.RetryInterval);
            }

            return retryCount + 1;
        }
    }
}