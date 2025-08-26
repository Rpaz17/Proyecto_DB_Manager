namespace OracleDBManager.Models;

public class CreateProcedureRequest
{
    public string ConnectionString { get; set; } = default!;
    public string Schema { get; set; } = default!;
    public string ProcedureName { get; set; } = default!;
    public string Source { get; set; } = default!;
}