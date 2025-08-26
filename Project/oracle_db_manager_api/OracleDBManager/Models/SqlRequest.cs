namespace OracleDBManager.Models;

public record SqlRequest(string ConnectionString, string Sql, int? MaxRows = 500);
