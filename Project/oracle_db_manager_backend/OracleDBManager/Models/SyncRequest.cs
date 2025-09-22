namespace OracleDBManager.Models;

public class SyncRequest
{
    public string OracleConnection { get; set; } = "";
    public string PostgresConnection { get; set; } = "";
    public string? OracleOwner { get; set; }
}