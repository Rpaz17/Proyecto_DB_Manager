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

            //Analyze tables

            //Validate row counts

        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
        }
        return result;
    }

    private async Task<List<TableDef>> GetOracleTablesAsync(string cs, string owner)
    {
        var list = new List<TableDef>();
        await using var conn = new OracleConnection(cs);
        await conn.OpenAsync();

        string schema = owner ?? (await GetCurrentUser(conn));

        //Tables
        var tables = new List<(string Owner, string TableName)>();
        await using (var cmd = new OracleCommand())
        {
            cmd.CommandText = @"SELECT OWNER, TABLE_NAME 
                                FROM ALL_TABLES 
                                WHERE OWNER = :owner 
                                ORDER BY TABLE_NAME";
            cmd.Parameters.Add(new OracleParameter("owner", schema.ToUpperInvariant()));
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        //Columns
        foreach (var (own, tabN) in tables)
        {
            var td = new TableDef { Owner = own, TableName = tabN, Columns = new List<ColumnDef>() };
            await using var cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandText = @"SELECT COLUMN_NAME, DATA_TYPE, DATA_LENGTH 
                                FROM ALL_TAB_COLUMNS 
                                WHERE OWNER = :owner AND TABLE_NAME = :tableName 
                                ORDER BY COLUMN_ID";
            cmd.Parameters.Add(new OracleParameter("owner", own.ToUpperInvariant()));
            cmd.Parameters.Add(new OracleParameter("tableName", tabN.ToUpperInvariant()));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var Name = reader.GetString(0);
                var DataType = ComposeOracleType(
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetValue(2),
                    reader.IsDBNull(3) ? null : reader.GetValue(3),
                    reader.IsDBNull(4) ? null : reader.GetValue(4)
                );
                var nullable = reader.GetString(5) == "Y";
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
}
