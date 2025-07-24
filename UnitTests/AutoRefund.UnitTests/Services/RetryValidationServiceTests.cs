using Fanex.Settlement.ScaleOut.Models;
using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.Extensions.Options;

namespace AutoRefund.UnitTests.Services;

public class RetryValidationServiceTests
{
    private readonly VerificationStatusService verificationStatusService;

    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly RetryValidationService service;

    public RetryValidationServiceTests()
    {
        verificationStatusService = Substitute.For<VerificationStatusService>();
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        var appSettings = new AppSettingsKey
        {
            RetryCount = 2,
            RetryInterval = 100,
            RetryValidate = 100,
            RetryValidateCount = 2
        };

        var options = Options.Create(appSettings);

        service = new RetryValidationServiceImp(verificationStatusService, options, scaleOutSettleService);
    }

    [Fact]
    public async Task IsCheckingOddsAndMarkedTicket_ExistRunningOdds_RetryValidate()
    {
        verificationStatusService.CheckRunningOdds(Arg.Any<Match>()).Returns(new CheckOddsCriteria { ErrCode = 1 });
        var match = new Match();

        var actual = await service.IsCheckingOddsAndMarkedTicket(match);

        await verificationStatusService.Received(3).CheckRunningOdds(Arg.Any<Match>());
        Assert.Equal(2, match.TryCount);
        Assert.False(actual);
    }

    [Fact]
    public async Task IsCheckingOddsAndMarkedTicket_NotExistHindrance_Return()
    {
        verificationStatusService.CheckRunningOdds(Arg.Any<Match>()).Returns(new CheckOddsCriteria());
        var match = new Match();

        var actual = await service.IsCheckingOddsAndMarkedTicket(match);

        await verificationStatusService.Received(1).CheckRunningOdds(Arg.Any<Match>());
        Assert.Equal(0, match.TryCount);
        Assert.True(actual);
    }

    [Fact]
    public async Task ValidateEventTicket_ExistErrorMatchInQueue_RetryValidate()
    {
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(new ResultMessage { ErrorCode = 1001003, ErrorMessage = "test" });

        var actual = await service.ValidateEventTicket(new EventTicketRequest());

        await scaleOutSettleService.Received(3).ValidateEventAndTicket(Arg.Any<EventTicketRequest>());
        Assert.False(actual.IsSuccessful);
    }

    [Fact]
    public async Task ValidateEventTicket_ExistNormalError_ReturnErrorNotRetry()
    {
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(new ResultMessage { ErrorCode = 123, ErrorMessage = "test" });

        var actual = await service.ValidateEventTicket(new EventTicketRequest());

        await scaleOutSettleService.Received(1).ValidateEventAndTicket(Arg.Any<EventTicketRequest>());
        Assert.False(actual.IsSuccessful);
    }
}