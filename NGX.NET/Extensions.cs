namespace NGX.NET;

public static class Extensions
{
    private const uint FailureMask = 0xFFF00000u;

    extension(NGXResult result)
    {
        public bool IsSuccess => ((uint)result & FailureMask) is not (uint)NGXResult.Fail;

        public bool IsFailure => !result.IsSuccess;

        public void CheckError(string? operation = null)
        {
            if (result.IsFailure)
            {
                throw new NGXException(result, operation);
            }
        }
    }
}
