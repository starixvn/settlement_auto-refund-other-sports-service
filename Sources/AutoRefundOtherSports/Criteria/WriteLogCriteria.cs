namespace AutoRefundOtherSports.Criteria;

public class WriteLogCriteria : NonQueryCommand
{
    public WriteLogCriteria(Match match, string errorMessage)
    {
        MatchId = match.MatchId;
        Remark = errorMessage;
    }

    public int UserId { get; set; }

    public int MatchId { get; set; }

    public string Remark { get; set; }

    public string Ip { get; } = string.Empty;

    public int ActionId { get; } = 109;

    public override bool IsValid() => MatchId > 0;

    public override string GetSettingKey()
    {
        return "WriteLog_UserLog_Settle";
    }
}