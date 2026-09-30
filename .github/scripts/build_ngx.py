"""Prepare one pinned SDK, build and audit NGX bridges, then merge four targets.

SDK downloads, libclang, generated C++ and native compilation stay in Actions.
Only the merged ast.json and verified native binaries are committed.
"""

from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor
import ctypes
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import struct
import urllib.request

from parse_ngx_ast import extract, IMPLEMENTATION_HEADERS

PLATFORMS = {
    "win-x64": ("Windows_x86_64", "x64/nvsdk_ngx_s.lib"),
    "win-arm64": ("Windows_aarch64", "nvsdk_ngx_s.lib"),
    "linux-x64": ("Linux_x86_64", "libnvsdk_ngx.a"),
    "linux-arm64": ("Linux_aarch64", "libnvsdk_ngx.a"),
}
VULKAN_COMMIT = "9a0f3099c8a9607a7c0f3127d8abfdc19a93e8c5"


def request(url):
    headers = {"User-Agent": "NGX.NET"}
    if url.startswith("https://api.github.com/") and os.environ.get("GH_TOKEN"):
        headers["Authorization"] = "Bearer " + os.environ["GH_TOKEN"]
    with urllib.request.urlopen(urllib.request.Request(url, headers=headers), timeout=180) as response:
        return response.read()


def api(path):
    return json.loads(request("https://api.github.com/repos/" + path))


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def prepare(root):
    release = api("NVIDIA/DLSS/releases/latest")
    if release["draft"] or release["prerelease"]:
        raise RuntimeError("A published stable SDK release is required.")
    commit = api("NVIDIA/DLSS/commits/" + release["tag_name"])["sha"]
    tree = api(f"NVIDIA/DLSS/git/trees/{commit}?recursive=1")
    if tree.get("truncated"):
        raise RuntimeError("Incomplete SDK tree.")
    paths = {x["path"]: x for x in tree["tree"] if x["type"] == "blob"}
    selected = [p for p in paths if p.startswith("include/") and p.endswith(".h")]
    for platform, library in PLATFORMS.values():
        selected += [f"lib/{platform}/{library}"]
        features = [p for p in paths if p.startswith(f"lib/{platform}/rel/")]
        if len(features) != 3:
            raise RuntimeError("Review changed runtime inventory: " + platform)
        selected += features
    source = {"repository": "https://github.com/NVIDIA/DLSS", "release": release["tag_name"],
              "commit": commit, "vulkanCommit": VULKAN_COMMIT, "inputs": []}

    def download(path):
        data = request(f"https://raw.githubusercontent.com/NVIDIA/DLSS/{commit}/{path}")
        blob = hashlib.sha1(f"blob {len(data)}\0".encode() + data).hexdigest()
        if blob != paths[path]["sha"]:
            raise RuntimeError("Git object checksum mismatch: " + path)
        dest = root / path
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(data)
        return {"path": path, "sha256": hashlib.sha256(data).hexdigest(), "gitBlob": blob,
                "classification": "loader-implementation" if Path(path).name in IMPLEMENTATION_HEADERS else "application" if path.startswith("include/") else "native"}

    with ThreadPoolExecutor(max_workers=8) as pool:
        source["inputs"] = sorted(pool.map(download, selected), key=lambda x: x["path"])
    vk_tree = api(f"KhronosGroup/Vulkan-Headers/git/trees/{VULKAN_COMMIT}?recursive=1")
    if vk_tree.get("truncated"):
        raise RuntimeError("Incomplete Vulkan dependency tree.")
    def vk_download(item):
        p = item["path"]
        data = request(f"https://raw.githubusercontent.com/KhronosGroup/Vulkan-Headers/{VULKAN_COMMIT}/{p}")
        if hashlib.sha1(f"blob {len(data)}\0".encode() + data).hexdigest() != item["sha"]:
            raise RuntimeError("Vulkan checksum mismatch: " + p)
        dest = root / "vulkan" / p
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(data)
    with ThreadPoolExecutor(max_workers=8) as pool:
        list(pool.map(vk_download, [x for x in vk_tree["tree"] if x["type"] == "blob" and x["path"].startswith("include/")]))
    save(root / "source.json", source)
    print(f"Pinned {release['tag_name']} at {commit}; {len(selected)} SDK inputs", flush=True)


