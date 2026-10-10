"""Verify generated bindings, managed conversion, callbacks, lifetime, and exports."""

import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent


def execute(arguments):
    result = subprocess.run(arguments, cwd=ROOT, text=True, capture_output=True)
    if result.returncode:
        raise RuntimeError(result.stdout + result.stderr)
    return result.stdout


def verify_exports(ast):
    imports = set()
    for source in (ROOT / "NGX.NET/API").glob("*.g.cs"):
        imports.update(re.findall(r'EntryPoint = "([^"]+)"', source.read_text(encoding="utf-8-sig")))
    imports.add("NGX_Bridge_Parameter_Reset")
    for rid, platform in ast["platforms"].items():
        windows = rid.startswith("win-")
        binary = ROOT / "native" / rid / ("ngx-bridge.dll" if windows else "libngx-bridge.so")
        output = execute(["objdump", "-p" if windows else "-T", str(binary)])
        if windows:
            exports = set(re.findall(r"^\s*\d+\s+0x[0-9a-fA-F]+\s+(\S+)\s*$", output, re.MULTILINE))
        else:
            exports = {
                line.split()[-1]
                for line in output.splitlines()
                if re.match(r"^[0-9a-fA-F]+\s+g\s+DF\s+", line) and "*UND*" not in line
            }
        expected = {function["export"] for function in platform["functions"]} | {"NGX_Bridge_Parameter_Reset"}
        if expected - exports or expected - imports:
            raise RuntimeError(f"{rid}: missing exports {expected - exports}; missing imports {expected - imports}")
        print(f"PASS {rid}: {len(expected)} expected binary exports and managed import names")


def verify_generation():
    generator = ["dotnet", "run", "--project", "NGX.NET.Generator/NGX.NET.Generator.csproj", "--"]
    with tempfile.TemporaryDirectory(prefix="ngx-generation-") as directory:
        root = Path(directory)
        (root / "NGX.NET.Generator").mkdir()
        shutil.copyfile(ROOT / "NGX.NET.Generator/ast.json", root / "NGX.NET.Generator/ast.json")
        print(execute([*generator, str(root)]), end="")
        print(execute(["dotnet", "run", "--project", "verification/Generation/Generation.csproj", "--", str(root), str(ROOT)]), end="")
        handle = root / "NGX.NET/Structs/NGXHandle.g.cs"
        canonical = handle.read_bytes()
        handle.write_bytes(canonical.removeprefix(b"\xef\xbb\xbf"))
        repaired = execute([*generator, str(root)])
        if "1 changed." not in repaired or handle.read_bytes() != canonical:
            raise RuntimeError("The generator did not restore canonical UTF-8 BOM bytes: " + repaired)
        repeated = execute([*generator, str(root)])
        if "0 changed." not in repeated:
            raise RuntimeError("Fresh generation was not deterministic: " + repeated)
    print("PASS fresh generator output, BOM repair and deterministic regeneration without formatting")


def main():
    verify_generation()
    print(execute(["dotnet", "run", "--project", str(HERE / "Marshalling.csproj"), "-c", "Debug", "--", str(ROOT)]), end="")
    print(execute(["dotnet", "run", "--project", str(HERE / "Consumer/Consumer.csproj"), "-c", "Debug"]), end="")
    negative = subprocess.run(
        ["dotnet", "build", str(HERE / "Consumer/Consumer.csproj"), "-c", "Debug", "--no-restore", "--nologo", "-p:ProbeNativeVisibility=true"],
        cwd=ROOT, capture_output=True, text=True,
    )
    if negative.returncode == 0 or "CS0122" not in negative.stdout + negative.stderr:
        raise RuntimeError("Native visibility was not rejected as expected:\n" + negative.stdout + negative.stderr)
    print("PASS negative compiler test: internal native type rejected with CS0122")
    ast_path = ROOT / "NGX.NET.Generator/ast.json"
    ast = json.loads(ast_path.read_text())
    verify_exports(ast)
    generated = execute(["dotnet", "run", "--project", "NGX.NET.Generator/NGX.NET.Generator.csproj", "--", str(ROOT)])
    if "0 changed." not in generated:
        raise RuntimeError("Generator was not deterministic: " + generated)
    print("PASS deterministic regeneration: 0 changed files")
    print("Validation complete. NVIDIA GPU/runtime and NativeAOT execution remain unverified.")


if __name__ == "__main__":
    main()
