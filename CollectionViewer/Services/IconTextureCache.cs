using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CollectionViewer.Api;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace CollectionViewer.Services;

/// <summary>Load state of one icon texture requested by the UI.</summary>
public enum IconState
{
    Loading,
    Ready,
    Failed,
}

/// <summary>
/// Downloads item icons/artwork from FFXIV Collect (proxied through v2.xivapi.com) and keeps them
/// as ImGui textures for the plugin's lifetime. ImGui draws happen synchronously on the render
/// thread, so this cache is fire-and-forget from the UI's perspective: Draw() calls
/// <see cref="RequestTexture"/> every frame, which kicks off a background download at most once
/// per URL and returns whatever is currently available (null while loading).
/// </summary>
public sealed class IconTextureCache : IDisposable
{
    private sealed class Entry
    {
        public IconState State = IconState.Loading;
        public IDalamudTextureWrap? Texture;
    }

    private readonly FfxivCollectClient client;
    private readonly ITextureProvider textureProvider;
    private readonly IPluginLog log;
    private readonly ConcurrentDictionary<string, Entry> entries = new();

    public IconTextureCache(FfxivCollectClient client, ITextureProvider textureProvider, IPluginLog log)
    {
        this.client = client;
        this.textureProvider = textureProvider;
        this.log = log;
    }

    /// <summary>Returns the texture for this URL if it is already loaded, starting a background
    /// download the first time this URL is requested. Never blocks.</summary>
    public IDalamudTextureWrap? RequestTexture(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        var entry = entries.GetOrAdd(url, StartLoad);
        return entry.State == IconState.Ready ? entry.Texture : null;
    }

    private Entry StartLoad(string url)
    {
        var entry = new Entry();
        _ = LoadAsync(url, entry);
        return entry;
    }

    private async Task LoadAsync(string url, Entry entry)
    {
        try
        {
            var bytes = await client.DownloadImageAsync(url, CancellationToken.None).ConfigureAwait(false);
            var texture = await textureProvider.CreateFromImageAsync(bytes, url).ConfigureAwait(false);
            entry.Texture = texture;
            entry.State = IconState.Ready;
        }
        catch (Exception ex)
        {
            log.Debug($"[CollectionViewer] Failed to load icon '{url}': {ex.Message}");
            entry.State = IconState.Failed;
        }
    }

    public void Dispose()
    {
        foreach (var entry in entries.Values)
            entry.Texture?.Dispose();
        entries.Clear();
    }
}
