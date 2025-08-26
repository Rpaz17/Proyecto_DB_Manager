using Microsoft.AspNetCore.Mvc;
using OracleDBManager.Models;
using OracleDBManager.Services;

namespace OracleDBManager.Controllers;

[ApiController]
[Route("api/[controller]")]

public class MetaDataController : ControllerBase
{
    private readonly OracleService _oracle;
    public MetaDataController(OracleService oracle) => _oracle = oracle;

    [HttpGet("Schemas")]
    public async Task<IActionResult> GetSchemas([FromHeader(name = "ConnectionString")] string conn)
    {
        var list = await _oracle.GetSchemaNameAsync(conn);
        return Ok(list);
    }

    [HttpGet("Tree")]
    public async Task<IActionResult> GetTree([FromHeader(name = "ConnectionString")] string conn)
    {
        var tree = await _oracle.GetTreeAsync(conn);
        return Ok(tree);
    }
}