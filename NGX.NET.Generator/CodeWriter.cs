namespace NGX.NET.Generator;

internal class CodeWriter
{
    private readonly StringBuilder text = new();

    private int indentationLevel;
    private bool hasPendingBlankLine;
    private bool isBlockStart;

    public override string ToString()
    {
        return text.ToString();
    }

    internal void WriteLine(string value)
    {
        if (hasPendingBlankLine && text.Length is not 0)
        {
            text.AppendLine();
        }

        hasPendingBlankLine = false;
        isBlockStart = false;
        text.Append(' ', indentationLevel * 4);
        text.Append(value);
        text.AppendLine();
    }

    internal void WriteBlankLine()
    {
        hasPendingBlankLine = !isBlockStart;
    }

    internal void BeginBlock(string declaration)
    {
        WriteLine(declaration);
        WriteLine("{");
        indentationLevel++;
        isBlockStart = true;
    }

    internal void EndBlock()
    {
        hasPendingBlankLine = false;
        indentationLevel--;
        WriteLine("}");
        WriteBlankLine();
    }

    internal static CodeWriter CreateFile()
    {
        CodeWriter text = new();
        text.WriteLine("namespace NGX.NET;");
        text.WriteBlankLine();

        return text;
    }
}
