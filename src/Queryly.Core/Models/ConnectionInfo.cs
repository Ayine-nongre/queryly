public class ConnectionInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string Name { get; set; }
    public DatabaseType DbType { get; set; }
    public required string ConnectionString { get; set; }
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    public bool IsFavorite { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    
    public void UpdateLastUsed()
    {
        LastUsed = DateTime.UtcNow;
    }
}