using Fanex.Data.Repository;
using Microsoft.Extensions.Options;

namespace AutoRefundOtherSports.Services;

public interface IVerificationStatusService
{
    Task<CheckOddsCriteria> CheckRunningOdds(Match match);

    Task<Boolean> CheckTicketExclusion(Match match);

    Task<TicketCheckingCriteria> CheckTicketSettled(Match match);
}

public class VerificationStatusServiceImp : IVerificationStatusService
{
    private readonly ICreateQueryService createQueryService;

    private readonly IDynamicRepository repository;

    private readonly AppSettingsKey appSettingsKey;

    public VerificationStatusServiceImp(
        ICreateQueryService createQueryService,
        IDynamicRepository repository,
        IOptions<AppSettingsKey> appSettingsKey)
    {
        this.createQueryService = createQueryService;
        this.repository = repository;
        this.appSettingsKey = appSettingsKey.Value;
    }

    public async Task<CheckOddsCriteria> CheckRunningOdds(Match match)
    {
        var criteria = createQueryService.CreateQueryCheckOdds(match);

        await repository.ExecuteAsync(criteria);

        return criteria;
    }

    public async Task<Boolean> CheckTicketExclusion(Match match)
    {
        var criteria = new CheckMarkedTicketsCriteria
        {
            MatchId = match.MatchId,
            ResourceIds = string.Join(",", match.ResultDetails.Select(item => item.ResourceId).ToList())
        };

        foreach (var server in appSettingsKey.SiteDbServer)
        {
            criteria.DbServer = server;
            await repository.ExecuteAsync(criteria);
            if (criteria.IsExistMarkedTicket)
            {
                return criteria.IsExistMarkedTicket;
            }
        }

        return false;
    }

    public async Task<TicketCheckingCriteria> CheckTicketSettled(Match match)
    {
        var criteria = createQueryService.CreateQueryCheckTicketRunning(match);

        foreach (var server in appSettingsKey.SiteDbServer)
        {
            criteria.DbServer = server;
            await repository.ExecuteAsync(criteria);
            if (criteria.ErrCode > 0)
            {
                return criteria;
            }
        }

        return criteria;
    }
}