using AutoRefundOtherSports.Criteria;
using AutoRefundOtherSports.Models;
using Fanex.Caching;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Services
{
    public interface IMatchService
    {
        Task<List<Match>> GetMatches();

        Task Update(Match match, string remark, int status);

        Task<Result> Push(Match match);

        Task CheckMissedMatch();
    }

    public class MatchServiceImp : IMatchService
    {
        private readonly IDynamicRepository repository;

        private const string KEY_LEAGUEID = "Settlement-AutoPayout-AutoInstant-LeagueId";

        private const int Failed = 3;

        private readonly ICache cache;

        private readonly AppSettingsKey appSettingsKey;

        private readonly ICreateQueryService createQueryService;

        public MatchServiceImp(IDynamicRepository repository,
            ICache cache,
            IOptions<AppSettingsKey> appSettingsKey,
            ICreateQueryService createQueryService)
        {
            this.repository = repository;
            this.cache = cache;
            this.appSettingsKey = appSettingsKey.Value;
            this.createQueryService = createQueryService;
        }

        public async Task<List<Match>> GetMatches()
        {
            var resultDetails = (await repository.FetchAsync<ResultDetail>(new ResultListingCriteria())).ToList();
            var matches = GroupMatch(resultDetails);

            cache.TryGet(KEY_LEAGUEID, out int leagueId);

            return appSettingsKey.EnvironmentName switch
            {
                "PRO" => leagueId > 0
                                       ? (from m in matches where m.LeagueId != leagueId select m).ToList()
                                       : matches,
                "UAT" => (from m in matches where m.LeagueId == leagueId select m).ToList(),
                _ => matches,
            };
        }

        private List<Match> GroupMatch(List<ResultDetail> resultDetails)
        {
            var matches = resultDetails.GroupBy(result => new { result.MatchId, result.LeagueId, result.TypeId })
                .Select(match => new Match
                {
                    MatchId = match.Key.MatchId,
                    LeagueId = match.Key.LeagueId,
                    TypeId = match.Key.TypeId,
                    ResultDetails = match.ToList()
                }).ToList();
            var matchesAndHedgeMatches = new List<Match>(matches);
            matches.ForEach(match =>
            {
                var hedgeMatch = GetHedgeMatch(match.MatchId);
                if (hedgeMatch != null)
                {
                    matchesAndHedgeMatches.Add(new Match
                    {
                        MatchId = hedgeMatch.MatchId,
                        ResultDetails = match.ResultDetails,
                        LeagueId = match.LeagueId,
                        TypeId = match.TypeId,
                        IsHedgeMatch = true
                    });
                }
            });

            return matchesAndHedgeMatches;
        }

        private HedgeMatchDetail GetHedgeMatch(int matchId)
        {
            var criteria = new GetHedgeMatchCriteria { MatchId = matchId };
            return repository.Get<HedgeMatchDetail>(criteria);
        }

        public async Task Update(Match match, string remark, int status)
        {
            var criteria = createQueryService.QueryUpdateResult(match, remark, status);

            await repository.ExecuteAsync(criteria);
        }

        public async Task<Result> Push(Match match)
        {
            var criteria = createQueryService.QuerySettle(match);
            await repository.ExecuteAsync(criteria);

            return new Result
            {
                ErrCode = criteria.ErrCode,
                ErrMsg = criteria.ErrMsg
            };
        }

        public async Task CheckMissedMatch()
        {
            var criteria = new UpdateResultStatusCriteria
            {
                StatusId = Failed
            };
            await repository.ExecuteAsync(criteria);
        }
    }
}