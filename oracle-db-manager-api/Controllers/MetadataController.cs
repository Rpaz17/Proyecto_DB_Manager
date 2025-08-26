using Microsoft.AspNetCore.Mvc;
using OracleDbManager.Models;
using OracleDbManager.Services;

namespace OracleDbManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetadataController : ControllerBase
{
    private readonly OracleService _oracle;
    public MetadataController(OracleService oracle) => _oracle = oracle;

    [HttpGet("schemas")]
    public async Task<IActionResult> Schemas([FromHeader(Name = "ConnectionString")] string conn)
    {
        var list = await _oracle.GetSchemasAsync(conn);
        return Ok(list);
    }

    [HttpGet("tree")]
    public async Task<IActionResult> Tree([FromHeader(Name = "ConnectionString")] string conn)
    {
        var tree = await _oracle.GetTreeAsync(conn);
        return Ok(tree);
    }
}
