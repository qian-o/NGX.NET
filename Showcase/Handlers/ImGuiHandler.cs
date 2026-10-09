using System.Globalization;
using System.Numerics;
using Hexa.NET.ImGui;
using Showcase.Models;

namespace Showcase.Handlers;

internal unsafe class ImGuiHandler : IDisposable
{
    private const float TextSize = 20;

    private static readonly Vector4 Accent = new(0.9f, 0.77f, 0.51f, 1);
    private static readonly QualityMode[] QualityModes = [QualityMode.Off, QualityMode.MaxQuality, QualityMode.Balanced, QualityMode.MaxPerformance, QualityMode.UltraPerformance];

    private readonly ImGuiContextPtr context;

    private float fontDensity;

    public ImGuiHandler()
    {
        context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();
        io.Handle->IniFilename = null;
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
        UpdateFont(Vector2.One);
    }

    public byte[] FontPixels { get; private set; } = [];

    public int FontWidth { get; private set; }

    public int FontHeight { get; private set; }

    // Rebuild before NewFrame; the renderer replaces the GPU atlas before drawing.
    public bool UpdateFont(Vector2 framebufferScale)
    {
        float density = framebufferScale.X;
        if (!float.IsFinite(density) || density <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(framebufferScale));
        }

        if (fontDensity == density)
        {
            return false;
        }

        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.Clear();
        ImFontConfigPtr config = ImGui.ImFontConfig();

        try
        {
            config.SizePixels = TextSize;
            config.RasterizerDensity = density;
            config.OversampleH = 2;
            config.OversampleV = 1;

            io.Fonts.AddFontFromFileTTF(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "msyh.ttf"), config.SizePixels, config.Handle, io.Fonts.GetGlyphRangesDefault());

            byte* pixels;
            int width;
            int height;
            int bytesPerPixel;
            io.Fonts.GetTexDataAsRGBA32(&pixels, &width, &height, &bytesPerPixel);
            FontWidth = width;
            FontHeight = height;
            FontPixels = new ReadOnlySpan<byte>(pixels, width * height * bytesPerPixel).ToArray();
        }
        finally
        {
            config.Destroy();
        }

        io.Fonts.SetTexID(new ImTextureID(1));
        fontDensity = density;

        return true;
    }

    public void Update(float delta, Vector2 size, Vector2 dpiScale, Renderer renderer)
    {
        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();
        // ImGui and Silk input share window coordinates; drawing scales to framebuffer pixels.
        io.DisplaySize = size / dpiScale;
        io.DisplayFramebufferScale = dpiScale;
        io.DeltaTime = Math.Max(delta, 1e-4f);
        ImGui.NewFrame();
        Build(renderer);
        ImGui.Render();
    }

    public void Dispose()
    {
        ImGui.DestroyContext(context);
    }

    private static void Build(Renderer renderer)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        Vector2 margin = new(12);
        ImGui.SetNextWindowPos(margin, ImGuiCond.FirstUseEver);
        Vector2 available = Vector2.Max(new(1), io.DisplaySize - (margin * 2));
        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, available);

        if (ImGui.Begin("Settings", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted(renderer.AdapterName);
            ImGui.TextUnformatted($"FPS: {Rate(renderer.PresentedFps)}");
            ImGui.Separator();
            Graphics(renderer);
            ImGui.Separator();
            ImGui.Checkbox("Pause Animation", ref renderer.AnimationPaused);
        }

        ImGui.End();
    }

    private static void Graphics(Renderer renderer)
    {
        ref RenderSettings settings = ref renderer.Settings;
        RenderCapabilities capabilities = renderer.Capabilities;
        ImGui.TextUnformatted("DLSS Super Resolution");

        // A content-sized window needs an explicit item width; using the remaining
        // window width here would feed its previous size back into auto-sizing.
        float previewWidth = QualityModes.Max(static mode => ImGui.CalcTextSize(QualityLabel(mode)).X);
        ImGui.SetNextItemWidth(previewWidth + ImGui.GetFrameHeight() + (ImGui.GetStyle().FramePadding.X * 2));
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

    private static string Rate(double? fps)
    {
        return fps?.ToString("F0", CultureInfo.InvariantCulture) ?? "--";
    }

    private static string QualityLabel(QualityMode mode)
    {
        return mode switch
        {
            QualityMode.Off => "Off",
            QualityMode.MaxQuality => "Quality",
            QualityMode.Balanced => "Balanced",
            QualityMode.MaxPerformance => "Performance",
            QualityMode.UltraPerformance => "Ultra Performance",
            _ => "Off"
        };
    }
}
