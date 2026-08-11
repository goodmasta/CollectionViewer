# Collection Viewer

A Dalamud plugin (Dalamud v15) for Final Fantasy XIV that shows a character's
[FFXIV Collect](https://ffxivcollect.com) collection — mounts, minions, orchestrions, emotes,
hairstyles, and bardings — right inside the game.

> ⚠️ **Requires internet access to FFXIV Collect.** This plugin has no offline mode — it fetches
> everything live from the FFXIV Collect API. Your game/network needs outbound HTTPS access to:
> - `ffxivcollect.com` — collection data (required for everything)
> - `v2.xivapi.com` — item icons
> - `na.finalfantasyxiv.com` / `eu.finalfantasyxiv.com` / `jp.finalfantasyxiv.com` — Lodestone
>   search, only used for right-click lookups and "Detect automatically"
>
> **How to check:** open `https://ffxivcollect.com/api/characters/1` in a browser, or run
> `curl https://ffxivcollect.com/api/characters/1`. You should get JSON back starting with
> `{"id":1,"name":"Macaroni Gratin",...}`. A timeout, connection error, or blocked/redirected page
> means a firewall, VPN, or network policy is blocking it — fix that first, since no plugin
> setting can work around it.

> **Built with AI.** This plugin's code, architecture, and documentation were generated with the
> help of an AI coding assistant (Claude Code / Anthropic Claude), based on a set of requirements
> and iterative feedback from the maintainer. Review it accordingly before trusting it in
> production.

🌐 Русская версия: [README.ru.md](README.ru.md)

## Interface languages

**English** (default) and **Russian**, switchable at any time in the settings window
(`/pcollection config` → Language). All windows, filters, error messages, and the context menu
entry update immediately when you switch.

## Features

- **My collection** — `/pcollection` opens the collection of the character ID saved in settings.
  A "Detect automatically" button in settings resolves it from your currently logged-in character.
- **View any player's collection** — right-click a player anywhere the game exposes its default
  context menu (chat, party/alliance list, target of target, etc.) → "View collection (FFXIV
  Collect)".
- **Six collection categories**: mounts, minions, orchestrions, emotes, hairstyles, bardings.
  Each shows owned/total progress with a percentage bar.
- **Item list per category** with icons, and for missing items, the cheapest current market price
  FFXIV Collect knows about (in gil).
- **Filters**, all combinable: owned / missing / all, tradeable / non-tradeable / all, and a gil
  price range (from / to) for tradeable items.
- **Sorting**: by name, cheapest first, or most expensive first. Owned items are always grouped
  before missing ones; the sort applies within each group.
- **Search** by item name within a category.
- **Caching** — responses are cached in memory and on disk with a configurable TTL (default 30
  minutes), so reopening a collection doesn't hit the API again until it's stale. A "Refresh"
  button forces a re-fetch.
- Footer attribution links back to ffxivcollect.com in every window (non-commercial personal
  project, not affiliated with FFXIV Collect).

## How it's built

```
CollectionViewer/
  Api/            FFXIV Collect HTTP client (FfxivCollectClient) and response DTOs
  Data/           Static reference data: collection categories, world → Lodestone region map
  Services/
    LodestoneResolver    resolves a character name + world to a Lodestone id (see below)
    CollectionService    in-memory + on-disk cache with TTL, sits between the UI and the API client
    IconTextureCache     downloads item icons and turns them into ImGui textures
    ContextMenuService   adds the right-click "View collection" menu entry
  Windows/
    CollectionWindow     the single, reusable viewer (own collection, or any looked-up character)
    ConfigWindow          settings: language, own character id, cache TTL
  Utility/          AsyncOperation<T> (bridges async fetches to ImGui's synchronous Draw loop),
                    error-message formatting
  Configuration.cs   IPluginConfiguration (language, own id, cache TTL)
  Localization.cs    all UI strings for both languages
  Plugin.cs           entry point: wires services together, registers the command, context menu,
                      and Server Info Bar entry
```

