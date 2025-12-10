using Spectre.Console;

AnsiConsole.Write(
    new FigletText("Queryly")
        .Centered()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[grey]Your local database companion[/]");

if (args.Length == 0)
{
    ShowHelp();
    return;
}

var command = args[0].ToLower();

if (command == "connect" && args.Length > 1)
{
    var subCommand = args[1].ToLower();

    if (subCommand == "list") { await ConnectCommand.ListConnectionsAsync(); return; }
    else if (subCommand == "add") { await ConnectCommand.AddConnectionAsync(); return;}
    else if (subCommand == "test" && args.Length > 2) { var name = args[2]; await ConnectCommand.TestConnectionAsync(name); return; }
    else if (subCommand == "remove" && args.Length > 2) { var name = args[2]; await ConnectCommand.RemoveConnectionAsync(name); return; }
}
else if (command == "schema" && args.Length > 2)
{
    var subCommand = args[1].ToLower();
    var connectionName = args[2];

    if (subCommand == "list") { await SchemaCommand.ListTablesAsync(connectionName); return; }
    else if (subCommand == "info" && args.Length > 3) { var tableName = args[3]; await SchemaCommand.ShowTableInfoAsync(connectionName, tableName); return; }
    else if (subCommand == "tree") { await SchemaCommand.ShowSchemaTreeAsync(connectionName); return; }
}

static void ShowHelp()
{
    AnsiConsole.MarkupLine("[bold]Queryly Commands:[/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold blue]Connection Management:[/]");
    AnsiConsole.MarkupLine("  connect list    - List all connections");
    AnsiConsole.MarkupLine("  connect add     - Add a new connection");
    AnsiConsole.MarkupLine("  connect test <name> - Test a connection by name");
    AnsiConsole.MarkupLine("  connect remove <name> - Remove a connection by name");

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold blue]Schema Exploration:[/]");
    AnsiConsole.MarkupLine("  schema list <connection-name> - List all tables in the specified connection");
}