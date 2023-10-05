namespace AutoRefundOtherSports.Exceptions;

public class MatchNoTicketException : AutoRefundException
{
    public MatchNoTicketException()
    {
        Description = "Auto refund failed: the match has no ticket.";
    }
}