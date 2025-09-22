namespace OracleDBManager.Models;

public class SyncResult
{
    public bool Success { get; set; }
    public List<string> TablesCopied { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}