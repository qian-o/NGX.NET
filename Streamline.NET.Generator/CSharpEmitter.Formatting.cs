using System.Text;
using System.Text.RegularExpressions;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private static string FormatSource(string source)
    {
        source = Regex.Replace(source, @"(?m)^([ \t]*)/// <(summary|remarks|returns|exception)([^>\r\n]*)>(.+)</\2>$", match =>
            $"{match.Groups[1].Value}/// <{match.Groups[2].Value}{match.Groups[3].Value}>\n"
            + $"{match.Groups[1].Value}/// {match.Groups[4].Value.Trim()}\n"
            + $"{match.Groups[1].Value}/// </{match.Groups[2].Value}>");

        string[] lines = source.Split('\n');
        StringBuilder result = new();
        string previous = "";
        bool blank = false;

        foreach (string sourceLine in lines)
        {
            string line = sourceLine.TrimEnd();
            string statement = line.TrimStart();

            if (statement.Length == 0)
            {
                blank = true;
                continue;
            }

            bool closesBlock = statement is "}" or "};" or "];";
            bool continuesBlock = statement.StartsWith("else", StringComparison.Ordinal)
                || statement.StartsWith("catch", StringComparison.Ordinal)
                || statement == "finally";
            bool startsStatement = statement.StartsWith("if (", StringComparison.Ordinal)
                || statement.StartsWith("for (", StringComparison.Ordinal)
                || statement.StartsWith("foreach (", StringComparison.Ordinal)
                || statement.StartsWith("switch (", StringComparison.Ordinal)
                || statement.StartsWith("return ", StringComparison.Ordinal);
            bool endsStatement = previous.EndsWith(';') || previous is "}" or "};";

            if (previous.Length > 0 && previous is not ("{" or "[") && !closesBlock && !continuesBlock
                && (blank || (startsStatement && endsStatement) || previous == "}"))
            {
                result.Append('\n');
            }

            result.Append(line).Append('\n');
            previous = statement;
            blank = false;
        }

        return result.ToString();
    }
}
