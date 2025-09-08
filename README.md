# CS2 Database Cleanup Plugin

A CounterStrike Sharp plugin that tracks player data in a MySQL database and automatically executes cleanup rules based on configurable time periods.

## Features

- **Player Tracking**: Automatically tracks player connections and disconnections
- **Database Storage**: Stores player data including SteamID, name, first login, last login, and last logout
- **Configurable Cleanup Rules**: Execute custom SQL queries based on time-based conditions
- **Automatic Execution**: Cleanup rules are executed on round start (configurable)

## Database Schema

The plugin creates a `player_data` table with the following structure:

```sql
CREATE TABLE player_data (
    steamid BIGINT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    first_login DATETIME NOT NULL,
    last_login DATETIME NOT NULL,
    last_logout DATETIME NULL,
    INDEX idx_last_login (last_login),
    INDEX idx_first_login (first_login)
);
```

## Configuration

The plugin uses a `config.json` file with the following structure:

```json
{
  "Database": {
    "Host": "your_database_host",
    "Port": 3306,
    "Username": "your_username",
    "Password": "your_password",
    "DatabaseName": "your_database_name"
  },
  "Rules": [
    {
      "Rule": "@lastLogin>30d",
      "Queries": [
        "DELETE FROM wp_player_skins WHERE steamid = @steamid;",
        "DELETE FROM wp_player_stats WHERE steamid = @steamid;"
      ]
    },
    {
      "Rule": "@firstLogin>60d",
      "Queries": [
        "DELETE FROM players WHERE steamid = @steamid;"
      ]
    }
  ]
}
```

### Rule Syntax

Rules follow the format: `@field>Xd` where:
- `@field` can be:
  - `@lastLogin` - Last login time
  - `@firstLogin` - First login time  
  - `@lastLogout` - Last logout time
- `X` is the number of days
- `d` indicates days (currently the only supported unit)

### Query Variables

In your SQL queries, you can use:
- `@steamid` - Will be replaced with the player's SteamID

## Installation

1. Build the plugin using .NET 8.0
2. Copy the compiled DLL to your CounterStrike Sharp plugins folder
3. Configure the `config.json` file with your database credentials
4. Restart your server

## Requirements

- CounterStrike Sharp
- MySQL Database
- .NET 9.0 Runtime
