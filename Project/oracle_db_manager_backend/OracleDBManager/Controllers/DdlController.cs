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
        var result = await _oracle.CreateTableAsync(request);
        if (result is not null && result.status == true)
        {
            return Ok(result);
        }
        return BadRequest(new { Message = $"Error creating table: {result?.Message}" });
    }

    [HttpPost("create-view")]
    public async Task<IActionResult> CreateView([FromBody] CreateViewRequest req)
    {
        var result = await _oracle.CreateViewAsync(req);

        if (result is not null && result.status == true)
        {
            return Ok(result);
        }
        return BadRequest(new { Message = $"Error creating view: {result?.Message}" });
    }


    [HttpPost("create-procedure")]
    public async Task<IActionResult> CreateProcedure([FromBody] CreateProcedureRequest req)
    {
        var result = await _oracle.CreateProcedureAsync(req);
        if (result is not null && result.status == true)
        {
            return Ok(result);
        }
        return BadRequest(new { Message = $"Error creating procedure: {result?.Message}" });
    }
}


