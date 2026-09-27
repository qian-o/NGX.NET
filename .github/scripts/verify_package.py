"""Inspect the managed package and consume it with an isolated local NuGet source."""

import argparse
import hashlib
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET
import zipfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    root = args.root.resolve()
    package = max((root / "Streamline.NET/bin/Release").glob("Streamline.NET.*.nupkg"), key=lambda path: path.stat().st_mtime_ns)
    with zipfile.ZipFile(package) as archive:
        names = set(archive.namelist())
        assert "lib/net10.0/Streamline.NET.dll" in names
        assert "lib/net10.0/Streamline.NET.xml" in names
        assert "LICENSE" in names and "README.md" in names
        assert not any(name.startswith("runtimes/") or name.endswith((".h", ".hpp")) for name in names)
        assert all(not name.endswith(".dll") or name == "lib/net10.0/Streamline.NET.dll" for name in names)
        manifest = ET.fromstring(archive.read(next(name for name in names if name.endswith(".nuspec"))))
        namespace = {"n": manifest.tag.split("}")[0].removeprefix("{")}
        metadata = manifest.find("n:metadata", namespace)
        license_node = metadata.find("n:license", namespace)
        assert license_node.attrib["type"] == "expression" and license_node.text == "MIT"
        assert not manifest.findall(".//n:dependency", namespace)
        version = metadata.find("n:version", namespace).text

    package_hash = hashlib.sha256(package.read_bytes()).hexdigest()
    consumer = root / ".work/package-consumer" / package_hash[:16]
    consumer.mkdir(parents=True, exist_ok=True)
    # Mask ancestor development settings to verify an ordinary package consumer.
    for name in ("Directory.Build.props", "Directory.Packages.props"):
        (consumer / name).write_text("<Project />\n", encoding="utf-8")
    project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
    properties = ET.SubElement(project, "PropertyGroup")
    for name, value in {"OutputType": "Exe", "TargetFramework": "net10.0", "ImplicitUsings": "enable", "Nullable": "enable", "NuGetAudit": "false"}.items():
        ET.SubElement(properties, name).text = value
    references = ET.SubElement(project, "ItemGroup")
    ET.SubElement(references, "PackageReference", Include="Streamline.NET", Version=version)
    ET.ElementTree(project).write(consumer / "Consumer.csproj", encoding="utf-8", xml_declaration=True)
    configuration = ET.Element("configuration")
    sources = ET.SubElement(configuration, "packageSources")
    ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", key="local-wrapper", value=str(package.parent))
    ET.ElementTree(configuration).write(consumer / "NuGet.Config", encoding="utf-8", xml_declaration=True)
    (consumer / "Program.cs").write_text('''using Streamline.NET;

DLSSOptions options = new() { Mode = DLSSMode.MaxQuality, OutputWidth = 1920, OutputHeight = 1080 };
DLSSOptimalSettings settings = new();
GetSettings operation = SL.DLSSGetOptimalSettings;
if (options.StructVersion == 0 || settings.StructVersion == 0 || operation is null)
{
    return 1;
}
Console.WriteLine("Package consumer compiled the safe convenience API and initialized native defaults.");
return 0;

delegate SLResult GetSettings(in DLSSOptions options, ref DLSSOptimalSettings settings);
''', encoding="utf-8")
    subprocess.run(["dotnet", "restore", str(consumer / "Consumer.csproj"), "--configfile", str(consumer / "NuGet.Config"),
                    "--packages", str(consumer / "packages")], check=True)
    subprocess.run(["dotnet", "run", "--project", str(consumer / "Consumer.csproj"), "-c", "Release", "--no-restore"], check=True)
    print(f"Verified {package.name}: managed assembly, XML documentation, README and MIT license; no runtime package dependencies or native assets.")


if __name__ == "__main__":
    main()
