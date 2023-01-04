using System.Xml.Serialization;

namespace AutoRefundOtherSports.Models;

[XmlRoot("Resources")]
public class Resources<T>
{
    [XmlElement("Resource")]
    public List<T> ResourceItems { get; set; }
}