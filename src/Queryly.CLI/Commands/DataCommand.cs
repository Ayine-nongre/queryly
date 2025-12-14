using System.Data;
using Spectre.Console;
using Queryly.Core.Connections;
using Queryly.Core.Query;
using Azure;
using Queryly.Providers.PostgreSql;

namespace Queryly.CLI.Commands;

public static class DataCommand
{
    public static async Task BrowseTableAsync(string connectionName, string tableName)
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

            var provider = GetProvider(connection.DbType);
            using var dbConnection = await provider.OpenConnectionAsync(connection.ConnectionString);
            var executor = new QueryExecutor(dbConnection);

            // Get total row count
            var count = await executor.ExecuteScalarAsync($"SELECT COUNT(*) FROM [{tableName}]");
            var totalRows = Convert.ToInt32(count);
            var pageSize = 50;
            var totalPages = (int)Math.Ceiling(totalRows / (double)pageSize);

            var pageInfo = new PaginationInfo(1, pageSize)
            {
                TotalRows = totalRows,
                TotalPages = totalPages
            };

            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine($"[bold blue]Browsing Table:[/] {tableName} ([grey]{totalRows:N0} rows, {totalPages} pages[/])\n");

                var sql = $"SELECT * FROM \"{tableName}\" LIMIT {pageSize} OFFSET {pageInfo.GetOffset()}";
                var result = await WithLoadingAsync("Loading page data...", () => executor.ExecuteQueryAsync(sql));

