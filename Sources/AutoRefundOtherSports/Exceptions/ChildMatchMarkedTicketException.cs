namespace AutoRefundOtherSports.Exceptions;

public class ChildMatchMarkedTicketException : AutoRefundException
{
    public ChildMatchMarkedTicketException()
    {
        Description = "Auto refund child match failed: marked tickets exist.";
    }
}