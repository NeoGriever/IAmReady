using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace IAmReady;

[Serializable]
public sealed class Config : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public int YesCount = 5;
    public bool IsActive = false;
    public int CurrentLang = 0;
    public List<string> RegexPatterns = new();

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
