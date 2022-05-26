using AutoRefundOtherSports.Criteria;
using AutoRefundOtherSports.Models;
using Fanex.Caching;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Services
{
    public interface IMatchService
    {
        Task<List<Match>> GetMatches();

        Task Update(Match match, string remark, int status);

        Task Push(Match match);

        Task CheckMissedMatch();
    }

    public class MatchServiceImp : IMatchService
    {
        private readonly IDynamicRepository repository;

        private const string KEY_LEAGUEID = "Settlement-AutoPayout-AutoInstant-LeagueId";

        private readonly ICache cache;

        private readonly AppSettingsKey appSettingsKey;

        private readonly ICreateQueryService createQueryService;

        private readonly ILoggingService loggingService;

        private const int DeploymentError = 99;

        public MatchServiceImp(IDynamicRepository repository,
            ICache cache,
            IOptions<AppSettingsKey> appSettingsKey,
            ICreateQueryService createQueryService,
            ILoggingService loggingService)
        {
            this.repository = repository;
            this.cache = cache;
            this.appSettingsKey = appSettingsKey.Value;
            this.createQueryService = createQueryService;
            this.loggingService = loggingService;
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

            return matches;
        }

        private async Task<HedgeMatchDetail> GetHedgeMatch(int matchId)
        {
            var criteria = new GetHedgeMatchCriteria { MatchId = matchId };
            return await repository.GetAsync<HedgeMatchDetail>(criteria);
        }

        public async Task Update(Match match, string remark, int status)
        {
            var criteria = createQueryService.QueryUpdateResult(match, remark, status);

            await repository.ExecuteAsync(criteria);
        }

        public async Task Push(Match match)
        {
            var criteria = createQueryService.QuerySettle(match);
            await repository.ExecuteAsync(criteria);
            if (criteria.ErrCode != DeploymentError)
            {
                if (criteria.ErrCode != 0)
                {
                    await loggingService.WriteLog(match, criteria.ErrMsg);
                    await Update(match, criteria.ErrMsg, MatchStatus.Failed);
                }
                else
                {
                    await SettleHedgeMatch(match, criteria);
                }
            }
            else
            {
                SetFlagDeploySpName();
            }
        }

        public async Task CheckMissedMatch()
        {
            var criteria = new UpdateResultStatusCriteria
            {
                StatusId = MatchStatus.Failed
            };
            await repository.ExecuteAsync(criteria);
        }

        private async Task SettleHedgeMatch(Match match, SettleCriteria criteria)
        {
            var hedgeMatch = await GetHedgeMatch(match.MatchId);
            if (hedgeMatch != null)
            {
                criteria.MatchId = hedgeMatch.MatchId;
                await repository.ExecuteAsync(criteria);
                if (criteria.ErrCode != 0)
                {
                    await loggingService.WriteLog(match, criteria.ErrMsg);
                }
            }
        }

        private void SetFlagDeploySpName()
        {
            var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(int.Parse(appSettingsKey.DeploymentDelay)));
            cache.Set(appSettingsKey.DeploymentSpName, 1, cachePolicy);
        }
    }
}