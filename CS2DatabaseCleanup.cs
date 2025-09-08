using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Events;
using Newtonsoft.Json;

namespace CS2DatabaseCleanup;

public class CS2DatabaseCleanup : BasePlugin
{
    public override string ModuleName => "CS2 Database Cleanup";
    public override string ModuleVersion => "1.0.0";

    private DatabaseService? _databaseService;
    private PluginConfig? _config;
    private readonly string _configPath;

    public CS2DatabaseCleanup()
    {
        _configPath = Path.Combine(ModuleDirectory, "config.json");
    }

    public override void Load(bool hotReload)
    {
        LoadConfig();

        if (_config?.Database != null)
        {
            _databaseService = new DatabaseService(_config.Database, _config.Rules);
            Task.Run(async () => await _databaseService.InitializeDatabaseAsync());
        }
        else
        {
            Server.PrintToConsole("[CS2DatabaseCleanup] Failed to load configuration");
        }

        RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        Server.PrintToConsole("[CS2DatabaseCleanup] Plugin loaded successfully");
    }

    private void LoadConfig()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                CreateDefaultConfig();
            }

            var configJson = File.ReadAllText(_configPath);
            _config = JsonConvert.DeserializeObject<PluginConfig>(configJson);
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[CS2DatabaseCleanup] Failed to load config: {ex.Message}");
        }
    }

    private void CreateDefaultConfig()
    {
        var defaultConfig = new PluginConfig
        {
            Database = new DatabaseConfig
            {
                Host = "your_database_host",
                Port = 3306,
                Username = "your_username",
                Password = "your_password",
                DatabaseName = "your_database_name"
            },
            Rules = new List<CleanupRule>
            {
                new()
                {
                    Rule = "@lastLogin>30d",
                    Queries = new List<string>
                    {
                        "DELETE FROM wp_player_skins WHERE steamid = @steamid;",
                        "DELETE FROM wp_player_stats WHERE steamid = @steamid;"
                    }
                },
                new()
                {
                    Rule = "@firstLogin>60d",
                    Queries = new List<string>
                    {
                        "DELETE FROM players WHERE steamid = @steamid;"
                    }
                }
            }
        };

        var configJson = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
        File.WriteAllText(_configPath, configJson);

        Server.PrintToConsole($"[CS2DatabaseCleanup] Created default config at: {_configPath}");
    }

    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

        var steamId = player.SteamID;
        var playerName = player.PlayerName;

        Task.Run(async () =>
        {
            if (_databaseService != null)
            {
                await _databaseService.UpdatePlayerConnectAsync(steamId, playerName);
            }
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

        var steamId = player.SteamID;

        Task.Run(async () =>
        {
            if (_databaseService != null)
            {
                await _databaseService.UpdatePlayerDisconnectAsync(steamId);
            }
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Execute cleanup rules on round start (you can change this to map change if preferred)
        Task.Run(async () =>
        {
            if (_databaseService != null)
            {
                await _databaseService.ExecuteCleanupRulesAsync();
            }
        });

        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        Server.PrintToConsole("[CS2DatabaseCleanup] Plugin unloaded");
    }
}
