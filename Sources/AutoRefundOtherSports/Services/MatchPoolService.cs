using AutoRefundOtherSports.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Services
{
    public interface IMatchPoolService
    {
        Task<List<Match>> PeekNewMatch();

        void RemoveMatch(Match match);
    }

    public class MatchPoolServiceImp : IMatchPoolService
    {
        private readonly IMatchService matchService;

        private readonly List<Match> poolOfMatches;

        private readonly object lockObject;

        public MatchPoolServiceImp(IMatchService matchService)
        {
            this.matchService = matchService;
            poolOfMatches = new List<Match>();
            lockObject = new object();
        }

        public List<Match> PoolOfMatches
        {
            get
            {
                return poolOfMatches;
            }
        }

        public void RemoveMatch(Match match)
        {
            lock (lockObject)
            {
                poolOfMatches.RemoveAll(m => m.MatchId == match.MatchId);
            }
        }

        public async Task<List<Match>> PeekNewMatch()
        {
            var matches = await matchService.GetMatches();

            lock (lockObject)
            {
                var newMatches = matches.Except(poolOfMatches).ToList();
                poolOfMatches.AddRange(newMatches);

                return newMatches;
            }
        }
    }
}