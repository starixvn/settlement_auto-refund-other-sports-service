using AutoRefundOtherSports.Criteria;
using AutoRefundOtherSports.Models;
using Fanex.Data.Repository;
using System.Linq;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Services
{
    public interface IVerificationStatusService
    {
        Task<CheckOddsCriteria> CheckRunningOdds(Match match);

        Task<CheckMarkedTicketsCriteria> CheckTicketExclusion(Match match);

        Task<TicketCheckingCriteria> CheckTicketSettled(Match match);
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

        public async Task<CheckOddsCriteria> CheckRunningOdds(Match match)
        {
            var criteria = createQueryService.CreateQueryCheckOdds(match);

            await repository.ExecuteAsync(criteria);

            return criteria;
        }

        public async Task<CheckMarkedTicketsCriteria> CheckTicketExclusion(Match match)
        {
            var criteria = new CheckMarkedTicketsCriteria
            {
                MatchId = match.MatchId,
                ResourceIds = string.Join(",", match.ResultDetails.Select(item => item.ResourceId).ToList())
            };

            await repository.ExecuteAsync(criteria);

            return criteria;
        }

        public async Task<TicketCheckingCriteria> CheckTicketSettled(Match match)
        {
            var criteria = createQueryService.CreateQueryCheckTicketRunning(match);

            await repository.ExecuteAsync(criteria);

            return criteria;
        }
    }
}