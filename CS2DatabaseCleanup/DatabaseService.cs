using MySql.Data.MySqlClient;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace CS2DatabaseCleanup;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly List<CleanupRule> _cleanupRules;

    public DatabaseService(DatabaseConfig config, List<CleanupRule> cleanupRules)
    {
        _connectionString = $"Server={config.Host};Port={config.Port};Database={config.DatabaseName};Uid={config.Username};Pwd={config.Password};";
        _cleanupRules = cleanupRules;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var createTableQuery = @"
                CREATE TABLE IF NOT EXISTS player_data (
                    steamid BIGINT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    first_login DATETIME NOT NULL,
                    last_login DATETIME NOT NULL,
                    last_logout DATETIME NULL,
                    INDEX idx_last_login (last_login),
                    INDEX idx_first_login (first_login)
                )";

            using var command = new MySqlCommand(createTableQuery, connection);
            await command.ExecuteNonQueryAsync();
            
            Server.PrintToConsole("[CS2DatabaseCleanup] Database initialized successfully");
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[CS2DatabaseCleanup] Database initialization failed: {ex.Message}");
        }
    }

    public async Task UpdatePlayerConnectAsync(ulong steamId, string playerName)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO player_data (steamid, name, first_login, last_login) 
                VALUES (@steamid, @name, @now, @now)
                ON DUPLICATE KEY UPDATE 
                    name = @name,
                    last_login = @now";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@steamid", steamId);
            command.Parameters.AddWithValue("@name", playerName);
            command.Parameters.AddWithValue("@now", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[CS2DatabaseCleanup] Failed to update player connect: {ex.Message}");
        }
    }

    public async Task UpdatePlayerDisconnectAsync(ulong steamId)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE player_data SET last_logout = @now WHERE steamid = @steamid";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@steamid", steamId);
            command.Parameters.AddWithValue("@now", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[CS2DatabaseCleanup] Failed to update player disconnect: {ex.Message}");
        }
    }
