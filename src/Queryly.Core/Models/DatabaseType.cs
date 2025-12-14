public enum DatabaseType
{
    SQLite,
    SQLServer,
    PostgreSQL,
    MySQL
}

public static class DatabaseTypeExtensions
{
    public static string TypeToString(this DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.SQLite => "SQLite",
            DatabaseType.SQLServer => "SQLServer",
            DatabaseType.MySQL => "MySQL",
            DatabaseType.PostgreSQL => "PostgreSQL",
            _ => "Unknown"
        };
    }
}