using System;
using System.Runtime.Serialization;

namespace AutoRefundOtherSports.Exceptions
{
    [Serializable]
    public class RunningOddsException : AutoRefundException
    {
        public RunningOddsException()
        {
            Description = "Auto Refund failed: some related odds not closed.";
        }

        public RunningOddsException(string message)
            : base(message)
        {
        }

        public RunningOddsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected RunningOddsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}