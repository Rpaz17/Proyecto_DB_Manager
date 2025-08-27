namespace OracleDBManager.Models;

public class QueryResult
{
    public List<string> Columns { get; set; } = new();
    public List<List<object?>> Rows { get; set; } = new();
    public int? RowsAffected { get; set; }
    public string? Message { get; set; } //For DDL messages
}