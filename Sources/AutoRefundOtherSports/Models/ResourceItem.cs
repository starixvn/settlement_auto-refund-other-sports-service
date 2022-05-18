using System.Xml.Serialization;

namespace AutoRefundOtherSports.Models
{
    public class ResourceItem
    {
        public ResourceItem()
        {
        }

        [XmlAttribute("Id")]
        public string ResourceId { get; set; }

        public ResourceItem(string id)
        {
            ResourceId = id;
        }
    }
}