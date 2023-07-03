namespace AutoRefundOtherSports.Criteria;

public class InsertLogCriteria : NonQueryCommand
{
    public InsertLogCriteria(SettleCriteria criteria, int sporttype)
    {
        MatchId = criteria.MatchId;
        Action = criteria.Action;
        Sporttype = sporttype;
        AdminId = criteria.AdminId;
        SelectedResources = criteria.SelectedResources;
    }

    public int MatchId { get; set; }

    public string Action { get; set; }

    public int Sporttype { get; set; }

    public bool IsAuto { get; set; } = true;

    public int AdminId { get; set; }

    public string SelectedResources { get; set; }

    public override string GetSettingKey() => "NAP_SettlementLog_Ins";

    public override bool IsValid()
    {
        return MatchId > 0;
    }
}