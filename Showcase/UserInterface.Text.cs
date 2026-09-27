namespace Showcase;

internal sealed unsafe partial class UserInterface
{
    private static readonly Dictionary<string, string> ChineseText = new(StringComparer.Ordinal)
    {
        ["Show"] = "显示",
        ["Render FPS"] = "渲染 FPS",
        ["Upscaling"] = "超分辨率",
        ["Frame generation"] = "插帧",
        ["Ray tracing"] = "光追",
        ["Off"] = "关闭",
        ["Quality"] = "质量",
        ["Balanced"] = "均衡",
        ["Performance"] = "性能",
        ["Ultra Performance"] = "超高性能"
    };

    private string T(string english) => chinese && ChineseText.TryGetValue(english, out string? translated) ? translated : english;
}
