using Newtonsoft.Json;

namespace XPlat.VUI.Models
{
    [JsonObject]
    public class GoogleUserStorage
    {
        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; }
    }
}
