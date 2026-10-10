namespace NGX.NET.Generator;

internal class CodeWriter
{
    private readonly StringBuilder text = new();

    private int indent;
    private bool blankLine;
    private bool blockStart;

    public override string ToString()
    {
        return text.ToString();
    }

    internal void Line(string value)
    {
        if (blankLine && text.Length is not 0)
        {
            text.Append('\n');
        }

        blankLine = false;
        blockStart = false;
        text.Append(' ', indent * 4);
        text.Append(value);
        text.Append('\n');
    }

    internal void BlankLine()
    {
        blankLine = !blockStart;
    }

    internal void BeginBlock(string declaration, bool continuation = false)
    {
        if (continuation)
        {
            blankLine = false;
        }

        Line(declaration);
        Line("{");
        indent++;
        blockStart = true;
    }

    internal void EndBlock(string suffix = "")
    {
        blankLine = false;
        indent--;
        Line("}" + suffix);
        BlankLine();
    }

    internal static void WriteGuard(CodeWriter text, string condition, string statement, bool separate = true)
    {
        if (separate)
        {
            text.BlankLine();
        }

        text.BeginBlock($"if ({condition})");
        text.Line(statement);
        text.EndBlock();
    }

    internal static CodeWriter CreateFile()
    {
        CodeWriter text = new();
        text.Line("#nullable enable");
        text.BlankLine();
        text.Line("namespace NGX.NET;");
        text.BlankLine();

        return text;
    }
}
