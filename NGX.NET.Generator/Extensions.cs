using System.Text.Json;

namespace NGX.NET.Generator;

internal static class Extensions
{
    extension(JsonElement element)
    {
        internal string Text(string name)
        {
            return element.GetProperty(name).GetString()!;
        }

        internal int Number(string name)
        {
            return element.GetProperty(name).GetInt32();
        }

        internal JsonElement.ArrayEnumerator Items(string name)
        {
            return element.GetProperty(name).EnumerateArray();
        }
    }
}
