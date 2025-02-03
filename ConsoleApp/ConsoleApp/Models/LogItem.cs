namespace ConsoleApp.Models;

public class LogItem
{
    public string Id { get; set; }
    public DateTime DateTime { get; set; }
    public string NikoOrderId { get; set; }
    public string url { get; set; }
    public int statusCode { get; set; }
    public string? ErrorMessage { get; set; }
}