### How character lookup works

FFXIV Collect uses a character's **Lodestone character ID** as its own id — confirmed directly:
`ffxivcollect.com/api/characters/1` is the exact same character as
`lodestone/character/1/`. There's no separate "FFXIV Collect account number".

FFXIV Collect's public API has no endpoint to search by character name or world, and no endpoint
to look up a character by an arbitrary Lodestone ID search. The only way to turn "this player's
name and home world" into a Lodestone ID is the official Lodestone search page itself, which has
no JSON API — only HTML. So resolving a right-clicked player (or your own character, via
"Detect automatically") is a best-effort HTML scrape of that search page
(`Services/LodestoneResolver.cs`), matching on exact name + world. If the resulting ID isn't
registered on FFXIV Collect (or the profile/collection is private), that's reported as a clear
in-window message rather than failing silently.

Because it depends on Lodestone's page structure, this lookup is inherently best-effort — a
Lodestone layout change could require updating the parsing regex. The reliable path is entering
your own FFXIV Collect / Lodestone ID once in settings (it's just the number in your profile URL).

## Building

Requirements:
- .NET SDK (built and tested against the .NET 10 SDK; the project targets whatever
  `Dalamud.NET.Sdk/15.0.0` resolves to)
- A local Dalamud/XIVLauncher install for the SDK to find dev hooks
  (`%AppData%\XIVLauncher\addon\Hooks\dev\`) — not required in CI, see the release workflow.

```
dotnet build CollectionViewer.sln -c Release
```

`Dalamud.NET.Sdk` automatically packages the built DLL + manifest into
`CollectionViewer/bin/x64/Release/CollectionViewer/latest.zip` — no separate packaging step
needed.

## Installing

### As a custom repository (recommended for regular use)

1. In-game: `/xlsettings` → Experimental → Custom Plugin Repositories.
2. Add: `https://raw.githubusercontent.com/goodmasta/CollectionViewer/main/pluginmaster.json`
3. Save, then find "Collection Viewer" in `/xlplugins` and install it like any other plugin.

`.github/workflows/release.yml` rebuilds and republishes automatically on every push to `main`:
it bumps `pluginmaster.json`'s version/timestamp to match the `<Version>` in the `.csproj`, moves
a rolling `latest` tag, and updates the `latest` GitHub Release with the freshly packaged zip.
Bumping the version only requires editing `<Version>` in `CollectionViewer/CollectionViewer.csproj`
and pushing.

### As a dev plugin (for local development)

1. In-game: `/xlsettings` → Experimental → Dev Plugin Locations → add the path to
   `CollectionViewer/bin/x64/Debug` (or `Release`).
2. `/xlplugins` → Dev Tools → Installed Dev Plugins → enable Collection Viewer.

## Configuration

`/pcollection config` opens settings:

- **Language** — English or Russian.
- **My character** — your FFXIV Collect / Lodestone ID, entered manually or via "Detect
  automatically" (uses your currently logged-in character's name + home world).
- **Cache** — TTL in minutes for cached collection data.

## Known limitations

- Name/world lookup (right-click, auto-detect) depends on Lodestone's search page markup — an
  unofficial method that could break if the site changes.
- Icons are requested as PNG (not WebP) from the `v2.xivapi.com` proxy, to avoid depending on the
  OS having a WebP codec.
- The EU/JP world-to-region table in `Data/WorldRegions.cs` is a static list based on current data
  centers (Chaos/Light = EU, Elemental/Gaia/Mana/Meteor = JP); everything else, including
  Oceania/Materia (confirmed by direct query), routes through `na.finalfantasyxiv.com`.

## Credits

Collection data from [FFXIV Collect](https://ffxivcollect.com). This project is an independent,
non-commercial personal tool and is not affiliated with or endorsed by FFXIV Collect.
