using System.Runtime.Serialization;

namespace AutoRefundOtherSports.Exceptions;

[Serializable]
public class AutoRefundException : Exception
{
    public AutoRefundException()
    {
    }

    public AutoRefundException(string message)
        : base(message)
    {
        Description = message;
    }

    public AutoRefundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    [Obsolete(DiagnosticId = "SYSLIB0051")]
    protected AutoRefundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public string Description { get; set; }
}