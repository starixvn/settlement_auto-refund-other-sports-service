using AutoRefundOtherSports.Exceptions;
using Microsoft.Extensions.Options;

namespace AutoRefund.UnitTests.Services;

public class VerificationServiceTests
{
    private readonly RetryValidationService retryValidationService;

    private readonly VerificationServiceImp service;

    public VerificationServiceTests()
    {
        retryValidationService = Substitute.For<RetryValidationService>();
        var verificationStatusService1 = Substitute.For<VerificationStatusService>();
        var appSettings = new AppSettingsKey
        {
            RetryCount = 5,
            RetryInterval = 300
        };
        Options.Create(appSettings);
        service = new VerificationServiceImp(retryValidationService, verificationStatusService1);
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
}