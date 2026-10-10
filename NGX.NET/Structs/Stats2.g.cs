#nullable enable

namespace NGX.NET;

public readonly struct Stats2(ulong vramAllocatedBytes, uint optLevel, uint isDevSnippetBranch)
{
    public ulong VRAMAllocatedBytes { get; } = vramAllocatedBytes;

    public uint OptLevel { get; } = optLevel;

    public uint IsDevSnippetBranch { get; } = isDevSnippetBranch;
}
