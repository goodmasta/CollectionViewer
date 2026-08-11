using System.Text.Json.Serialization;

namespace CollectionViewer.Api.Models;

/// <summary>
/// One entry from a /characters/{id}/{collection}/owned or /missing list (also matches the
/// shape of the full catalog endpoints, e.g. /mounts). Fields not needed for display are
/// intentionally left out; System.Text.Json ignores unknown members by default.
/// </summary>
public sealed class CollectionItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("patch")]
    public string? Patch { get; set; }

    [JsonPropertyName("owned")]
    public string? OwnedPercent { get; set; }

    /// <summary>Nullable because FFXIV Collect sends JSON null (not just true/false) for some
    /// items, e.g. job-specific bardings and "Eternal Bonding" - confirmed by direct API query.</summary>
    [JsonPropertyName("tradeable")]
    public bool? Tradeable { get; set; }

    /// <summary>Cheapest current market listing found by FFXIV Collect for this item, if any.
    /// Only present on non-owned (missing) tradeable items.</summary>
    [JsonPropertyName("market")]
    public MarketDto? Market { get; set; }

    /// <summary>Preferred artwork for display: the big "image" if present, otherwise "icon".</summary>
    [JsonIgnore]
    public string? DisplayImageUrl => !string.IsNullOrEmpty(Image) ? Image : Icon;
}

/// <summary>Cheapest known market listing for an item, as embedded in owned/missing entries.</summary>
public sealed class MarketDto
{
    [JsonPropertyName("price")]
    public long Price { get; set; }

    [JsonPropertyName("world")]
    public string World { get; set; } = string.Empty;

    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; set; }
}
