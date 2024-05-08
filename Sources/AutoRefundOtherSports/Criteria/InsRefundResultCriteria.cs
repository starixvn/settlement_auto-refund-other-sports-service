namespace AutoRefundOtherSports.Criteria;

public class InsRefundResultCriteria : INonQueryCommand
{
    public int MatchId { get; set; }

    public string ResourceIds { get; set; }

    public string GetSettingKey() => "NAP_Set_AutoRefundResultOS_Ins";

    public bool IsValid() => MatchId > 0;
}