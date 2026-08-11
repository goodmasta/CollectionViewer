using System;
using System.Collections.Generic;
using CollectionViewer.Api.Models;

namespace CollectionViewer.Services;

/// <summary>Cached character summary with the time it was fetched.</summary>
public sealed class CharacterSnapshot
{
    public CharacterSummaryDto Summary { get; set; } = null!;

    public DateTime FetchedAtUtc { get; set; }
}

/// <summary>Cached owned/missing item lists for one collection category.</summary>
public sealed class CategorySnapshot
{
    public string CategorySegment { get; set; } = string.Empty;

    public List<CollectionItemDto> Owned { get; set; } = new();

    public List<CollectionItemDto> Missing { get; set; } = new();

    public DateTime FetchedAtUtc { get; set; }
}

/// <summary>Everything cached for a single character, as persisted to a single disk cache file.</summary>
public sealed class CharacterCacheFile
{
    public CharacterSnapshot? Character { get; set; }

    public Dictionary<string, CategorySnapshot> Categories { get; set; } = new();
}
