namespace Streamline.NET.Generator;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
            InterfaceSnapshot snapshot = AstJsonParser.Parse(Path.Combine(root, "Streamline.NET.Generator", "streamline-ast.json"));
            CSharpEmitter emitter = new(snapshot, new(snapshot), Path.Combine(root, "Streamline.NET"));
            return emitter.Generate();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}
