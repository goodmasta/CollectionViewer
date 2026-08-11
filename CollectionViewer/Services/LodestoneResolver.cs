using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CollectionViewer.Data;

namespace CollectionViewer.Services;

/// <summary>One hit from a Lodestone character search.</summary>
/// <param name="LodestoneId">The Lodestone character id, which FFXIV Collect also uses as its own character id.</param>
public sealed record LodestoneSearchHit(int LodestoneId, string Name, string World);

/// <summary>
/// Resolves an in-game character (name + world) to a Lodestone character id by querying the
/// official Lodestone character search page and scraping the HTML result list.
/// </summary>
/// <remarks>
/// FFXIV Collect's public API has no "search by name/world" or "search by Lodestone id" endpoint
/// (confirmed: /api/characters, /api/characters?name=..., /api/characters/search all 404).
/// It was also confirmed that FFXIV Collect uses the Lodestone character id directly as its own
/// character id (ffxivcollect.com/api/characters/1 is "Macaroni Gratin" on Aegis, which is exactly
/// Lodestone character https://na.finalfantasyxiv.com/lodestone/character/1/).
/// So resolving "who is this player on FFXIV Collect" requires first resolving their Lodestone id,
/// and the only public source for that lookup is Lodestone's own character search page - there is
/// no JSON API for it (XIVAPI v1 used to proxy this but the current XIVAPI v2 dropped it).
/// This is therefore a best-effort scrape: it is throttled, cached by the caller, and any layout
/// change on Lodestone's side will simply make it return no match rather than throw.
/// </remarks>
public sealed class LodestoneResolver : IDisposable
{
    // Matches one search result entry and captures: lodestone id, character name, world name.
    // Sample fragment: <div class="entry"><a href="/lodestone/character/61975230/" class="entry__link">
    // ...<p class="entry__name">Ash Yugiri</p><p class="entry__world">...</i>Gilgamesh [Aether]</p>...
    private static readonly Regex EntryRegex = new(
        "href=\"/lodestone/character/(?<id>\\d+)/\"\\s+class=\"entry__link\">.*?" +
        "<p class=\"entry__name\">(?<name>[^<]*)</p>\\s*" +
        "<p class=\"entry__world\">.*?</i>(?<world>[^\\[<]*)\\s*\\[",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly HttpClient http;

    public LodestoneResolver()
    {
        http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        // Lodestone blocks requests without a browser-like User-Agent.
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
    }

    /// <summary>
    /// Looks up the Lodestone id of a character by exact name and world match.
    /// Returns null if Lodestone has no character with that exact name on that exact world
    /// (search is fuzzy server-side, so an exact-match filter is applied client-side).
    /// </summary>
    public async Task<int?> ResolveLodestoneIdAsync(string characterName, string worldName, CancellationToken ct = default)
    {
        var subdomain = WorldRegions.GetLodestoneSubdomain(worldName);
        var url = $"https://{subdomain}.finalfantasyxiv.com/lodestone/character/" +
                   $"?q={Uri.EscapeDataString(characterName)}&worldname={Uri.EscapeDataString(worldName)}";

        string html;
        try
        {
            html = await http.GetStringAsync(url, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new LodestoneLookupException("Failed to reach Lodestone to search for the character.", ex);
        }

        foreach (Match match in EntryRegex.Matches(html))
        {
            var name = match.Groups["name"].Value.Trim();
            var world = match.Groups["world"].Value.Trim();
            if (string.Equals(name, characterName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(world, worldName, StringComparison.OrdinalIgnoreCase))
            {
                return int.Parse(match.Groups["id"].Value);
            }
        }

        return null;
    }

    public void Dispose() => http.Dispose();
}

/// <summary>Thrown when the Lodestone search page could not be reached or parsed.</summary>
public sealed class LodestoneLookupException : Exception
{
    public LodestoneLookupException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
