using System;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace IAmReady;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    private const string CommandName = "/iar";

    public Config Configuration { get; }
    internal static int Counter = 0;
    internal static string LocalPlayerName => PlayerState.CharacterName ?? string.Empty;

    private readonly WindowSystem windowSystem = new("IAmReady");
    private readonly IAmReadyWindow mainWindow;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Config ?? new Config();

        Lang.Current = (Language)Configuration.CurrentLang;

        mainWindow = new IAmReadyWindow(this);
        windowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = "Open I Am Ready." });

        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += OnOpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += OnOpenMainUi;

        ChatGui.ChatMessage += OnChatMessage;

        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", OnSelectYesno);

        Log.Information("IAmReady loaded.");
    }

    public void Dispose()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectYesno", OnSelectYesno);
        ChatGui.ChatMessage -= OnChatMessage;
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= OnOpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= OnOpenMainUi;
        CommandManager.RemoveHandler(CommandName);
        windowSystem.RemoveAllWindows();
    }

    private void OnCommand(string command, string args)
    {
        mainWindow.IsOpen = true;
    }

    private void OnOpenMainUi()
    {
        mainWindow.IsOpen = true;
    }

    private unsafe void OnSelectYesno(AddonEvent type, AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;

        if (!Configuration.IsActive)
            return;

        // Ready Check SelectYesno has a visible "Wait" button with NodeId=11
        var isReadyCheck = false;
        for (var i = 0; i < addon->UldManager.NodeListCount; i++)
        {
            var node = addon->UldManager.NodeList[i];
            if (node != null && node->NodeId == 11 && node->IsVisible())
            {
                isReadyCheck = true;
                break;
            }
        }

        if (!isReadyCheck)
            return;

        if (addon == null)
            return;

        if (Counter < Configuration.YesCount)
        {
            var callbackIndex = 0; // Ja
            if (Configuration.UseNaturalDelay)
            {
                var addonAddr = (nint)addon;
                var delay = TimeSpan.FromMilliseconds(new Random().Next(700, 2501));
                Log.Information($"Ready check: YES ({Counter + 1}/{Configuration.YesCount}) — delayed by {delay.TotalMilliseconds:F0}ms");
                Framework.RunOnTick(() =>
                {
                    unsafe { ((AtkUnitBase*)addonAddr)->FireCallbackInt(callbackIndex); }
                }, delay);
                Counter++;
            }
            else
            {
                addon->FireCallbackInt(callbackIndex);
                Counter++;
                Log.Information($"Ready check: YES ({Counter}/{Configuration.YesCount})");
            }
        }
        else
        {
            var callbackIndex = 10; // Nein
            if (Configuration.UseNaturalDelay)
            {
                var addonAddr = (nint)addon;
                var delay = TimeSpan.FromMilliseconds(new Random().Next(700, 2501));
                Log.Information($"Ready check: NO ({Counter}/{Configuration.YesCount}) — delayed by {delay.TotalMilliseconds:F0}ms");
                Framework.RunOnTick(() =>
                {
                    unsafe { ((AtkUnitBase*)addonAddr)->FireCallbackInt(callbackIndex); }
                }, delay);
            }
            else
            {
                addon->FireCallbackInt(callbackIndex);
                Log.Information($"Ready check: NO ({Counter}/{Configuration.YesCount})");
            }
        }
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (type != XivChatType.Party && type != XivChatType.CrossParty)
            return;

        var text = message.TextValue;

        foreach (var pattern in Configuration.RegexPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            try
            {
                if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
                {
                    Counter = 0;
                    Log.Information($"Counter reset by chat pattern: {pattern}");
                    return;
                }
            }
            catch
            {
            }
        }
    }
}
