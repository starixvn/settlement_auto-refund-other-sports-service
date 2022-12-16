using Fanex.Data;
using Fanex.Data.Repository;

namespace AutoRefundOtherSports.Criteria
{
    public class SettleCriteria : NonQueryCommand
    {
        public int AdminId { get; }

        public int MatchId { get; set; }

        public string SelectedResources { get; set; }

        public string Action { get; } = "REFUND";

        public bool IsAuto { get; } = true;

        public int ErrCode { get; set; }

        public string ErrMsg { get; set; }

        [SpParam(Ignored = true)]
        public string DbServer { get; set; }

        [SpParam(Ignored = true)]
        public string SpName => "NAP_Admin_Settlement_Profile_Push";

        public override string GetSettingKey()
        {
            return $"NAP_Admin_Settlement_Profile_Push_{DbServer}";
        }

        public override bool IsValid()
        {
            return MatchId > 0;
        }
    }
}