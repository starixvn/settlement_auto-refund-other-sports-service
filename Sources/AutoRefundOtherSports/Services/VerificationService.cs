using AutoRefundOtherSports.Exceptions;
using AutoRefundOtherSports.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Services
{
    public interface IVerificationService
    {
        Task VerifyResultRunning(Match match, int retryIndex);

        Task VerifyResultStatus(Match match);
    }

    public class VerificationServiceImp : IVerificationService
    {
        private readonly List<int> resultStatusRunning = new List<int> { 0, 1 };

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
            match.ResultDetails.ForEach(item =>
            {
                if (!resultStatusRunning.Contains(item.ResultStatusId))
                {
                    throw new SettledException { StopLoop = true };
                }
            });

            await verificationStatusService.CheckTicketSettled(match);
        }

        public async Task VerifyResultStatus(Match match)
        {
            await verificationStatusService.CheckRunningOdds(match);
            await verificationStatusService.CheckTicketExclusion(match);
        }
    }
}