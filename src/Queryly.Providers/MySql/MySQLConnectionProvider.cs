using System.Data.Common;
using MySqlConnector;

public class MySQLConnectionProvider : IConnectionProvider
{
    public DatabaseType Type => DatabaseType.MySQL;

    public async Task<DbConnection> OpenConnectionAsync(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        try
        {
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (MySqlException ex)
        {
            throw new Exception($"Failed to open MySQL connection: {ex.Message}", ex);
        }
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
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
        if (connection is not MySqlConnection)
            throw new ArgumentException("Connection must be a MySQL connection.", nameof(connection));

        var sqlConn = (MySqlConnection)connection;
        var builder = new MySqlConnectionStringBuilder(sqlConn.ConnectionString);

        return new List<string> { builder.Database ?? "mysql" };
    }

    public async Task<List<TableInfo>> GetTablesAsync(DbConnection connection, string database)
    {
        if (connection is not MySqlConnection)
            throw new ArgumentException("Connection must be a MySQL connection.", nameof(connection));

        try
        {
            var tables = new List<TableInfo>();
            var sqlConn = (MySqlConnection)connection;

            var command = sqlConn.CreateCommand();
            command.CommandText = $@"SELECT table_name, table_type FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema = DATABASE();";

            command.Parameters.AddWithValue("@database", database);
            var tableNames = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    tableNames.Add(tableName);
                }
            }

            foreach (var table in tableNames)
            {
                var countCommand = sqlConn.CreateCommand();
                countCommand.CommandText = $"SELECT COUNT(*) FROM `{table}`;";
                var rowCount = (long)(await countCommand.ExecuteScalarAsync() ?? 0L);
                tables.Add(new TableInfo
                {
                    Name = table,
                    RowCount = rowCount
                });
            }

            return tables;
        }
        catch (MySqlException ex)
        {
            throw new Exception($"Failed to retrieve tables: {ex.Message}", ex);
        }
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(DbConnection connection, string database, string table)
    {
        if (connection is not MySqlConnection)
            throw new ArgumentException("Connection must be a MySQL connection.", nameof(connection));

        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentNullException(nameof(table), "Table name cannot be null.");

        try
        {
            var columns = new List<ColumnInfo>();
            var sqlConn = (MySqlConnection)connection;

            var command = sqlConn.CreateCommand();
            command.CommandText = @"SELECT
                        c.column_name,
                        c.data_type,
                        c.is_nullable,
                        c.column_default,
                        CASE
                            WHEN k.column_name IS NOT NULL THEN 'YES'
                            ELSE 'NO'
                        END AS is_primary_key
                    FROM information_schema.columns c
                    LEFT JOIN information_schema.key_column_usage k
                        ON c.table_schema = k.table_schema
                    AND c.table_name   = k.table_name
                    AND c.column_name  = k.column_name
                    AND k.constraint_name = 'PRIMARY'
                    WHERE c.table_schema = DATABASE()
                    AND c.table_name = @TableName
                    ORDER BY c.ordinal_position;
                    ";

            command.Parameters.AddWithValue("@TableName", table);
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var columnName = reader.GetString(0);
                    var dataType = reader.GetString(1);
                    var isNullable = reader.GetString(2);
                    var columnDefault = reader.IsDBNull(3) ? null : reader.GetString(3);
                    var isPrimaryKey = reader.GetString(4) == "YES";
                    columns.Add(new ColumnInfo
                    {
                        Name = columnName,
                        DataType = dataType,
                        IsNullable = isNullable == "YES",
                        DefaultValue = columnDefault,
                        IsPrimaryKey = isPrimaryKey
                    });
                }
            }
            return columns;
        }
        catch (MySqlException ex)
        {
            throw new Exception($"Failed to retrieve columns for table '{table}': {ex.Message}", ex);
        }
    }
}