# Queryly

> Your lightweight, cross-platform database companion

Queryly is a fast, developer-friendly CLI tool for managing local databases. Built in pure C# for .NET 8+, it provides an intuitive terminal interface for common database operations without the overhead of heavyweight GUI tools.

## 🚀 Features

- **Multiple Databases** - SQLite, PostgreSQL, MySQL, and SQL Server support
- **Connection Management** - Save, test, and organize database connections
- **Schema Exploration** - Browse tables, view column structures, and explore indexes
- **Data Browsing** - View and navigate table data with a beautiful terminal UI
- **Query Execution** - Run custom SQL queries with an interactive query mode
- **Data Export** - Export tables to CSV or JSON formats
- **Cross-Platform** - Works on Windows, macOS, and Linux
- **Lightweight** - Fast startup, minimal dependencies

## 📋 Requirements

- .NET 8.0 SDK or later
- Supported databases:
  - ✅ SQLite (fully supported)
  - ✅ SQL Server (fully supported)
  - ✅ PostgreSQL (fully supported)
  - ✅ MySQL (fully supported)

## 🛠️ Installation

### Build from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/queryly.git
cd queryly

# Build the project
dotnet build

# Run Queryly
dotnet run --project src/Queryly.CLI/Queryly.CLI.csproj
```

### Install as Global Tool (Coming Soon)

```bash
dotnet tool install -g queryly
```

## 📖 Quick Start

### 1. Add Your First Connection

```bash
queryly connect add
```

You'll be prompted to enter:
- Connection name (e.g., "LocalDB")
- Database type (SQLite, SQL Server, etc.)
- Connection string (e.g., `mydb.db`)

The connection will be tested before saving.

### 2. List Your Connections

```bash
queryly connect list
```

Shows all saved connections with their type and last used date.

### 3. Browse Database Schema

```bash
# List all tables
queryly schema list LocalDB

# View table structure
queryly schema info LocalDB users

# View schema as tree
queryly schema tree LocalDB
```

### 4. Work with Data

```bash
# Browse table data (first 50 rows)
queryly data browse LocalDB users

# Interactive query mode
queryly data query LocalDB

# Export table to CSV
queryly data export LocalDB users csv

# Export table to JSON
queryly data export LocalDB users json
```

## 📚 Command Reference

### Connection Management

| Command | Description |
|---------|-------------|
| `queryly connect add` | Add a new database connection interactively |
| `queryly connect list` | Display all saved connections |
| `queryly connect test <name>` | Test a connection by name |
| `queryly connect remove <name>` | Remove a saved connection |

**Examples:**

```bash
# Add a SQLite connection
queryly connect add
# Enter: name=MyDB, type=SQLite, connString=Data Source=./app.db

# Test the connection
queryly connect test MyDB

# Remove a connection
queryly connect remove MyDB
```

### Schema Exploration

| Command | Description |
|---------|-------------|
| `queryly schema list <connection>` | List all tables in the database |
| `queryly schema info <connection> <table>` | Show detailed table structure |
| `queryly schema tree <connection>` | Display schema as a visual tree |

**Examples:**

```bash
# List all tables
queryly schema list MyDB

# Show structure of 'users' table
queryly schema info MyDB users

# View entire schema as tree
queryly schema tree MyDB
```

### Data Operations

| Command | Description |
|---------|-------------|
| `queryly data browse <connection> <table>` | View table data (50 rows) |
| `queryly data query <connection>` | Interactive SQL query mode |
| `queryly data export <connection> <table> <format>` | Export table data |

**Examples:**

```bash
# Browse users table
queryly data browse MyDB users

# Enter query mode
queryly data query MyDB
# Then type SQL queries like:
# SELECT * FROM users WHERE age > 25
# Type 'exit' to quit

# Export to CSV
queryly data export MyDB users csv

# Export to JSON
queryly data export MyDB users json
```

## 🏗️ Architecture

Queryly is built with a clean, layered architecture:

```
┌─────────────────────────────────────┐
│         CLI Layer                   │
│    (Commands, UI, Program.cs)       │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│         Core Layer                  │
│  (Models, Interfaces, Business      │
│   Logic, Connection Management)     │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│       Providers Layer               │
│  (Database-specific implementations)│
│   SQLite, SQL Server, PostgreSQL    │
└─────────────────────────────────────┘
```

### Project Structure

```
Queryly/
├── src/
│   ├── Queryly.Core/           # Business logic
│   │   ├── Models/             # Data models
│   │   ├── Connections/        # Connection management
│   │   ├── Schema/             # Schema operations
│   │   ├── Query/              # Query execution
│   │   └── Exceptions/         # Custom exceptions
│   ├── Queryly.Providers/      # Database providers
│   │   ├── Sqlite/             # SQLite implementation
│   │   ├── SqlServer/          # SQL Server (future)
│   │   ├── PostgreSql/         # PostgreSQL (future)
│   │   └── MySql/              # MySQL (future)
│   └── Queryly.CLI/            # CLI application
│       ├── Commands/           # Command implementations
│       ├── Configuration/      # Config helpers
│       └── Program.cs          # Entry point
└── tests/
    └── Queryly.Tests/          # Unit tests
