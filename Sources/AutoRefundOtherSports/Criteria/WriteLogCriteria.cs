using AutoRefundOtherSports.Models;
using Fanex.Data.Repository;

namespace AutoRefundOtherSports.Criteria
{
    public class WriteLogCriteria : NonQueryCommand
    {
        public WriteLogCriteria(Match match, string errorMessage)
        {
            MatchId = match.MatchId;
            Remark = errorMessage;
        }

        public int UserId { get; }

        public int MatchId { get; set; }

        public string Remark { get; set; }

        public string IP { get; } = string.Empty;

        public int ActionId { get; } = 109;

        public override bool IsValid()
        {
            return MatchId > 0;
        }

        public override string GetSettingKey()
        {
            return "WriteLog_UserLog_Settle";
        }
    }
}