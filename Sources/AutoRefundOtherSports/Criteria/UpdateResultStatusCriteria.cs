using Fanex.Data.Repository;

namespace AutoRefundOtherSports.Criteria
{
    public class UpdateResultStatusCriteria : NonQueryCommand
    {
        public int MatchId { get; set; }

        public string ResourceIds { get; set; }

        public int TypeId { get; set; }

        public int StatusId { get; set; }

        public string Remark { get; set; }

        public override bool IsValid()
        {
            return MatchId > 0;
        }

        public override string GetSettingKey()
        {
            return "NAP_Set_AutoRefundOS_ResultUpd";
        }
    }
}