```

## 🔧 Configuration

Queryly stores its configuration in your home directory:

**Location:**
- Windows: `C:\Users\<username>\.queryly\`
- macOS/Linux: `~/.queryly/`

**Files:**
- `connections.json` - Saved database connections

**Example connections.json:**

```json
{
  "connections": [
    {
      "id": "abc123...",
      "name": "LocalDB",
      "dbType": "SQLite",
      "connectionString": "Data Source=./myapp.db",
      "lastUsed": "2025-12-13T10:30:00Z",
      "isFavorite": false,
      "metadata": {}
    }
  ],
  "activeConnectionId": "abc123..."
}
```
## 🔌 Database Providers

### SQLite

**Connection String Format:**
```
Data Source=path/to/database.db
```

**Examples:**
```bash
# Local file
Data Source=./mydb.db
Data Source=/home/user/databases/app.db

# In-memory database
Data Source=:memory:

# Read-only
Data Source=mydb.db;Mode=ReadOnly
```

**Supported Features:**
- ✅ Connection management
- ✅ Schema exploration
- ✅ Table browsing
- ✅ Query execution
- ✅ Data export

### PostgreSQL

**Connection String Format:**
```
Host=localhost;Database=mydb;Username=postgres;Password=secret
```

**Examples:**
```bash
# Local database
Host=localhost;Database=myapp;Username=postgres;Password=mypass

# With port
Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=mypass

# With SSL
Host=localhost;Database=myapp;Username=postgres;Password=mypass;SSL Mode=Require

# Remote database
Host=db.example.com;Database=production;Username=admin;Password=secure123
```

**Supported Features:**
- ✅ Connection management
- ✅ Schema exploration
- ✅ Table browsing
- ✅ Query execution
- ✅ Data export

### MySQL

**Connection String Format:**
```
Server=localhost;Database=mydb;User=root;Password=secret
```

**Examples:**
```bash
# Local database
Server=localhost;Database=myapp;User=root;Password=mypass

# With port
Server=localhost;Port=3306;Database=myapp;User=root;Password=mypass

# Remote database
Server=db.example.com;Database=production;User=admin;Password=secure123

# With SSL
Server=localhost;Database=myapp;User=root;Password=mypass;SslMode=Required
```

**Supported Features:**
- ✅ Connection management
- ✅ Schema exploration
- ✅ Table browsing
- ✅ Query execution
- ✅ Data export

### SQL Server

**Connection String Format:**
```
Server=localhost;Database=mydb;User Id=sa;Password=secret;TrustServerCertificate=True
```

**Examples:**
```bash
# Local database
Server=localhost;Database=myapp;User Id=sa;Password=YourPassword123;TrustServerCertificate=True

# LocalDB
Server=(localdb)\MSSQLLocalDB;Database=myapp;Integrated Security=true

# With instance name
Server=localhost\SQLEXPRESS;Database=myapp;User Id=sa;Password=pass;TrustServerCertificate=True

# Remote database
Server=db.example.com;Database=production;User Id=admin;Password=secure123;TrustServerCertificate=True

