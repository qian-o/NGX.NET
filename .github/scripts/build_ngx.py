"""Prepare one SDK, build NGX bridges, and merge four targets.

SDK downloads, AST extraction and native compilation stay in Actions.
The workflow submits bridge sources, bindings, ast.json and binaries in a PR.
"""

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
import tarfile
import urllib.request

from parse_ngx_ast import extract, latest_cpp_standard

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


def request(url):
    headers = {"User-Agent": "NGX.NET"}
    if url.startswith("https://api.github.com/") and os.environ.get("GH_TOKEN"):
        headers["Authorization"] = "Bearer " + os.environ["GH_TOKEN"]
    with urllib.request.urlopen(urllib.request.Request(url, headers=headers), timeout=180) as response:
        return response.read()


def api(path):
    return json.loads(request("https://api.github.com/repos/" + path))


def write_text(path, value, encoding="utf-8"):
    with path.open("w", encoding=encoding, newline="\n") as stream:
        stream.write(value)


def save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    write_text(path, json.dumps(data, indent=2, ensure_ascii=False) + "\n")


def download(repository, commit, paths, root):
    def copy(path):
        data = request(f"https://raw.githubusercontent.com/{repository}/{commit}/{path}")
        destination = root / path
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(data)

    with ThreadPoolExecutor(max_workers=8) as pool:
        list(pool.map(copy, paths))


def prepare(root):
    release = api("NVIDIA/DLSS/releases/latest")
    commit = api("NVIDIA/DLSS/commits/" + release["tag_name"])["sha"]
    tree = api(f"NVIDIA/DLSS/git/trees/{commit}?recursive=1")
    paths = [item["path"] for item in tree["tree"] if item["type"] == "blob"]
    selected = [path for path in paths if path.startswith("include/") and path.endswith(".h")]
    for platform, library in PLATFORMS.values():
        selected.append(f"lib/{platform}/{library}")
        selected.extend(path for path in paths if path.startswith(f"lib/{platform}/rel/"))
    download("NVIDIA/DLSS", commit, selected, root)

    vulkan_commit = api("KhronosGroup/Vulkan-Headers/commits/HEAD")["sha"]
    tree = api(f"KhronosGroup/Vulkan-Headers/git/trees/{vulkan_commit}?recursive=1")
    headers = [item["path"] for item in tree["tree"] if item["type"] == "blob" and item["path"].startswith("include/")]
    download("KhronosGroup/Vulkan-Headers", vulkan_commit, headers, root / "vulkan")
    save(root / "source.json", {"repository": "https://github.com/NVIDIA/DLSS", "release": release["tag_name"], "commit": commit, "vulkanCommit": vulkan_commit})
    print(f"Using {release['tag_name']} at {commit}", flush=True)


def install_llvm(scratch, rid):
    name, checksum = LLVM_ARCHIVES["windows" if rid.startswith("win-") else rid]
    archive = scratch / name
    print("Using " + name, flush=True)
    url = f"https://github.com/llvm/llvm-project/releases/download/llvmorg-{LLVM_VERSION}/{name}"
    with urllib.request.urlopen(url, timeout=180) as response, archive.open("wb") as output:
        shutil.copyfileobj(response, output)

    with archive.open("rb") as stream:
        if hashlib.file_digest(stream, "sha256").hexdigest() != checksum:
            raise RuntimeError("LLVM archive checksum does not match: " + name)

    destination = scratch / "llvm"
    with tarfile.open(archive) as package:
        package.extractall(destination, filter="data")
    archive.unlink()
    return next(destination.glob("*/bin/clang.exe" if rid.startswith("win-") else "*/bin/clang")).parents[1]


def run(command, cwd):
    command = list(map(str, command))
    print(" ".join(command), flush=True)
    subprocess.run(command, cwd=cwd, check=True)


