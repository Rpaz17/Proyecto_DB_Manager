namespace OracleDbManager.Models;

public class CreateRoutineRequest
{
    public string ConnectionString { get; set; } = default!;
    public string Schema { get; set; } = default!;
    public string Name { get; set; } = default!;
    /// <summary>Raw PL/SQL source including CREATE OR REPLACE PROCEDURE/FUNCTION ... END;</summary>
    public string Source { get; set; } = default!;
}
