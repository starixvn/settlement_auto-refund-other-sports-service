using AutoRefundOtherSports.Exceptions;
using AutoRefundOtherSports.Models;
using System.Collections.Generic;
using System.Linq;
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

            var resultNotRunning = match.ResultDetails.FirstOrDefault(item => !resultStatusRunning.Contains(item.ResultStatusId));
            if (resultNotRunning != null)
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

            var resultCheckingTicket = await verificationStatusService.CheckTicketExclusion(match);
            if (resultCheckingTicket.IsExistMarkedTicket)
            {
                throw new TicketExclusionException();
            }
        }
    }
}