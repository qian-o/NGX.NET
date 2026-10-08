"""Run from any directory: python3 verification/Marshalling/run.py

Requires .NET 10, Python 3 and LLVM-compatible objdump. Does not compile C++ or
load NVIDIA libraries. Tests exercise production Native constructors, callback thunks and lifetime management.
The expected-failing visibility compilation is checked for CS0122.
"""

import hashlib
import json
from pathlib import Path
import re
import subprocess


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
    targets = {}
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
        targets[rid] = {
            "dataRecords": sum(not record["opaque"] for record in platform["records"]),
            "opaqueRecords": sum(record["opaque"] for record in platform["records"]),
            "functions": len(platform["functions"]),
            "expectedExportsIncludingReset": len(expected),
            "matchedExports": len(expected & exports),
            "unexpectedExports": sorted(exports - expected),
            "binarySha256": hashlib.sha256(binary.read_bytes()).hexdigest(),
        }
        print(f"PASS {rid}: {len(expected)} expected binary exports and managed import names")
    return targets


def main():
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
    targets = verify_exports(ast)
    result_path = HERE / "results.json"
    results = json.loads(result_path.read_text())
    results["safeConsumer"] = "production signatures compiled with AllowUnsafeBlocks=false; native SDK calls were not executed"
    results["nativeVisibility"] = "external access rejected by compiler with CS0122"
    results["nativeExportAudit"] = targets
    results["astSha256"] = hashlib.sha256(ast_path.read_bytes()).hexdigest()
    results["validationSha256"] = {
        str(path.relative_to(HERE)): hashlib.sha256(path.read_bytes()).hexdigest()
        for path in sorted(HERE.rglob("*"))
        if path.is_file() and not {"bin", "obj"}.intersection(path.relative_to(HERE).parts)
        and path.suffix in {".cs", ".csproj", ".py"}
    }
    results["contractFindings"] = [
        "Constructors clean up partially initialized native values before rethrowing.",
        "Initialization storage and callbacks remain rooted until matching successful shutdown; CUDA descriptors use stable storage.",
        "Helper parameter snapshots retain native pointer inputs until replacement, Reset, DestroyParameters, or backend shutdown. Omitted DLSSG options preserve earlier matrix storage.",
        "Failed helper calls preserve earlier snapshots because native failure may occur before or after pointer-map writes.",
        "Opaque handles remain borrowed values and require explicit SDK release; the binding does not manage GPU resource completion."
    ]
    generated = execute(["dotnet", "run", "--project", "NGX.NET.Generator/NGX.NET.Generator.csproj", "--", str(ROOT)])
    if "0 changed." not in generated:
        raise RuntimeError("Generator was not deterministic: " + generated)
    print("PASS deterministic regeneration: 0 changed files")
    results["regeneration"] = "0 changed files"
    sources = sorted(p for folder in ["NGX.NET", "NGX.NET.Generator"] for p in (ROOT / folder).rglob("*.cs") if not {"bin", "obj"}.intersection(p.relative_to(ROOT).parts))
    results["managedSourcesSha256"] = hashlib.sha256(b"".join(str(p.relative_to(ROOT)).encode() + b"\0" + p.read_bytes() for p in sources)).hexdigest()
    result_path.write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n")
    print("Validation complete. NVIDIA GPU/runtime and NativeAOT execution remain unverified.")


if __name__ == "__main__":
    main()
