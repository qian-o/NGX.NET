# Streamline.NET

Independent .NET 10 wrapper for NVIDIA Streamline. The package contains managed
code only; applications supply their own Streamline runtime and plugin paths.

Implementation and interface extraction are in progress. The binding contract is
specified in [Streamline.NET.Design.md](Streamline.NET.Design.md).

## Development

Interface extraction runs exclusively in GitHub Actions. The C# generator consumes
`Streamline.NET.Generator/streamline-ast.json` locally without downloading SDK headers.

```sh
dotnet build Streamline.NET.slnx -c Release
```
