using AutoRefundOtherSports.Constants;
using Fanex.Settlement.ScaleOut.Models;
using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.Extensions.Options;

namespace AutoRefund.UnitTests.Services;

public class SettleServiceTests
{
    private readonly CreateQueryService createQueryService;

    private readonly IDynamicRepository repository;

    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly RetryValidationService retryValidationService;

    private readonly LoggingService loggingService;

    private readonly SettleServiceImp service;

    public SettleServiceTests()
    {
        createQueryService = Substitute.For<CreateQueryService>();
        repository = Substitute.For<IDynamicRepository>();
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        retryValidationService = Substitute.For<RetryValidationService>();
        loggingService = Substitute.For<LoggingService>();
        var appSetting = new AppSettingsKey
        {
            RetryValidateCount = 4,
            RetryValidate = 1000,
            SiteDbServer = ["01", "02"]
        };
        var appSettings = Options.Create(appSetting);
        service = new SettleServiceImp(createQueryService, repository, scaleOutSettleService, retryValidationService, loggingService, appSettings);
    }

    [Fact]
    public async Task SettleMatch_ExistError_WriteLogAndUpdateFail()
    {
        var match = new Match
        {
            ResultDetails =
            [
                new ResultDetail()
                {
                    SourceId = SourceIds.TennisGameWinner
                }
            ]
        };

        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria());
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(new ResultMessage { ErrorMessage = "test", ErrorCode = 1 });

        await service.SettleMatch(match);

        await loggingService.DidNotReceive().Update(Arg.Any<Match>(), "", MatchStatus.Running);
        await scaleOutSettleService.DidNotReceive().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await repository.DidNotReceive().ExecuteAsync(Arg.Any<SettleCriteria>());

        await loggingService.Received().WriteLog(Arg.Any<Match>(), "test");
        await loggingService.Received().Update(Arg.Any<Match>(), "test", MatchStatus.Failed);
    }

    [Fact]
    public async Task SettleMatch_PassedValidate_UpdateAndSettleMatch()
    {
        var match = new Match
        {
            ResultDetails = []
        };

        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria());
        retryValidationService.ValidateEventTicket(Arg.Any<EventTicketRequest>()).Returns(new Result());

        await service.SettleMatch(match);

        await loggingService.Received().Update(Arg.Any<Match>(), "", MatchStatus.Running);
        await loggingService.Received().InsertResettleLog(Arg.Any<SettleCriteria>(), Arg.Any<int>());
        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await repository.Received(2).ExecuteAsync(Arg.Any<SettleCriteria>());
    }

    [Fact]
    public async Task SettleMatch_PassedValidateAndExistHedgeMatch_SettleBothMainAndHedgeMatch()
    {
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = []
        };

        createQueryService.QuerySettle(Arg.Is<Match>(m => m.MatchId == 1)).Returns(new SettleCriteria { MatchId = 1 });
        retryValidationService.ValidateEventTicket(Arg.Any<EventTicketRequest>()).Returns(new Result());
        repository.GetAsync<HedgeMatchDetail>(Arg.Any<GetHedgeMatchCriteria>()).Returns(new HedgeMatchDetail { MatchId = 2 });

        await service.SettleMatch(match);

        await loggingService.Received().Update(Arg.Is<Match>(m => m.MatchId == 1), "", MatchStatus.Running);
        await scaleOutSettleService.Received(2).UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await repository.Received(4).ExecuteAsync(Arg.Any<SettleCriteria>());
    }
}