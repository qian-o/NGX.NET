namespace NGX.NET.Generator;

internal class AstRuntimeAsset(string rid, string assetName, string relativeUrl, string sha256)
{
    public readonly string Rid = rid;

    public readonly string AssetName = assetName;

    public readonly string RelativeUrl = relativeUrl;

    public readonly string Sha256 = sha256;
}
