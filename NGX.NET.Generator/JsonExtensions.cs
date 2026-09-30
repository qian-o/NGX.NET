using System.Text.Json;

namespace NGX.NET.Generator;

internal static class JsonExtensions
{
    public static string Text(this JsonElement element, string name) => element.GetProperty(name).GetString()!;

    public static int Number(this JsonElement element, string name) => element.GetProperty(name).GetInt32();

    public static JsonElement.ArrayEnumerator Items(this JsonElement element, string name) => element.GetProperty(name).EnumerateArray();
}
