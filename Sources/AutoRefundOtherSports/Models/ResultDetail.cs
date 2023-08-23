using System.Xml.Serialization;

namespace AutoRefundOtherSports.Models;

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
    public byte SettlementStatusId { get; set; }

    [XmlIgnore]
    public byte TypeId { get; set; }

    [XmlIgnore]
    public string SourceId { get; set; }

    [XmlIgnore]
    public int ParentId { get; set; }

    [XmlIgnore]
    public int SportType { get; set; }
}