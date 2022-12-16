using AutoRefundOtherSports.Models;
using Microsoft.Extensions.Options;

namespace AutoRefundOtherSports.Services
{
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
            var running = true;
            var retryCount = 0;
            await matchService.Update(match, "", MatchStatus.Running);
            while (running && retryCount < appSettingsKey.RetryCount + 1)
            {
                try
                {
                    await verificationService.VerifyResultRunning(match, retryCount);
                    await verificationService.VerifyResultStatus(match);
                    await matchService.Push(match);
                    running = false;
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
                        running = false;
                    }
                }
                finally
                {
                    matchPoolService.RemoveMatch(match);
                }
            }
        }

        private async Task HandleAutoRefundException(Match match, AutoRefundException exception)
        {
            await loggingService.WriteLog(match, exception);
            await matchService.Update(match, exception.Description, MatchStatus.Failed);
        }

        private async Task<int> ProcessWhenCachedException(int retryCount, Match match, AutoRefundException exception)
        {
            if (retryCount >= appSettingsKey.RetryCount)
            {
                await HandleAutoRefundException(match, exception);
            }
            else
            {
                await matchService.Update(match, exception.Description, MatchStatus.Running);
                await Task.Delay(appSettingsKey.RetryInterval);
            }

            return retryCount + 1;
        }
    }
}