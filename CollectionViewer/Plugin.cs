using System;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using CollectionViewer.Api;
using CollectionViewer.Services;
using CollectionViewer.Windows;

namespace CollectionViewer;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IContextMenu ContextMenuGui { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDtrBar DtrBar { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/pcollection";

    /// <summary>Single instance for the plugin's lifetime, so static-service-style code (e.g.
    /// <see cref="Loc"/>) can reach the loaded config without every call site threading it through.</summary>
    internal static Configuration Configuration { get; private set; } = null!;

    public FfxivCollectClient FfxivCollectClient { get; }
    public LodestoneResolver LodestoneResolver { get; }
    public CollectionService CollectionService { get; }
    public IconTextureCache IconTextureCache { get; }
    private ContextMenuService ContextMenuService { get; }

    public readonly WindowSystem WindowSystem = new("CollectionViewer");
    public CollectionWindow CollectionWindow { get; }
    public ConfigWindow ConfigWindow { get; }
    private readonly IDtrBarEntry dtrEntry;
    private readonly CommandInfo commandInfo;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        FfxivCollectClient = new FfxivCollectClient();
        LodestoneResolver = new LodestoneResolver();
        CollectionService = new CollectionService(FfxivCollectClient, Configuration, PluginInterface.ConfigDirectory.FullName);
        IconTextureCache = new IconTextureCache(FfxivCollectClient, TextureProvider, Log);

        CollectionWindow = new CollectionWindow(this);
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(CollectionWindow);
        WindowSystem.AddWindow(ConfigWindow);

        ContextMenuService = new ContextMenuService(this, ContextMenuGui, ChatGui, Log);

        commandInfo = new CommandInfo(OnCommand);
        CommandManager.AddHandler(CommandName, commandInfo);

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Dalamud has no public API to inject a button into the game's own bottom-of-screen
        // Main Command List (the native "Character" flyout) - that UI is closed, native game
        // UI with no plugin extension point. A server info bar (DTR) entry is the supported,
        // always-visible equivalent: one click opens "My collection" from anywhere in the game.
        dtrEntry = DtrBar.Get("Collection Viewer");
        dtrEntry.OnClick = _ => ToggleMainUi();

        ApplyLanguage();
    }

    /// <summary>Refreshes every piece of localized text that isn't redrawn every frame (command
    /// help, DTR bar entry) to match <see cref="Configuration.Language"/>. Call after the user
    /// changes the language in settings; window text updates on its own each Draw().</summary>
    public void ApplyLanguage()
    {
        var loc = Loc.Current;
        commandInfo.HelpMessage = loc.CommandHelp;
        dtrEntry.Text = loc.DtrText;
        dtrEntry.Tooltip = loc.DtrTooltip;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        dtrEntry.Remove();

        CommandManager.RemoveHandler(CommandName);

        WindowSystem.RemoveAllWindows();
        CollectionWindow.Dispose();
        ConfigWindow.Dispose();

        ContextMenuService.Dispose();
        IconTextureCache.Dispose();
        CollectionService.Dispose();
        LodestoneResolver.Dispose();
        FfxivCollectClient.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        if (args.Trim().Equals("config", StringComparison.OrdinalIgnoreCase))
        {
            ToggleConfigUi();
            return;
        }

        ToggleMainUi();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    private void ToggleMainUi()
    {
        if (Configuration.OwnCharacterId is { } id)
            CollectionWindow.OpenForCharacter(id, Loc.Current.MyCollectionLabel);
        else
            ConfigWindow.IsOpen = true;
    }
}
