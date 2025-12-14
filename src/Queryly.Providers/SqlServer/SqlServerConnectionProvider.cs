using System.Data.Common;
using Microsoft.Data.SqlClient;

public class SqlServerConnectionProvider
{
    public DatabaseType Type = DatabaseType.SQLServer;

    public async Task<DbConnection> OpenConnectionAsync(string connectionString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
            }

            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to open SQLServer connection.", ex);
        }
    }
}