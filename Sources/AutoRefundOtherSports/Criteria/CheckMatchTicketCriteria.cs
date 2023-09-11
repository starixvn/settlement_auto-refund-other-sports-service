using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.Data;

namespace AutoRefundOtherSports.Criteria;

public class CheckMatchTicketCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public override string GetSettingKey() => $"NAP_Set_MatchTicket_Checking_{DbServer}";

    public override bool IsValid() => MatchId > 0;
}