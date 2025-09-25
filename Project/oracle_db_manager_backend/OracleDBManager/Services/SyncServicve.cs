
using OracleDBManager.Models;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace OracleDBManager.Services;

public class SyncService
{
    private readonly PostgresService _pg;
    public SyncService(PostgresService pg)
    {
        _pg = pg;
    }

    public async Task<SyncResult> StartAsync(SyncRequest req, IProgress<ProgressSync>? progress = null)
    {
        var result = new SyncResult();
        try
        {
            progress?.Report(new ProgressSync { Stage = "Planning", Percent = 5 });

            //Read table list from Oracle
            var tables = await GetOracleTablesAsync(req.OracleConnection, req.OracleOwner);

            //create schemas and tables in Postgres
            progress?.Report(new ProgressSync { Stage = "Creating Schema", Percent = 20 });
            foreach (var t in tables)
            {
                var createSql = BuildPgCreateTableSql(t);
                await _pg.ExecSync(req.PostgresConnection, createSql);
            }

            //Copy data
            progress?.Report(new ProgressSync { Stage = "Copying Data", Percent = 56 });
            int done = 0;
            foreach (var t in tables)
            {
                var cols = t.Columns.Select(c => c.ColumnName).ToArray();
                var rows = StreamTableRowsAsync(req.OracleConnection, t.Owner, t.TableName, cols);
                await _pg.BulkCopyAsync(req.PostgresConnection, t.PgQualifiedName, cols, rows);

                result.TablesCopied.Add($"{t.Owner}.{t.TableName}");
                done++;
                progress?.Report(new ProgressSync { Stage = "Copying Data", CurrentTable = $"{t.Owner}.{t.TableName}", Percent = 40 + (int)(50.0 * done / tables.Count) });
            }








            //Validate row counts
            progress?.Report(new ProgressSync { Stage = "Done", Percent = 100 });
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.ToString());
            result.Success = false;
            progress?.Report(new ProgressSync { Stage = "Error", Percent = 100, Message = ex.Message });
        }
        return result;
    }

    private async Task<List<TableDef>> GetOracleTablesAsync(string cs, string owner)
    {
        var list = new List<TableDef>();
        await using var conn = new Oracle.ManagedDataAccess.Client.OracleConnection(cs);
        try { await conn.OpenAsync(); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not open Oracle connection. Check ConnectionString header (user/pass/host/port/service).", ex);
        }

        await using (var ping = conn.CreateCommand())
        {
            ping.CommandText = "SELECT 1 FROM DUAL";
            await ping.ExecuteScalarAsync();
        }

        string currentUser;
        await using (var who = conn.CreateCommand())
        {
            who.CommandText = "SELECT USER FROM DUAL";
            currentUser = (String)(await who.ExecuteScalarAsync() ?? "USER");
        }

        var schema = currentUser;
        var useAll = false;

        //Tables
        var tables = new List<(string Owner, string TableName)>();

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"SELECT USER AS OWNER, TABLE_NAME FROM USER_TABLES ORDER BY TABLE_NAME";

            await using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess);



            while (await reader.ReadAsync())
            {
                tables.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        //Columns
        foreach (var (own, tabN) in tables)
        {
            var td = new TableDef { Owner = own, TableName = tabN, Columns = new() };
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT COLUMN_NAME, DATA_TYPE, DATA_LENGTH, DATA_PRECISION, DATA_SCALE, NULLABLE
                                FROM USER_TAB_COLUMNS
                                WHERE TABLE_NAME = :tableName 

                                ORDER BY COLUMN_ID";
            cmd.Parameters.Add(new OracleParameter("tableName", tabN));


            await using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess);
            while (await reader.ReadAsync())
            {
                var Name = reader.GetString(0);
                var DataType = ComposeOracleType(
                    reader.GetString(1), //DataType
                    reader.IsDBNull(2) ? null : reader.GetValue(2), //DataLength
                    reader.IsDBNull(3) ? null : reader.GetValue(3), //DataPrecision
                    reader.IsDBNull(4) ? null : reader.GetValue(4)  //DataScale
                );
                var nullable = reader.GetString(5) == "Y"; //Nullable
                td.Columns.Add(new ColumnDef
                {
                    ColumnName = Name,
                    DataType = DataType,
                    Nullable = nullable
                });
            }
            list.Add(td);
        }
        return list;
    }

    private static async Task<string> GetCurrentUser(OracleConnection conn)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT USER FROM DUAL";
        var user = await cmd.ExecuteScalarAsync() as string;
        return user ?? "USER";
    }

    public static string ComposeOracleType(string dataType, object? len, object? prec, object? scale)
    {
        dataType = dataType.ToUpperInvariant();
        if (dataType.Contains("CHAR") && len is not null) return $"{dataType}({len})";
        if (dataType == "NUMBER")
        {
            if (prec is not null)
            {
                if (scale is not null && Convert.ToInt32(scale) > 0)
                    return $"{dataType}({prec},{scale})";
                return $"{dataType}({prec})";
            }
        }
        if (dataType.StartsWith("TIMESTAMP"))
            return dataType;
        return dataType;
    }

    private static string MapOracleToPgType(string oracleTypeUpper)
    {
        //can be optimized!!!
        var t = oracleTypeUpper.ToUpperInvariant();
        if (t.StartsWith("VARCHAR2") || t.StartsWith("NVARCHAR2") || t.StartsWith("CHAR") || t.StartsWith("NCHAR"))
            return "TEXT";
        if (t.StartsWith("NUMBER"))
            return "NUMERIC";
        if (t.StartsWith("FLOAT"))
            return "REAL";
        if (t.StartsWith("DATE") || t.StartsWith("TIMESTAMP"))
            return "TIMESTAMP WITHOUT TIME ZONE";
        if (t.StartsWith("CLOB") || t.StartsWith("NCLOB"))
            return "TEXT";
        if (t.StartsWith("BLOB"))
            return "BYTEA";
        return "TEXT";
    }

    private string BuildPgCreateTableSql(TableDef table)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE SCHEMA IF NOT EXISTS \"{table.Owner.ToLower()}\";");
        sb.Append($"CREATE TABLE IF NOT EXISTS {table.PgQualifiedName} (");
        for (int i = 0; i < table.Columns.Count; i++)
        {
            var col = table.Columns[i];
            var pgType = MapOracleToPgType(col.DataType);
            var nullable = col.Nullable ? "" : " NOT NULL";
            sb.Append($"\"{col.ColumnName.ToLower()}\" {pgType}{nullable}");
            if (i < table.Columns.Count - 1) sb.Append(", ");
        }
        sb.AppendLine(");");
        return sb.ToString();
    }

    private async IAsyncEnumerable<object?[]> StreamTableRowsAsync(string cs, string owner, string table, string[] columns)
    {
        await using var conn = new OracleConnection(cs);
        await conn.OpenAsync();

        var colList = string.Join(",", columns.Select(c => $"\"{c}\""));
        await using var cmd = conn.CreateCommand();
        var sql = $"SELECT {colList} FROM \"{owner}\".\"{table}\"";
        cmd.CommandText = sql;

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
        var fieldCount = reader.FieldCount;
        while (await reader.ReadAsync())
        {
            var rows = new object?[fieldCount];
            for (int i = 0; i < fieldCount; i++)
            {
                rows[i] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
            }
            yield return rows;
        }
    }
}
