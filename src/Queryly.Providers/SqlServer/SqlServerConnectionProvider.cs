using System.Data.Common;
using Microsoft.Data.SqlClient;

public class SqlServerConnectionProvider : IConnectionProvider
{
    public DatabaseType Type => DatabaseType.SQLServer;

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

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public List<string> GetDatabasesAsync(DbConnection connection)
    {
        if (connection is not SqlConnection)
            throw new ArgumentException("Connection must be a PostgreSQL connection.", nameof(connection));

        var sqlConn = (SqlConnection)connection;
        var builder = new SqlConnectionStringBuilder(sqlConn.ConnectionString);

        return new List<string> { builder.InitialCatalog ?? "SQL server" };
    }

    public async Task<List<TableInfo>> GetTablesAsync(DbConnection connection, string database)
    {
        if (connection is not SqlConnection)
            throw new ArgumentException("Connection must be a SQL Server connection.", nameof(connection));

        try
        {
            var tables = new List<TableInfo>();
            var sqlConn = (SqlConnection)connection;

            var command = @"SELECT TABLE_SCHEMA, TABLE_NAME 
                          FROM INFORMATION_SCHEMA.TABLES 
                          WHERE TABLE_TYPE = 'BASE TABLE'";

            using (var cmd = new SqlCommand(command, sqlConn))
            {
                cmd.Parameters.AddWithValue("@DatabaseName", database);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var schema = reader.GetString(0);
                    var tableName = reader.GetString(1);
                    tables.Add(new TableInfo { Schema = schema, Name = tableName });
                }
            }
            return tables;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve tables: {ex.Message}", ex);
        }
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(DbConnection connection, string database, string table)
    {
        if (connection is not SqlConnection)
            throw new ArgumentException("Connection must be a SQLServer connection.", nameof(connection));

        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentNullException(nameof(table), "Table name cannot be null.");

        try
        {
            var columns = new List<ColumnInfo>();
            var sqlConn = (SqlConnection)connection;

            var command = @"SELECT COLUMN_NAME, DATA_TYPE 
                            FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_NAME = @TableName";

            using (var cmd = new SqlCommand(command, sqlConn))
            {
                cmd.Parameters.AddWithValue("@DatabaseName", database);
                cmd.Parameters.AddWithValue("@TableName", table);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        columns.Add(new ColumnInfo { Name = columnName, DataType = dataType });
                    }
                }
            }
            return columns;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve columns for table '{table}': {ex.Message}", ex);
        }
    }
}