using Queryly.Core.Connections;
using Spectre.Console;

public class SchemaCommand
{
    public static async Task ListTablesAsync(string name)
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
            .StartAsync("Fetching schema...", async ctx =>
            {
                var provider = GetProvider(connection.DbType);
                var sqlConn = await provider.OpenConnectionAsync(connection.ConnectionString);
                var tables = await provider.GetTablesAsync(sqlConn, name);

                if (tables.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]! No tables found in the database.[/]");
                    return;
                }

                var table = new Table();
                table.AddColumn("Table Name");
                table.AddColumn(new TableColumn("Rows").RightAligned());
                
                foreach (var tbl in tables)
                {
                    table.AddRow(
                        tbl.Name,
                        tbl.RowCount.ToString("N0")
                    );
                }
                
                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[grey]Total: {tables.Count} table(s)[/]");
            });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
        }
    }

    public static async Task ShowTableInfoAsync(string name, string tableName)
    {
        try {
            var configPath = ConfigurationHelper.GetConnectionsFilePath();
        var manager = new ConnectionManager(configPath);
        var connection = await manager.GetByNameAsync(name);

        if (connection == null)
        {
            AnsiConsole.MarkupLine($"[red]✗ Connection '{name}' not found.[/]");
            return;
        }

        await AnsiConsole.Status()
          .StartAsync("Fetching table info...", async ctx =>
          {
              var provider = GetProvider(connection.DbType);
              var sqlConn = await provider.OpenConnectionAsync(connection.ConnectionString);
              var columns = await provider.GetColumnsAsync(sqlConn, name, tableName);

              if (columns.Count == 0)
              {
                  AnsiConsole.MarkupLine("[yellow]! No columns found in the table.[/]");
                  return;
              }

              AnsiConsole.MarkupLine($"[bold]Table:[/] {tableName}\n");
              AnsiConsole.MarkupLine("[bold]Columns:[/]");

              var table = new Table();
              table.AddColumn("Column Name");
              table.AddColumn("Data Type");
              table.AddColumn(new TableColumn("Is Nullable").Centered());
              table.AddColumn(new TableColumn("Is Primary Key").Centered());
              table.AddColumn(new TableColumn("Default Value").Centered());
              
              foreach (var col in columns)
              {
                  table.AddRow(
                      col.Name,
                      col.DataType,
                      col.IsNullable ? "[green]Yes[/]" : "[red]No[/]",
                      col.IsPrimaryKey ? "[blue]PK[/]" : "",
                      col.DefaultValue ?? ""
                  );
              }
              
              AnsiConsole.Write(table);
              AnsiConsole.MarkupLine($"\n[grey]Total: {columns.Count} column(s)[/]");
          });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
        }
    }

    public static async Task ShowSchemaTreeAsync(string connectionName)
{
    try
    {
        var configPath = ConfigurationHelper.GetConnectionsFilePath();
        var manager = new ConnectionManager(configPath);
        var connection = await manager.GetByNameAsync(connectionName);

        if (connection == null)
        {
            AnsiConsole.MarkupLine($"[red]✗ Connection '{connectionName}' not found.[/]");
            return;
        }

        await AnsiConsole.Status()
            .StartAsync("Loading schema...", async ctx =>
            {
                var provider = GetProvider(connection.DbType);
                using var sqlConn = await provider.OpenConnectionAsync(connection.ConnectionString);
                
                var databases = provider.GetDatabasesAsync(sqlConn);
                var database = databases.FirstOrDefault() ?? "main";
                var tables = await provider.GetTablesAsync(sqlConn, database);

                if (tables.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No tables found.[/]");
                    return;
                }

                var tree = new Tree($"[bold blue]{connectionName}[/] ([grey]{connection.DbType}[/])");

                foreach (var table in tables)
                {
                    var tableNode = tree.AddNode($"[bold]{table.Name}[/] [grey]({table.RowCount:N0} rows)[/]");
                    
                    var columns = await provider.GetColumnsAsync(sqlConn, database, table.Name);
                    
                    foreach (var col in columns)
                    {
                        var pkMarker = col.IsPrimaryKey ? " [blue](PK)[/]" : "";
                        var nullMarker = col.IsNullable ? " [grey](nullable)[/]" : "";
                        tableNode.AddNode($"{col.Name} [yellow]{col.DataType}[/]{pkMarker}{nullMarker}");
                    }
                }

                // 7. Display the tree
                AnsiConsole.Write(tree);
            });
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
            _ => throw new NotSupportedException($"Database type {type} is not supported yet.")
        };
    }
}