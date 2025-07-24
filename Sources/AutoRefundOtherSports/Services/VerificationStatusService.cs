namespace AutoRefundOtherSports.Services;

public interface VerificationStatusService
{
    Task<CheckOddsCriteria> CheckRunningOdds(Match match);

    Task<(bool, bool)> IsTicketExisting(Match match);
}

public class VerificationStatusServiceImp(
    CreateQueryService createQueryService,
    IDynamicRepository repository,
    IOptions<AppSettingsKey> appSettingsKey)
    : VerificationStatusService
{
    public async Task<CheckOddsCriteria> CheckRunningOdds(Match match)
    {
        var criteria = createQueryService.CreateQueryCheckOdds(match);

        await repository.ExecuteAsync(criteria);

        return criteria;
    }

    public async Task<(bool, bool)> IsTicketExisting(Match match)
    {
        var criteria = new CheckMatchTicketCriteria
        {
            MatchId = match.MatchId
        };

        var tasks = appSettingsKey.Value.SiteDbServer.Select(s => GetTicketInfo(s, criteria));

        var ticketInfos = (await Task.WhenAll(tasks.ToArray())).ToList();

        var isExistTicket = ticketInfos.Exists(ticketInfo => ticketInfo.IsExistTicket);
        var isExistTicketSettled = ticketInfos.Exists(ticketInfo => ticketInfo.IsExistSettledTicket);

        return (isExistTicket, isExistTicketSettled);
    }

    private async Task<TicketCheck> GetTicketInfo(string server, CheckMatchTicketCriteria criteria)
    {
        criteria.DbServer = server;
        return await repository.GetAsync<TicketCheck>(criteria);
    }
}