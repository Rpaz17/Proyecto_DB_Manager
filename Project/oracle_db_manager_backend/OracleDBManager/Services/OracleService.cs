using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using OracleDBManager.Models;
using System.Text;

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

            node["tables"] = await FetchList(ConnectionString, "SELECT TABLE_NAME FROM USER_TABLES ORDER BY TABLE_NAME");
            node["views"] = await FetchList(ConnectionString, "SELECT VIEW_NAME FROM USER_VIEWS ORDER BY VIEW_NAME");
            node["procedures"] = await FetchList(ConnectionString, "SELECT PROCEDURE_NAME FROM USER_PROCEDURES ORDER BY PROCEDURE_NAME");
            node["functions"] = await FetchList(ConnectionString, "SELECT OBJECT_NAME FROM USER_OBJECTS WHERE OBJECT_TYPE = 'FUNCTION' ORDER BY OBJECT_NAME");
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
    public async Task<string> GetMermaidClassDiagramAsync(string connectionString)
    {
        using var conn = NewConn(connectionString);
        await conn.OpenAsync();

        // Gather tables
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var tcmd = conn.CreateCommand())
        {
            tcmd.CommandText = @"SELECT TABLE_NAME FROM USER_TABLES ORDER BY TABLE_NAME";
            using var r = await tcmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) tables.Add(r.GetString(0));
        }

        // Gather columns
        var cols = new Dictionary<string, List<(string name, string type)>>(StringComparer.OrdinalIgnoreCase);
        using (var ccmd = conn.CreateCommand())
        {
            ccmd.CommandText = @"
          SELECT TABLE_NAME,
                 COLUMN_NAME,
                 DATA_TYPE,
                 DATA_LENGTH,
                 DATA_PRECISION,
                 DATA_SCALE
          FROM USER_TAB_COLUMNS
          ORDER BY TABLE_NAME, COLUMN_ID";
            using var r = await ccmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                string t = r.GetString(0);
                string c = r.GetString(1);
                string dt = r.GetString(2);

                // Pretty type string
                string typeLabel = dt;
                bool hasLength = !r.IsDBNull(3) && r.GetInt32(3) > 0;
                bool hasPrec = !r.IsDBNull(4);
                bool hasScale = !r.IsDBNull(5);

                if (dt.Contains("CHAR", StringComparison.OrdinalIgnoreCase) && hasLength)
                {
                    typeLabel = $"{dt}({r.GetInt32(3)})";
                }
                else if ((dt.Equals("NUMBER", StringComparison.OrdinalIgnoreCase) || dt.Equals("DECIMAL", StringComparison.OrdinalIgnoreCase)) && hasPrec)
                {
                    int p = Convert.ToInt32(r.GetDecimal(4));
                    if (hasScale && !r.IsDBNull(5))
                    {
                        int s = Convert.ToInt32(r.GetDecimal(5));
                        typeLabel = $"{dt}({p},{s})";
                    }
                    else
                    {
                        typeLabel = $"{dt}({p})";
                    }
                }

                if (!cols.TryGetValue(t, out var list)) cols[t] = list = new();
                list.Add((c, typeLabel));
            }
        }

        // Gather FK relations
        var rels = new HashSet<(string pkTable, string fkTable, string pkCol, string fkCol)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
            SELECT
                pk.table_name   AS pk_table,
                fk.table_name   AS fk_table,
                pkc.column_name AS pk_col,
                fkc.column_name AS fk_col,
                fkc.position    AS pos
            FROM user_constraints fk
            JOIN user_cons_columns fkc
              ON fk.constraint_name = fkc.constraint_name
            JOIN user_constraints pk
              ON fk.r_constraint_name = pk.constraint_name
            JOIN user_cons_columns pkc
              ON pk.constraint_name = pkc.constraint_name
             AND pkc.position = fkc.position
           WHERE fk.constraint_type = 'R'
           ORDER BY fk.table_name, fkc.position";
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                rels.Add((r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3)));
                tables.Add(r.GetString(0));
                tables.Add(r.GetString(1));
            }
        }

        // Mermaid ClassDiagram syntax
        var sb = new StringBuilder();
        sb.AppendLine("classDiagram");

        // Classes with attributes: "TYPE NAME"
        foreach (var t in tables.OrderBy(x => x))
        {
            sb.AppendLine($"class {t} {{");
            if (cols.TryGetValue(t, out var list))
            {
                foreach (var (name, type) in list)
                    sb.AppendLine($"  {type} {name}");
            }
            sb.AppendLine("}");
        }

        // Relations
        foreach (var (pkT, fkT, pkC, fkC) in rels.OrderBy(x => x.fkTable).ThenBy(x => x.pkTable))
            sb.AppendLine($"{fkT} --> {pkT} : {fkC}→{pkC}");

        return sb.ToString();
    }

    private static string Quote(string ident) => $"\"{ident.Replace("\"", "\"\"")}\"";
}