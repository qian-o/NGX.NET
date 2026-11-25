"""Prepare one SDK, build NGX bridges, merge targets, and verify release artifacts."""

from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tarfile
import urllib.request
from xml.etree import ElementTree
import zipfile

from parse_ast import extract, latest_cpp_standard

REPOSITORY = "NVIDIA/DLSS"
VULKAN_REPOSITORY = "KhronosGroup/Vulkan-Headers"
PACKAGE = "NGX.NET"
PREFIX = "Ngx"

PLATFORMS = {
    "win-x64": ("Windows_x86_64", "x64/nvsdk_ngx_s.lib"),
    "win-arm64": ("Windows_aarch64", "nvsdk_ngx_s.lib"),
    "linux-x64": ("Linux_x86_64", "libnvsdk_ngx.a"),
    "linux-arm64": ("Linux_aarch64", "libnvsdk_ngx.a"),
}

LLVM_VERSION = "21.1.7"
LLVM_ARCHIVES = {
    "windows": ("clang+llvm-21.1.7-x86_64-pc-windows-msvc.tar.xz", "70a2b73f2f14f787557f90abf380e7170b54e97b893218999144de5284b4f8f8"),
    "linux-x64": ("LLVM-21.1.7-Linux-X64.tar.xz", "621ab8424178ffc28db0facc5aefd3fc11f5dea339aac171b36fa0b8d4b368cb"),
    "linux-arm64": ("LLVM-21.1.7-Linux-ARM64.tar.xz", "aa85ddc8ba95ac5f2febddb51a6891ee0e57ac058c6455395d5a4bfa5650d44b"),
}


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while True:
            chunk = stream.read(1024 * 1024)
            if not chunk:
                break
            digest.update(chunk)
    return digest.hexdigest()


def verify_native_binary(path: Path, rid: str) -> None:
    expected_magic = b"MZ" if rid.startswith("win-") else b"\x7fELF"
    with path.open("rb") as stream:
        magic = stream.read(4)
    if not magic.startswith(expected_magic):
        raise RuntimeError(f"Invalid native binary header for {rid} in {path}: {magic!r}")


def request(url: str) -> bytes:
    headers = {"User-Agent": PACKAGE}
    if url.startswith("https://api.github.com/") and os.environ.get("GH_TOKEN"):
        headers["Authorization"] = "Bearer " + os.environ["GH_TOKEN"]
    with urllib.request.urlopen(urllib.request.Request(url, headers=headers), timeout=180) as response:
        return response.read()


def api(repository: str, path: str) -> dict:
    return json.loads(request(f"https://api.github.com/repos/{repository}/{path}"))


def write_text(path: Path, value: str, encoding: str = "utf-8") -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding=encoding, newline="\n") as stream:
        stream.write(value)


def save(path: Path, data: dict) -> None:
    write_text(path, json.dumps(data, indent=2, ensure_ascii=False) + "\n")


def download(repository: str, commit: str, paths: list[str], root: Path) -> None:
    def copy(path: str) -> None:
        destination = root / path
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(request(f"https://raw.githubusercontent.com/{repository}/{commit}/{path}"))

    with ThreadPoolExecutor(max_workers=8) as pool:
        list(pool.map(copy, paths))


def verify_runtimes(source: dict) -> None:
    runtimes = source.get("runtimes")
    if not isinstance(runtimes, dict) or set(runtimes) != set(PLATFORMS):
        raise RuntimeError(f"SDK runtimes manifest must target {sorted(PLATFORMS)}.")
    for rid, manifest in runtimes.items():
        if not isinstance(manifest, dict) or not manifest:
            raise RuntimeError(f"Empty runtime manifest for {rid}.")
        for path, digest in manifest.items():
            if not isinstance(digest, str) or not re.fullmatch(r"[0-9a-f]{64}", digest):
                raise RuntimeError(f"Invalid runtime SHA-256 digest for {rid}/{path}.")


