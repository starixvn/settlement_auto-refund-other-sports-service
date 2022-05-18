using AutoRefundOtherSports.Models;
using Fanex.Caching;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Services
{
    public interface IRefundController
    {
        void Process(Match match);
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

        private const int DeploymentError = 99;

        private readonly ICache cache;

        public RefundControllerImp(
            IMatchService matchService,
            IVerificationService verificationService,
            IMatchPoolService matchPoolService,
            ILoggingService loggingService,
            IOptions<AppSettingsKey> appSettingsKey,
            ICache cache)
        {
            this.matchService = matchService;
            this.verificationService = verificationService;
            this.matchPoolService = matchPoolService;
            this.loggingService = loggingService;
            this.cache = cache;
            this.appSettingsKey = appSettingsKey.Value;
        }

        public async void Process(Match match)
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
                    ProcessAfterRefund(match, result);
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
                        HandleAutoRefundException(match, exception);
                        started = false;
                    }
                }
            }
        }

        private void HandleAutoRefundException(Match match, AutoRefundException exception)
        {
            loggingService.WriteLog(match, exception);
            matchService.Update(match, exception.Description, Failed);
            matchPoolService.RemoveMatch(match);
        }

        private void ProcessAfterRefund(Match match, Result result)
        {
            if (!result.IsSuccessful)
            {
                loggingService.WriteLog(match, result.ErrMsg);
                matchService.Update(match, result.ErrMsg, Failed);
                matchPoolService.RemoveMatch(match);
            }
        }

        private async Task<int> ProcessWhenCachedException(int retryCount, Match match, AutoRefundException exception)
        {
            await matchService.Update(match, exception.Description, Running);
            if (retryCount >= appSettingsKey.RetryCount)
            {
                Console.WriteLine(DateTime.Now);
                HandleAutoRefundException(match, exception);
                matchPoolService.RemoveMatch(match);
            }
            else
            {
                await Task.Delay(appSettingsKey.RetryInterval);
            }

            return retryCount + 1;
        }
    }
}