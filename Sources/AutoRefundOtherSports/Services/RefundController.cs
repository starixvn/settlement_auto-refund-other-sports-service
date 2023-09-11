using AutoRefundOtherSports.Exceptions;
using Microsoft.AspNetCore.Http.Features;

namespace AutoRefundOtherSports.Services;

public interface RefundController
{
    Task Process(Match match);
}

public class RefundControllerImp : RefundController
{
    private readonly MatchService matchService;

    private readonly VerificationService verificationService;

    private readonly VerificationStatusService verificationStatusService;

    private readonly MatchPoolService matchPoolService;

    private readonly SettleService settleService;

    public RefundControllerImp(
        MatchService matchService,
        VerificationService verificationService,
        MatchPoolService matchPoolService,
        SettleService settleService,
        VerificationStatusService verificationStatusService)
    {
        this.matchService = matchService;
        this.verificationService = verificationService;
        this.matchPoolService = matchPoolService;
        this.settleService = settleService;
        this.verificationStatusService = verificationStatusService;
    }

    public async Task Process(Match match)
    {
        try
        {
            await VerifyResult(match);
            await settleService.SettleMatch(match);
        }
        catch (AutoRefundException exception)
        {
            await matchService.HandleAutoRefundException(match, exception);
        }
        finally
        {
            matchPoolService.RemoveMatch(match);
        }
    }

    private async Task VerifyResult(Match match)
    {
        if (match.IsExistsGameWinner)
        {
            await VerifyResultForGameWinner(match);
            return;
        }

        verificationService.VerifyResultRunning(match);
        await verificationService.VerifyResultStatus(match);
    }

    private async Task VerifyResultForGameWinner(Match match)
    {
        var isExistMarkedTicket = await verificationStatusService.IsExistMarkedTicket(match);

        if (isExistMarkedTicket)
        {
            throw new ChildMatchMarkedTicketException();
        }

        var isExistTicket = await verificationStatusService.IsTicketExisting(match);

        if (!isExistTicket)
        {
            throw new MatchNoTicketException();
        }
    }
}