def prepare(root: Path) -> None:
    release = api(REPOSITORY, "releases/latest")["tag_name"]
    commit = api(REPOSITORY, "commits/" + release)["sha"]
    tree = api(REPOSITORY, f"git/trees/{commit}?recursive=1")
    if tree.get("truncated"):
        raise RuntimeError("SDK tree response is truncated.")
    paths = [item["path"] for item in tree["tree"] if item["type"] == "blob"]
    selected = [path for path in paths if path.startswith("include/") and path.endswith(".h")]
    for platform, library in PLATFORMS.values():
        selected.append(f"lib/{platform}/{library}")
        selected.extend(path for path in paths if path.startswith(f"lib/{platform}/rel/"))
    download(REPOSITORY, commit, selected, root)

    runtimes = {}
    for rid, (platform, _) in PLATFORMS.items():
        rel_dir = root / "lib" / platform / "rel"
        files = sorted(p for p in rel_dir.iterdir() if p.is_file())
        if not files:
            raise RuntimeError(f"No official runtime files found under {rel_dir} for {rid}")
        manifest = {}
        for p in files:
            verify_native_binary(p, rid)
            manifest[f"lib/{platform}/rel/{p.name}"] = file_sha256(p)
        runtimes[rid] = manifest
        shutil.rmtree(rel_dir)

    vulkan_commit = api(VULKAN_REPOSITORY, "tags?per_page=1")[0]["commit"]["sha"]
    vulkan_tree = api(VULKAN_REPOSITORY, f"git/trees/{vulkan_commit}?recursive=1")
    if vulkan_tree.get("truncated"):
        raise RuntimeError("Vulkan headers tree response is truncated.")
    headers = [item["path"] for item in vulkan_tree["tree"] if item["type"] == "blob" and item["path"].startswith("include/")]
    download(VULKAN_REPOSITORY, vulkan_commit, headers, root / "vulkan")
    save(
        root / "source.json",
        {
            "repository": f"https://github.com/{REPOSITORY}",
            "release": release,
            "commit": commit,
            "vulkanCommit": vulkan_commit,
            "runtimes": runtimes,
        },
    )
    print(f"Using {release} at {commit}", flush=True)


def install_llvm(scratch: Path, rid: str) -> Path:
    name, checksum = LLVM_ARCHIVES["windows" if rid.startswith("win-") else rid]
    archive = scratch / name
    print("Using " + name, flush=True)
    url = f"https://github.com/llvm/llvm-project/releases/download/llvmorg-{LLVM_VERSION}/{name}"
    with urllib.request.urlopen(url, timeout=180) as response, archive.open("wb") as output:
        shutil.copyfileobj(response, output)
    if file_sha256(archive) != checksum:
        raise RuntimeError("LLVM archive checksum does not match: " + name)
    destination = scratch / "llvm"
    with tarfile.open(archive) as package:
        if sys.version_info >= (3, 12):
            package.extractall(destination, filter="data")
        else:
            package.extractall(destination)
    archive.unlink()
    return next(destination.glob("*/bin/clang.exe" if rid.startswith("win-") else "*/bin/clang")).parents[1]


def run(command: list, cwd: Path) -> None:
    command = list(map(str, command))
    print(" ".join(command), flush=True)
    subprocess.run(command, cwd=cwd, check=True)


def check_exports(actual: list[str], platform: dict, rid: str) -> None:
    expected = {f["export"] for f in platform["functions"]} | {"NGX_Bridge_Parameter_Reset"}
    actual_set = set(actual)
    if actual_set != expected:
        raise RuntimeError(f"Native exports differ for {rid}: missing={sorted(expected - actual_set)}, unexpected={sorted(actual_set - expected)}")


