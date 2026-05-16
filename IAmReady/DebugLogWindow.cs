using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace IAmReady;

public sealed class DebugLogWindow : Window, IDisposable
{
    private readonly List<string> entries = new();
    private readonly object entriesLock = new();
    private const int MaxEntries = 500;

    public Action? OnCloseCallback { get; set; }

    public DebugLogWindow()
        : base("Debug Logger###IAmReadyDebugLog")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(900, 1024),
        };
        Size = new Vector2(600, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void AddEntry(string entry)
    {
        lock (entriesLock)
        {
            entries.Add(entry);
            while (entries.Count > MaxEntries)
                entries.RemoveAt(0);
        }
    }

    public override void Draw()
    {
        lock (entriesLock)
        {
            if (ImGui.Button("Clear"))
                entries.Clear();

            ImGui.SameLine();
            ImGui.Text($"({entries.Count} entries)");

            if (ImGui.BeginChild("##debuglog_scroll"))
            {
                foreach (var entry in entries)
                    ImGui.TextUnformatted(entry);

                if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 10)
                    ImGui.SetScrollHereY(1.0f);
            }
            ImGui.EndChild();
        }
    }

    public override void OnClose()
    {
        OnCloseCallback?.Invoke();
    }

    public void Dispose() { }
}
