using System.Xml.Serialization;

namespace AutoRefundOtherSports.Models
{
    public class ResultDetail
    {
        [XmlIgnore]
        public int MatchId { get; set; }

        [XmlIgnore]
        public int LeagueId { get; set; }

        [XmlAttribute("Id")]
        public string ResourceId { get; set; }

        [XmlIgnore]
        public byte ResultStatusId { get; set; }

        [XmlIgnore]
        public byte TypeId { get; set; }
    }
}