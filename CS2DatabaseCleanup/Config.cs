using Newtonsoft.Json;

namespace CS2DatabaseCleanup;

public class DatabaseConfig
{
    [JsonProperty("Host")]
    public string Host { get; set; } = "localhost";
    
    [JsonProperty("Port")]
    public int Port { get; set; } = 3306;
    
    [JsonProperty("Username")]
    public string Username { get; set; } = "";
    
    [JsonProperty("Password")]
    public string Password { get; set; } = "";
    
    [JsonProperty("DatabaseName")]
    public string DatabaseName { get; set; } = "";
}

public class CleanupRule
{
    [JsonProperty("Rule")]
    public string Rule { get; set; } = "";
    
    [JsonProperty("Queries")]
    public List<string> Queries { get; set; } = new();
}

public class PluginConfig
{
    [JsonProperty("Database")]
    public DatabaseConfig Database { get; set; } = new();
    
    [JsonProperty("Rules")]
    public List<CleanupRule> Rules { get; set; } = new();
}
