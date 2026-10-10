#nullable enable

namespace NGX.NET;

public readonly struct Stats1(ulong vramAllocatedBytes, uint optLevel)
{
    public ulong VRAMAllocatedBytes { get; } = vramAllocatedBytes;

    public uint OptLevel { get; } = optLevel;
}
