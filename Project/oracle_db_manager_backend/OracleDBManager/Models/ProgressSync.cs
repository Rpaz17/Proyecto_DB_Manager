namespace OracleDBManager.Models;

public class ProgressSync
{
    public string Stage { get; set; } = "";
    public string? CurrentTable { get; set; }
    public int Percent { get; set; }
    public string? Message { get; set; }
}