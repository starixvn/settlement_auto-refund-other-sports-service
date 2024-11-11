using System.Xml.Serialization;

namespace AutoRefundOtherSports.Models;

public class CheckOddsItem
{
    private readonly List<ResourceItem> resourceIdItems = [];

    [XmlAttribute("MatchId")]
    public int MatchId { get; set; }

    [XmlElement("ResourceId")]
    public List<ResourceItem> ResourceIdItems
    {
        get
        {
            ResourceIds.ForEach(item =>
            {
                resourceIdItems.Add(new ResourceItem { ResourceId = item });
            });
            return resourceIdItems;
        }
    }

    [XmlIgnore]
    public List<string> ResourceIds { get; set; }
}