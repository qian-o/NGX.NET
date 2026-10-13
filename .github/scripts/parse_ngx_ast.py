"""Extract application declarations and ABI layouts on the target Actions runner."""

from __future__ import annotations

import os
from pathlib import Path
import re
import subprocess


# Optional header-only CUDA loaders expose Core signatures outside the application ABI.
IMPLEMENTATION_HEADERS = {"nvsdk_ngx_loader.h", "nvsdk_ngx_standalone_common.h", "nvsdk_ngx_standalone_cuda.h"}


def latest_cpp_standard(compiler: Path):
    result = subprocess.run([str(compiler), "-x", "c++", "-std=help", "-fsyntax-only", "-"], input="", text=True, capture_output=True)
    return re.findall(r"use '(c\+\+[^']+)'", result.stderr)[-1]


def extract(sdk: Path, scratch: Path, rid: str, llvm: Path, standard: str):
    from clang import cindex as cx

    windows = rid.startswith("win-")
    compiler = llvm / "bin" / ("clang.exe" if windows else "clang++")
    cx.Config.set_library_file(str(llvm / ("bin/libclang.dll" if windows else "lib/libclang.so")))
    resource = subprocess.check_output([str(compiler), "-print-resource-dir"], text=True).strip()

    target = ("aarch64" if rid.endswith("arm64") else "x86_64") + ("-pc-windows-msvc" if windows else "-linux-gnu")
    flags = ["-x", "c++", "-std=" + standard, "--target=" + target, "-resource-dir=" + resource, "-I" + str(sdk / "include"), "-I" + str(sdk / "vulkan" / "include"), "-DNGX_ENABLE_DEPRECATED_SHUTDOWN", "-DNGX_ENABLE_DEPRECATED_GET_PARAMETERS"]
    if windows:
        version = subprocess.run(["cl"], text=True, capture_output=True).stderr
        flags += ["-fms-extensions", "-fms-compatibility", "-fms-compatibility-version=" + re.search(r"\b\d+\.\d+\.\d+\b", version).group(), "-DNOMINMAX"]
        for path in os.environ["INCLUDE"].split(";"):
            flags += ["-isystem", path]
    else:
        search = subprocess.run([str(compiler), "-E", "-x", "c++", "-", "-v"], input="", text=True, capture_output=True, check=True).stderr
        for line in search.split("#include <...> search starts here:")[1].split("End of search list.")[0].splitlines():
            flags += ["-isystem", line.strip()]

    records, enums, functions, aliases, macros = {}, {}, {}, {}, {}
    units = {}

    def source(cursor):
        if not cursor.location.file:
            return None
        p = Path(cursor.location.file.name)
        return p.relative_to(sdk / "include").as_posix() if p.is_relative_to(sdk / "include") else None

    def type_info(t):
        canonical = t.get_canonical()
        kind = canonical.kind.name
        info = {"cpp": t.spelling, "kind": kind, "size": canonical.get_size()}
        if kind in ("POINTER", "LVALUEREFERENCE", "RVALUEREFERENCE"):
            info["element"] = type_info(canonical.get_pointee())
        elif kind == "CONSTANTARRAY":
            info.update(element=type_info(canonical.element_type), count=canonical.element_count)
        elif kind in ("FUNCTIONPROTO", "FUNCTIONNOPROTO"):
            info.update(result=type_info(canonical.get_result()), arguments=[type_info(a) for a in canonical.argument_types()])
        elif kind in ("RECORD", "ENUM"):
            decl = canonical.get_declaration()
            info["name"] = decl.spelling
            if decl.is_anonymous():
                info["name"] = "Anonymous_" + str(decl.location.line) + "_" + str(decl.location.column)
            collect(decl, info["name"])
        return info

    def collect(c, forced_name=None):
        name = forced_name or c.spelling
        kind = c.kind.name
        if kind in ("STRUCT_DECL", "UNION_DECL", "CLASS_DECL"):
            if name in records and (records[name].get("fields") or not c.is_definition()):
                return
            record = {"name": name, "kind": kind, "size": c.type.get_size(), "opaque": not c.is_definition() or name in ("NVSDK_NGX_Handle", "NVSDK_NGX_Parameter"), "fields": []}
            records[name] = record
            if not record["opaque"]:
                for field in c.get_children():
                    if field.kind == cx.CursorKind.FIELD_DECL:
                        record["fields"].append({"name": field.spelling, "offset": field.get_field_offsetof(), "type": type_info(field.type), "declaration": " ".join(t.spelling for t in field.get_tokens())})
        elif kind == "ENUM_DECL" and name not in enums:
            enums[name] = {"name": name, "values": [{"name": e.spelling, "value": e.enum_value} for e in c.get_children() if e.kind == cx.CursorKind.ENUM_CONSTANT_DECL]}

    def walk(c):
        header = source(c)
        if header:
            if c.kind in (cx.CursorKind.STRUCT_DECL, cx.CursorKind.UNION_DECL, cx.CursorKind.ENUM_DECL, cx.CursorKind.CLASS_DECL):
                collect(c)
            elif c.kind == cx.CursorKind.TYPEDEF_DECL:
                alias = type_info(c.underlying_typedef_type)
                if alias["kind"] == "POINTER" and alias["element"]["kind"] == "FUNCTIONPROTO":
                    aliases[c.spelling] = alias
            elif c.kind == cx.CursorKind.FUNCTION_DECL:
                name = c.spelling
                if name not in functions and (windows or "D3D" not in name):
                    functions[name] = {
                        "name": name, "header": header, "line": c.location.line,
                        "result": type_info(c.result_type),
                        "parameters": [{"name": p.spelling or f"arg{i}", "type": type_info(p.type)} for i, p in enumerate(c.get_arguments())],
                        "inline": c.is_definition()}
            elif c.kind == cx.CursorKind.MACRO_DEFINITION:
                tokens = list(c.get_tokens())
                macros[c.spelling] = {"name": c.spelling, "tokens": [t.spelling for t in tokens[1:]], "functionLike": len(tokens) > 1 and tokens[1].spelling == "(" and tokens[0].extent.end.offset == tokens[1].extent.start.offset}
        if c.kind in (cx.CursorKind.TRANSLATION_UNIT, cx.CursorKind.LINKAGE_SPEC, cx.CursorKind.UNEXPOSED_DECL, cx.CursorKind.NAMESPACE):
            for child in c.get_children():
                walk(child)

    # Separate units avoid repeated shared inline helpers in the D3D/CUDA/Vulkan headers.
    headers = sorted(p.relative_to(sdk / "include").as_posix() for p in (sdk / "include").rglob("*.h") if p.relative_to(sdk / "include").as_posix() not in IMPLEMENTATION_HEADERS)
    for header in headers:
        if not windows and ("_d3d" in header or header in ("nvsdk_ngx_helpers_dlssd.h", "nvsdk_ngx_helpers_dlssg.h")):
            continue
        unit = scratch / (header + ".cpp")
        unit.parent.mkdir(parents=True, exist_ok=True)
        prologue = "#include <stdint.h>\n#include <stddef.h>\n#include <wchar.h>\n#include <string.h>\n#include <vulkan/vulkan.h>\n#include \"nvsdk_ngx.h\"\n"
        if "dlssd_vk" in header:
            prologue += '#include "nvsdk_ngx_helpers_vk.h"\n'
        if "dlssd_d3d" in header or header == "nvsdk_ngx_helpers_dlssd.h":
            prologue += '#include "nvsdk_ngx_helpers_d3d.h"\n'
        if "dlssd_cuda" in header:
            prologue += '#include "nvsdk_ngx_helpers_cuda.h"\n'
        with unit.open("w", encoding="utf-8", newline="\n") as stream:
            stream.write(prologue + '#include "' + header + '"\n')
        tu = cx.Index.create().parse(str(unit), args=flags, options=cx.TranslationUnit.PARSE_DETAILED_PROCESSING_RECORD)
        errors = [str(d) for d in tu.diagnostics if d.severity >= cx.Diagnostic.Error]
        if errors:
            raise RuntimeError(header + "\n" + "\n".join(errors))
        walk(tu.cursor)
        units[header] = str(unit)
    return {"functions": sorted(functions.values(), key=lambda f: f["name"]),
            "records": sorted(records.values(), key=lambda r: r["name"]),
            "enums": sorted(enums.values(), key=lambda e: e["name"]),
            "aliases": aliases, "macros": sorted(macros.values(), key=lambda m: m["name"])}, units
