"""Prepare one SDK, build NGX bridges, and merge four targets.

SDK downloads, libclang, generated C++ and native compilation stay in Actions.
The workflow commits the generated bindings, ast.json, and native binaries.
"""

from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor
import json
import os
from pathlib import Path
import shutil
import subprocess
import urllib.request

from parse_ngx_ast import extract

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


def save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


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

    tree = api(f"KhronosGroup/Vulkan-Headers/git/trees/{VULKAN_COMMIT}?recursive=1")
    headers = [item["path"] for item in tree["tree"] if item["type"] == "blob" and item["path"].startswith("include/")]
    download("KhronosGroup/Vulkan-Headers", VULKAN_COMMIT, headers, root / "vulkan")
    save(root / "source.json", {"repository": "https://github.com/NVIDIA/DLSS", "release": release["tag_name"], "commit": commit, "vulkanCommit": VULKAN_COMMIT})
    print(f"Using {release['tag_name']} at {commit}", flush=True)


def run(command, cwd):
    command = list(map(str, command))
    print(" ".join(command), flush=True)
    subprocess.run(command, cwd=cwd, check=True)


def build(root, scratch, output, rid):
    root, scratch, output = root.resolve(), scratch.resolve(), output.resolve()
    scratch.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    source = json.loads((root / "source.json").read_text())
    ast, units = extract(root, scratch, rid)
    windows = rid.startswith("win-")
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
    sources = []
    for header, bodies in wrappers.items():
        p = scratch / (header + ".bridge.cpp")
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(Path(units[header]).read_text() + "\n" + "\n".join(bodies))
        sources.append(p)
    platform, lib = PLATFORMS[rid]
    loader = root / "lib" / platform / lib
    native = output / "native" / rid
    native.mkdir(parents=True, exist_ok=True)
    bridge = native / ("ngx-bridge.dll" if windows else "libngx-bridge.so")
    if windows:
        definition = scratch / "ngx-bridge.def"
        definition.write_text("LIBRARY ngx-bridge\nEXPORTS\n" + "\n".join(exports) + "\n")
        run(["cl", "/nologo", "/LD", "/MT", "/O2", "/EHsc", "/std:c++17", "/DNGX_ENABLE_DEPRECATED_SHUTDOWN", "/DNGX_ENABLE_DEPRECATED_GET_PARAMETERS", "/I" + str(root / "include"), "/I" + str(root / "vulkan/include"), *sources, loader, "advapi32.lib", "ole32.lib", "shell32.lib", "version.lib", "shlwapi.lib", "user32.lib", "/link", "/DEF:" + str(definition), "/OUT:" + str(bridge)], scratch)
    else:
        script = scratch / "ngx-bridge.map"
        script.write_text("{\n global:\n" + "\n".join("    " + n + ";" for n in exports) + "\n local: *;\n};\n")
        run(["g++", "-shared", "-fPIC", "-O2", "-std=c++17", "-fvisibility=hidden", "-DNGX_ENABLE_DEPRECATED_SHUTDOWN", "-DNGX_ENABLE_DEPRECATED_GET_PARAMETERS", "-I" + str(root / "include"), "-I" + str(root / "vulkan/include"), *sources, "-Wl,--whole-archive", loader, "-Wl,--no-whole-archive", "-ldl", "-pthread", "-Wl,-z,defs", "-Wl,--version-script=" + str(script), "-o", bridge], scratch)
    for p in (root / "lib" / platform / "rel").iterdir():
        shutil.copy2(p, native / p.name)
    save(output / "ast.json", {"source": source, "platform": ast})
    print(f"Built {rid}: {bridge.name}", flush=True)


def merge(root, output):
    platforms = {}
    source = None
    for rid in PLATFORMS:
        artifact = root / f"ngx-{rid}"
        fragment = json.loads((artifact / "ast.json").read_text())
        source = fragment["source"]
        platforms[rid] = fragment["platform"]
        destination = output / "native" / rid
        if destination.exists():
            shutil.rmtree(destination)
        shutil.copytree(artifact / "native" / rid, destination)
    save(output / "NGX.NET.Generator" / "ast.json", {"source": source, "platforms": platforms})


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
