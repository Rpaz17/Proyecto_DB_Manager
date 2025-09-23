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
    public async Task<IActionResult> StartCurrent([FromHeader(Name = "ConnectionString")] string oracleConnection, [FromHeader(Name = "PostgresConnectionString")] string? pgConnection = null)
    {
        var pgConn = !string.IsNullOrWhiteSpace(pgConnection) ? pgConnection : Environment.GetEnvironmentVariable("POSTGRES_CONN");
        if (string.IsNullOrWhiteSpace(oracleConnection) || string.IsNullOrWhiteSpace(pgConn))
            return BadRequest("Connection strings are required");
        var req = new SyncRequest { OracleConnection = oracleConnection, PostgresConnection = pgConn };
        var progress = new Progress<ProgressSync>(p => { });
        var result = await _sync.StartAsync(req, progress);
        return Ok(result);
    }

}