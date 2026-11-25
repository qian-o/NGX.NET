namespace NGX.NET.Generator;

internal class AstSource(string repository, string release, string commit, AstRuntimeAsset[] runtimes)
{
    public readonly string Repository = repository;

    public readonly string Release = release;

    public readonly string Commit = commit;

    public readonly AstRuntimeAsset[] Runtimes = runtimes;
}
