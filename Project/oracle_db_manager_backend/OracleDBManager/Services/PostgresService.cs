using Npgsql;

namespace OracleDBManager.Services;

public class PostgresService
{
    public NpgsqlConnection NewConn(string cs) => new NpgsqlConnection(cs);

    public async Task ExecSync(string cs, string sql)
    {
        await using var conn = NewConn(cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task BulkCopyAsync(string cs, string table, string[] cols, IAsyncEnumerable<object?[]> rows)
    {
        await using var conn = NewConn(cs);
        await conn.OpenAsync();
        var colList = string.Join(",", cols.Select(c => $"\"{c.ToLower()}\""));
        await using var writer = await conn.BeginBinaryImportAsync(
            $"COPY {table} ({colList}) FROM STDIN BINARY");

        await foreach (var r in rows)
        {
            await writer.StartRowAsync();
            foreach (var v in r) await writer.WriteAsync(v);
        }
        await writer.CompleteAsync();
    }
}
