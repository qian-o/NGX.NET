namespace Marshalling;

internal static class Program
{
    private static int Main(string[] args)
    {
        string root = Path.GetFullPath(args.Length is 0 ? "." : args[0]);
        try
        {
            LayoutChecks.Run(root);
            EnumAndImportChecks.Run(root);
            ResultChecks.Run();
            SurfaceChecks.Run();
            DiscoveryChecks.Run();
            InvalidInputChecks.Run();
            FixedStringChecks.Run();
            ScopeChecks.Run();
            EvaluationChecks.Run();
            GBufferChecks.Run();
            CallbackChecks.Run();
            InitializationChecks.Run();
            ParameterChecks.Run();
            FrameGenerationRetentionChecks.Run();
            BackendCleanupChecks.Run();
            CudaChecks.Run();
            DefaultChecks.Run();
            HandleChecks.Run();
            ConcurrentLifetimeChecks.Run();
            DlssAllocationChecks.Run();
            AllocationChecks.Run();

            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);

            return 1;
        }
    }
}