def build(root: Path, scratch: Path, output: Path, rid: str) -> None:
    root, scratch, output = root.resolve(), scratch.resolve(), output.resolve()
    scratch.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    source = json.loads((root / "source.json").read_text(encoding="utf-8"))
    verify_runtimes(source)
    windows = rid.startswith("win-")
    llvm = install_llvm(scratch, rid)
    compiler = llvm / "bin" / ("clang.exe" if windows else "clang++")
    standard = latest_cpp_standard(compiler)
    ast, units = extract(root, scratch, rid, llvm, standard)
    exports, wrappers = [], {}
    prefix = '__declspec(dllexport)' if windows else '__attribute__((visibility("default")))'
    for f in ast["functions"]:
        if not f["inline"]:
            exports.append(f["name"])
            f["export"] = f["name"]
            continue
        name = "NGX_Bridge_" + f["name"]
        params, arguments = [], []
        for p in f["parameters"]:
            cpp = p["type"]["cpp"]
            arg = p["name"]
            if p["type"]["kind"] in ("LVALUEREFERENCE", "RVALUEREFERENCE"):
                cpp = cpp.replace("&", "*")
                arg = "*" + arg
            params.append(cpp + " " + p["name"])
            arguments.append(arg)
        body = f'extern "C" {prefix} {f["result"]["cpp"]} {name}({", ".join(params)})\n{{\n    return {f["name"]}({", ".join(arguments)});\n}}\n'
        wrappers.setdefault(f["header"], []).append(body)
        exports.append(name)
        f["export"] = name
    reset = 'extern "C" ' + prefix + ' void NGX_Bridge_Parameter_Reset(NVSDK_NGX_Parameter* parameters)\n{\n    parameters->Reset();\n}\n'
    wrappers.setdefault("nvsdk_ngx_params.h", []).append(reset)
    exports.append("NGX_Bridge_Parameter_Reset")
    generated = scratch / "bridge"
    generated.mkdir(parents=True, exist_ok=True)
    sources = []
    for header, bodies in wrappers.items():
        p = generated / (header + ".bridge.cpp")
        p.parent.mkdir(parents=True, exist_ok=True)
        write_text(p, Path(units[header]).read_text(encoding="utf-8") + "\n" + "\n".join(bodies))
        sources.append(p)
    platform, lib = PLATFORMS[rid]
    loader = root / "lib" / platform / lib
    native = output / "native" / rid
    native.mkdir(parents=True, exist_ok=True)
    bridge = native / ("ngx-bridge.dll" if windows else "libngx-bridge.so")
    if windows:
        definition = generated / "ngx-bridge.def"
        write_text(definition, "LIBRARY ngx-bridge\nEXPORTS\n" + "\n".join(exports) + "\n")
        run(["cl", "/nologo", "/LD", "/MT", "/O2", "/EHsc", "/std:c++latest", "/Brepro", "/DNGX_ENABLE_DEPRECATED_SHUTDOWN", "/DNGX_ENABLE_DEPRECATED_GET_PARAMETERS", "/I" + str(root / "include"), "/I" + str(root / "vulkan/include"), *sources, loader, "advapi32.lib", "ole32.lib", "shell32.lib", "version.lib", "shlwapi.lib", "user32.lib", "/link", "/BREPRO", "/DEF:" + str(definition), "/OUT:" + str(bridge)], scratch)
    else:
        script = generated / "ngx-bridge.map"
        write_text(script, "{\n global:\n" + "\n".join("    " + n + ";" for n in exports) + "\n local: *;\n};\n")
        run([compiler, "-shared", "-fPIC", "-O2", "-std=" + standard, "-fvisibility=hidden", "-DNGX_ENABLE_DEPRECATED_SHUTDOWN", "-DNGX_ENABLE_DEPRECATED_GET_PARAMETERS", "-I" + str(root / "include"), "-I" + str(root / "vulkan/include"), *sources, "-Wl,--whole-archive", loader, "-Wl,--no-whole-archive", "-ldl", "-pthread", "-Wl,-z,defs", "-Wl,--version-script=" + str(script), "-o", bridge], scratch)

    if windows:
        symbols = subprocess.check_output([str(llvm / "bin/llvm-readobj.exe"), "--coff-exports", str(bridge)], text=True)
        actual = re.findall(r"^\s+Name: (\S+)\s*$", symbols, re.MULTILINE)
    else:
        symbols = subprocess.check_output([str(llvm / "bin/llvm-nm"), "--dynamic", "--defined-only", "--extern-only", "--format=posix", str(bridge)], text=True)
        actual = [line.split()[0] for line in symbols.splitlines() if line.strip()]
    check_exports(actual, ast, rid)
    checksum = file_sha256(bridge)
    save(output / "ast.json", {"source": source, "rid": rid, "platform": ast, "natives": {bridge.name: checksum}, "exports": sorted(actual)})
    print(f"Built {rid}: {bridge.name}", flush=True)


def merge(root: Path, output: Path) -> None:
    artifacts = {}
    platforms = {}
    natives = {}
    source = None
    for rid in PLATFORMS:
        artifact = root / f"target-{rid}"
        if not artifact.is_dir() and len(PLATFORMS) == 1:
            artifact = root
        artifacts[rid] = artifact
        fragment = json.loads((artifact / "ast.json").read_text(encoding="utf-8"))
        if fragment["rid"] != rid:
            raise RuntimeError("Target artifact RID does not match: " + rid)

        if source is None:
            source = fragment["source"]
        elif fragment["source"] != source:
            raise RuntimeError("SDK sources differ between target artifacts: " + rid)
        verify_runtimes(source)

        binary = "ngx-bridge.dll" if rid.startswith("win-") else "libngx-bridge.so"
        bridge = artifact / "native" / rid / binary
        if not bridge.is_file():
            raise RuntimeError("Native bridge is missing from target artifact: " + rid)
        if fragment["natives"] != {binary: file_sha256(bridge)}:
            raise RuntimeError("Native bridge differs from its export check: " + rid)
        check_exports(fragment["exports"], fragment["platform"], rid)

        natives[rid] = fragment["natives"]
        platforms[rid] = fragment["platform"]

    packaging = output / "NuGet.Packaging.props"
    metadata = packaging.read_text(encoding="utf-8-sig")
    version = source["release"].removeprefix("v")
    metadata, replacements = re.subn(r"(?<=<Version>)[^<]*(?=</Version>)", lambda _: version, metadata)
    if replacements != 1:
        raise RuntimeError("NuGet.Packaging.props must contain one package Version.")

    destination = output / "native"
    if destination.exists():
        shutil.rmtree(destination)
    for rid, artifact in artifacts.items():
        shutil.copytree(artifact / "native" / rid, destination / rid)

    merged_source = {
        "repository": source["repository"],
        "release": source["release"],
        "commit": source["commit"],
        "vulkanCommit": source["vulkanCommit"],
        "natives": natives,
        "runtimes": source["runtimes"],
    }
    save(output / f"{PACKAGE}.Generator" / "ast.json", {"source": merged_source, "platforms": platforms})
    write_text(packaging, metadata, encoding="utf-8-sig")


