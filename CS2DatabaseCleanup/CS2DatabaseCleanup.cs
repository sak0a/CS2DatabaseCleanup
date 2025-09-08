namespace CS2DatabaseCleanup;

using CounterStrikeSharp.API.Core;

public class CS2DatabaseCleanup : BasePlugin
{
    public override string ModuleName => "Hello World Plugin";

    public override string ModuleVersion => "0.0.1";

    public override void Load(bool hotReload)
    {
        Console.WriteLine("Hello World!");
    }
}
