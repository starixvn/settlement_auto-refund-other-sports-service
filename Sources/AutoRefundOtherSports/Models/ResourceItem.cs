using System.Xml.Serialization;

namespace AutoRefundOtherSports.Models;

public class ResourceItem
{
    [XmlAttribute("Id")]
    public string ResourceId { get; set; }
}