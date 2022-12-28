namespace AutoRefundOtherSports.Services;

public interface CreateQueryService
{
    SettleCriteria QuerySettle(Match match);

    UpdateResultStatusCriteria QueryUpdateResult(Match match, string remark, int status);

    CheckOddsCriteria CreateQueryCheckOdds(Match match);

    TicketCheckingCriteria CreateQueryCheckTicketRunning(Match match);
}

public class CreateQueryServiceImp : CreateQueryService
{
    private readonly XmlConvertService xmlConvertService;

    private const int StatusRefunded = 1;

    public CreateQueryServiceImp(XmlConvertService xmlConvertService)
    {
        this.xmlConvertService = xmlConvertService;
    }

    public SettleCriteria QuerySettle(Match match)
    {
        var resourcesResult = CreateResources(match.ResultDetails);
        var criteria = new SettleCriteria
        {
            MatchId = match.MatchId,
            SelectedResources = resourcesResult,
            BroadcastSessionId = Guid.NewGuid().ToString()
        };

        return criteria;
    }

    private string CreateResources(List<ResultDetail> selectedResources)
    {
        var resources = new Resources<ResultDetail>
        {
            ResourceItems = selectedResources
        };

        return xmlConvertService.Serialize(resources);
    }

    public UpdateResultStatusCriteria QueryUpdateResult(Match match, string remark, int status)
    {
        return new UpdateResultStatusCriteria
        {
            MatchId = match.MatchId,
            ResourceIds = string.Join(",", match.ResultDetails.Select(item => item.ResourceId).ToList()),
            StatusId = status,
            TypeId = match.TypeId,
            Remark = remark
        };
    }

    public CheckOddsCriteria CreateQueryCheckOdds(Match match)
    {
        return new CheckOddsCriteria
        {
            MatchId = match.MatchId,
            SelectedResource = CreateMatchResources(match.ResultDetails)
        };
    }

    public TicketCheckingCriteria CreateQueryCheckTicketRunning(Match match)
    {
        return new TicketCheckingCriteria
        {
            MatchId = match.MatchId,
            SportType = match.SportType,
            ResourceIds = string.Join(",", match.ResultDetails.Where(item => item.SettlementStatusId != StatusRefunded).Select(item => item.ResourceId).ToList())
        };
    }

    private string CreateMatchResources(List<ResultDetail> selectedResources)
    {
        var items = new CheckOddsItem
        {
            ResourceIds = selectedResources.Select(item => item.ResourceId).ToList()
        };

        var resources = new Resources<CheckOddsItem>
        {
            ResourceItems = new List<CheckOddsItem> { items }
        };

        return xmlConvertService.Serialize(resources);
    }
}