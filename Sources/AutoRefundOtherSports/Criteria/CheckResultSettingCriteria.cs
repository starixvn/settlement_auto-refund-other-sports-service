using Fanex.Data.Repository;

namespace AutoRefundOtherSports.Criteria
{
    public class CheckResultSettingCriteria : NonQueryCommand
    {
        public int MatchId { get; set; }

        public string SelectedResources { get; set; }

        public string Action { get; } = "REFUND";

        public int ErrCode { get; set; }

        public string ErrMsg { get; set; }

        public override string GetSettingKey()
        {
            return "NAP_Set_ResultSettingChecking";
        }

        public override bool IsValid() => MatchId > 0;
    }
}