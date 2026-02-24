using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace IAmReady;

public sealed class IAmReadyWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string newPattern = string.Empty;

    public IAmReadyWindow(Plugin plugin)
        : base($"I Am Ready v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}###IAmReady")
    {
        this.plugin = plugin;
        Size = new Vector2(320, 280);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        WindowName = $"I Am Ready v{ver}###IAmReady";

        var viewport = ImGui.GetMainViewport();
        var center = viewport.WorkPos + viewport.WorkSize * 0.5f;
        ImGui.SetNextWindowPos(center - (Size ?? new Vector2(320, 280)) * 0.5f, ImGuiCond.FirstUseEver);
    }

    public override void Draw()
    {
        var config = plugin.Configuration;

        var languages = new[] { "English", "Deutsch" };
        var langIdx = config.CurrentLang;
        ImGui.SetNextItemWidth(120);
        if (ImGui.Combo("##lang", ref langIdx, languages, languages.Length))
        {
            config.CurrentLang = langIdx;
            Lang.Current = (Language)langIdx;
            config.Save();
        }

        ImGui.SameLine();

        if (config.IsActive)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        else
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));

        if (ImGui.Button(config.IsActive ? Lang.T("active") : Lang.T("inactive"), new Vector2(100, 30)))
        {
            config.IsActive = !config.IsActive;
            config.Save();
        }
        ImGui.PopStyleColor();

        ImGui.Separator();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 1.0f));
        if (ImGui.Button(Lang.T("start"), new Vector2(-1, 40)))
        {
            Plugin.Counter = 0;
        }
        ImGui.PopStyleColor();

        var yesCount = config.YesCount;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputInt(Lang.T("yes_count"), ref yesCount))
        {
            config.YesCount = Math.Clamp(yesCount, 1, 999);
            config.Save();
        }

        var counter = Plugin.Counter;
        var max = config.YesCount;
        var fraction = Math.Clamp((float)counter / max, 0f, 1f);
        var isNoPhase = counter >= max;

        if (isNoPhase)
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
        else
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));

        var overlayText = isNoPhase
            ? $"{Lang.T("no")} ({counter}/{max})"
            : $"{Lang.T("yes")} ({counter}/{max})";
        ImGui.ProgressBar(fraction, new Vector2(-1, 24), overlayText);
        ImGui.PopStyleColor();

        ImGui.Separator();

        ImGui.Text(Lang.T("regex_patterns"));

        for (var i = 0; i < config.RegexPatterns.Count; i++)
        {
            var pattern = config.RegexPatterns[i];
            ImGui.SetNextItemWidth(-80);
            if (ImGui.InputText($"##pattern{i}", ref pattern, 256))
            {
                config.RegexPatterns[i] = pattern;
                config.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button($"{Lang.T("remove")}##rm{i}"))
            {
                config.RegexPatterns.RemoveAt(i);
                config.Save();
                break;
            }
        }

        ImGui.SetNextItemWidth(-80);
        ImGui.InputText("##newpattern", ref newPattern, 256);
        ImGui.SameLine();
        if (ImGui.Button(Lang.T("add"), new Vector2(-1, 0)))
        {
            if (!string.IsNullOrWhiteSpace(newPattern))
            {
                config.RegexPatterns.Add(newPattern);
                newPattern = string.Empty;
                config.Save();
            }
        }
    }
}
