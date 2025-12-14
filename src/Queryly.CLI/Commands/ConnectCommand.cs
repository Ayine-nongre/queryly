using Spectre.Console;
using Queryly.Core.Connections;
using Queryly.Providers.PostgreSql;

public static class ConnectCommand
{
    public static async Task ListConnectionsAsync()
    {
        try
        {
            var configPath = ConfigurationHelper.GetConnectionsFilePath();
            var manager = new ConnectionManager(configPath);
            var connections = await manager.GetAllAsync();
            
            if (connections.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No connections found.[/]");
                AnsiConsole.MarkupLine("Use [bold]queryly connect add[/] to add a connection.");
                return;
            }
            
            var table = new Table();
            table.AddColumn("Name");
            table.AddColumn("Type");
            table.AddColumn("Last Used");
            table.AddColumn("Favorite");
            
            foreach (var conn in connections)
            {
                var favorite = conn.IsFavorite ? "⭐" : "";
                table.AddRow(
                    conn.Name,
                    conn.DbType.ToString(),
                    conn.LastUsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    favorite
                );
            }
            
            AnsiConsole.Write(table);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
        }
    }
    
    public static async Task AddConnectionAsync()
    {
        try
        {
            var name = AnsiConsole.Ask<string>("Connection [green]name[/]:");
            
            var type = AnsiConsole.Prompt(
                new SelectionPrompt<DatabaseType>()
                    .Title("Select [green]database type[/]:")
                    .AddChoices(DatabaseType.SQLite)
                    .AddChoices(DatabaseType.PostgreSQL));
            
            var connString = AnsiConsole.Ask<string>("Connection [green]string[/]:");
            
            var connection = new ConnectionInfo
            {
                Name = name,
                DbType = type,
                ConnectionString = connString
            };
            
            var testPassed = false;
            await AnsiConsole.Status()
                .StartAsync("Testing connection...", async ctx =>
                {
                    var provider = GetProvider(type);
                    testPassed = await provider.TestConnectionAsync(connString);
                    
                    if (testPassed)
                    {
                        AnsiConsole.MarkupLine("[green]✓ Connection successful![/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]✗ Connection test failed![/]");
                        AnsiConsole.MarkupLine("Please check your connection string.");
                    }
                });
            
            if (!testPassed)
                return;
            
            var configPath = ConfigurationHelper.GetConnectionsFilePath();
            var manager = new ConnectionManager(configPath);
            await manager.SaveAsync(connection);
            
            AnsiConsole.MarkupLine($"[green]✓[/] Saved connection '[bold]{name}[/]'");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
        }
    }
    
    public static async Task TestConnectionAsync(string name)
    {
        try
        {
            var configPath = ConfigurationHelper.GetConnectionsFilePath();
            var manager = new ConnectionManager(configPath);
            var connection = await manager.GetByNameAsync(name);

            if (connection == null)
            {
                AnsiConsole.MarkupLine($"[red]✗ Connection '{name}' not found.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Testing connection...", async ctx =>
                {
                    var provider = GetProvider(connection.DbType);
                    var result = await provider.TestConnectionAsync(connection.ConnectionString);

                    if (result)
                    {
                        AnsiConsole.MarkupLine("[green]✓ Connection successful![/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]✗ Connection test failed![/]");
                    }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
        }
    }

    public static async Task RemoveConnectionAsync(string name)
    {
        try
        {
            var configPath = ConfigurationHelper.GetConnectionsFilePath();
            var manager = new ConnectionManager(configPath);
            var connection = await manager.GetByNameAsync(name);

            if (connection == null)
            {
                AnsiConsole.MarkupLine($"[red]✗ Connection '{name}' not found.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"Connection: [bold]{connection.Name}[/]");
            AnsiConsole.MarkupLine($"Type: {connection.DbType}");
            AnsiConsole.MarkupLine($"Last used: {connection.LastUsed.ToLocalTime():yyyy-MM-dd HH:mm}");
            AnsiConsole.WriteLine();

            var confirm = AnsiConsole.Confirm($"Remove '[yellow]{name}[/]'?");
            
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
                return;
            }

            await manager.DeleteAsync(connection.Id);
            AnsiConsole.MarkupLine($"[green]✓[/] Removed connection '[bold]{name}[/]'");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
        }
    }

    private static IConnectionProvider GetProvider(DatabaseType type)
    {
        return type switch
        {
            DatabaseType.SQLite => new SqliteConnectionProvider(),
            DatabaseType.PostgreSQL => new PostgreSQLConnectionProvider(),
            _ => throw new NotSupportedException($"Database type {type} is not supported yet.")
        };
    }
}