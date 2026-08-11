using System.Collections.Generic;

namespace CollectionViewer.Data;

/// <summary>Describes one collection category exposed by /characters/{id}/{category}/{owned|missing}.</summary>
/// <param name="ApiSegment">Path segment used by the FFXIV Collect API. Also used to look up the
/// localized tab label via <see cref="LocStrings.CategoryName"/>.</param>
public sealed record CollectionCategoryDefinition(string ApiSegment);

/// <summary>
/// The set of collection categories this plugin displays. New categories can be added here
/// without touching the client, cache, or UI code, as long as the API exposes the same
/// /owned and /missing routes and the item shape matches <see cref="Api.Models.CollectionItemDto"/>
/// (and both language packs in Localization.cs gain a matching CategoryXxx string).
/// </summary>
public static class CollectionCategories
{
    public static readonly IReadOnlyList<CollectionCategoryDefinition> All = new[]
    {
        new CollectionCategoryDefinition("mounts"),
        new CollectionCategoryDefinition("minions"),
        new CollectionCategoryDefinition("orchestrions"),
        new CollectionCategoryDefinition("emotes"),
        new CollectionCategoryDefinition("hairstyles"),
        new CollectionCategoryDefinition("bardings"),
    };
}
