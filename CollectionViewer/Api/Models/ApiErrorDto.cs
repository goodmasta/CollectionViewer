using System.Text.Json.Serialization;

namespace CollectionViewer.Api.Models;

/// <summary>Error body shape used by FFXIV Collect, e.g. {"status":404,"error":"Not found"}.</summary>
public sealed class ApiErrorDto
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
