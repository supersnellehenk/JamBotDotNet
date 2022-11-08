namespace JamBotDotNet;

public static class DotEnv
{
    public static void Load()
    {
        var lines = File.ReadAllLines(".env");
        foreach (var line in lines)
        {
            var parts = line.Split('=');
            var key = parts[0];
            var value = parts[1];
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}