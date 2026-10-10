namespace Marshalling;

internal static class ResultChecks
{
    private const uint FailureMask = 0xFFF00000u;

    private static readonly uint[] SyntheticValues = [0u, 2u, 0xBACFFFFFu, 0xBAD00000u, 0xBADABCDEu, 0xBADFFFFFu, 0xBAE00000u, uint.MaxValue];

    internal static void Run()
    {
        foreach (NGXResult result in Enum.GetValues<NGXResult>().Concat(SyntheticValues.Select(static value => (NGXResult)value)))
        {
            bool failed = ((uint)result & FailureMask) == (uint)NGXResult.Fail;
            Assert(result.IsFailure == failed && result.IsSuccess != failed, "Result success and failure match the SDK mask");

            if (!failed)
            {
                result.CheckError();
                result.CheckError("Synthetic operation");

                continue;
            }

            CheckFailure(result, null);
            CheckFailure(result, "Synthetic operation");
        }

        Console.WriteLine("PASS SDK result-mask classification and CheckError details for every enum value and synthetic success/failure codes");
    }

    private static void CheckFailure(NGXResult result, string? operation)
    {
        try
        {
            result.CheckError(operation);
        }
        catch (NGXException exception)
        {
            Assert(exception.Result == result && exception.Message.StartsWith($"{operation ?? "NGX"}:", StringComparison.Ordinal) && exception.Message.EndsWith($"(0x{(uint)result:X8})", StringComparison.Ordinal), "CheckError preserves result, operation and hexadecimal failure code");

            return;
        }

        throw new InvalidOperationException("CheckError did not throw for a failing SDK result.");
    }
}
