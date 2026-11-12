using QLS.Chassis.Core.Configuration;

namespace QLS.Chassis.Core.Sample.Demos;

/// <summary>Typed configuration reads with defaults, and the order sources are checked in.</summary>
internal static class ConfigurationDemo
{
    public static void Run(IConfigReader config)
    {
        Section.Title("Configuration");

        // appsettings.json (copied next to the sample) sets Orders:MaxItems to 50 and Orders:AllowedCurrencies.
        Console.WriteLine($"Orders:MaxItems = {config.GetIntOrDefault("Orders:MaxItems", 25)} (from appsettings.json)");
        Console.WriteLine($"Orders:AllowedCurrencies = {string.Join(", ", config.GetCsvSetOrDefault("Orders:AllowedCurrencies", ["USD"]))}");
        Console.WriteLine($"Orders:VerboseLogging = {config.GetBoolOrDefault("Orders:VerboseLogging", false)} (not set: the default)");

        // An environment variable fills in a key appsettings.json doesn't have...
        Environment.SetEnvironmentVariable("SAMPLE_REGION", "eu-west-1");
        Console.WriteLine($"SAMPLE_REGION = {config.GetRequiredString("SAMPLE_REGION")} (from the environment)");

        // ...but doesn't override one it does: appsettings.json is checked first.
        Environment.SetEnvironmentVariable("Orders:MaxItems", "99");
        Console.WriteLine($"Orders:MaxItems = {config.GetIntOrDefault("Orders:MaxItems", 25)} (appsettings.json still wins over the environment's 99)");

        try
        {
            config.GetRequiredString("Orders:QueueUrl");
        }
        catch (ConfigurationException ex)
        {
            Console.WriteLine($"Missing required key '{ex.Key}': {ex.Message}");
        }
    }
}
