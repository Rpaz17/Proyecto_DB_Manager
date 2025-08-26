namespace OracleDBManager.Models;

public class CreateTableRequest
{
    public string ConnectionString { get; set; } = default!;
    public string Schema { get; set; } = default!;
    public string TableName { get; set; } = default!;
    public List<ColumnDefinition> Columns { get; set; } = new();
    public string? PrimaryKeyName { get; set; } = default!;
}

public class ColumnDefinition
{
    public string Name { get; set; } = default!;
    public string DataType { get; set; } = default!;
    public int? Lenght { get; set; }
    public bool Nullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; } = false;
}