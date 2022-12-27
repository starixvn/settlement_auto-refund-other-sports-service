using AutoRefundOtherSports.Exceptions;
using Microsoft.Extensions.Options;

namespace AutoRefund.UnitTests.Services;

public class VerificationServiceTests
{
    private readonly IVerificationStatusService verificationStatusService;

    private readonly ILoggingService loggingService;

    private readonly IVerificationService service;

    public VerificationServiceTests()
    {
        verificationStatusService = Substitute.For<IVerificationStatusService>();
        loggingService = Substitute.For<ILoggingService>();
        var appSettings = new AppSettingsKey
        {
            RetryCount = 5,
            RetryInterval = 300
        };
        var options = Options.Create(appSettings);
        service = new VerificationServiceImp(verificationStatusService, options, loggingService);
    }

    [Fact]
    public void VerifyResultRunning_ResultSettled_ThrowException()
    {
        var match = new Match
        {
            ResultDetails = new List<ResultDetail>
                {
                    new ResultDetail { SettlementStatusId = 4 }
                }
        };
        Assert.Throws<SettledException>(() => service.VerifyResultRunning(match));
    }

    [Fact]
    public void VerifyResultRunning_ResultUncalc_ThrowException()
    {
        var match = new Match
        {
            ResultDetails = new List<ResultDetail>
            {
                new ResultDetail { ResultStatusId = 4 }
            }
        };
        Assert.Throws<SettledException>(() => service.VerifyResultRunning(match));
    }

    [Fact]
    public void VerifyResultRunning_ResultValid_NotThrowException()
    {
        var match = new Match
        {
            ResultDetails = new List<ResultDetail>
                {
                    new ResultDetail { ResultStatusId = 1 }
                }
        };

        var exception = Record.Exception(() => service.VerifyResultRunning(match));
        Assert.Null(exception);
    }

    [Fact]
    public async Task VerifyResultStatus_ExistRunningOdds_ThrowExceptionAfterRetry()
    {
        var match = new Match();
        verificationStatusService.CheckRunningOdds(match).Returns(new CheckOddsCriteria { ErrCode = 1 });

        await Assert.ThrowsAsync<OddsAndMarkedTicketException>(() => service.VerifyResultStatus(match));
        await verificationStatusService.Received(6).CheckRunningOdds(match);
        await verificationStatusService.DidNotReceive().IsExistMarkedTicket(Arg.Any<Match>());
    }

    [Fact]
    public async Task VerifyResultStatus_ExistMarkedTicket_ThrowExceptionAfterRetry()
    {
        var match = new Match();
        verificationStatusService.CheckRunningOdds(match).Returns(new CheckOddsCriteria());
        verificationStatusService.IsExistMarkedTicket(match).Returns(true);

        await Assert.ThrowsAsync<OddsAndMarkedTicketException>(() => service.VerifyResultStatus(match));
        await verificationStatusService.Received(6).CheckRunningOdds(match);
        await verificationStatusService.Received(6).IsExistMarkedTicket(Arg.Any<Match>());
    }

    [Fact]
    public async Task VerifyResultStatus_ResultValid_NotThrowExceptionAndNotRetry()
    {
        var match = new Match();
        verificationStatusService.CheckRunningOdds(match).Returns(new CheckOddsCriteria());
        verificationStatusService.IsExistMarkedTicket(match).Returns(false);

        var exception = await Record.ExceptionAsync(() => service.VerifyResultStatus(match));
        Assert.Null(exception);
        await verificationStatusService.Received(1).CheckRunningOdds(match);
        await verificationStatusService.Received(1).IsExistMarkedTicket(Arg.Any<Match>());
    }
}