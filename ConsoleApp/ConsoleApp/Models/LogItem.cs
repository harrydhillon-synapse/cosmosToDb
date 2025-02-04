using Newtonsoft.Json;

namespace ConsoleApp.Models;

public class LogItem
{
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonProperty("dateTime")]
    public DateTime DateTime { get; set; }
    
    [JsonProperty("nikoOrderId")]
    public string? NikoOrderId { get; set; }
    
    [JsonProperty("url")]
    public string url { get; set; }
    
    [JsonProperty("statusCode")]
    public int? StatusCode { get; set; }
    
    [JsonProperty("errorMessage")]
    public string? ErrorMessage { get; set; }
}