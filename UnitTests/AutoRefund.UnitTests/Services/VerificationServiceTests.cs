using AutoRefundOtherSports.Exceptions;
using Microsoft.Extensions.Options;

namespace AutoRefund.UnitTests.Services;

public class VerificationServiceTests
{
    private readonly RetryValidationService retryValidationService;

    private readonly VerificationService service;

    public VerificationServiceTests()
    {
        retryValidationService = Substitute.For<RetryValidationService>();
        var appSettings = new AppSettingsKey
        {
            RetryCount = 5,
            RetryInterval = 300
        };
        var options = Options.Create(appSettings);
        service = new VerificationServiceImp(retryValidationService);
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