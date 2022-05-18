using AutoRefundOtherSports.Criteria;
using AutoRefundOtherSports.Exceptions;
using AutoRefundOtherSports.Models;
using Fanex.Data.Repository;
using System.Linq;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Services
{
    public interface IVerificationStatusService
    {
        Task CheckMarkedResult(Match match);

        Task CheckRunningOdds(Match match);

        Task CheckTicketExclusion(Match match);

        Task CheckTicketSettled(Match match);
    }

    public class VerificationStatusServiceImp : IVerificationStatusService
    {
        private readonly ICreateQueryService createQueryService;

        private readonly IDynamicRepository repository;

        public VerificationStatusServiceImp(
            ICreateQueryService createQueryService,
            IDynamicRepository repository)
        {
            this.createQueryService = createQueryService;
            this.repository = repository;
        }

        public async Task CheckMarkedResult(Match match)
        {
            var criteria = createQueryService.CreateQueryCheckResultSetting(match);

            await repository.ExecuteAsync(criteria);

            if (criteria.ErrCode > 0)
            {
                throw new MarkedResultException();
            }
        }

        public async Task CheckRunningOdds(Match match)
        {
            var criteria = createQueryService.CreateQueryCheckOdds(match);

            await repository.ExecuteAsync(criteria);

            if (criteria.ErrCode > 0)
            {
                throw new RunningOddsException();
            }
        }

        public async Task CheckTicketExclusion(Match match)
        {
            var criteria = new CheckMarkedTicketsCriteria
            {
                MatchId = match.MatchId,
                ResourceIds = string.Join(",", match.ResultDetails.Select(item => item.ResourceId).ToList())
            };

            await repository.ExecuteAsync(criteria);

            if (criteria.IsExistMarkedTicket)
            {
                throw new TicketExclusionException();
            };
        }

        public async Task CheckTicketSettled(Match match)
        {
            var criteria = createQueryService.CreateQueryCheckTicketRunning(match);

            await repository.ExecuteAsync(criteria);

            if (criteria.ErrCode > 0)
            {
                throw new TicketSettledException();
            };
        }
    }
}