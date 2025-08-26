using Microsoft.AspNetCore.Mvc;
using OracleDbManager.Models;
using OracleDbManager.Services;

namespace OracleDbManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SqlController : ControllerBase
{
    private readonly OracleService _oracle;
    public SqlController(OracleService oracle) => _oracle = oracle;

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] SqlRequest req)
    {
        var result = await _oracle.ExecuteSqlAsync(req.ConnectionString, req.Sql, req.MaxRows);
        return Ok(result);
    }
}
