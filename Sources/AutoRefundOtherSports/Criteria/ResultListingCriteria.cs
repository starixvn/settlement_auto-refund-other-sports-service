using Fanex.Data.Repository;

namespace AutoRefundOtherSports.Criteria
{
    public class ResultListingCriteria : CriteriaBase
    {
        public int SportType { get; set; }

        public override bool IsValid()
        {
            return true;
        }

        public override string GetSettingKey()
        {
            return "NAP_Set_AutoRefund_ScheduleSel";
        }
    }
}