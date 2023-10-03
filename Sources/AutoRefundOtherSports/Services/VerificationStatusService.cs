using AutoRefundOtherSports.Models;

namespace AutoRefundOtherSports.Services;

public interface VerificationStatusService
{
    Task<CheckOddsCriteria> CheckRunningOdds(Match match);

    Task<bool> IsExistMarkedTicket(Match match);

    Task<bool> IsTicketExisting(Match match);
}

public class VerificationStatusServiceImp : VerificationStatusService
{
    private readonly CreateQueryService createQueryService;

    private readonly IDynamicRepository repository;

    private readonly AppSettingsKey appSettingsKey;

    public VerificationStatusServiceImp(
        CreateQueryService createQueryService,
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

    public async Task<bool> IsExistMarkedTicket(Match match)
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

    public async Task<bool> IsTicketExisting(Match match)
    {
        var criteria = new CheckMatchTicketCriteria
        {
            MatchId = match.MatchId,
        };

        foreach (var server in appSettingsKey.SiteDbServer)
        {
            criteria.DbServer = server;

            var result = await repository.GetAsync<TicketCheck>(criteria) ?? new TicketCheck();

            if (result.IsExistTicket)
            {
                return TicketStatus.Exists;
            }
        }

        return false;
    }
}