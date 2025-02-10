using Newtonsoft.Json;

namespace ConsoleApp.Models
{
    public class CosmosLogItem
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("dateTime")]
        public DateTime DateTime { get; set; }

        [JsonProperty("nikoOrderId")]
        public string? NikoOrderId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        [JsonProperty("statusCode")]
        public int? StatusCode { get; set; }

        [JsonProperty("response")]
        public OrderResponse? Response { get; set; }

        public class OrderResponse
        {
            [JsonProperty("payload")]
            public string Payload { get; set; } = string.Empty;
        }
    }
}