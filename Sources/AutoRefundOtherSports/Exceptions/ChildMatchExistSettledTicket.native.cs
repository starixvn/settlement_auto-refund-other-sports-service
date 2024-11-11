namespace AutoRefundOtherSports.Exceptions;

public class ChildMatchExistSettledTicket : AutoRefundException
{
    public ChildMatchExistSettledTicket()
    {
        Description = "Auto refund child match failed: settled tickets exist.";
    }
}