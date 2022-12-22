using Fanex.Data.Repository;

namespace AutoRefundOtherSports.Criteria;

public class GetHedgeMatchCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey()
    {
        return "NAP_Set_MatchSelHedge";
    }

    public override bool IsValid() => MatchId > 0;
}