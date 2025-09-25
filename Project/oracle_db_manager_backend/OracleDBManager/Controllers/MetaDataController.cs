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

    [HttpGet("schemas")]
    public async Task<IActionResult> GetSchemas([FromHeader(Name = "ConnectionString")] string conn)
    {
        var list = await _oracle.GetSchemaNameAsync(conn);
        return Ok(list);
    }

    [HttpGet("Tree")]
    public async Task<IActionResult> GetTree([FromHeader(Name = "ConnectionString")] string conn)
    {
        var tree = await _oracle.GetTreeAsync(conn);
        return Ok(tree);
    }

    [HttpGet("class-graph")]
    public async Task<IActionResult> GetClassGraph([FromHeader(Name = "ConnectionString")] string conn)
    {
        var mermaid = await _oracle.GetMermaidClassDiagramAsync(conn);
        return Ok(new { mermaid });
    }

}