                if (!result.IsSuccess)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Query failed: {Markup.Escape(result.ErrorMessage!)}[/]");
                    return;
                }

                DisplayDataTable(result.Data, tableName, result.ExecutionTime);
                ShowPaginationFooter(pageInfo);

                var command = AnsiConsole.Ask<string>("[blue]Command>[/]").Trim().ToLower();

                if (command == "n")
                {
                    if (!pageInfo.HasNextPage)
                    {
                        AnsiConsole.MarkupLine("[yellow]! Already on last page.[/]");
                        await Task.Delay(1000);
                        continue;
                    }
                    pageInfo.NextPage();
                }
                else if (command == "p")
                {
                    if (!pageInfo.HasPreviousPage)
                    {
                        AnsiConsole.MarkupLine("[yellow]! Already on first page.[/]");
                        await Task.Delay(1000);
                        continue;
                    }
                    pageInfo.PreviousPage();
                }
                else if (command == "g" || command == "go")
                {
                    var pageNumber = AnsiConsole.Ask<int>($"Enter page number (1-{totalPages}):");

                    if (pageNumber < 1 || pageNumber > totalPages)
                    {
                        AnsiConsole.MarkupLine($"[red]✗ Invalid page number.[/]");
                        await Task.Delay(1500);
                        continue;
                    }

                    pageInfo.GoToPage(pageNumber);
                }
                else if (command == "h" || command == "help")
                {
                    ShowHelp();
                    AnsiConsole.Ask<string>("\n[grey]Press Enter to continue...[/]");
                    continue;
                }
                else if (command == "q")
                {
                    break;
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]✗ Unknown command. Type 'h' for help.[/]");
                    await Task.Delay(1500);
                }
            }

            AnsiConsole.MarkupLine("[grey]Exited browse mode.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
        }
    }

    public static async Task ExecuteQueryAsync(string connectionName)
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

            var provider = GetProvider(connection.DbType);
            using var dbConnection = await provider.OpenConnectionAsync(connection.ConnectionString);
            var executor = new QueryExecutor(dbConnection);

            AnsiConsole.MarkupLine($"[bold blue]Query Mode[/] - {connectionName}");
            AnsiConsole.MarkupLine("[grey]Enter SQL queries. Type 'exit' to quit.[/]\n");

            while (true)
            {
                var sql = AnsiConsole.Ask<string>("[blue]SQL>[/]");

                if (sql.Trim().ToLower() == "exit")
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(sql))
                {
                    continue;
                }

                var result = await executor.ExecuteQueryAsync(sql);

                if (!result.IsSuccess)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Error: {result.ErrorMessage}[/]");
                    continue;
                }

                DisplayDataTable(result.Data, "Results", result.ExecutionTime);
                AnsiConsole.WriteLine();
            }

            AnsiConsole.MarkupLine("[grey]Exited query mode.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
        }
    }

    public static async Task ExportTableAsync(string connectionName, string tableName, string format)
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
                .StartAsync($"Exporting {tableName}...", async ctx =>
                {
                    var provider = GetProvider(connection.DbType);
                    using var dbConnection = await provider.OpenConnectionAsync(connection.ConnectionString);
                    var executor = new QueryExecutor(dbConnection);

                    var sql = $"SELECT * FROM [{tableName}]";
                    var result = await executor.ExecuteQueryAsync(sql);

                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]✗ Query failed: {result.ErrorMessage}[/]");
                        return;
                    }

                    var fileName = $"{tableName}_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";

                    if (format.ToLower() == "csv")
                    {
                        await ExportToCsvAsync(result.Data, fileName);
                    }
                    else if (format.ToLower() == "json")
                    {
                        await ExportToJsonAsync(result.Data, fileName);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗ Unsupported format: {format}[/]");
                        return;
                    }

                    AnsiConsole.MarkupLine($"[green]✓[/] Exported to [bold]{fileName}[/] ({result.Data?.Rows.Count ?? 0} rows)");
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
        }
    }

    private static void DisplayDataTable(DataTable? data, string title, TimeSpan executionTime)
    {
        if (data == null || data.Rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No data found.[/]");
            return;
        }

        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.Title = new TableTitle($"[bold]{title}[/]");

        foreach (DataColumn column in data.Columns)
        {
            table.AddColumn(column.ColumnName);
        }

        var rowCount = Math.Min(data.Rows.Count, 50);
        for (int i = 0; i < rowCount; i++)
        {
            var row = data.Rows[i];
            var values = new string[data.Columns.Count];

            for (int j = 0; j < data.Columns.Count; j++)
            {
                var value = row[j];
                values[j] = value == DBNull.Value ? "[grey]NULL[/]" : value.ToString() ?? "";

                if (values[j].Length > 50)
                {
                    values[j] = values[j].Substring(0, 47) + "...";
                }
            }

            table.AddRow(values);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[grey]Showing {rowCount} of {data.Rows.Count} row(s) | Query time: {executionTime.TotalMilliseconds:F2}ms[/]");
    }

    private static async Task ExportToCsvAsync(DataTable? data, string fileName)
    {
        if (data == null) return;

        using var writer = new StreamWriter(fileName);

        var headers = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        await writer.WriteLineAsync(headers);

        foreach (DataRow row in data.Rows)
        {
            var values = row.ItemArray.Select(v =>
            {
                var str = v?.ToString() ?? "";
                if (str.Contains(",") || str.Contains("\"") || str.Contains("\n"))
                {
                    str = "\"" + str.Replace("\"", "\"\"") + "\"";
                }
                return str;
            });
            await writer.WriteLineAsync(string.Join(",", values));
        }
    }

    private static async Task ExportToJsonAsync(DataTable? data, string fileName)
    {
        if (data == null) return;

        var rows = new List<Dictionary<string, object?>>();

        foreach (DataRow row in data.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (DataColumn col in data.Columns)
            {
                dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }
            rows.Add(dict);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(rows, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(fileName, json);
    }

    public static void ShowPaginationFooter(PaginationInfo pageInfo)
    {
        int startRow = ((pageInfo.PageNumber - 1) * pageInfo.PageSize) + 1;
        int endRow = Math.Min(pageInfo.PageNumber * pageInfo.PageSize, pageInfo.TotalRows);

        // Header line
        AnsiConsole.MarkupLine("[grey]──────────────────────────────────────────────────────────────[/]");

        // Page and row summary
        AnsiConsole.MarkupLine(
            $"[bold] Page {pageInfo.PageNumber} of {pageInfo.TotalPages}[/]    " +
            $"[grey]Rows {startRow}–{endRow} of {pageInfo.TotalRows}[/]");

        // Footer line
        AnsiConsole.MarkupLine("[grey]──────────────────────────────────────────────────────────────[/]");

        // Commands
        AnsiConsole.MarkupLine(
            "Commands:  " +
            "[blue][[N]][/]ext  •  " +
            "[blue][[P]][/]rev  •  " +
            "[blue][[G]][/]o to page  •  " +
            "[red][[Q]][/]uit");

        // Another separator
        AnsiConsole.MarkupLine("[grey]──────────────────────────────────────────────────────────────[/]\n");
    }

    public static async Task<T> WithLoadingAsync<T>(string message, Func<Task<T>> action)
    {
        T result = default!;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow"))
            .StartAsync(message, async ctx =>
            {
                result = await action();
            });

        return result;
    }

    private static void ShowHelp()
    {
        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.Title = new TableTitle("[bold]Navigation Commands[/]");

        table.AddColumn("Command");
        table.AddColumn("Description");

        table.AddRow("[blue]n[/]", "Go to next page");
        table.AddRow("[blue]p[/]", "Go to previous page");
        table.AddRow("[blue]g[/] or [blue]go[/]", "Go to specific page number");
        table.AddRow("[blue]h[/] or [blue]help[/]", "Show this help");
        table.AddRow("[blue]q[/]", "Quit and return to terminal");

        AnsiConsole.Write(table);
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