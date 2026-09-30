using System.Globalization;
using System.Numerics;
using ImGuiNET;
using Showcase.Models;

namespace Showcase.Handlers;

internal sealed unsafe class UserInterface : IDisposable
{
    private readonly nint context;
    private float uiScale = 1;
    private const float TextSize = 16;
    private const float AtlasTextSize = 32;
    private static readonly Vector4 Accent = new(0.9f, 0.77f, 0.51f, 1);
    private static readonly QualityMode[] QualityModes =
    [
        QualityMode.Off,
        QualityMode.MaxQuality,
        QualityMode.Balanced,
        QualityMode.MaxPerformance,
        QualityMode.UltraPerformance
    ];

    public byte[] FontPixels { get; }

    public int FontWidth { get; }

    public int FontHeight { get; }

    public UserInterface()
    {
        context = ImGui.CreateContext();
        ImGuiIOPtr io = ImGui.GetIO();
        io.NativePtr->IniFilename = null;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        ImGui.StyleColorsDark();
        ImGuiStylePtr style = ImGui.GetStyle();
        style.WindowRounding = 5;
        style.FrameRounding = 4;
        style.WindowPadding = new(12, 10);
        style.FramePadding = new(6, 3);
        style.ItemSpacing = new(6, 6);
        style.Colors[(int)ImGuiCol.WindowBg] = new(0.04f, 0.05f, 0.065f, 0.97f);
        style.Colors[(int)ImGuiCol.Border] = new(0.22f, 0.25f, 0.29f, 1);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new(0.13f, 0.16f, 0.20f, 1);
        style.Colors[(int)ImGuiCol.FrameBg] = new(0.1f, 0.13f, 0.16f, 1);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new(0.18f, 0.22f, 0.26f, 1);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new(0.22f, 0.26f, 0.30f, 1);
        style.Colors[(int)ImGuiCol.Header] = new(0.19f, 0.22f, 0.25f, 1);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new(0.27f, 0.30f, 0.33f, 1);
        style.Colors[(int)ImGuiCol.Button] = new(0.19f, 0.23f, 0.27f, 1);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new(0.28f, 0.33f, 0.37f, 1);
        style.Colors[(int)ImGuiCol.CheckMark] = Accent;
        string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string? fontPath = new[]
        {
            "segoeui.ttf",
            "arial.ttf"
        }.Select(name => Path.Combine(fonts, name)).FirstOrDefault(File.Exists);

        ImFontConfigPtr config = ImGuiNative.ImFontConfig_ImFontConfig();

        try
        {
            config.SizePixels = AtlasTextSize;
            config.OversampleH = 2;
            config.OversampleV = 1;

            if (fontPath is not null)
            {
                io.Fonts.AddFontFromFileTTF(fontPath, config.SizePixels, config, io.Fonts.GetGlyphRangesDefault());
            }
            else
            {
                io.Fonts.AddFontDefault(config);
            }

            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
            FontWidth = width;
            FontHeight = height;
            FontPixels = new ReadOnlySpan<byte>(pixels, width * height * bytesPerPixel).ToArray();
        }
        finally
        {
            config.Destroy();
        }

        io.Fonts.SetTexID(1);
        io.FontGlobalScale = TextSize / AtlasTextSize;
    }

    public void Build(RHI rhi, float delta)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new(rhi.Window.Width, rhi.Window.Height);
        io.DisplayFramebufferScale = Vector2.One;
        io.DeltaTime = Math.Max(delta, 1e-4f);
        float scale = rhi.Window.DpiScale;

        if (scale != uiScale)
        {
            ImGui.GetStyle().ScaleAllSizes(scale / uiScale);
            uiScale = scale;
        }

        io.FontGlobalScale = TextSize / AtlasTextSize * uiScale;
        ImGui.NewFrame();
        Vector2 margin = new Vector2(12) * uiScale;
        ImGui.SetNextWindowPos(margin, ImGuiCond.FirstUseEver);
        Vector2 available = Vector2.Max(new(1), io.DisplaySize - margin * 2);
        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, available);

        if (ImGui.Begin("Settings", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted(rhi.AdapterName);
            ImGui.TextUnformatted($"FPS: {Rate(rhi.PresentedFps)}");
            ImGui.Separator();
            Graphics(rhi);
            ImGui.Separator();
            ImGui.Checkbox("Pause Animation", ref rhi.AnimationPaused);
        }

        ImGui.End();
        ImGui.Render();
    }

    private void Graphics(RHI rhi)
    {
        RenderSettings settings = rhi.Settings;
        RenderCapabilities capabilities = rhi.Capabilities;
        ImGui.TextUnformatted("DLSS Super Resolution");

        // A content-sized window needs an explicit item width; using the remaining
        // window width here would feed its previous size back into auto-sizing.
        float previewWidth = QualityModes.Max(mode => ImGui.CalcTextSize(QualityLabel(mode)).X);
        ImGui.SetNextItemWidth(previewWidth + ImGui.GetFrameHeight() + ImGui.GetStyle().FramePadding.X * 2);
        ImGui.BeginDisabled(!capabilities.Dlss && !settings.RayReconstruction);

        if (ImGui.BeginCombo("##DLSS", QualityLabel(settings.Quality)))
        {
            foreach (QualityMode mode in QualityModes)
            {
                if (ImGui.Selectable(QualityLabel(mode), settings.Quality == mode))
                {
                    settings.Quality = mode;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.EndDisabled();
        ImGui.BeginDisabled(!capabilities.FrameGeneration);
        ImGui.Checkbox("DLSS Frame Generation", ref settings.FrameGeneration);
        ImGui.EndDisabled();
        ImGui.BeginDisabled(!capabilities.RayReconstruction);

        if (ImGui.Checkbox("DLSS Ray Reconstruction", ref settings.RayReconstruction) && !settings.RayReconstruction && !capabilities.Dlss)
        {
            settings.Quality = QualityMode.Off;
        }

        ImGui.EndDisabled();
    }

    private static string Rate(double? fps) => fps?.ToString("F0", CultureInfo.InvariantCulture) ?? "--";

    private static string QualityLabel(QualityMode mode) => mode switch
    {
        QualityMode.Off => "Off",
        QualityMode.MaxQuality => "Quality",
        QualityMode.Balanced => "Balanced",
        QualityMode.MaxPerformance => "Performance",
        QualityMode.UltraPerformance => "Ultra Performance",
        _ => "Off"
    };

    public void Dispose() => ImGui.DestroyContext(context);
}
