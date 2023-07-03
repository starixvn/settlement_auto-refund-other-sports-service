namespace AutoRefundOtherSports.Criteria;

public class ResultListingCriteria : CriteriaBase
{
    public int SportType { get; set; }

    public override bool IsValid() => SportType > 0;

    public override string GetSettingKey()
    {
        return "NAP_Set_AutoRefund_ScheduleSel";
    }
}