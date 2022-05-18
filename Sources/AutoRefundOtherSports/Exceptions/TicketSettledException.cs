using System;
using System.Runtime.Serialization;

namespace AutoRefundOtherSports.Exceptions
{
    [Serializable]
    public class TicketSettledException : AutoRefundException
    {
        public TicketSettledException()
        {
            Description = "Auto Refund failed: settled tickets exist.";
        }

        public TicketSettledException(string message)
            : base(message)
        {
        }

        public TicketSettledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected TicketSettledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}