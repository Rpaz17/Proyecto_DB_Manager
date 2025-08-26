namespace OracleDbManager.Models;

public class CreateTableRequest
{
    public string ConnectionString { get; set; } = default!;
    public string Schema { get; set; } = default!;
    public string TableName { get; set; } = default!;
    public List<ColumnDef> Columns { get; set; } = new();
    public string? PrimaryKeyName { get; set; }
}
public class ColumnDef
{
    public string Name { get; set; } = default!;
    public string DataType { get; set; } = default!; // e.g., VARCHAR2, NUMBER, DATE
    public int? Length { get; set; } // for VARCHAR2
    public bool Nullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; } = false;
}
