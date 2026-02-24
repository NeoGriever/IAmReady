using System.Collections.Generic;

namespace IAmReady;

public enum Language { EN, DE }

public static class Lang
{
    public static Language Current = Language.EN;

    private static readonly Dictionary<string, string[]> Strings = new()
    {
        { "start", new[] { "Start", "Start" } },
        { "active", new[] { "Active", "Aktiv" } },
        { "inactive", new[] { "Inactive", "Inaktiv" } },
        { "yes_count", new[] { "Yes Count", "Ja-Anzahl" } },
        { "regex_patterns", new[] { "Regex Patterns", "Regex-Muster" } },
        { "add", new[] { "Add", "Hinzufügen" } },
        { "remove", new[] { "Remove", "Entfernen" } },
        { "language", new[] { "Language", "Sprache" } },
        { "yes", new[] { "YES", "JA" } },
        { "no", new[] { "NO", "NEIN" } },
    };

    public static string T(string key)
    {
        if (Strings.TryGetValue(key, out var vals))
            return vals[(int)Current];
        return key;
    }
}