def verify_natives(root: Path, source: dict) -> None:
    natives = source.get("natives")
    if not isinstance(natives, dict) or set(natives) != set(PLATFORMS):
        raise RuntimeError(f"ast.json source.natives must target {sorted(PLATFORMS)}.")
    native_root = root / "native"
    actual_rids = {p.name for p in native_root.iterdir() if p.is_dir()} if native_root.is_dir() else set()
    if actual_rids != set(natives):
        raise RuntimeError(f"Native RID directories mismatch: {actual_rids ^ set(natives)}")
    for rid, expected_files in natives.items():
        rid_dir = native_root / rid
        actual_files = {p.name for p in rid_dir.iterdir() if p.is_file()}
        if actual_files != set(expected_files):
            raise RuntimeError(f"Native files mismatch for {rid}: {actual_files ^ set(expected_files)}")
        for name, expected_sha in expected_files.items():
            path = rid_dir / name
            verify_native_binary(path, rid)
            if file_sha256(path) != expected_sha:
                raise RuntimeError(f"Native checksum mismatch for {rid}/{name}.")
    print(f"Verified {sum(len(files) for files in natives.values())} core native binaries across {len(natives)} RIDs.")


def verify_manifest(root: Path, source: dict) -> None:
    verify_runtimes(source)
    props_path = root / PACKAGE / "buildTransitive" / f"{PACKAGE}.props"
    targets_path = root / PACKAGE / "buildTransitive" / f"{PACKAGE}.targets"
    props = ElementTree.parse(props_path).getroot()
    release = props.findtext(f"PropertyGroup/_{PREFIX}SdkRelease")
    commit = props.findtext(f"PropertyGroup/_{PREFIX}SdkCommit")
    base_url = props.findtext(f"PropertyGroup/_{PREFIX}DefaultBaseUrl")
    repo_path = source["repository"].removeprefix("https://github.com/").rstrip("/")
    expected_base_url = f"https://raw.githubusercontent.com/{repo_path}/{source['commit']}"
    if release != source["release"] or commit != source["commit"] or base_url != expected_base_url:
        raise RuntimeError(f"buildTransitive/{PACKAGE}.props metadata does not match ast.json source.")

    actual_entries = {
        (element.attrib["Rid"], element.attrib["AssetName"], element.attrib["RelativeUrl"], element.attrib["Sha256"])
        for element in props.findall(f"ItemGroup/_{PREFIX}ManifestEntry")
    }
    expected_entries = {
        (rid, Path(relative_url).name, relative_url, digest.upper())
        for rid, assets in source["runtimes"].items()
        for relative_url, digest in assets.items()
    }
    if actual_entries != expected_entries:
        raise RuntimeError(f"buildTransitive/{PACKAGE}.props runtime entries do not match ast.json.")

    ElementTree.parse(targets_path)
    targets_text = targets_path.read_text(encoding="utf-8-sig")
    for token in (f"{PREFIX}DownloadRuntime", f"{PREFIX}DownloadAssets", f"{PACKAGE}.props", "System.Net.Http", "ExcludeFromSingleFile"):
        if token not in targets_text:
            raise RuntimeError(f"buildTransitive/{PACKAGE}.targets missing expected symbol: {token}")
    print(f"Verified buildTransitive runtime manifest ({len(expected_entries)} entries) and targets.")


