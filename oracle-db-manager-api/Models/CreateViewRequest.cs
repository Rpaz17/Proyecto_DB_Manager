namespace OracleDbManager.Models;

public class CreateViewRequest
{
    public string ConnectionString { get; set; } = default!;
    public string Schema { get; set; } = default!;
    public string ViewName { get; set; } = default!;
    public string SelectSql { get; set; } = default!;
}
