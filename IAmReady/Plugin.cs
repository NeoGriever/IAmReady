using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Game.Chat;
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

    internal DebugLogWindow? DebugLogWindow;
    private bool debugLoggerActive;

    internal bool DebugLoggerActive
    {
        get => debugLoggerActive;
        set
        {
            if (value == debugLoggerActive) return;
            debugLoggerActive = value;
            if (value) EnableDebugLogger(); else DisableDebugLogger();
        }
    }

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
        DisableDebugLogger();
        DebugLogWindow?.Dispose();
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

    private void EnableDebugLogger()
    {
        if (DebugLogWindow == null)
        {
            DebugLogWindow = new DebugLogWindow();
            windowSystem.AddWindow(DebugLogWindow);
        }
        DebugLogWindow.IsOpen = true;
        DebugLogWindow.OnCloseCallback = () =>
        {
            debugLoggerActive = false;
            AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, OnDebugAddonSetup);
        };
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, OnDebugAddonSetup);
    }

    private void DisableDebugLogger()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, OnDebugAddonSetup);
        if (DebugLogWindow != null)
            DebugLogWindow.IsOpen = false;
    }

    private unsafe void OnDebugAddonSetup(AddonEvent type, AddonArgs args)
    {
        try
        {
            var addon = (AtkUnitBase*)args.Addon.Address;
            var setupArgs = (AddonSetupArgs)args;
            var entry = FormatAddonInfo(addon, setupArgs);
            DebugLogWindow?.AddEntry(entry);
        }
        catch (Exception ex)
        {
            DebugLogWindow?.AddEntry($"[{DateTime.Now:HH:mm:ss.fff}] Error: {ex.Message}");
        }
    }

    private unsafe string FormatAddonInfo(AtkUnitBase* addon, AddonSetupArgs setupArgs)
    {
        var sb = new StringBuilder();
        var time = DateTime.Now.ToString("HH:mm:ss.fff");
        var addonName = Encoding.UTF8.GetString(addon->Name).TrimEnd('\0');

        sb.AppendLine($"[{time}] Addon: {addonName} (Id: {addon->Id})");

        // AtkValues
        var valueCount = setupArgs.AtkValueCount;
        var values = (AtkValue*)setupArgs.AtkValues;
        sb.AppendLine($"  === AtkValues (Count: {valueCount}) ===");

        for (var i = 0; i < valueCount; i++)
        {
            try
            {
                var v = &values[i];
                var typeStr = v->Type.ToString();
                string valStr;

                switch (v->Type)
                {
                    case AtkValueType.String:
                    case AtkValueType.String8:
                        valStr = $"\"{v->String}\"";
                        break;
                    case AtkValueType.Int:
                        valStr = v->Int.ToString();
                        break;
                    case AtkValueType.UInt:
                        valStr = v->UInt.ToString();
                        break;
                    case AtkValueType.Bool:
                        valStr = (v->Int != 0).ToString();
                        break;
                    case AtkValueType.Float:
                        valStr = v->Float.ToString("F2");
                        break;
                    default:
                        valStr = $"(raw: {v->Int})";
                        break;
                }

                sb.AppendLine($"  [{i}] Type={typeStr,-16} Value={valStr}");
            }
            catch
            {
                sb.AppendLine($"  [{i}] (error reading value)");
            }
        }

        // Nodes
        sb.AppendLine($"  === Nodes (Count: {addon->UldManager.NodeListCount}) ===");
        for (var i = 0; i < addon->UldManager.NodeListCount; i++)
        {
            try
            {
                var node = addon->UldManager.NodeList[i];
                if (node == null) continue;

                var nodeType = node->Type.ToString();
                var visible = node->IsVisible();
                var line = $"  [{i}] NodeId={node->NodeId,-5} Type={nodeType,-12} Visible={visible}";

                if (node->Type == NodeType.Text)
                {
                    var textNode = (AtkTextNode*)node;
                    var text = textNode->NodeText.ToString();
                    if (!string.IsNullOrEmpty(text))
                        line += $"  Text=\"{text}\"";
                }

                sb.AppendLine(line);
            }
            catch
            {
                sb.AppendLine($"  [{i}] (error reading node)");
            }
        }

        return sb.ToString();
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        var type = (XivChatType)(int)message.LogKind;
        if (type != XivChatType.Party && type != XivChatType.CrossParty)
            return;

        var text = message.Message.TextValue;

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
