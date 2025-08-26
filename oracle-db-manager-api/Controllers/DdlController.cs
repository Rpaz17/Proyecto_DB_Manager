using Microsoft.AspNetCore.Mvc;
using OracleDbManager.Models;
using OracleDbManager.Services;

namespace OracleDbManager.Controllers;

/// <summary>
/// Controller for handling Data Definition Language (DDL) operations.
/// </summary>

[ApiController]
[Route("api/[controller]")]
public class DdlController : ControllerBase
{
    private readonly OracleService _oracle;
    /// <summary>
    /// Initializes a new instance of the <see cref="DdlController"/> class with the specified Oracle service.
    /// </summary>
    /// <param name="oracle">The Oracle service used for DDL operations.</param>
    public DdlController(OracleService oracle) => _oracle = oracle;


    [HttpPost("create-table")]
    public async Task<IActionResult> CreateTable([FromBody] CreateTableRequest req)
    {
        await _oracle.CreateTableAsync(req);
        return Ok(new { message = "Table created." });
    }


    [HttpPost("create-view")]
    public async Task<IActionResult> CreateView([FromBody] CreateViewRequest req)
    {
        await _oracle.CreateViewAsync(req);
        return Ok(new { message = "View created." });
    }

    [HttpPost("create-routine")]
    public async Task<IActionResult> CreateRoutine([FromBody] CreateRoutineRequest req)
    {
        await _oracle.CreateRoutineAsync(req);
        return Ok(new { message = "Routine created." });
    }
}