# Windows Authentication
Server=localhost;Database=myapp;Integrated Security=true
```

**Supported Features:**
- ✅ Connection management
- ✅ Schema exploration (with schema support)
- ✅ Table browsing
- ✅ Query execution
- ✅ Data export

## 🎨 Technology Stack

- **Language:** C# 12 / .NET 8
- **Database Access:** ADO.NET, Dapper
- **Terminal UI:** Spectre.Console
- **Configuration:** System.Text.Json
- **Testing:** xUnit, Moq, FluentAssertions

## 🤝 Contributing

Contributions are welcome! Here's how you can help:

1. **Report Bugs** - Open an issue with details and reproduction steps
2. **Suggest Features** - Share your ideas for new functionality
3. **Submit Pull Requests** - Fix bugs or add features
4. **Improve Documentation** - Help make the docs clearer

### Development Setup

```bash
# Clone the repo
git clone https://github.com/yourusername/queryly.git
cd queryly

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the CLI
dotnet run --project src/Queryly.CLI/Queryly.CLI.csproj
```

### Adding a New Database Provider

To add support for a new database:

1. Create a new folder in `Queryly.Providers/` (e.g., `MySql/`)
2. Implement `IConnectionProvider` interface
3. Override database-specific SQL queries
4. Add to `GetProvider()` method in commands
5. Update documentation

See `SqliteConnectionProvider.cs` as a reference implementation.

## 🐛 Troubleshooting

### Connection Test Fails

**Problem:** "Connection test failed" when adding a connection

**Solutions:**
- Verify your connection string format
- Check that the database file exists (for SQLite)
- Ensure you have read permissions
- For SQLite, verify the path is correct (relative or absolute)

### Command Not Found

**Problem:** `queryly: command not found`

**Solutions:**
- Make sure you're using the full `dotnet run` command
- Or install as a global tool (when available)
- Check that .NET SDK is installed: `dotnet --version`

### No Tables Found

**Problem:** "No tables found" when listing schema

**Solutions:**
- Verify the database has tables: use another tool to check
- Ensure connection string points to the correct database
- Check that you have read permissions

### Export Fails

**Problem:** Export command fails or produces empty file

**Solutions:**
- Ensure you have write permissions in the current directory
- Check that the table name is correct (case-sensitive on some systems)
- Verify the table has data: `queryly data browse <conn> <table>`

## 📝 Examples

### Working with a SQLite Database

```bash
# Create a test database
sqlite3 myapp.db
sqlite> CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, email TEXT);
sqlite> INSERT INTO users VALUES (1, 'Alice', 'alice@example.com');
sqlite> INSERT INTO users VALUES (2, 'Bob', 'bob@example.com');
sqlite> .quit

# Add to Queryly
queryly connect add
# name: MyApp
# type: SQLite  
# connection string: Data Source=myapp.db

# Explore
queryly schema list MyApp
queryly schema info MyApp users
queryly data browse MyApp users

# Query
queryly data query MyApp
SQL> SELECT * FROM users WHERE name LIKE 'A%'
SQL> exit

# Export
queryly data export MyApp users csv
```

### Interactive Query Session

```bash
$ queryly data query MyDB

Query Mode - MyDB
Enter SQL queries. Type 'exit' to quit.

SQL> SELECT COUNT(*) FROM users
┌────────────┐
│ COUNT(*)   │
├────────────┤
│ 1234       │
└────────────┘
Showing 1 of 1 row(s) | Query time: 2.34ms

SQL> SELECT name, email FROM users LIMIT 5
┌──────────────┬──────────────────────┐
│ name         │ email                │
├──────────────┼──────────────────────┤
│ Alice Smith  │ alice@example.com    │
│ Bob Jones    │ bob@example.com      │
│ Carol Lee    │ carol@example.com    │
│ David Kim    │ david@example.com    │
│ Emma Wilson  │ emma@example.com     │
└──────────────┴──────────────────────┘
Showing 5 of 5 row(s) | Query time: 1.23ms

SQL> exit
Exited query mode.
```

## 🗺️ Roadmap

### Current Version (v0.1)
- ✅ SQLite support
- ✅ Basic CRUD operations
- ✅ Schema exploration
- ✅ Data export (CSV/JSON)

### Upcoming Features
- 🔄 Pagination and filtering
- 🔄 Data editing (INSERT, UPDATE, DELETE)
- 🔄 Query history and saved queries
- 🔄 Database comparison tools
- 🔄 Migration script generation
- 🔄 Performance optimization
- 🔄 GUI version (Avalonia)

## 📄 License

MIT License - see LICENSE file for details

## 👏 Acknowledgments

- Built with [Spectre.Console](https://spectreconsole.net/) for beautiful terminal UI
- Inspired by tools like DBeaver, HeidiSQL, and TablePlus
- Created out of frustration with installing SQL Server Management Studio on Ubuntu 😄

## 📧 Contact

- GitHub: [@Ayine-nongre](https://github.com/Ayine-nongre)
- Issues: [GitHub Issues](https://github.com/Ayine-nongre/queryly/issues)
- LinkedIn: [Eugene Atinbire](https://linkedin.com/in/ayine-nongre)

---

**Built with ❤️ by developers, for developers**

*Queryly - Because sometimes you just need to see what's in the database.*
