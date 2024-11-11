using AutoRefundOtherSports.Exceptions;
using Microsoft.Extensions.Options;

namespace AutoRefund.UnitTests.Services;

public class VerificationServiceTests
{
    private readonly RetryValidationService retryValidationService;

    private readonly VerificationStatusService verificationStatusService;

    private readonly VerificationService service;

    public VerificationServiceTests()
    {
        retryValidationService = Substitute.For<RetryValidationService>();
        verificationStatusService = Substitute.For<VerificationStatusService>();
        var appSettings = new AppSettingsKey
        {
            RetryCount = 5,
            RetryInterval = 300
        };
        Options.Create(appSettings);
        service = new VerificationServiceImp(retryValidationService, verificationStatusService);
    }

    [Fact]
    public void VerifyResultRunning_ResultSettled_ThrowException()
    {
        var match = new Match
        {
            ResultDetails = [new ResultDetail { SettlementStatusId = 4 }]
        };
        Assert.Throws<SettledException>(() => service.VerifyResultRunning(match));
    }

    [Fact]
    public void VerifyResultRunning_ResultUnCalc_ThrowException()
    {
        var match = new Match
        {
            ResultDetails = [new ResultDetail { ResultStatusId = 4 }]
        };
        Assert.Throws<SettledException>(() => service.VerifyResultRunning(match));
    }

    [Fact]
    public void VerifyResultRunning_ResultValid_NotThrowException()
    {
        var match = new Match
        {
            ResultDetails = [new ResultDetail { ResultStatusId = 1 }]
        };

        var exception = Record.Exception(() => service.VerifyResultRunning(match));
        Assert.Null(exception);
    }

    [Fact]
    public async Task VerifyResultStatus_ValidationFailure_ThrowExceptionAfterRetry()
    {
        var match = new Match();
        retryValidationService.IsCheckingOddsAndMarkedTicket(match).Returns(false);

        await Assert.ThrowsAsync<OddsAndMarkedTicketException>(() => service.VerifyResultStatus(match));
    }

    [Fact]
    public async Task VerifyResultStatus_ValidationPassed_DoNotThrowAnyThing()
    {
        var match = new Match();
        retryValidationService.IsCheckingOddsAndMarkedTicket(match).Returns(true);

        var exception = await Record.ExceptionAsync(() => service.VerifyResultStatus(match));
        Assert.Null(exception);
    }

    [Fact]
    public async Task VerifyResultForGameWinner_ExistedMarkedTicket_ThrowException()
    {
        var match = new Match();
        verificationStatusService.IsExistMarkedTicket(Arg.Any<Match>()).Returns(true);

        await Assert.ThrowsAsync<ChildMatchMarkedTicketException>(() => service.VerifyResultForGameWinner(match));
    }

    [Fact]
    public async Task VerifyResultForGameWinner_NotExistedMarkedTicketAndNoTicket_ThrowException()
    {
        var match = new Match();
        verificationStatusService.IsExistMarkedTicket(Arg.Any<Match>()).Returns(false);
        verificationStatusService.IsTicketExisting(Arg.Any<Match>()).Returns((false, true));
        await Assert.ThrowsAsync<MatchNoTicketException>(() => service.VerifyResultForGameWinner(match));
    }

    [Fact]
    public async Task VerifyResultForGameWinner_NotExistedMarkedTicketAndSettledTicket_ThrowException()
    {
        var match = new Match();
        verificationStatusService.IsExistMarkedTicket(Arg.Any<Match>()).Returns(false);
        verificationStatusService.IsTicketExisting(Arg.Any<Match>()).Returns((true, true));
        await Assert.ThrowsAsync<ChildMatchExistSettledTicket>(() => service.VerifyResultForGameWinner(match));
    }

    [Fact]
    public async Task VerifyResultForGameWinner_ValidationPassed_DoNotThrowAnyThing()
    {
        var match = new Match();
        verificationStatusService.IsExistMarkedTicket(Arg.Any<Match>()).Returns(false);
        verificationStatusService.IsTicketExisting(Arg.Any<Match>()).Returns((true, false));

        var exception = await Record.ExceptionAsync(() => service.VerifyResultForGameWinner(match));
        Assert.Null(exception);
    }
}