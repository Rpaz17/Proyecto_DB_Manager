using Npgsql;
using Oracle.ManagedDataAccess.Types;
using System.Globalization;

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
            foreach (var v in r)
            {
                var convertedValue = ConvertOrToPoValue(v);
                await writer.WriteAsync(convertedValue);
            }
        }
        await writer.CompleteAsync();
    }

    private static object? ConvertOrToPoValue(object? value)
    {
        if (value == null || value == DBNull.Value) return null;

        if (value is OracleDecimal od)
        {
            if (od.IsNull) return null;

            try { return od.Value; }
            catch { return ParseNumericString(od.ToString()); }
        }

        if (value is OracleDate odt)
        {
            if (odt.IsNull) return null;
            return odt.Value;
        }

        if (value is OracleTimeStamp ots)
        {
            if (ots.IsNull) return null;
            return ots.Value;
        }

        if (value is OracleString os)
        {
            if (os.IsNull) return null;
            return os.Value;
        }

        if (value is OracleClob oracleClob)
        {
            if (oracleClob.IsNull) return null;
            return oracleClob.Value;
        }

        if (value is OracleBlob oracleBlob)
        {
            if (oracleBlob.IsNull) return null;
            return oracleBlob.Value;
        }

        if (value is decimal || value is double || value is float)
            return ParseNumericString(value.ToString());
        return value;
    }

    private static object? ParseNumericString(string? numStr)
    {
        if (string.IsNullOrWhiteSpace(numStr)) return null;
        numStr = numStr.Trim();

        if (numStr.Equals("Infinity", StringComparison.OrdinalIgnoreCase) ||
           numStr.Equals("+Infinity", StringComparison.OrdinalIgnoreCase))
            return decimal.MaxValue;
        if (numStr.Equals("-Infinity", StringComparison.OrdinalIgnoreCase))
            return decimal.MinValue;
        if (numStr.Equals("NaN", StringComparison.OrdinalIgnoreCase))
            return null;
        if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
            return dec;
        return null;
    }
}
