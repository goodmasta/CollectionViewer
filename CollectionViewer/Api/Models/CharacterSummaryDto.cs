using System.Text.Json.Serialization;

namespace CollectionViewer.Api.Models;

/// <summary>Response shape of GET /characters/{id}.</summary>
public sealed class CharacterSummaryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("server")]
    public string Server { get; set; } = string.Empty;

    [JsonPropertyName("data_center")]
    public string DataCenter { get; set; } = string.Empty;

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("portrait")]
    public string? Portrait { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    [JsonPropertyName("mounts")]
    public CategoryCountsDto? Mounts { get; set; }

    [JsonPropertyName("minions")]
    public CategoryCountsDto? Minions { get; set; }

    [JsonPropertyName("orchestrions")]
    public CategoryCountsDto? Orchestrions { get; set; }

    [JsonPropertyName("emotes")]
    public CategoryCountsDto? Emotes { get; set; }

    [JsonPropertyName("hairstyles")]
    public CategoryCountsDto? Hairstyles { get; set; }

    [JsonPropertyName("bardings")]
    public CategoryCountsDto? Bardings { get; set; }
}

/// <summary>Per-category count summary embedded in <see cref="CharacterSummaryDto"/>.</summary>
public sealed class CategoryCountsDto
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Whether the character has made this specific collection public. Null when the API omits the field (treated as public).</summary>
    [JsonPropertyName("public")]
    public bool? Public { get; set; }
}
