using System.Runtime.Serialization;

namespace AutoRefundOtherSports.Exceptions;

[Serializable]
public class OddsAndMarkedTicketException : AutoRefundException
{
    public OddsAndMarkedTicketException()
    {
        Description = "ERROR! Cannot proceed due to unclosed odds or marked tickets.";
    }

    public OddsAndMarkedTicketException(string message)
        : base(message)
    {
    }

    public OddsAndMarkedTicketException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected OddsAndMarkedTicketException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}