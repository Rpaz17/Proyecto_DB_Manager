using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using OracleDBManager.Models;

namespace OracleDBManager.Services;

public class OracleService
{
    public OracleService()
    {

    }

    public OracleConnection NewConn(string ConnectionString)
    {
        return new OracleConnection(ConnectionString);
    }

    public async Task<QueryResult> ExecuteSqlAsync(string ConnectionString, string sql, int? MaxRows = 500) //cambio aqui
    {
        using var conn = NewConn(ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        try
        {
            using var reader = await cmd.ExecuteReaderAsync();
            var result = new QueryResult();
            var schema = reader.GetColumnSchema(); //cambio aqui
            result.Columns = schema.Select(c => c.ColumnName ?? "COL").ToList(); //cambio aqui

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
                if (MaxRows.HasValue && count >= MaxRows.Value) break; //cambio aqui
            }
            return result;
        }
        catch (OracleException ex) when (ex.Number == 942 || ex.Number == 6550) //tabla o vista no existente o problema de conexion
        {
            var rows = await cmd.ExecuteNonQueryAsync();
            return new QueryResult { RowsAffected = rows, Message = "Statement executed" };
        }
    }

    public async Task<List<string>> GetSchemaNameAsync(string ConnectionString)
    {
        using var conn = NewConn(ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT USERNAME FROM ALL_USERS ORDER BY USERNAME";
        var list = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) //cambio aqui
        {
            list.Add(reader.GetString(0));
        }
        return list;
    }

    private async Task<List<string>> FetchList(string ConnectionString, string sql)
    {

        using var conn = NewConn(ConnectionString);
        await conn.OpenAsync();

        var list = new List<string>();

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            using var r = await cmd.ExecuteReaderAsync(); //cambio aqui
            while (await r.ReadAsync())
                list.Add(r.GetString(0));
        }
        finally
        {
            conn.Close();
        }
        return list;
    }

    public async Task<Dictionary<string, object>> GetTreeAsync(string ConnectionString)
    {
        var tree = new Dictionary<string, object>();
        var node = new Dictionary<string, object>();
        using var conn = NewConn(ConnectionString);
        await conn.OpenAsync();

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT GLOBAL_NAME FROM GLOBAL_NAME";
            var dbName = await cmd.ExecuteScalarAsync() as string ?? "DB";

            node["tables"] = await FetchList(ConnectionString, "SELECT OBJECT_NAME FROM DBA_OBJECTS WHERE OBJECT_TYPE = 'TABLE' ORDER BY OBJECT_NAME");
            node["views"] = await FetchList(ConnectionString, "SELECT OBJECT_NAME FROM DBA_OBJECTS WHERE OBJECT_TYPE = 'VIEW' ORDER BY OBJECT_NAME");
            node["procedures"] = await FetchList(ConnectionString, "SELECT OBJECT_NAME FROM DBA_OBJECTS WHERE OBJECT_TYPE = 'PROCEDURE' ORDER BY OBJECT_NAME");
            node["functions"] = await FetchList(ConnectionString, "SELECT OBJECT_NAME FROM DBA_OBJECTS WHERE OBJECT_TYPE = 'FUNCTION' ORDER BY OBJECT_NAME");
            tree[dbName] = node;

        }
        finally
        {
            conn.Close();
        }

        return tree;
    }

    public async Task<Result> CreateTableAsync(CreateTableRequest req)
    {
        try
        {
            using var conn = NewConn(req.ConnectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();

            string cols = string.Join(", ", req.Columns.Select(c =>
            {
                string type = c.DataType.ToUpperInvariant();
                if (c.Length.HasValue && type.Contains("CHAR")) type += $"({c.Length.Value})";
                string nullable = c.Nullable ? " " : " NOT NULL";
                return $"{Quote(c.Name)} {type}{nullable}";
            }));

            var pkCols = req.Columns.Where(c => c.IsPrimaryKey).Select(c => Quote(c.Name)).ToList();
            string pkSql = pkCols.Any() ? $", CONSTRAINT {Quote(req.PrimaryKeyName ?? $"{req.TableName}_PK")} PRIMARY KEY ({string.Join(", ", pkCols)})" : "";
            cmd.CommandText = $"CREATE TABLE {req.TableName} ({cols}{pkSql})";
            await cmd.ExecuteNonQueryAsync();
            return new Result { status = true, Message = "Created Table successfully" };
        }
        catch (Exception ex)
        {
            return new Result { status = false, Message = $"Error creating table: {ex.Message}" };
        }
    }

    public async Task<Result> CreateViewAsync(CreateViewRequest req)
    {
        try
        {
            using var conn = NewConn(req.ConnectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE VIEW {Quote(req.Schema)}.{Quote(req.ViewName)} AS {req.SelectSql}";
            await cmd.ExecuteNonQueryAsync();
            return new Result { status = true, Message = "Created View successfully" };
        }
        catch (Exception ex)
        {
            return new Result { status = false, Message = ex.Message };
        }
    }

    public async Task<Result> CreateProcedureAsync(CreateProcedureRequest req)
    {
        try
        {
            using var conn = NewConn(req.ConnectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE OR REPLACE PROCEDURE {Quote(req.Schema)}.{Quote(req.ProcedureName)} AS {req.Source}";
            await cmd.ExecuteNonQueryAsync();
            return new Result { status = true, Message = "Created Procedure successfully" };
        }
        catch (Exception ex)
        {
            return new Result { status = false, Message = ex.Message };
        }

    }
    private static string Quote(string ident) => $"\"{ident.Replace("\"", "\"\"")}\"";
}