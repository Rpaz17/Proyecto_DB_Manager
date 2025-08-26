using Microsoft.AspNetCore.Mvc;
using OracleDBManager.Models;
using OracleDBManager.Services;

namespace OracleDBManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DdlController : ControllerBase
{
    private readonly OracleService _oracle;
    public DdlController(OracleService oracle) => _oracle = oracle;

    [HttpPost("create-table")]
    public async Task<IActionResult> CreateTable([FromBody] CreateTableRequest request)
    {
        await _oracle.CreateTableAsync(request);
        return Ok(new { Message = "Table created" });
    }

    [HttpPost("create-view")]
    public async Task<IActionResult> CreateView([FromBody] CreateViewRequest req)
    {
        await _oracle.CreateViewAsync(req);
        return Ok(new { Message = "View created" });
    }


    [HttpPost("create-procedure")]
    public async Task<IActionResult> CreateProcedure([FromBody] CreateProcedureRequest req)
    {
        await _oracle.CreateProcedureAsync(req);
        return Ok(new { message = "Procedure created." });
    }
}


