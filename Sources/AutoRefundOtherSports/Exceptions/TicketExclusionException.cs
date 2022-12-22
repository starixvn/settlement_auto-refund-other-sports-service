using System.Runtime.Serialization;

namespace AutoRefundOtherSports.Exceptions;

[Serializable]
public class TicketExclusionException : AutoRefundException
{
    public TicketExclusionException()
    {
        Description = "ERROR! Cannot proceed due to marked tickets. Please unmark them and try again.";
    }

    public TicketExclusionException(string message)
        : base(message)
    {
    }

    public TicketExclusionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected TicketExclusionException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}