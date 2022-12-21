using Kaden.XmlConverter;
using Newtonsoft.Json;

namespace AutoRefund.UnitTests.Services;

public class CreateQueryServiceTests
{
    private readonly XmlConvertService xmlConvertService;

    private readonly ICreateQueryService service;

    private readonly Match match;

    public CreateQueryServiceTests()
    {
        xmlConvertService = new XmlConvertServiceImp();
        service = new CreateQueryServiceImp(xmlConvertService);
        match = new Match()
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>
                {
                    new ResultDetail
                    {
                        ResourceId = "0101"
                    }
                }
        };
    }

    [Fact]
    public void QuerySettle_Always_SettleResultRunning()
    {
        match.ResultDetails.Add(new ResultDetail { SettlementStatusId = 1, ResourceId = "0102" });
        var criteria = new SettleCriteria
        {
            MatchId = 1,
            SelectedResources = "<Resources><Resource Id=\"0101\" /></Resources>"
        };

        var result = service.QuerySettle(match);

        Assert.Equal(JsonConvert.SerializeObject(result), JsonConvert.SerializeObject(criteria));
    }

    [Fact]
    public void QueryUpdateResult_Always_ReturnsExpectedData()
    {
        match.TypeId = 1;
        match.ResultDetails.Add(new ResultDetail { ResourceId = "0102" });
        var criteria = new UpdateResultStatusCriteria
        {
            MatchId = 1,
            ResourceIds = "0101,0102",
            StatusId = 1,
            TypeId = 1,
            Remark = "123"
        };

        var result = service.QueryUpdateResult(match, "123", 1);

        Assert.Equal(JsonConvert.SerializeObject(result), JsonConvert.SerializeObject(criteria));
    }

    [Fact]
    public void CreateQueryCheckOdds_Always_ReturnsExpectedData()
    {
        var criteria = new CheckOddsCriteria
        {
            MatchId = 1,
            SelectedResource = "<Resources><Resource MatchId=\"0\"><ResourceId Id=\"0101\" /></Resource></Resources>"
        };
        var result = service.CreateQueryCheckOdds(match);

        Assert.Equal(JsonConvert.SerializeObject(result), JsonConvert.SerializeObject(criteria));
    }

    [Fact]
    public void CreateQueryCheckTicketRunning_Always_ReturnsExpectedData()
    {
        match.TypeId = 1;
        match.ResultDetails.Add(new ResultDetail { ResourceId = "0102" });
        var criteria = new TicketCheckingCriteria
        {
            MatchId = 1,
            ResourceIds = "0101,0102",
            TypeId = 1,
        };

        var result = service.CreateQueryCheckTicketRunning(match);

        Assert.Equal(JsonConvert.SerializeObject(result), JsonConvert.SerializeObject(criteria));
    }
}