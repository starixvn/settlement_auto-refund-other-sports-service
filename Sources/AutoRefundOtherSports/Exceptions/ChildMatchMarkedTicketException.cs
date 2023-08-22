using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Exceptions;

public class ChildMatchMarkedTicketException : AutoRefundException
{
    public ChildMatchMarkedTicketException()
    {
        Description = "auto refund failed: marked tickets exist.";
    }
}