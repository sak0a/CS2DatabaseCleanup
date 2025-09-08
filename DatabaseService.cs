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

    public async Task ExecuteCleanupRulesAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var rule in _cleanupRules)
            {
                var eligiblePlayers = await GetEligiblePlayersAsync(connection, rule.Rule);

                foreach (var steamId in eligiblePlayers)
                {
                    foreach (var query in rule.Queries)
                    {
                        try
                        {
                            var processedQuery = query.Replace("@steamid", steamId.ToString());
                            using var command = new MySqlCommand(processedQuery, connection);
                            var affectedRows = await command.ExecuteNonQueryAsync();

                            Server.PrintToConsole($"[CS2DatabaseCleanup] Executed cleanup for SteamID {steamId}: {affectedRows} rows affected");
                        }
                        catch (Exception ex)
                        {
                            Server.PrintToConsole($"[CS2DatabaseCleanup] Failed to execute cleanup query for SteamID {steamId}: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[CS2DatabaseCleanup] Failed to execute cleanup rules: {ex.Message}");
        }
    }

    private async Task<List<ulong>> GetEligiblePlayersAsync(MySqlConnection connection, string rule)
    {
        var eligiblePlayers = new List<ulong>();

        try
        {
            var (field, days) = ParseRule(rule);
            if (field == null || days == 0) return eligiblePlayers;

            var cutoffDate = DateTime.Now.AddDays(-days);
            var query = $"SELECT steamid FROM player_data WHERE {field} < @cutoffDate";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@cutoffDate", cutoffDate);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                eligiblePlayers.Add(Convert.ToUInt64(reader["steamid"]));
            }
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[CS2DatabaseCleanup] Failed to get eligible players for rule '{rule}': {ex.Message}");
        }

        return eligiblePlayers;
    }

    private (string? field, int days) ParseRule(string rule)
    {
        // Parse rules like "@lastLogin>30d" or "@firstLogin>60d"
        if (!rule.StartsWith("@")) return (null, 0);

        var parts = rule.Substring(1).Split('>');
        if (parts.Length != 2) return (null, 0);

        var field = parts[0].ToLower() switch
        {
            "lastlogin" => "last_login",
            "firstlogin" => "first_login",
            "lastlogout" => "last_logout",
            _ => null
        };

        if (field == null) return (null, 0);

        var durationStr = parts[1];
        if (!durationStr.EndsWith("d")) return (null, 0);

        if (int.TryParse(durationStr.Substring(0, durationStr.Length - 1), out var days))
        {
            return (field, days);
        }

        return (null, 0);
    }
}
