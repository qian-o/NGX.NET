using System.Globalization;
using System.Numerics;
using System.Text.Unicode;
using Hexa.NET.ImGui;
using NGX.NET;
using Showcase.Models;

namespace Showcase.Handlers;

internal unsafe class ImGuiHandler : DisposableObject
{
    private const float TextSize = 20.0f;

    private static readonly Vector4 Accent = new(0.9f, 0.77f, 0.51f, 1.0f);
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
        style.WindowRounding = 5.0f;
        style.FrameRounding = 4.0f;
        style.WindowPadding = new(12.0f, 10.0f);
        style.FramePadding = new(6.0f, 3.0f);
        style.ItemSpacing = new(6.0f, 6.0f);
        style.Colors[(int)ImGuiCol.WindowBg] = new(0.04f, 0.05f, 0.065f, 0.97f);
        style.Colors[(int)ImGuiCol.Border] = new(0.22f, 0.25f, 0.29f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new(0.13f, 0.16f, 0.2f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBg] = new(0.1f, 0.13f, 0.16f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new(0.18f, 0.22f, 0.26f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new(0.22f, 0.26f, 0.3f, 1.0f);
        style.Colors[(int)ImGuiCol.Header] = new(0.19f, 0.22f, 0.25f, 1.0f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new(0.27f, 0.3f, 0.33f, 1.0f);
        style.Colors[(int)ImGuiCol.Button] = new(0.19f, 0.23f, 0.27f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new(0.28f, 0.33f, 0.37f, 1.0f);
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
        if (!float.IsFinite(density) || density <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(framebufferScale));
        }

        if (fontDensity == density)
        {
            return false;
        }

        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();

        if (io.Fonts.TexData.Handle != null)
        {
            // Clear the retired atlas ID so ImGui can reclaim its texture data next frame.
            io.Fonts.TexData.SetTexID(default);
        }

        io.Fonts.Clear();
        ImFontConfigPtr config = ImGui.ImFontConfig();

        try
        {
            config.SizePixels = TextSize;
            config.RasterizerDensity = density;
            config.OversampleH = 2;
            config.OversampleV = 1;

            io.Fonts.AddFontFromFileTTF(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "msyh.ttf"), config.SizePixels, config.Handle, io.Fonts.GetGlyphRangesDefault());

            // Prebuild the fixed-size atlas because both renderers upload one font texture.
            io.Fonts.TexDesiredFormat = ImTextureFormat.Rgba32;
            ImGuiP.ImFontAtlasBuildMain(io.Fonts);
            ImTextureDataPtr texture = io.Fonts.TexData;
            FontWidth = texture.Width;
            FontHeight = texture.Height;
            FontPixels = new ReadOnlySpan<byte>(texture.GetPixels(), texture.GetSizeInBytes()).ToArray();
        }
        finally
        {
            config.Destroy();
        }

        io.Fonts.TexData.SetTexID(new ImTextureID(1));
        fontDensity = density;

        return true;
    }

    public void Update(float deltaSeconds, Vector2 size, Vector2 dpiScale, Renderer renderer)
    {
        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();
        // ImGui and Silk input share window coordinates; drawing scales to framebuffer pixels.
        io.DisplaySize = size / dpiScale;
        io.DisplayFramebufferScale = dpiScale;
        io.DeltaTime = Math.Max(deltaSeconds, 0.0001f);
        ImGui.NewFrame();
        Build(renderer);
        ImGui.Render();
    }

    protected override void Destroy()
    {
        if (context.Handle != null)
        {
            ImGui.DestroyContext(context);
        }
    }

    private static void Build(Renderer renderer)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        Vector2 margin = new(12.0f);
        ImGui.SetNextWindowPos(margin, ImGuiCond.FirstUseEver);
        Vector2 available = Vector2.Max(new(1.0f), io.DisplaySize - (margin * 2.0f));
        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, available);

        if (ImGui.Begin("Settings", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted(renderer.AdapterName);
            DrawRate(renderer.PresentedFps);
            ImGui.Separator();
            DrawGraphicsSettings(renderer);
            ImGui.Separator();
            ImGui.TextUnformatted("Daylight timeline");
            const float TimelineWidth = 260.0f;
            // Clock labels are a visual convention for the daylight arc.
            int minutes = 360 + (int)MathF.Round(renderer.Settings.DaylightProgress * 720.0f);
            ImGui.SetNextItemWidth(TimelineWidth);
            Span<byte> timeLabel = stackalloc byte["00:00\0".Length];
            Utf8.TryWrite(timeLabel, CultureInfo.InvariantCulture, $"{minutes / 60:00}:{minutes % 60:00}\0", out _);
            ImGui.SliderFloat("##Daylight"u8, ref renderer.Settings.DaylightProgress, 0.0f, 1.0f, timeLabel, ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput);
            float timelineStart = ImGui.GetCursorPosX();
            ImGui.TextUnformatted("Sunrise");
            ImGui.SameLine(timelineStart + ((TimelineWidth - ImGui.CalcTextSize("Noon").X) * 0.5f));
            ImGui.TextUnformatted("Noon");
            ImGui.SameLine(timelineStart + TimelineWidth - ImGui.CalcTextSize("Sunset").X);
            ImGui.TextUnformatted("Sunset");

            if (renderer.Settings.Reconstruction is Reconstruction.Native)
            {
                Span<byte> filmLabel = stackalloc byte["Film: 4,294,967,295 samples / pixel\0".Length];
                Utf8.TryWrite(filmLabel, CultureInfo.InvariantCulture, $"Film: {renderer.AccumulatedSamples:N0} samples / pixel\0", out int written);
                ImGui.TextUnformatted(filmLabel[..written]);
            }

            ImGui.Separator();
            ImGui.Checkbox("Pause Animation", ref renderer.IsAnimationPaused);
        }

        ImGui.End();
    }

    private static void DrawGraphicsSettings(Renderer renderer)
    {
        ref RenderSettings settings = ref renderer.Settings;
        RenderCapabilities capabilities = renderer.Capabilities;
        ImGui.TextUnformatted("DLSS Super Resolution");

        // A content-sized window needs an explicit item width; using the remaining window width here would feed its previous size back into auto-sizing.
        float previewWidth = 0.0f;
        foreach (QualityMode mode in QualityModes)
        {
            previewWidth = Math.Max(previewWidth, ImGui.CalcTextSize(GetQualityLabel(mode)).X);
        }

        ImGui.SetNextItemWidth(previewWidth + ImGui.GetFrameHeight() + (ImGui.GetStyle().FramePadding.X * 2.0f));
        ImGui.BeginDisabled(!capabilities.DlssSupported && !settings.IsRayReconstructionEnabled);

        if (ImGui.BeginCombo("##DLSS"u8, GetQualityLabel(settings.Quality)))
        {
            foreach (QualityMode mode in QualityModes)
            {
                if (ImGui.Selectable(GetQualityLabel(mode), settings.Quality == mode))
                {
                    settings.Quality = mode;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.EndDisabled();
        ImGui.BeginDisabled(!capabilities.FrameGenerationSupported);
        ImGui.Checkbox("DLSS Frame Generation", ref settings.IsFrameGenerationEnabled);
        ImGui.EndDisabled();
        ImGui.BeginDisabled(!capabilities.RayReconstructionSupported);

        if (ImGui.Checkbox("DLSS Ray Reconstruction", ref settings.IsRayReconstructionEnabled) && !settings.IsRayReconstructionEnabled && !capabilities.DlssSupported)
        {
            settings.Quality = QualityMode.Off;
        }

        ImGui.EndDisabled();
    }

    private static void DrawRate(double? fps)
    {
        Span<byte> label = stackalloc byte["FPS: 18446744073709551615\0".Length];
        if (fps is double value && Utf8.TryWrite(label, CultureInfo.InvariantCulture, $"FPS: {value:F0}\0", out int written))
        {
            ImGui.TextUnformatted(label[..written]);
        }
        else
        {
            ImGui.TextUnformatted("FPS: --"u8);
        }
    }

    private static ReadOnlySpan<byte> GetQualityLabel(QualityMode mode)
    {
        return mode switch
        {
            QualityMode.Off => "Off"u8,
            QualityMode.MaxQuality => "Quality"u8,
            QualityMode.Balanced => "Balanced"u8,
            QualityMode.MaxPerformance => "Performance"u8,
            QualityMode.UltraPerformance => "Ultra Performance"u8,
            _ => "Off"u8
        };
    }
}
