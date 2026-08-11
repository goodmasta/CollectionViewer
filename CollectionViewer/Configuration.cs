using System;
using Dalamud.Configuration;

namespace CollectionViewer;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>UI language. Defaults to English.</summary>
    public PluginLanguage Language { get; set; } = PluginLanguage.English;

    /// <summary>The player's own FFXIV Collect / Lodestone character id, entered once in settings.</summary>
    public int? OwnCharacterId { get; set; }

    /// <summary>How long a cached collection response is considered fresh before it is refetched automatically.</summary>
    public int CacheTtlMinutes { get; set; } = 30;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
