namespace Marshalling;

internal static class ConcurrentLifetimeChecks
{
    internal static void Run()
    {
        (int initializationCount, int parameterDataCount, int parameterBackendCount) = NgxLifetime.Counts;

        Parallel.For(0, 64, static i =>
        {
            string backend = $"ScopedLockValidation{i}";
            nint parameters = i + 1;
            NativeCall? initialization = new();
            NgxLifetime.BeginInitialization(backend, 1, initialization);
            NgxLifetime.EndInitialization(backend, 1, true, ref initialization);

            NativeCall? values = new();
            NgxLifetime.RegisterParameters(backend, parameters);
            NgxLifetime.BeginParameters(parameters, "Validation", values);
            NgxLifetime.EndParameters(parameters, "Validation", true, true, ref values);
            NgxLifetime.Shutdown(backend, 0);
        });

        (int actualInitialization, int actualParameterData, int actualParameterBackends) = NgxLifetime.Counts;
        Assert(actualInitialization == initializationCount, "Concurrent lifetime cleanup: initialization");
        Assert(actualParameterData == parameterDataCount, "Concurrent lifetime cleanup: parameterData");
        Assert(actualParameterBackends == parameterBackendCount, "Concurrent lifetime cleanup: parameterBackends");

        Console.WriteLine("PASS scoped locks preserve concurrent registration and reentrant shutdown");
    }
}