def verify_package(root: Path, source: dict, package: Path | None) -> None:
    framework = ElementTree.parse(root / "Directory.Build.props").findtext("PropertyGroup/StandardTargetFramework")
    if not framework:
        raise RuntimeError("Directory.Build.props must specify StandardTargetFramework.")
    packaging = ElementTree.fromstring((root / "NuGet.Packaging.props").read_text(encoding="utf-8-sig"))
    version = packaging.findtext("PropertyGroup/Version")
    if not version:
        raise RuntimeError("NuGet.Packaging.props must specify Version.")

    nupkg = package.resolve() if package else root / PACKAGE / "bin/Release" / f"{PACKAGE}.{version}.nupkg"
    if not nupkg.is_file():
        raise RuntimeError(f"NuGet package not found: {nupkg}")
    snupkg = nupkg.with_suffix(".snupkg")
    if not snupkg.is_file():
        raise RuntimeError(f"Symbol package not found: {snupkg}")

    with zipfile.ZipFile(nupkg) as archive:
        names = set(archive.namelist())
        expected_native = {
            f"runtimes/{rid}/native/{name}"
            for rid, files in source["natives"].items()
            for name in files
        }
        actual_native = {name for name in names if name.startswith("runtimes/")}
        if actual_native != expected_native:
            raise RuntimeError(f"NuGet native payload mismatch: {actual_native ^ expected_native}")
        for entry in expected_native:
            parts = entry.split("/")
            if archive.read(entry) != (root / "native" / parts[1] / parts[3]).read_bytes():
                raise RuntimeError(f"NuGet changed native binary: {entry}")

        for build_file in (f"buildTransitive/{PACKAGE}.props", f"buildTransitive/{PACKAGE}.targets"):
            if build_file not in names:
                raise RuntimeError(f"NuGet missing {build_file}")
            if archive.read(build_file) != (root / PACKAGE / build_file).read_bytes():
                raise RuntimeError(f"NuGet changed {build_file}")

        assembly_path = f"lib/{framework}/{PACKAGE}.dll"
        nuspec_name = f"{PACKAGE}.nuspec"
        for required in ("LICENSE", "README.md", assembly_path, nuspec_name):
            if required not in names:
                raise RuntimeError(f"NuGet missing {required}")

        nuspec = ElementTree.fromstring(archive.read(nuspec_name).decode("utf-8"))
        ns = {"n": "http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd"}
        pkg_version = nuspec.findtext("n:metadata/n:version", namespaces=ns)
        if pkg_version != version:
            raise RuntimeError(f"Nuspec version mismatch: {pkg_version} != {version}")

    with zipfile.ZipFile(snupkg) as symbols:
        pdb_path = f"lib/{framework}/{PACKAGE}.pdb"
        if pdb_path not in set(symbols.namelist()):
            raise RuntimeError(f"Symbol package missing {pdb_path}")

    print(f"Verified NuGet package: {nupkg.name} and {snupkg.name} ({len(expected_native)} core native files, 0 runtime files).")


def verify(root: Path, package: Path | None = None) -> None:
    root = root.resolve()
    subprocess.run(["dotnet", "run", "--project", str(root / f"{PACKAGE}.Generator"), "-c", "Release", "--", str(root), "--verify"], check=True)
    ast = json.loads((root / f"{PACKAGE}.Generator" / "ast.json").read_text(encoding="utf-8"))
    source = ast["source"]
    verify_natives(root, source)
    verify_manifest(root, source)
    verify_package(root, source, package)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    operations = parser.add_subparsers(dest="operation", required=True)

    prepare_parser = operations.add_parser("prepare", help="Download and pin one official SDK.")
    prepare_parser.add_argument("--root", required=True, type=Path)

    build_parser = operations.add_parser("build", help="Extract declarations and build native target.")
    build_parser.add_argument("--root", required=True, type=Path)
    build_parser.add_argument("--scratch", required=True, type=Path)
    build_parser.add_argument("--output", required=True, type=Path)
    build_parser.add_argument("--rid", required=True, choices=list(PLATFORMS))

    merge_parser = operations.add_parser("merge", help="Merge validated targets to the repository.")
    merge_parser.add_argument("--root", required=True, type=Path)
    merge_parser.add_argument("--output", required=True, type=Path)

    verify_parser = operations.add_parser("verify", help="Verify generated bindings, native assets, and NuGet package.")
    verify_parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    verify_parser.add_argument("--package", type=Path)

    args = parser.parse_args()
    if args.operation == "prepare":
        prepare(args.root)
    elif args.operation == "build":
        build(args.root, args.scratch, args.output, args.rid)
    elif args.operation == "merge":
        merge(args.root, args.output)
    else:
        verify(args.root, args.package)


if __name__ == "__main__":
    main()