def run(command, cwd):
    print(" ".join(map(str, command)), flush=True)
    return subprocess.run(list(map(str, command)), cwd=cwd, check=True, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT).stdout


def verify_machine(path, rid):
    data = path.read_bytes()
    if rid.startswith("win-"):
        pe = struct.unpack_from("<I", data, 0x3C)[0]
        if data[:2] != b"MZ" or data[pe:pe + 4] != b"PE\0\0":
            raise RuntimeError("Invalid PE image: " + str(path))
        machine = struct.unpack_from("<H", data, pe + 4)[0]
        expected = 0xAA64 if rid.endswith("arm64") else 0x8664
    else:
        if data[:6] != b"\x7fELF\x02\x01":
            raise RuntimeError("Expected a 64-bit little-endian ELF image: " + str(path))
        machine = struct.unpack_from("<H", data, 18)[0]
        expected = 183 if rid.endswith("arm64") else 62
    if machine != expected:
        raise RuntimeError("Architecture mismatch: " + str(path))


def layout_assertions(ast, units):
    assertions = {}
    names = {}
    for record in ast["records"]:
        for field in record["fields"]:
            t = field["type"]
            if t["kind"] == "RECORD" and not t["name"].startswith(("NVSDK_NGX_", "Vk")):
                names[t["name"]] = f'decltype((({record["name"]}*)nullptr)->{field["name"]})'
    for record in ast["records"]:
        if record["opaque"]:
            continue
        header = record["header"] or "nvsdk_ngx_defs_vk.h"
        if header not in units:
            header = "nvsdk_ngx_helpers.h"
        name = names.get(record["name"], record["name"])
        checks = [f'static_assert(sizeof({name}) == {record["size"]});',
                  f'static_assert(alignof({name}) == {record["align"]});']
        for field in record["fields"]:
            if field["offset"] % 8:
                raise RuntimeError("Unreviewed bit field: " + record["name"])
            checks.append(f'static_assert(offsetof({name}, {field["name"]}) == {field["offset"] // 8});')
        assertions.setdefault(header, []).extend(checks)
    return assertions


