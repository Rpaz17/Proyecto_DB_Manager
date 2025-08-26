using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using OracleDbManager.Models;

namespace OracleDbManager.Services;

/// <summary>
/// Provides services for managing Oracle database operations.
/// </summary>
public class OracleService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleService"/> class.
    /// </summary>
    public OracleService()
    {

    }

    private OracleConnection NewConn(string connectionString)
    {
        return new OracleConnection(connectionString);
    }

    public async Task<QueryResult> ExecuteSqlAsync(string connectionString, string sql, int? maxRows = 500)
    {
        using var conn = NewConn(connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        try
        {
            using var reader = await cmd.ExecuteReaderAsync();
            var result = new QueryResult();
            var schema = reader.GetColumnSchema();
            result.Columns = schema.Select(c => c.ColumnName ?? "COL").ToList();

            int count = 0;
            while (await reader.ReadAsync())
            {
                var row = new List<object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row.Add(val);
                }
                result.Rows.Add(row);
                count++;
                if (maxRows.HasValue && count >= maxRows.Value) break;
            }
            return result;
        }
        catch (OracleException ex) when (ex.Number == 942 || ex.Number == 6550)
        {
            // If not a query, try non-query
            var rows = await cmd.ExecuteNonQueryAsync();
            return new QueryResult { RowsAffected = rows, Message = "Statement executed." };
        }
    }

    public async Task<List<string>> GetSchemasAsync(string connectionString)
    {
        using var conn = NewConn(connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT USERNAME FROM ALL_USERS ORDER BY USERNAME";
        var lst = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lst.Add(reader.GetString(0));
        }
        return lst;
    }

    public async Task<Dictionary<string, object>> GetTreeAsync(string connectionString)
    {
        using var conn = NewConn(connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT GLOBAL_NAME FROM GLOBAL_NAME";
        var dbName = await cmd.ExecuteScalarAsync() as string ?? "DB";

        async Task<List<string>> FetchList(string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var list = new List<string>();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(r.GetString(0));
            return list;
        }

        var tree = new Dictionary<string, object>();

        var node = new Dictionary<string, object>();
        node["tables"] = await FetchList("SELECT OBJECT_NAME FROM DBA_OBJECTS WHERE OBJECT_TYPE = 'TABLE' ORDER BY OBJECT_NAME");
        node["views"] = await FetchList("SELECT OBJECT_NAME FROM DBA_OBJECTS WHERE OBJECT_TYPE = 'VIEW' ORDER BY OBJECT_NAME");
        node["procedures"] = await FetchList("SELECT OBJECT_NAME FROM DBA_OBJECTS WHERE OBJECT_TYPE = 'PROCEDURE' ORDER BY OBJECT_NAME");
        node["functions"] = await FetchList("SELECT OBJECT_NAME FROM DBA_OBJECTS WHERE OBJECT_TYPE = 'FUNCTION' ORDER BY OBJECT_NAME");
        tree[dbName] = node;

        return tree;
    }

    public async Task CreateTableAsync(CreateTableRequest req)
    {
        using var conn = NewConn(req.ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();

        string cols = string.Join(", ", req.Columns.Select(c =>
        {
            string type = c.DataType.ToUpperInvariant();
            if (c.Length.HasValue && (type.Contains("CHAR")))
                type += $"({c.Length.Value})";
            string nullable = c.Nullable ? "" : " NOT NULL";
            return $"{Quote(c.Name)} {type}{nullable}";
        }));

        var pkCols = req.Columns.Where(c => c.IsPrimaryKey).Select(c => Quote(c.Name)).ToList();
        string pkSql = pkCols.Any()
            ? $", CONSTRAINT {Quote(req.PrimaryKeyName ?? $"{req.TableName}_PK")} PRIMARY KEY ({string.Join(", ", pkCols)})"
            : "";

        cmd.CommandText = $"CREATE TABLE {Quote(req.Schema)}.{Quote(req.TableName)} ({cols}{pkSql})";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CreateViewAsync(CreateViewRequest req)
    {
        using var conn = NewConn(req.ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE OR REPLACE VIEW {Quote(req.Schema)}.{Quote(req.ViewName)} AS {req.SelectSql}";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CreateRoutineAsync(CreateRoutineRequest req)
    {
        using var conn = NewConn(req.ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = req.Source;
        await cmd.ExecuteNonQueryAsync();
    }
    private static string Quote(string ident) => $"\"{ident.Replace("\"", "\"\"")}\"";
}
