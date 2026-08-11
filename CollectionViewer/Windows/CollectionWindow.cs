using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using CollectionViewer.Api.Models;
using CollectionViewer.Data;
using CollectionViewer.Services;
using CollectionViewer.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

namespace CollectionViewer.Windows;

/// <summary>How the item list within a category tab is filtered by ownership.</summary>
internal enum ItemFilterMode
{
    All,
    OwnedOnly,
    MissingOnly,
}

/// <summary>How the item list within a category tab is filtered by marketability.</summary>
internal enum MarketFilterMode
{
    All,
    TradeableOnly,
    NonTradeableOnly,
}

/// <summary>How the (already filtered) item list is ordered. Owned items are always listed before
/// missing ones regardless of this setting - it only controls the order within each group.</summary>
internal enum ItemSortMode
{
    Name,
    PriceAscending,
    PriceDescending,
}

/// <summary>Per-tab UI state: its own async load, search box text and filter/sort modes.</summary>
internal sealed class CategoryTabState
{
    public readonly AsyncOperation<CategorySnapshot> Op = new();
    public string SearchText = string.Empty;
    public ItemFilterMode Filter = ItemFilterMode.All;
    public MarketFilterMode MarketFilter = MarketFilterMode.All;
    public ItemSortMode Sort = ItemSortMode.Name;

    /// <summary>Gil range applied to items with a market price (0 = no bound on that side).
    /// Items without a market listing (owned items, or non-tradeable ones) are excluded whenever
    /// either bound is active, since there is nothing to compare.</summary>
    public int MinPrice;
    public int MaxPrice;
}

