namespace OracleDBManger.Models;

public class SqlRequest(string ConnectionString, string Sql, int? MaxRows = 500);
