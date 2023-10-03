using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Exceptions;

public class MatchNoTicketException : AutoRefundException
{
    public MatchNoTicketException()
    {
        Description = "Auto refund failed: the match has no ticket.";
    }
}