using Fanex.Data;
using Fanex.Data.Repository;

namespace AutoRefundOtherSports.Criteria;

public class TicketCheckingCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

        public int SportType { get; set; }

    public string ResourceIds { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    public override bool IsValid()
    {
        return MatchId > 0;
    }

    public override string GetSettingKey()
    {
        return $"NAP_Set_AutoRefund_MatchChecking_Ticket_{DbServer}";
    }
}