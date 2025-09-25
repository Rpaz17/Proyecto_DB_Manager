using Microsoft.AspNetCore.Mvc;
using OracleDBManager.Models;
using OracleDBManager.Services;

namespace OracleDBManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly SyncService _sync;

    public SyncController(SyncService sync)
    {
        _sync = sync;
    }

    //Use connection of Oracle from UI
    [HttpPost("start-current")]
    public async Task<IActionResult> StartCurrent(
        [FromHeader(Name = "ConnectionString")] string oracleConnection,
        [FromHeader(Name = "PgConnectionString")] string? pgConnection = null)
    {
        if (string.IsNullOrWhiteSpace(oracleConnection))
        {
            return BadRequest(new { message = "Oracle connection string is required" });
        }
        var pgConn = !string.IsNullOrWhiteSpace(pgConnection) ? pgConnection : Environment.GetEnvironmentVariable("POSTGRES_CONN");
        if (string.IsNullOrWhiteSpace(pgConn))
            return BadRequest(new { message = "Missing postgres connection string" });
        var req = new SyncRequest { OracleConnection = oracleConnection, PostgresConnection = pgConn, OracleOwner = null };
        var result = await _sync.StartAsync(req, new Progress<ProgressSync>(p => { }));
        return Ok(result);
    }

    [HttpGet("debug/oracle-ping")]
    public async Task<IActionResult> Ping([FromHeader(Name = "ConnectionString")] string cs)
    {
        try
        {
            await using var conn = new Oracle.ManagedDataAccess.Client.OracleConnection(cs);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM dual";
            var v = await cmd.ExecuteScalarAsync();
            return Ok(new { ok = true, value = v?.ToString() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.ToString());
        }
    }
}