namespace AutoRefundOtherSports.Exceptions;

public class MatchNoTicketException : AutoRefundException
{
    public MatchNoTicketException()
    {
        Description = "Auto refund child match failed: the match has no ticket.";
    }
}