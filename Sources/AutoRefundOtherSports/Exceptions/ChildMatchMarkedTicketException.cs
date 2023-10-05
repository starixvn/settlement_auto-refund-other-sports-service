namespace AutoRefundOtherSports.Exceptions;

public class ChildMatchMarkedTicketException : AutoRefundException
{
    public ChildMatchMarkedTicketException()
    {
        Description = "auto refund failed: marked tickets exist.";
    }
}