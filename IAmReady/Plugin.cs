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
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContentsFinderConfirm", OnDutyConfirm);

        Log.Information("IAmReady loaded.");
    }

    public void Dispose()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "ContentsFinderConfirm", OnDutyConfirm);
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

    private unsafe void OnDutyConfirm(AddonEvent type, AddonArgs args)
    {
        if (!Configuration.IsActive)
            return;

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null)
            return;

        if (Counter < Configuration.YesCount)
        {
            addon->FireCallbackInt(8);
            Counter++;
            Log.Information($"Duty confirm: YES ({Counter}/{Configuration.YesCount})");
        }
        else
        {
            addon->FireCallbackInt(9);
            Log.Information($"Duty confirm: NO ({Counter}/{Configuration.YesCount})");
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
