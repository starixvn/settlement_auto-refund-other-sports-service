using System.Runtime.Serialization;

namespace AutoRefundOtherSports;

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

    protected AutoRefundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public string Description { get; set; }

    public bool StopLoop { get; set; }
}