def build(root, scratch, output, rid):
    root, scratch, output = root.resolve(), scratch.resolve(), output.resolve()
    scratch.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    source = json.loads((root / "source.json").read_text())
    for item in source["inputs"]:
        if sha256(root / item["path"]) != item["sha256"]:
            raise RuntimeError("SDK input changed: " + item["path"])
    ast, units = extract(root, scratch, rid)
    save(output / "ast.json", {"source": source, "platform": ast})
    windows = rid.startswith("win-")
    exports, wrappers = [], {}
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
        prefix = '__declspec(dllexport)' if windows else '__attribute__((visibility("default")))'
        body = f'extern "C" {prefix} {f["result"]["cpp"]} {name}({", ".join(params)})\n{{\n    return {f["name"]}({", ".join(arguments)});\n}}\n'
        wrappers.setdefault(f["header"], []).append(body)
        exports.append(name)
        f["export"] = name
    reset = 'extern "C" ' + ('__declspec(dllexport)' if windows else '__attribute__((visibility("default")))') + ' void NGX_Bridge_Parameter_Reset(NVSDK_NGX_Parameter* parameters)\n{\n    parameters->Reset();\n}\n'
    wrappers.setdefault("nvsdk_ngx_params.h", []).append(reset)
    exports.append("NGX_Bridge_Parameter_Reset")
    assertions = layout_assertions(ast, units)
    for header in assertions:
        wrappers.setdefault(header, [])
    sources = []
    for header, bodies in wrappers.items():
        p = scratch / (header + ".bridge.cpp")
        p.write_text(Path(units[header]).read_text() + "\n" + "\n".join(assertions.get(header, [])) + "\n" + "\n".join(bodies))
        sources.append(p)
    platform, lib = PLATFORMS[rid]
    loader = root / "lib" / platform / lib
    native = output / "native" / rid
    native.mkdir(parents=True, exist_ok=True)
    bridge = native / ("ngx-bridge.dll" if windows else "libngx-bridge.so")
    if windows:
        definition = scratch / "ngx-bridge.def"
        definition.write_text("LIBRARY ngx-bridge\nEXPORTS\n" + "\n".join(exports) + "\n")
        print(run(["cl", "/nologo", "/LD", "/MT", "/O2", "/EHsc", "/std:c++17", "/DNGX_ENABLE_DEPRECATED_SHUTDOWN", "/DNGX_ENABLE_DEPRECATED_GET_PARAMETERS",
                   "/I" + str(root / "include"), "/I" + str(root / "vulkan/include"), *sources, loader,
                   "advapi32.lib", "ole32.lib", "shell32.lib", "version.lib", "shlwapi.lib", "user32.lib",
                   "/link", "/DEF:" + str(definition), "/OUT:" + str(bridge)], scratch))
        dump = run(["dumpbin", "/exports", bridge], scratch)
        actual = set(re.findall(r"^\s+\d+\s+[\dA-F]+\s+[\dA-F]+\s+(\S+)", dump, re.M))
    else:
        script = scratch / "ngx-bridge.map"
        script.write_text("{\n global:\n" + "\n".join("    " + n + ";" for n in exports) + "\n local: *;\n};\n")
        print(run(["g++", "-shared", "-fPIC", "-O2", "-std=c++17", "-fvisibility=hidden",
                   "-DNGX_ENABLE_DEPRECATED_SHUTDOWN", "-DNGX_ENABLE_DEPRECATED_GET_PARAMETERS",
                   "-I" + str(root / "include"), "-I" + str(root / "vulkan/include"), *sources,
                   "-Wl,--whole-archive", loader, "-Wl,--no-whole-archive", "-ldl", "-pthread",
                   "-Wl,-z,defs", "-Wl,--version-script=" + str(script), "-o", bridge], scratch))
        dump = run(["nm", "-D", "--defined-only", bridge], scratch)
        actual = {line.split()[-1] for line in dump.splitlines() if line.strip()}
    if actual != set(exports):
        raise RuntimeError(f"Export mismatch: missing={set(exports) - actual}, extra={actual - set(exports)}")
    for p in (root / "lib" / platform / "rel").iterdir():
        shutil.copy2(p, native / p.name)
    for path in native.iterdir():
        verify_machine(path, rid)
    # Execute a driver-independent loader export on each native runner. The
    # Windows ARM64 cross-build is validated structurally, not emulated.
    if rid != "win-arm64":
        module = ctypes.CDLL(str(bridge))
        describe = module.GetNGXResultAsString
        describe.argtypes = [ctypes.c_uint]
        describe.restype = ctypes.c_wchar_p
        if not describe(1):
            raise RuntimeError("NGX result conversion returned no string.")
    ast["exports"] = sorted(actual)
    ast["binaries"] = [{"name": p.name, "sha256": sha256(p)} for p in sorted(native.iterdir())]
    save(output / "ast.json", {"source": source, "platform": ast})
    print(f"{rid}: verified {len(exports)} exports; {len(ast['binaries'])} binaries", flush=True)


def merge(root, output):
    source, platforms = None, {}
    for rid in PLATFORMS:
        fragment = json.loads((root / rid / "ast.json").read_text())
        if source is not None and source != fragment["source"]:
            raise RuntimeError("Targets were not built from the same SDK inputs.")
        source = fragment["source"]
        platform = fragment["platform"]
        if platform["rid"] != rid or not platform.get("exports"):
            raise RuntimeError("Missing successful target: " + rid)
        for binary in platform["binaries"]:
            path = root / rid / "native" / rid / binary["name"]
            if sha256(path) != binary["sha256"]:
                raise RuntimeError("Artifact checksum mismatch: " + str(path))
        platforms[rid] = platform
    # Validation precedes every destination write. The workflow commits both
    # destinations together and refuses to overwrite a concurrently moved branch.
    for rid in PLATFORMS:
        destination = output / "native" / rid
        if destination.exists():
            shutil.rmtree(destination)
        shutil.copytree(root / rid / "native" / rid, destination)
    save(output / "NGX.NET.Generator" / "ast.json", {"schemaVersion": 1, "source": source, "platforms": platforms})


