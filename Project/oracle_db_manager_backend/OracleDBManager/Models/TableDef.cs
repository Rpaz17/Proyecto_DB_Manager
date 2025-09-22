namespace OracleDBManager.Models;

public class TableDef
{
    public string Owner { get; set; } = "";
    public string TableName { get; set; } = "";
    public List<ColumnDef> Columns { get; set; } = new();
    public string PgQualifiedName => $"\"{Owner.ToLower()}\".\"{TableName.ToLower()}\"";
}

public class ColumnDef
{
    public string ColumnName { get; set; } = "";
    public string DataType { get; set; } = "";
    public int? DataLength { get; set; }
    public int? DataPrecision { get; set; }
    public int? DataScale { get; set; }
    public bool Nullable { get; set; }
    public string PgDataType
    {
        get
        {
            return DataType.ToUpper() switch
            {
                "VARCHAR2" or "NVARCHAR2" or "CHAR" or "NCHAR" => DataLength.HasValue ? $"VARCHAR({DataLength.Value})" : "TEXT",
                "NUMBER" => DataPrecision.HasValue ? (DataScale.HasValue && DataScale.Value > 0 ? $"NUMERIC({DataPrecision.Value},{DataScale.Value})" : $"INTEGER") : "NUMERIC",
                "FLOAT" => "REAL",
                "DATE" => "TIMESTAMP",
                "TIMESTAMP" or "TIMESTAMP WITH TIME ZONE" or "TIMESTAMP WITH LOCAL TIME ZONE" => "TIMESTAMP",
                "CLOB" or "NCLOB" => "TEXT",
                "BLOB" => "BYTEA",
                _ => "TEXT"
            };
        }
    }
}