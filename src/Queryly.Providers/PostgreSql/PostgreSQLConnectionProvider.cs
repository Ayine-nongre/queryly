using System.Data.Common;
using Npgsql;

namespace Queryly.Providers.PostgreSql;

public class PostgreSQLConnectionProvider : IConnectionProvider
{
    public DatabaseType Type => DatabaseType.PostgreSQL;

    public async Task<DbConnection> OpenConnectionAsync(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        try
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (NpgsqlException ex)
        {
            throw new Exception($"Failed to open PostgreSQL connection: {ex.Message}", ex);
        }
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
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
        if (connection is not NpgsqlConnection)
            throw new ArgumentException("Connection must be a PostgreSQL connection.", nameof(connection));
        
        var sqlConn = (NpgsqlConnection)connection;
        var builder = new NpgsqlConnectionStringBuilder(sqlConn.ConnectionString);
        
        return new List<string> { builder.Database ?? "postgres" };
    }

    public async Task<List<TableInfo>> GetTablesAsync(DbConnection connection, string database)
    {
        if (connection is not NpgsqlConnection)
            throw new ArgumentException("Connection must be a PostgreSQL connection.", nameof(connection));

        try
        {
            var tables = new List<TableInfo>();
            var sqlConn = (NpgsqlConnection)connection;
            
            var command = sqlConn.CreateCommand();
            command.CommandText = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_type = 'BASE TABLE'
                ORDER BY table_name;";
            
            var tableNames = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    tableNames.Add(reader.GetString(0));
                }
            }

            foreach (var tableName in tableNames)
            {
                var countCommand = sqlConn.CreateCommand();
                countCommand.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\";";
                var rowCountObj = await countCommand.ExecuteScalarAsync() ?? 0L;
                var rowCount = Convert.ToInt64(rowCountObj);

                tables.Add(new TableInfo 
                { 
                    Name = tableName, 
                    RowCount = rowCount 
                });
            }
            
            return tables;
        }
        catch (NpgsqlException ex)
        {
            throw new Exception($"Failed to retrieve tables: {ex.Message}", ex);
        }
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(DbConnection connection, string database, string table)
    {
        if (connection is not NpgsqlConnection) 
            throw new ArgumentException("Connection must be a PostgreSQL connection.", nameof(connection));
        
        if (string.IsNullOrWhiteSpace(table)) 
            throw new ArgumentNullException(nameof(table), "Table name cannot be null.");
        
        try
        {
            var columns = new List<ColumnInfo>();
            var sqlConn = (NpgsqlConnection)connection;
            
            var command = sqlConn.CreateCommand();
            command.CommandText = @"
                SELECT 
                    c.column_name,
                    c.data_type,
                    c.is_nullable,
                    c.column_default,
                    CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary_key
                FROM information_schema.columns c
                LEFT JOIN (
                    SELECT ku.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage ku
                        ON tc.constraint_name = ku.constraint_name
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                        AND tc.table_name = @tableName
                        AND tc.table_schema = 'public'
                ) pk ON c.column_name = pk.column_name
                WHERE c.table_name = @tableName
                    AND c.table_schema = 'public'
                ORDER BY c.ordinal_position;";
            
            command.Parameters.AddWithValue("@tableName", table);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var columnName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var isNullable = reader.GetString(2) == "YES";
                var defaultValue = reader.IsDBNull(3) ? null : reader.GetString(3);
                var isPrimaryKey = reader.GetBoolean(4);
                
                columns.Add(new ColumnInfo 
                { 
                    Name = columnName, 
                    DataType = dataType,
                    IsNullable = isNullable,
                    DefaultValue = defaultValue,
                    IsPrimaryKey = isPrimaryKey
                });
            }
            
            return columns;
        }
        catch (NpgsqlException ex)
        {
            throw new Exception($"Failed to retrieve columns for table '{table}': {ex.Message}", ex);
        }
    }
}