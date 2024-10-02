namespace AutoRefundOtherSports.Services;

public interface CreateQueryService
{
    SettleCriteria QuerySettle(Match match);

    UpdateResultStatusCriteria QueryUpdateResult(Match match, string remark, int status);

    CheckOddsCriteria CreateQueryCheckOdds(Match match);
}

public class CreateQueryServiceImp : CreateQueryService
{
    private readonly XmlConvertService xmlConvertService;

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
            BroadcastSessionId = Guid.NewGuid().ToString(),
            AutoType = match.IsExistsGameWinner ? AutoType.RefundGameWinner : null
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
            Remark = remark,
            TryCount = match.TryCount
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