def build(root, scratch, output, rid):
    root, scratch, output = root.resolve(), scratch.resolve(), output.resolve()
    scratch.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    source = json.loads((root / "source.json").read_text())
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
    generated = output / "bridge" / rid
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
        run(["cl", "/nologo", "/LD", "/MT", "/O2", "/EHsc", "/std:c++latest", "/DNGX_ENABLE_DEPRECATED_SHUTDOWN", "/DNGX_ENABLE_DEPRECATED_GET_PARAMETERS", "/I" + str(root / "include"), "/I" + str(root / "vulkan/include"), *sources, loader, "advapi32.lib", "ole32.lib", "shell32.lib", "version.lib", "shlwapi.lib", "user32.lib", "/link", "/DEF:" + str(definition), "/OUT:" + str(bridge)], scratch)
    else:
        script = generated / "ngx-bridge.map"
        write_text(script, "{\n global:\n" + "\n".join("    " + n + ";" for n in exports) + "\n local: *;\n};\n")
        run([compiler, "-shared", "-fPIC", "-O2", "-std=" + standard, "-fvisibility=hidden", "-DNGX_ENABLE_DEPRECATED_SHUTDOWN", "-DNGX_ENABLE_DEPRECATED_GET_PARAMETERS", "-I" + str(root / "include"), "-I" + str(root / "vulkan/include"), *sources, "-Wl,--whole-archive", loader, "-Wl,--no-whole-archive", "-ldl", "-pthread", "-Wl,-z,defs", "-Wl,--version-script=" + str(script), "-o", bridge], scratch)
    for p in (root / "lib" / platform / "rel").iterdir():
        shutil.copy2(p, native / p.name)
    save(output / "ast.json", {"source": source, "platform": ast})
    print(f"Built {rid}: {bridge.name}", flush=True)


def merge(root, output):
    platforms = {}
    source = None
    for rid in PLATFORMS:
        artifact = root / f"ngx-{rid}"
        fragment = json.loads((artifact / "ast.json").read_text(encoding="utf-8"))
        if source is None:
            source = fragment["source"]
        elif fragment["source"] != source:
            raise RuntimeError("SDK sources differ between target artifacts: " + rid)

        binary = "ngx-bridge.dll" if rid.startswith("win-") else "libngx-bridge.so"
        linker_input = "ngx-bridge.def" if rid.startswith("win-") else "ngx-bridge.map"
        if not (artifact / "native" / rid / binary).is_file():
            raise RuntimeError("Native bridge is missing from target artifact: " + rid)

        generated = artifact / "bridge" / rid
        if not (generated / linker_input).is_file() or not any(generated.rglob("*.bridge.cpp")):
            raise RuntimeError("Bridge sources are missing from target artifact: " + rid)

        platforms[rid] = fragment["platform"]

    packaging = output / "NuGet.Packaging.props"
    metadata = packaging.read_text(encoding="utf-8-sig")
    version = source["release"].removeprefix("v")
    metadata, replacements = re.subn(r"(?<=<Version>)[^<]*(?=</Version>)", lambda match: version, metadata)
    if replacements != 1:
        raise RuntimeError("NuGet.Packaging.props must contain one package Version.")

    for rid in PLATFORMS:
        artifact = root / f"ngx-{rid}"
        for directory in ("bridge", "native"):
            destination = output / directory / rid
            if destination.exists():
                shutil.rmtree(destination)
            shutil.copytree(artifact / directory / rid, destination)

    save(output / "NGX.NET.Generator" / "ast.json", {"source": source, "platforms": platforms})
    write_text(packaging, metadata, encoding="utf-8-sig")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("operation", choices=("prepare", "build", "merge"))
    parser.add_argument("--root", required=True, type=Path)
    parser.add_argument("--scratch", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--rid", choices=PLATFORMS)
    args = parser.parse_args()
    if args.operation == "prepare":
        prepare(args.root)
    elif args.operation == "build":
        build(args.root, args.scratch, args.output, args.rid)
    else:
        merge(args.root, args.output)


if __name__ == "__main__":
    main()