def smoke(root, scratch, output, rid):
    root, scratch, output = root.resolve(), scratch.resolve(), output.resolve()
    scratch.mkdir(parents=True, exist_ok=True)
    source = scratch / "Smoke.cs"
    # Reuse the verifier's CPU byte/layout checks under NativeAOT on each RID.
    verification = (root / ".github/scripts/verify_ngx.cs").read_text(encoding="utf-8-sig")
    begin = "// BEGIN NATIVE AOT MATH CHECKS"
    end = "// END NATIVE AOT MATH CHECKS"
    if verification.count(begin) != 1 or verification.count(end) != 1:
        raise RuntimeError("Expected one shared NativeAOT math-check block.")
    start = verification.index(begin) + len(begin)
    stop = verification.index(end)
    if stop <= start:
        raise RuntimeError("Invalid shared NativeAOT math-check boundaries.")
    math_checks = verification[start:stop].strip()
    source.write_text(f"#:project {root / 'NGX.NET/NGX.NET.csproj'}\n#:property PublishAot=true\n#:property AllowUnsafeBlocks=true\n" + r"""
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NGX.NET;
using Ngx = NGX.NET.NGX;
unsafe
{
    if (!Ngx.Succeeded(NGXResult.Success) || !Ngx.Failed(NGXResult.Fail)) throw new Exception("Result predicates");
    if (sizeof(NGXBool8) != 1 || sizeof(nuint) != sizeof(void*)) throw new Exception("ABI widths");
    MathAbiChecks.Verify((condition, message) => { if (!condition) throw new Exception(message); });
    NGXDLSSGOptEvalParams options = new();
    if (options.MultiFrameCount != 1 || options.MultiFrameIndex != 1 || options.MinRelativeLinearDepthObjectSeparation != 40) throw new Exception("SDK defaults");
    const string value = "NGX \U0001F680";
    void* utf8 = NGXMarshal.StringToPtr(value, NGXEncoding.Utf8);
    void* wide = null;
    try
    {
        wide = NGXMarshal.StringToPtr(value, NGXEncoding.NativeWide);
        if (NGXMarshal.PtrToString(utf8, NGXEncoding.Utf8) != value || ((byte*)utf8)[4] != 0xF0 || ((byte*)utf8)[8] != 0) throw new Exception("UTF-8 string");
        if (NGXMarshal.PtrToString(wide, NGXEncoding.NativeWide) != value) throw new Exception("Native wchar_t round trip");
        if (OperatingSystem.IsWindows())
        {
            if (((ushort*)wide)[4] != 0xD83D || ((ushort*)wide)[5] != 0xDE80 || ((ushort*)wide)[6] != 0) throw new Exception("Windows wchar_t");
        }
        else if (((uint*)wide)[4] != 0x1F680 || ((uint*)wide)[5] != 0) throw new Exception("Linux wchar_t");
    }
    finally
    {
        NGXMarshal.Free(wide);
        NGXMarshal.Free(utf8);
    }
    // This macro contains a payload NUL before the added string terminator.
    const string reservedName = Ngx.EParameterReserved00;
    void* reserved = NGXMarshal.StringToPtr(reservedName, NGXEncoding.Utf8);
    try
    {
        if (!new ReadOnlySpan<byte>(reserved, 3).SequenceEqual<byte>([0x23, 0, 0]) || NGXMarshal.PtrToString(reserved, NGXEncoding.Utf8) != "#") throw new Exception("String constant NUL fidelity");
    }
    finally
    {
        NGXMarshal.Free(reserved);
    }
    utf8 = NGXMarshal.StringToPtr("A\0\0B", NGXEncoding.Utf8);
    wide = null;
    try
    {
        wide = NGXMarshal.StringToPtr("A\0\0B", NGXEncoding.NativeWide);
        if (!new ReadOnlySpan<byte>(utf8, 5).SequenceEqual<byte>([0x41, 0, 0, 0x42, 0]) || NGXMarshal.PtrToString(utf8, NGXEncoding.Utf8) != "A") throw new Exception("UTF-8 embedded NUL");
        if (NGXMarshal.PtrToString(wide, NGXEncoding.NativeWide) != "A") throw new Exception("Native wchar_t NUL prefix");
        if (OperatingSystem.IsWindows())
        {
            if (!new ReadOnlySpan<ushort>(wide, 5).SequenceEqual<ushort>([0x41, 0, 0, 0x42, 0])) throw new Exception("Windows wchar_t NUL payload");
        }
        else if (!new ReadOnlySpan<uint>(wide, 5).SequenceEqual<uint>([0x41, 0, 0, 0x42, 0])) throw new Exception("Linux wchar_t NUL payload");
    }
    finally
    {
        NGXMarshal.Free(wide);
        NGXMarshal.Free(utf8);
    }
    // The loader owns this returned string; conversion must not free it.
    void* description = Ngx.GetResultAsString(NGXResult.Success);
    string? message = NGXMarshal.PtrToString(description, NGXEncoding.NativeWide);
    if (string.IsNullOrEmpty(message) || NGXMarshal.PtrToString(description, NGXEncoding.NativeWide) != message) throw new Exception("Borrowed native wchar_t result");
    NGXVkImageSubresourceRange range = new() { AspectMask = 1, BaseMipLevel = 2, LevelCount = 3, BaseArrayLayer = 4, LayerCount = 5 };
    NGXResourceVK image = Ngx.CreateImageViewResourceVK((nint)0x1122, (nint)0x3344, range, NGXVkFormat.R8G8B8A8UNORM, 120, 60, true);
    if (!image.ReadWrite || image.Type != NGXResourceVKType.VKIMAGEVIEW || image.Resource.ImageViewInfo.ImageView != (nint)0x1122 || image.Resource.ImageViewInfo.Image != (nint)0x3344 || image.Resource.ImageViewInfo.Width != 120 || image.Resource.ImageViewInfo.Height != 60 || image.Resource.ImageViewInfo.SubresourceRange.LayerCount != 5) throw new Exception("Native structure argument/return ABI");
    NGXResourceVK buffer = Ngx.CreateBufferResourceVK((nint)0x5566, 4096, false);
    if (buffer.ReadWrite || buffer.Type != NGXResourceVKType.VKBUFFER || buffer.Resource.BufferInfo.Buffer != (nint)0x5566 || buffer.Resource.BufferInfo.SizeInBytes != 4096) throw new Exception("Native union return ABI");
    delegate* unmanaged[Cdecl]<float, NGXBool8*, void> callback = &Callbacks.Progress;
    NGXBool8 cancelled = false;
    callback(1, &cancelled);
    if (!cancelled) throw new Exception("C callback bool pointer");
}
Console.WriteLine("NativeAOT NGX loader, structure/union ABI, wchar_t, callback and defaults passed.");
static unsafe class Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void Progress(float progress, NGXBool8* cancelled) => *cancelled = progress == 1;
}
""" + "\n" + math_checks)
    published = scratch / "published"
    print(run(["dotnet", "publish", source, "-r", rid, "-c", "Release", "-o", published], root))
    destination = published / "runtimes" / rid / "native"
    if destination.exists():
        shutil.rmtree(destination)
    shutil.copytree(output / "native" / rid, destination)
    if rid != "win-arm64":
        print(run([published / ("Smoke.exe" if rid.startswith("win-") else "Smoke")], published))


