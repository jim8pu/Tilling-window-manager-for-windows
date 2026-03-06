namespace SeelenWM.Configs;

public class ConfigLoader
{
    public List<AppConfig> AppConfigs { get; private set; } = new();

    public ConfigLoader()
    {
        LoadDefaultRules();
    }

    private void LoadDefaultRules()
    {
        // 1. Explicit Exclusions (Launchers, Tools)
        AddUnmanageRule("Flow.Launcher.exe", AppIdentifierType.Exe);
        AddUnmanageRule("seelen-ui.exe", AppIdentifierType.Exe);
        AddUnmanageRule("PowerToys.PowerLauncher.exe", AppIdentifierType.Exe); // The main runner (Alt+Space)

        // 2. System Background Apps
        AppConfigs.Add(
            new AppConfig
            {
                Name = "System Background Apps",
                Identifier = new AppIdentifier
                {
                    Id = @"Windows\SystemApps",
                    Kind = AppIdentifierType.Path,
                    MatchingStrategy = MatchingStrategy.Contains,
                },
                Options = new List<AppExtraFlag>
                {
                    AppExtraFlag.NoInteractive,
                    AppExtraFlag.WmUnmanage,
                },
            }
        );

        // 3. Installers and Menus (Regex)
        AppConfigs.Add(
            new AppConfig
            {
                Name = "Base Ignored",
                Identifier = new AppIdentifier
                {
                    Id = @"(?i)((install)|(setup)|(complete)|(menu)|(notification))",
                    Kind = AppIdentifierType.Title,
                    MatchingStrategy = MatchingStrategy.Regex,
                },
                Options = new List<AppExtraFlag> { AppExtraFlag.WmUnmanage },
            }
        );

        // Prepare regexes and normalized strings
        foreach (var config in AppConfigs)
        {
            config.Prepare();
        }
    }

    private void AddUnmanageRule(string id, AppIdentifierType kind)
    {
        AppConfigs.Add(
            new AppConfig
            {
                Name = id,
                Identifier = new AppIdentifier
                {
                    Id = id,
                    Kind = kind,
                    MatchingStrategy = MatchingStrategy.Equals,
                },
                Options = new List<AppExtraFlag> { AppExtraFlag.WmUnmanage },
            }
        );
    }

    /// <summary>
    /// Checks if a window matches any configuration rule.
    /// Returns the first matching config, or null if handled dynamically.
    /// </summary>
    public AppConfig? FindMatch(string title, string className, string exe, string path)
    {
        return AppConfigs.FirstOrDefault(c => c.Identifier.Validate(title, className, exe, path));
    }
}