/// <summary>
/// The single, reusable collection viewer window. Used both for "Моя коллекция" and for any
/// friend/looked-up character - callers just call <see cref="OpenForCharacter"/> with a different
/// id, per the ТЗ requirement that friends open "the same window/viewer".
/// </summary>
public sealed class CollectionWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly AsyncOperation<CharacterSnapshot> characterOp = new();
    private readonly Dictionary<string, CategoryTabState> categoryStates = new();

    private int? characterId;
    private string windowLabel = string.Empty;

    public CollectionWindow(Plugin plugin)
        : base("Collection Viewer###CollectionViewerMainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 420),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        Size = new Vector2(620, 560);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    /// <summary>Opens the window targeting the given FFXIV Collect / Lodestone character id.
    /// Cached data for the same id is kept; switching to a different id resets all tab state.</summary>
    public void OpenForCharacter(int newCharacterId, string label)
    {
        if (characterId != newCharacterId)
        {
            characterId = newCharacterId;
            categoryStates.Clear();
            characterOp.Reset();
        }

        windowLabel = label;
        IsOpen = true;
        if (!characterOp.IsLoading && !characterOp.HasResult)
            TriggerCharacterLoad(force: false);
    }

    public override void Draw()
    {
        var loc = Loc.Current;
        WindowName = $"{loc.CollectionWindowTitle}###CollectionViewerMainWindow";

        characterOp.Poll();
        foreach (var state in categoryStates.Values)
            state.Op.Poll();

        if (characterId is not { } id)
        {
            ImGui.TextDisabled(loc.CharacterNotSelected);
            return;
        }

        DrawHeader(id, loc);
        ImGui.Separator();

        if (characterOp.IsLoading)
        {
            ImGui.TextDisabled(loc.LoadingCharacterData);
        }
        else if (characterOp.Error is { } charError)
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), ErrorMessages.Describe(charError));
            if (ImGui.Button(loc.Retry))
                TriggerCharacterLoad(force: true);
        }
        else if (characterOp.Result is { } snapshot)
        {
            var summary = snapshot.Summary;
            ImGui.TextUnformatted($"{summary.Name} @ {summary.Server} ({summary.DataCenter})");
            if (!summary.Verified)
            {
                ImGui.SameLine();
                ImGui.TextDisabled(loc.NotVerified);
            }

            using (var child = ImRaii.Child("CollectionTabsChild", new Vector2(0, -28 * ImGuiHelpers.GlobalScale), false))
            {
                if (child.Success)
                    DrawTabs(id, summary, loc);
            }
        }

        DrawFooter(loc);
    }

    private void DrawHeader(int id, LocStrings loc)
    {
        ImGui.TextUnformatted(windowLabel);
        ImGui.SameLine();
        if (ImGui.SmallButton(loc.Refresh))
            RefreshAll();
        ImGui.SameLine();
        ImGui.TextDisabled(loc.IdLabel(id));
    }

    private void DrawTabs(int id, CharacterSummaryDto summary, LocStrings loc)
    {
        using var tabBar = ImRaii.TabBar("CollectionCategoryTabs");
        if (!tabBar.Success)
            return;

        foreach (var category in CollectionCategories.All)
        {
            using var tab = ImRaii.TabItem($"{loc.CategoryName(category.ApiSegment)}###tab_{category.ApiSegment}");
            if (!tab.Success)
                continue;

            DrawCategoryTab(id, summary, category, loc);
        }
    }

    private void DrawCategoryTab(int id, CharacterSummaryDto summary, CollectionCategoryDefinition category, LocStrings loc)
    {
        var counts = GetCounts(summary, category.ApiSegment);
        if (counts?.Public == false)
        {
            ImGuiHelpers.ScaledDummy(4);
            ImGui.TextColored(new Vector4(1f, 0.7f, 0.3f, 1f), loc.CollectionHiddenPrivate);
            return;
        }

        if (!categoryStates.TryGetValue(category.ApiSegment, out var state))
        {
            state = new CategoryTabState();
            categoryStates[category.ApiSegment] = state;
        }

        if (!state.Op.IsLoading && !state.Op.HasResult && state.Op.Error == null)
            TriggerCategoryLoad(id, category.ApiSegment, force: false);

        if (state.Op.IsLoading)
        {
            ImGuiHelpers.ScaledDummy(4);
            ImGui.TextDisabled(loc.Loading);
            return;
        }

        if (state.Op.Error is { } error)
        {
            ImGuiHelpers.ScaledDummy(4);
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), ErrorMessages.Describe(error));
            if (ImGui.Button($"{loc.Retry}###retry_{category.ApiSegment}"))
                TriggerCategoryLoad(id, category.ApiSegment, force: true);
            return;
        }

        if (state.Op.Result is not { } data)
            return;

        var total = data.Owned.Count + data.Missing.Count;
        var progress = total == 0 ? 0f : (float)data.Owned.Count / total;
        ImGui.ProgressBar(progress, new Vector2(-1, 0), $"{data.Owned.Count} / {total} ({progress * 100:0.#}%)");

        ImGui.SetNextItemWidth(220 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint($"##search_{category.ApiSegment}", loc.SearchByNamePlaceholder, ref state.SearchText, 128);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180 * ImGuiHelpers.GlobalScale);
        DrawOwnedFilterCombo(category.ApiSegment, state, loc);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180 * ImGuiHelpers.GlobalScale);
        DrawMarketFilterCombo(category.ApiSegment, state, loc);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180 * ImGuiHelpers.GlobalScale);
        DrawSortCombo(category.ApiSegment, state, loc);

        DrawPriceRangeInputs(category.ApiSegment, state, loc);

        using var itemsChild = ImRaii.Child($"items_{category.ApiSegment}", Vector2.Zero, true);
        if (!itemsChild.Success)
            return;

        foreach (var (item, owned) in EnumerateFiltered(data, state))
            DrawItemRow(item, owned, loc);
    }

    private static void DrawOwnedFilterCombo(string segment, CategoryTabState state, LocStrings loc)
    {
        var labels = new[] { loc.FilterAll, loc.FilterOwnedOnly, loc.FilterMissingOnly };
        var currentIndex = (int)state.Filter;
        using var combo = ImRaii.Combo($"##ownedFilter_{segment}", labels[currentIndex]);
        if (!combo.Success)
            return;

        for (var i = 0; i < labels.Length; i++)
        {
            var selected = i == currentIndex;
            if (ImGui.Selectable(labels[i], selected))
                state.Filter = (ItemFilterMode)i;
            if (selected)
                ImGui.SetItemDefaultFocus();
        }
    }

    private static void DrawMarketFilterCombo(string segment, CategoryTabState state, LocStrings loc)
    {
        var labels = new[] { loc.MarketFilterAll, loc.MarketFilterTradeableOnly, loc.MarketFilterNonTradeableOnly };
        var currentIndex = (int)state.MarketFilter;
        using var combo = ImRaii.Combo($"##marketFilter_{segment}", labels[currentIndex]);
        if (!combo.Success)
            return;

        for (var i = 0; i < labels.Length; i++)
        {
            var selected = i == currentIndex;
            if (ImGui.Selectable(labels[i], selected))
                state.MarketFilter = (MarketFilterMode)i;
            if (selected)
                ImGui.SetItemDefaultFocus();
        }
    }

    private static void DrawSortCombo(string segment, CategoryTabState state, LocStrings loc)
    {
        var labels = new[] { loc.SortByName, loc.SortPriceAscending, loc.SortPriceDescending };
        var currentIndex = (int)state.Sort;
        using var combo = ImRaii.Combo($"##sort_{segment}", labels[currentIndex]);
        if (!combo.Success)
            return;

        for (var i = 0; i < labels.Length; i++)
        {
            var selected = i == currentIndex;
            if (ImGui.Selectable(labels[i], selected))
                state.Sort = (ItemSortMode)i;
            if (selected)
                ImGui.SetItemDefaultFocus();
        }
    }

    private static void DrawPriceRangeInputs(string segment, CategoryTabState state, LocStrings loc)
    {
        ImGui.TextUnformatted(loc.PriceFromLabel);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt($"##priceFrom_{segment}", ref state.MinPrice, 0);
        if (state.MinPrice < 0)
            state.MinPrice = 0;

        ImGui.SameLine();
        ImGui.TextUnformatted(loc.PriceToLabel);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt($"##priceTo_{segment}", ref state.MaxPrice, 0);
        if (state.MaxPrice < 0)
            state.MaxPrice = 0;
    }

    private static IEnumerable<(CollectionItemDto Item, bool Owned)> EnumerateFiltered(CategorySnapshot data, CategoryTabState state)
    {
        IEnumerable<(CollectionItemDto, bool)> items = state.Filter switch
        {
            ItemFilterMode.OwnedOnly => data.Owned.Select(i => (i, true)),
            ItemFilterMode.MissingOnly => data.Missing.Select(i => (i, false)),
            _ => data.Owned.Select(i => (i, true)).Concat(data.Missing.Select(i => (i, false))),
        };

        items = state.MarketFilter switch
        {
            MarketFilterMode.TradeableOnly => items.Where(pair => pair.Item1.Tradeable == true),
            MarketFilterMode.NonTradeableOnly => items.Where(pair => pair.Item1.Tradeable != true),
            _ => items,
        };

        if (state.MinPrice > 0 || state.MaxPrice > 0)
        {
            items = items.Where(pair =>
                pair.Item1.Market is { } market &&
                (state.MinPrice <= 0 || market.Price >= state.MinPrice) &&
                (state.MaxPrice <= 0 || market.Price <= state.MaxPrice));
        }

        if (!string.IsNullOrWhiteSpace(state.SearchText))
        {
            items = items.Where(pair =>
                pair.Item1.Name.Contains(state.SearchText, StringComparison.OrdinalIgnoreCase));
        }

        // Owned items are always grouped first; within each group, order by the chosen sort mode.
        // For price sorting, items with no market price (owned items, non-tradeable ones) sort last
        // in both directions rather than clustering at whichever end MinValue/MaxValue would imply.
        var grouped = items.OrderByDescending(pair => pair.Item2);
        return state.Sort switch
        {
            ItemSortMode.PriceAscending => grouped
                .ThenBy(pair => pair.Item1.Market is null)
                .ThenBy(pair => pair.Item1.Market?.Price ?? long.MaxValue),
            ItemSortMode.PriceDescending => grouped
                .ThenBy(pair => pair.Item1.Market is null)
                .ThenByDescending(pair => pair.Item1.Market?.Price ?? long.MinValue),
            _ => grouped.ThenBy(pair => pair.Item1.Name, StringComparer.OrdinalIgnoreCase),
        };
    }

    private void DrawItemRow(CollectionItemDto item, bool owned, LocStrings loc)
    {
        var iconSize = 32 * ImGuiHelpers.GlobalScale;
        var texture = plugin.IconTextureCache.RequestTexture(item.DisplayImageUrl);
        if (texture != null)
            ImGui.Image(texture.Handle, new Vector2(iconSize, iconSize));
        else
            ImGui.Dummy(new Vector2(iconSize, iconSize));

        ImGui.SameLine();
        bool nameHovered;
        using (ImRaii.PushColor(ImGuiCol.Text, owned ? new Vector4(0.6f, 1f, 0.6f, 1f) : new Vector4(0.75f, 0.75f, 0.75f, 1f)))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted((owned ? "[✓] " : "[ ] ") + item.Name);
            nameHovered = ImGui.IsItemHovered();
        }

        if (!owned && item.Market is { } market)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1f, 0.85f, 0.4f, 1f), loc.MarketPrice(market.Price, market.World));
        }

        if (nameHovered && !string.IsNullOrEmpty(item.Description))
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(400 * ImGuiHelpers.GlobalScale);
            ImGui.TextUnformatted(item.Description);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    private void DrawFooter(LocStrings loc)
    {
        ImGui.Separator();
        ImGui.TextDisabled(loc.Attribution);
        if (ImGui.IsItemClicked())
            Util.OpenLink("https://ffxivcollect.com");
    }

    private static CategoryCountsDto? GetCounts(CharacterSummaryDto summary, string segment) => segment switch
    {
        "mounts" => summary.Mounts,
        "minions" => summary.Minions,
        "orchestrions" => summary.Orchestrions,
        "emotes" => summary.Emotes,
        "hairstyles" => summary.Hairstyles,
        "bardings" => summary.Bardings,
        _ => null,
    };

    private void TriggerCharacterLoad(bool force)
    {
        if (characterId is not { } id)
            return;
        characterOp.Start(() => plugin.CollectionService.GetCharacterAsync(id, force, CancellationToken.None));
    }

    private void TriggerCategoryLoad(int id, string segment, bool force)
    {
        var state = categoryStates[segment];
        state.Op.Start(() => plugin.CollectionService.GetCategoryAsync(id, segment, force, CancellationToken.None));
    }

    private void RefreshAll()
    {
        TriggerCharacterLoad(force: true);
        foreach (var (segment, state) in categoryStates)
        {
            if (state.Op.HasResult || state.Op.Error != null)
                TriggerCategoryLoad(characterId!.Value, segment, force: true);
        }
    }

    public void Dispose()
    {
    }
}
