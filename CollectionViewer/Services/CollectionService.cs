using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CollectionViewer.Api;

namespace CollectionViewer.Services;

/// <summary>
/// Caching layer between the UI and <see cref="FfxivCollectClient"/>. Keeps an in-memory copy of
/// every character/category response fetched this session, backed by a per-character JSON file
/// on disk so the cache survives a game restart. A response is considered fresh for
/// <see cref="Configuration.CacheTtlMinutes"/>; callers can force a bypass (the "Обновить" button).
/// </summary>
public sealed class CollectionService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly FfxivCollectClient client;
    private readonly Configuration configuration;
    private readonly string cacheDirectory;
    private readonly ConcurrentDictionary<int, CharacterCacheFile> memoryCache = new();
    private readonly SemaphoreSlim diskLock = new(1, 1);

    public CollectionService(FfxivCollectClient client, Configuration configuration, string pluginConfigDirectory)
    {
        this.client = client;
        this.configuration = configuration;
        cacheDirectory = Path.Combine(pluginConfigDirectory, "cache");
        Directory.CreateDirectory(cacheDirectory);
    }

    public async Task<CharacterSnapshot> GetCharacterAsync(int characterId, bool forceRefresh, CancellationToken ct = default)
    {
        var entry = GetOrLoadEntry(characterId);
        if (!forceRefresh && entry.Character is { } cached && IsFresh(cached.FetchedAtUtc))
            return cached;

        var summary = await client.GetCharacterAsync(characterId, ct).ConfigureAwait(false);
        var snapshot = new CharacterSnapshot { Summary = summary, FetchedAtUtc = DateTime.UtcNow };
        entry.Character = snapshot;
        await PersistAsync(characterId, entry, ct).ConfigureAwait(false);
        return snapshot;
    }

    public async Task<CategorySnapshot> GetCategoryAsync(int characterId, string categorySegment, bool forceRefresh, CancellationToken ct = default)
    {
        var entry = GetOrLoadEntry(characterId);
        if (!forceRefresh &&
            entry.Categories.TryGetValue(categorySegment, out var cached) &&
            IsFresh(cached.FetchedAtUtc))
        {
            return cached;
        }

        var ownedTask = client.GetCollectionItemsAsync(characterId, categorySegment, owned: true, ct);
        var missingTask = client.GetCollectionItemsAsync(characterId, categorySegment, owned: false, ct);
        await Task.WhenAll(ownedTask, missingTask).ConfigureAwait(false);

        var snapshot = new CategorySnapshot
        {
            CategorySegment = categorySegment,
            Owned = ownedTask.Result,
            Missing = missingTask.Result,
            FetchedAtUtc = DateTime.UtcNow,
        };
        entry.Categories[categorySegment] = snapshot;
        await PersistAsync(characterId, entry, ct).ConfigureAwait(false);
        return snapshot;
    }

    private bool IsFresh(DateTime fetchedAtUtc) =>
        DateTime.UtcNow - fetchedAtUtc < TimeSpan.FromMinutes(Math.Max(1, configuration.CacheTtlMinutes));

    private CharacterCacheFile GetOrLoadEntry(int characterId)
    {
        return memoryCache.GetOrAdd(characterId, id => LoadFromDisk(id) ?? new CharacterCacheFile());
    }

    private CharacterCacheFile? LoadFromDisk(int characterId)
    {
        var path = GetCachePath(characterId);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CharacterCacheFile>(json, JsonOptions);
        }
        catch
        {
            // Corrupt or outdated cache file - ignore and start fresh.
            return null;
        }
    }

    private async Task PersistAsync(int characterId, CharacterCacheFile entry, CancellationToken ct)
    {
        await diskLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(entry, JsonOptions);
            await File.WriteAllTextAsync(GetCachePath(characterId), json, ct).ConfigureAwait(false);
        }
        catch
        {
            // Disk cache is a best-effort convenience; failures here must not break the UI.
        }
        finally
        {
            diskLock.Release();
        }
    }

    private string GetCachePath(int characterId) => Path.Combine(cacheDirectory, $"{characterId}.json");

    public void Dispose() => diskLock.Dispose();
}
