using System.Runtime.Serialization;

namespace AutoRefundOtherSports.Exceptions;

[Serializable]
public class MarkedResultException : AutoRefundException
{
    public MarkedResultException()
    {
        Description = "ERROR! Some results have not been marked as Refund.";
    }

    public MarkedResultException(string message)
        : base(message)
    {
    }

    public MarkedResultException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    [Obsolete(DiagnosticId = "SYSLIB0051")]
    protected MarkedResultException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}