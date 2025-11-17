using System.Text.Json.Serialization;

namespace GuideLens.Models
{
    public class Recommendation
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Neighborhood { get; set; } = string.Empty;
        public string TheBestOffer { get; set; } = string.Empty;
        public string NoteTip { get; set; } = string.Empty;

        [JsonIgnore]
        public string NameLower { get; set; } = string.Empty;

        [JsonIgnore]
        public string TheBestOfferLower { get; set; } = string.Empty;

        [JsonIgnore]
        public string NoteTipLower { get; set; } = string.Empty;
    }
}