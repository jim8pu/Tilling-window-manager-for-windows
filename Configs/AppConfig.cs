using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SeelenWM.Configs;

public enum AppIdentifierType
{
    Exe,
    Class,
    Title,
    Path,
}

public enum MatchingStrategy
{
    Equals,
    StartsWith,
    EndsWith,
    Contains,
    Regex,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppExtraFlag
{
    [JsonPropertyName("no-interactive")]
    NoInteractive,

    [JsonPropertyName("wm-float")]
    WmFloat,

    [JsonPropertyName("wm-force")]
    WmForce,

    [JsonPropertyName("unmanage")]
    WmUnmanage,

    [JsonPropertyName("vd-pinned")]
    VdPinned,
}

public class AppIdentifier
{
    public string Id { get; set; } = string.Empty;
    public AppIdentifierType Kind { get; set; }
    public MatchingStrategy MatchingStrategy { get; set; }
    public bool Negation { get; set; }

    // Recursive conditions (AND/OR)
    public List<AppIdentifier> And { get; set; } = new();
    public List<AppIdentifier> Or { get; set; } = new();

    private Regex? _regex;
    private string? _lowerId;

    public void Prepare()
    {
        if (MatchingStrategy == MatchingStrategy.Regex)
        {
            try
            {
                _regex = new Regex(Id, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            catch
            { /* Ignore invalid regex */
            }
        }

        if (Kind == AppIdentifierType.Path || Kind == AppIdentifierType.Exe)
        {
            _lowerId = Id.NormalizePath();
        }

        foreach (var item in And)
            item.Prepare();
        foreach (var item in Or)
            item.Prepare();
    }

    public bool Validate(string title, string className, string exe, string path)
    {
        bool result = CheckMatch(title, className, exe, path);

        if (Negation)
            result = !result;

        if (And.Count > 0 && !And.All(x => x.Validate(title, className, exe, path)))
            return false;
        if (Or.Count > 0 && Or.Any(x => x.Validate(title, className, exe, path)))
            return true;

        return result;
    }

    private bool CheckMatch(string title, string className, string exe, string path)
    {
        // Normalize inputs
        exe = exe.ToLowerInvariant();
        path = path.NormalizePath();

        return MatchingStrategy switch
        {
            MatchingStrategy.Equals => Kind switch
            {
                AppIdentifierType.Title => title.Equals(Id, StringComparison.Ordinal),
                AppIdentifierType.Class => className.Equals(Id, StringComparison.Ordinal),
                AppIdentifierType.Exe => exe.Equals(_lowerId, StringComparison.Ordinal),
                AppIdentifierType.Path => path.Equals(_lowerId, StringComparison.Ordinal),
                _ => false,
            },
            MatchingStrategy.StartsWith => Kind switch
            {
                AppIdentifierType.Title => title.StartsWith(Id, StringComparison.Ordinal),
                AppIdentifierType.Class => className.StartsWith(Id, StringComparison.Ordinal),
                AppIdentifierType.Exe => exe.StartsWith(_lowerId!, StringComparison.Ordinal),
                AppIdentifierType.Path => path.StartsWith(_lowerId!, StringComparison.Ordinal),
                _ => false,
            },
            MatchingStrategy.Contains => Kind switch
            {
                AppIdentifierType.Title => title.Contains(Id, StringComparison.Ordinal),
                AppIdentifierType.Class => className.Contains(Id, StringComparison.Ordinal),
                AppIdentifierType.Exe => exe.Contains(_lowerId!, StringComparison.Ordinal),
                AppIdentifierType.Path => path.Contains(_lowerId!, StringComparison.Ordinal),
                _ => false,
            },
            MatchingStrategy.Regex => Kind switch
            {
                AppIdentifierType.Title => _regex?.IsMatch(title) ?? false,
                AppIdentifierType.Class => _regex?.IsMatch(className) ?? false,
                AppIdentifierType.Exe => _regex?.IsMatch(exe) ?? false,
                AppIdentifierType.Path => _regex?.IsMatch(path) ?? false,
                _ => false,
            },
            _ => false,
        };
    }
}

public class AppConfig
{
    public string Name { get; set; } = string.Empty;
    public AppIdentifier Identifier { get; set; } = new();
    public List<AppExtraFlag> Options { get; set; } = new();

    public void Prepare()
    {
        Identifier.Prepare();
    }
}

public static class StringExtensions
{
    public static string NormalizePath(this string path)
    {
        return path.Replace('\\', '/').ToLowerInvariant();
    }
}
