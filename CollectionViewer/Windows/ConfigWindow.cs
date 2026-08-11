using System;
using System.Numerics;
using System.Threading;
using CollectionViewer.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

namespace CollectionViewer.Windows;

/// <summary>Settings window: language, own character id, and cache TTL.</summary>
public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;

    private string ownIdText = string.Empty;

    private readonly AsyncOperation<int?> resolveOwnOp = new();
    private bool ownAutoApplied;
    private string? ownAutoMessage;

    public ConfigWindow(Plugin plugin)
        : base("Collection Viewer###CollectionViewerConfigWindow")
    {
        this.plugin = plugin;
        configuration = Plugin.Configuration;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 320),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        Size = new Vector2(520, 360);
        SizeCondition = ImGuiCond.FirstUseEver;

        ownIdText = configuration.OwnCharacterId?.ToString() ?? string.Empty;
    }

    public override void Draw()
    {
        var loc = Loc.Current;
        WindowName = $"{loc.ConfigWindowTitle}###CollectionViewerConfigWindow";

        resolveOwnOp.Poll();

        if (resolveOwnOp.HasResult && !ownAutoApplied)
        {
            ownAutoApplied = true;
            if (resolveOwnOp.Result is { } foundOwnId)
            {
                ownIdText = foundOwnId.ToString();
                configuration.OwnCharacterId = foundOwnId;
                configuration.Save();
            }
        }

        DrawLanguageSection(loc, plugin);
        ImGui.Separator();
        DrawOwnCharacterSection(loc);
        ImGui.Separator();
        DrawCacheSection(loc);
        ImGui.Separator();
        DrawAttribution(loc);
    }

    private static void DrawLanguageSection(LocStrings loc, Plugin plugin)
    {
        var configuration = Plugin.Configuration;
        var labels = new[] { "English", "Русский" };
        var currentIndex = (int)configuration.Language;

        ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo(loc.LanguageLabel, labels[currentIndex]))
        {
            if (combo.Success)
            {
                for (var i = 0; i < labels.Length; i++)
                {
                    var selected = i == currentIndex;
                    if (ImGui.Selectable(labels[i], selected))
                    {
                        configuration.Language = (PluginLanguage)i;
                        configuration.Save();
                        plugin.ApplyLanguage();
                    }

                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }
            }
        }
    }

    private void DrawOwnCharacterSection(LocStrings loc)
    {
        ImGui.TextUnformatted(loc.MyCharacterHeader);
        ImGui.TextWrapped(loc.MyCharacterExplanation);

        ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
        ImGui.InputText("FFXIV Collect / Lodestone ID##own", ref ownIdText, 16, ImGuiInputTextFlags.CharsDecimal);

        var parsed = int.TryParse(ownIdText, out var ownId) ? ownId : (int?)null;
        using (ImRaii.Disabled(parsed is null))
        {
            if (ImGui.Button(loc.Save))
            {
                configuration.OwnCharacterId = parsed;
                configuration.Save();
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(configuration.OwnCharacterId is null))
        {
            if (ImGui.Button(loc.OpenMyCollection))
                plugin.CollectionWindow.OpenForCharacter(configuration.OwnCharacterId!.Value, loc.MyCollectionLabel);
        }

        using (ImRaii.Disabled(resolveOwnOp.IsLoading || !Plugin.PlayerState.IsLoaded))
        {
            if (ImGui.Button(loc.DetectAutomatically))
            {
                ownAutoMessage = null;
                var name = Plugin.PlayerState.CharacterName;
                var world = Plugin.PlayerState.HomeWorld.ValueNullable?.Name.ToString();
                if (string.IsNullOrEmpty(world))
                {
                    ownAutoMessage = loc.CouldNotDetermineHomeWorld;
                }
                else
                {
                    ownAutoApplied = false;
                    resolveOwnOp.Start(() => plugin.LodestoneResolver.ResolveLodestoneIdAsync(name, world, CancellationToken.None));
                }
            }
        }

        if (!Plugin.PlayerState.IsLoaded)
        {
            ImGui.TextDisabled(loc.UnavailableNotIngame);
        }
        else if (resolveOwnOp.IsLoading)
        {
            ImGui.TextDisabled(loc.SearchingLodestone);
        }
        else if (resolveOwnOp.Error is { } ownErr)
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), ErrorMessages.Describe(ownErr));
        }
        else if (resolveOwnOp.HasResult)
        {
            ImGui.TextColored(resolveOwnOp.Result is not null ? new Vector4(0.6f, 1f, 0.6f, 1f) : new Vector4(1f, 0.7f, 0.3f, 1f),
                resolveOwnOp.Result is { } foundId
                    ? loc.IdFoundAndSaved(foundId)
                    : loc.PersonNotFoundOnLodestone);
        }
        else if (ownAutoMessage != null)
        {
            ImGui.TextColored(new Vector4(1f, 0.7f, 0.3f, 1f), ownAutoMessage);
        }
    }

    private void DrawCacheSection(LocStrings loc)
    {
        ImGui.TextUnformatted(loc.CacheHeader);
        var ttl = configuration.CacheTtlMinutes;
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderInt(loc.CacheTtlLabel, ref ttl, 5, 180))
        {
            configuration.CacheTtlMinutes = ttl;
            configuration.Save();
        }
    }

    private static void DrawAttribution(LocStrings loc)
    {
        ImGui.TextDisabled(loc.Attribution);
        if (ImGui.IsItemClicked())
            Util.OpenLink("https://ffxivcollect.com");
    }

    public void Dispose()
    {
    }
}
