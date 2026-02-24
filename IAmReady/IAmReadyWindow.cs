using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace IAmReady;

public sealed class IAmReadyWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public IAmReadyWindow(Plugin plugin)
        : base($"I Am Ready v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}###IAmReady")
    {
        this.plugin = plugin;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(290, 240),
            MaximumSize = new Vector2(400, 1024),
        };
        Size = new Vector2(320, 300);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        WindowName = $"I Am Ready v{ver}###IAmReady";

        var viewport = ImGui.GetMainViewport();
        var center = viewport.WorkPos + viewport.WorkSize * 0.5f;
        ImGui.SetNextWindowPos(center - (Size ?? new Vector2(320, 300)) * 0.5f, ImGuiCond.FirstUseEver);
    }

    public override void Draw()
    {
        var config = plugin.Configuration;

        // --- Kopfzeile: Active/Inactive Toggle (links) + Sprach-Buttons (rechts) ---
        if (config.IsActive)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        else
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));

        if (ImGui.Button(config.IsActive ? Lang.T("active") : Lang.T("inactive"), new Vector2(100, 0)))
        {
            config.IsActive = !config.IsActive;
            config.Save();
        }
        ImGui.PopStyleColor();

        // Sprach-Buttons rechts
        var btnWidthDE = ImGui.CalcTextSize("DE").X + ImGui.GetStyle().FramePadding.X * 2;
        var btnWidthEN = ImGui.CalcTextSize("EN").X + ImGui.GetStyle().FramePadding.X * 2;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var buttonsWidth = btnWidthEN + spacing + btnWidthDE;

        ImGui.SameLine(ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() - buttonsWidth);

        // EN button
        if (config.CurrentLang == 0)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 1.0f));
        else
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
        if (ImGui.SmallButton("EN"))
        {
            config.CurrentLang = 0;
            Lang.Current = Language.EN;
            config.Save();
        }
        ImGui.PopStyleColor();

        ImGui.SameLine();

        // DE button
        if (config.CurrentLang == 1)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 1.0f));
        else
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
        if (ImGui.SmallButton("DE"))
        {
            config.CurrentLang = 1;
            Lang.Current = Language.DE;
            config.Save();
        }
        ImGui.PopStyleColor();

        // --- Separator ---
        ImGui.Separator();

        // --- Start-Button (volle Breite, Höhe 40) ---
        if (config.IsActive)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 1.0f));
        else
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));

        if (ImGui.Button(Lang.T("start"), new Vector2(-1, 40)))
        {
            Plugin.Counter = 0;
        }
        ImGui.PopStyleColor();

        // Easter Egg
        if (Plugin.LocalPlayerName == "Amystra Hanako")
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.7f, 1.0f));
            ImGui.Text(FontAwesomeIcon.Heart.ToIconString());
            ImGui.PopStyleColor();
            ImGui.PopFont();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Du bist die Beste");
        }

        // --- YesCount Slider ---
        var yesCount = config.YesCount;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.SliderInt(Lang.T("yes_count"), ref yesCount, 0, 16))
        {
            config.YesCount = yesCount;
            config.Save();
        }

        // --- Fortschrittsbalken ---
        var counter = Plugin.Counter;
        var max = config.YesCount;
        float fraction;
        bool isNoPhase;

        if (max == 0)
        {
            fraction = 1.0f;
            isNoPhase = true;
        }
        else
        {
            fraction = Math.Clamp((float)counter / max, 0f, 1f);
            isNoPhase = counter >= max;
        }

        if (isNoPhase)
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
        else
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));

        var overlayText = isNoPhase
            ? $"{Lang.T("no")} ({counter}/{max})"
            : $"{Lang.T("yes")} ({counter}/{max})";
        ImGui.ProgressBar(fraction, new Vector2(-1, 24), overlayText);
        ImGui.PopStyleColor();

        // --- Separator ---
        ImGui.Separator();

        // --- Regex-Sektion (einklappbar) ---
        if (ImGui.CollapsingHeader(Lang.T("regex_patterns")))
        {
            var iconBtnWidth = ImGui.GetFrameHeight();
            var inputWidth = ImGui.GetContentRegionAvail().X - (iconBtnWidth * 2 + spacing * 2);

            for (var i = 0; i < config.RegexPatterns.Count; i++)
            {
                var pattern = config.RegexPatterns[i];
                ImGui.SetNextItemWidth(inputWidth);
                if (ImGui.InputText($"##pattern{i}", ref pattern, 256))
                {
                    config.RegexPatterns[i] = pattern;
                    config.Save();
                }

                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Copy.ToIconString()}##copy{i}", new Vector2(iconBtnWidth, 0)))
                {
                    config.RegexPatterns.Insert(i + 1, config.RegexPatterns[i]);
                    config.Save();
                }
                ImGui.PopFont();

                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Times.ToIconString()}##rm{i}", new Vector2(iconBtnWidth, 0)))
                {
                    config.RegexPatterns.RemoveAt(i);
                    config.Save();
                    i--;
                }
                ImGui.PopFont();
            }

            if (ImGui.SmallButton(Lang.T("add_regex")))
            {
                config.RegexPatterns.Add(string.Empty);
                config.Save();
            }
        }
    }
}
