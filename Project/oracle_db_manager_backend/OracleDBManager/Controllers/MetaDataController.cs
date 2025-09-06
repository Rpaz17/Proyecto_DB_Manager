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

    [HttpGet("table-ddl/{tableName}")]
    public async Task<IActionResult> GetTableDdl(
        [FromHeader(Name = "ConnectionString")] string conn, string tableName, [FromQuery] string? schema = null)
    {
        var ddl = await _oracle.GetTableDdlAsync(conn, tableName, schema);
        return Ok(new { Table = tableName, Schema = schema, DDL = ddl });
    }
}