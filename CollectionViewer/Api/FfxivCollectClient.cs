using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CollectionViewer.Api.Models;

namespace CollectionViewer.Api;

/// <summary>
/// Thin, async HTTP wrapper around the public FFXIV Collect API (https://ffxivcollect.com/api/).
/// Holds no state beyond the HttpClient; caching lives in CollectionService.
/// </summary>
public sealed class FfxivCollectClient : IDisposable
{
    private const string BaseUrl = "https://ffxivcollect.com/api/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient http;

    public FfxivCollectClient()
    {
        http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(15),
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Dalamud-CollectionViewer-Plugin/1.0 (+non-commercial personal use)");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    /// <summary>Fetches the character summary (category counts, public flags, verification state).</summary>
    /// <exception cref="CharacterNotFoundException">The character id is not registered on FFXIV Collect.</exception>
    public async Task<CharacterSummaryDto> GetCharacterAsync(int characterId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"characters/{characterId}", ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, characterId, category: null, ct).ConfigureAwait(false);

        var body = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var dto = await JsonSerializer.DeserializeAsync<CharacterSummaryDto>(body, JsonOptions, ct).ConfigureAwait(false);
        return dto ?? throw new FfxivCollectException("FFXIV Collect returned an empty character response.");
    }

    /// <summary>
    /// Fetches the full owned or missing item list for one collection category of a character.
    /// The API has no separate "all owned" route with pagination cut-off; both /owned and /missing
    /// return the complete list in one response, so no paging is needed here.
    /// </summary>
    /// <param name="categorySegment">API path segment, e.g. "mounts", "minions", "orchestrions",
    /// "emotes", "hairstyles", "bardings" (see <see cref="CollectionViewer.Data.CollectionCategories"/>).</param>
    /// <param name="owned">true for the "/owned" route, false for "/missing".</param>
    /// <exception cref="CharacterNotFoundException">The character id is not registered on FFXIV Collect.</exception>
    /// <exception cref="CollectionPrivateException">The character exists but hid this collection.</exception>
    public async Task<System.Collections.Generic.List<CollectionItemDto>> GetCollectionItemsAsync(
        int characterId, string categorySegment, bool owned, CancellationToken ct = default)
    {
        var segment = owned ? "owned" : "missing";
        var response = await http.GetAsync($"characters/{characterId}/{categorySegment}/{segment}", ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, characterId, categorySegment, ct).ConfigureAwait(false);

        var body = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var list = await JsonSerializer.DeserializeAsync<System.Collections.Generic.List<CollectionItemDto>>(body, JsonOptions, ct).ConfigureAwait(false);
        return list ?? new System.Collections.Generic.List<CollectionItemDto>();
    }

    /// <summary>Downloads raw image bytes for an item icon/artwork URL, for use with ITextureProvider.CreateFromImageAsync.</summary>
    public async Task<byte[]> DownloadImageAsync(string url, CancellationToken ct = default)
    {
        // FFXIV Collect icons are served via v2.xivapi.com's asset proxy with an explicit
        // format=webp query param. Dalamud's built-in image decoders reliably handle PNG/JPEG
        // but WebP support depends on the OS codec pack, so we ask for PNG instead when we can.
        var normalized = url.Contains("v2.xivapi.com", StringComparison.OrdinalIgnoreCase)
            ? url.Replace("format=webp", "format=png", StringComparison.OrdinalIgnoreCase)
            : url;

        using var response = await http.GetAsync(normalized, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, int characterId, string? category, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new FfxivCollectRateLimitException();

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new CharacterNotFoundException(characterId);

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new CollectionPrivateException(characterId, category ?? "profile");

        string errorText;
        try
        {
            var body = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var error = await JsonSerializer.DeserializeAsync<ApiErrorDto>(body, JsonOptions, ct).ConfigureAwait(false);
            errorText = error?.Error ?? response.ReasonPhrase ?? "unknown error";
        }
        catch
        {
            errorText = response.ReasonPhrase ?? "unknown error";
        }

        throw new FfxivCollectException($"FFXIV Collect request failed ({(int)response.StatusCode}): {errorText}");
    }

    public void Dispose() => http.Dispose();
}
