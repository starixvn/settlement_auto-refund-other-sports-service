using Fanex.Settlement.ScaleOut.Models;
using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.Extensions.Options;

namespace AutoRefund.UnitTests.Services;

public class SettleServiceTests
{
    private readonly ICreateQueryService createQueryService;

    private readonly IDynamicRepository repository;

    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly SettleService service;

    public SettleServiceTests()
    {
        createQueryService = Substitute.For<ICreateQueryService>();
        repository = Substitute.For<IDynamicRepository>();
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        var appSettings = Options.Create(new AppSettingsKey { SiteDbServer = new List<string> { "01", "02" } });
        service = new SettleServiceImp(createQueryService, repository, scaleOutSettleService, appSettings);
    }

    [Fact]
    public async Task SettleMatch_ExistError_NotUpdateAndSettleMatch()
    {
        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria());
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(new ResultMessage { ErrorCode = 1 });

        var actual = service.SettleMatch(new Match());

        await scaleOutSettleService.DidNotReceive().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await repository.DidNotReceive().ExecuteAsync(Arg.Any<SettleCriteria>());
    }

    [Fact]
    public async Task SettleMatch_PassedValidate_UpdateAndSettleMatch()
    {
        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria());
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(new ResultMessage());

        var actual = service.SettleMatch(new Match());

        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await repository.Received(2).ExecuteAsync(Arg.Any<SettleCriteria>());
    }
}