using System;
using System.Runtime.Serialization;

namespace AutoRefundOtherSports.Exceptions
{
    [Serializable]
    public class TicketExclusionException : AutoRefundException
    {
        public TicketExclusionException()
        {
            Description = "Auto Refund failed: marked tickets exist.";
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
}