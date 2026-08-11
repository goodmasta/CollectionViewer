using System;
using System.Threading;
using System.Threading.Tasks;
using CollectionViewer.Utility;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;

namespace CollectionViewer.Services;

/// <summary>
/// Adds a "Посмотреть коллекцию (FFXIV Collect)" entry to the game's default right-click menu
/// (works in chat, party/alliance list, target of target, search info, etc. - anywhere Dalamud
/// exposes a <see cref="MenuTargetDefault"/> with a resolvable home world).
/// </summary>
/// <remarks>
/// There is no API that maps an arbitrary in-game character straight to an FFXIV Collect id, so
/// clicking the entry first resolves the player's Lodestone id from their name + home world via
/// <see cref="LodestoneResolver"/> (best-effort HTML scrape of the official Lodestone search),
/// then opens the collection window for that id - which will itself report "not found" if the
/// player never registered on FFXIV Collect, satisfying the ТЗ's required fallback behavior.
/// </remarks>
public sealed class ContextMenuService : IDisposable
{
    private readonly Plugin plugin;
    private readonly IContextMenu contextMenu;
    private readonly IChatGui chatGui;
    private readonly IPluginLog log;

    public ContextMenuService(Plugin plugin, IContextMenu contextMenu, IChatGui chatGui, IPluginLog log)
    {
        this.plugin = plugin;
        this.contextMenu = contextMenu;
        this.chatGui = chatGui;
        this.log = log;
        contextMenu.OnMenuOpened += OnMenuOpened;
    }

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (args.MenuType != ContextMenuType.Default)
            return;
        if (args.Target is not MenuTargetDefault target)
            return;
        if (string.IsNullOrEmpty(target.TargetName))
            return;
        if (!target.TargetHomeWorld.IsValid)
            return;

        var name = target.TargetName;
        var world = target.TargetHomeWorld.Value.Name.ToString();

        args.AddMenuItem(new MenuItem
        {
            Name = Loc.Current.ContextMenuViewCollection,
            OnClicked = _ => OnClicked(name, world),
        });
    }

    private void OnClicked(string name, string world)
    {
        chatGui.Print(Loc.Current.SearchingLodestoneFor(name, world), "Collection Viewer");
        _ = ResolveAndOpenAsync(name, world);
    }

    private async Task ResolveAndOpenAsync(string name, string world)
    {
        try
        {
            var lodestoneId = await plugin.LodestoneResolver.ResolveLodestoneIdAsync(name, world, CancellationToken.None).ConfigureAwait(false);
            if (lodestoneId is null)
            {
                chatGui.PrintError(Loc.Current.PlayerNotFoundOnLodestone(name, world), "Collection Viewer");
                return;
            }

            plugin.CollectionWindow.OpenForCharacter(lodestoneId.Value, $"{name} @ {world}");
        }
        catch (Exception ex)
        {
            log.Warning(ex, "[CollectionViewer] Lodestone lookup failed for {Name} @ {World}", name, world);
            chatGui.PrintError(ErrorMessages.Describe(ex), "Collection Viewer");
        }
    }

    public void Dispose() => contextMenu.OnMenuOpened -= OnMenuOpened;
}