def verify_package(root):
    import zipfile
    ast = json.loads((root / "NGX.NET.Generator/ast.json").read_text())
    packages = list((root / "NGX.NET/bin/Release").glob("*.nupkg"))
    if len(packages) != 1:
        raise RuntimeError("Expected one freshly built package.")
    expected = {f"runtimes/{rid}/native/{b['name']}": b["sha256"] for rid, platform in ast["platforms"].items() for b in platform["binaries"]}
    with zipfile.ZipFile(packages[0]) as package:
        actual = {n for n in package.namelist() if n.startswith("runtimes/")}
        if set(expected) != actual:
            raise RuntimeError("Incomplete NuGet RID mapping.")
        for path, checksum in expected.items():
            if hashlib.sha256(package.read(path)).hexdigest() != checksum:
                raise RuntimeError("Packaged binary changed: " + path)
    print(f"Verified {len(expected)} native NuGet assets including versioned Linux .so files.")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("operation", choices=("prepare", "build", "merge", "smoke", "verify-package"))
    parser.add_argument("--root", required=True, type=Path)
    parser.add_argument("--scratch", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--rid", choices=PLATFORMS)
    args = parser.parse_args()
    if args.operation != "verify-package" and os.environ.get("GITHUB_ACTIONS") != "true":
        raise SystemExit("SDK extraction and native builds run exclusively in GitHub Actions.")
    try:
        if args.operation == "prepare":
            prepare(args.root)
        elif args.operation == "build":
            build(args.root, args.scratch, args.output, args.rid)
        elif args.operation == "merge":
            merge(args.root, args.output)
        elif args.operation == "smoke":
            smoke(args.root, args.scratch, args.output, args.rid)
        else:
            verify_package(args.root)
    except subprocess.CalledProcessError as error:
        print(error.stdout, flush=True)
        raise


if __name__ == "__main__":
    main()
