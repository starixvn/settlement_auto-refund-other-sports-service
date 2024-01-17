using System.Runtime.Serialization;

namespace AutoRefundOtherSports.Exceptions;

[Serializable]
public class SettledException : AutoRefundException
{
    public SettledException()
    {
        Description = "ERROR! Some results have been settled incorrectly already.";
    }

    public SettledException(string message)
        : base(message)
    {
    }

    public SettledException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    [Obsolete(DiagnosticId = "SYSLIB0051")]
    protected SettledException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}